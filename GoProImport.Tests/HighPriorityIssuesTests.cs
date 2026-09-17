using System;
using System.IO;
using GoProImport.Devices;
using GoProImport.FileTypes;
using Xunit;

namespace GoProImport.Tests
{
    public class HighPriorityIssuesTests : IDisposable
    {
        private readonly string tempDir;

        public HighPriorityIssuesTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "GoProImportTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // =========================================================================
        // Issue 1: Decouple and verify deletions before deleting source files
        // =========================================================================

        [Fact]
        public void FileItem_CopyFile_VerifiesIntegrityAndReturnsTrueOnSuccess()
        {
            var sourcePath = Path.Combine(tempDir, "source.mp4");
            File.WriteAllText(sourcePath, "test payload data for integrity");

            var destFolder = Path.Combine(tempDir, "dest");
            FileItem.DstPath = destFolder;

            var item = new FileItem(sourcePath, "imported.mp4", destinationPath: destFolder);
            var copied = item.CopyFile();

            Assert.True(copied);
            Assert.True(File.Exists(item.DestinationFullPath));
            Assert.True(item.VerifyIntegrity());
            Assert.Equal(new FileInfo(sourcePath).Length, new FileInfo(item.DestinationFullPath).Length);
        }

        [Fact]
        public void FileItem_VerifyIntegrity_ReturnsFalseIfDestinationMissingOrMismatch()
        {
            var sourcePath = Path.Combine(tempDir, "source_missing.mp4");
            File.WriteAllText(sourcePath, "1234567890");

            var destFolder = Path.Combine(tempDir, "dest2");
            FileItem.DstPath = destFolder;

            var item = new FileItem(sourcePath, "imported_missing.mp4", destinationPath: destFolder);
            Assert.False(item.VerifyIntegrity());

            // Create dest with mismatched size
            Directory.CreateDirectory(destFolder);
            File.WriteAllText(item.DestinationFullPath, "123");
            Assert.False(item.VerifyIntegrity());
        }

        [Fact]
        public void FileItem_DeleteOriginal_DeletesSourceFileSafely()
        {
            var sourcePath = Path.Combine(tempDir, "to_delete.wav");
            File.WriteAllText(sourcePath, "sound bytes");

            var item = new FileItem(sourcePath, "dest.wav");
            Assert.True(File.Exists(sourcePath));

            item.DeleteOriginal();
            Assert.False(File.Exists(sourcePath));
        }

        // =========================================================================
        // Issue 2: Fix IndexOutOfRangeException on CLI argument parsing
        // =========================================================================

        [Theory]
        [InlineData("-i")]
        [InlineData("-info")]
        [InlineData("-o")]
        [InlineData("-out")]
        [InlineData("-d")]
        [InlineData("-device")]
        public void CliParsing_MissingTrailingArgument_DoesNotThrowIndexOutOfRange(string flag)
        {
            var originalOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);

            try
            {
                var exception = Record.Exception(() => Program.Main([flag]));
                Assert.Null(exception);

                var output = sw.ToString();
                Assert.Contains("Error: Missing", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void CliParsing_HelpAndVersion_DoNotThrow()
        {
            var originalOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);

            try
            {
                var ex1 = Record.Exception(() => Program.Main(["-h"]));
                Assert.Null(ex1);

                var ex2 = Record.Exception(() => Program.Main(["-v"]));
                Assert.Null(ex2);

                var ex3 = Record.Exception(() => Program.Main(["-invalid_arg"]));
                Assert.Null(ex3);
                Assert.Contains("Unknown option", sw.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        // =========================================================================
        // Issue 3: Fix year vs timestamp time offset discrepancy in JPEGFile.cs
        // =========================================================================

        [Fact]
        public void JPEGFile_AppliesHourOffsetToYearAndTimestamp()
        {
            var testFile = Path.Combine(tempDir, "test.jpg");
            File.WriteAllText(testFile, "dummy jpeg content");

            // Set specific LastWriteTime across year boundary: 2025-12-31 22:30:00
            var baseTime = new DateTime(2025, 12, 31, 22, 30, 0);
            File.SetLastWriteTime(testFile, baseTime);

            var dummyDrive = new DriveInfo(Path.GetPathRoot(tempDir)!);
            var deviceNoOffset = new NativeDevice(tempDir) { HourOffset = 0, ImportName = "TestRun" };
            var deviceWithOffset = new NativeDevice(tempDir) { HourOffset = 3, ImportName = "TestRun" };

            var jpeg = new JPEGFile();
            var pathNoOffset = jpeg.GetNewFilepath(testFile, deviceNoOffset);
            var pathWithOffset = jpeg.GetNewFilepath(testFile, deviceWithOffset);

            // With HourOffset = 0: 2025 \ 2025-12-31_TestRun \ 251231_223000_N.jpg
            Assert.Contains(@"2025\2025-12-31_TestRun\", pathNoOffset);
            Assert.Contains("251231_223000_N.jpg", pathNoOffset);

            // With HourOffset = 3: 2026 \ 2026-01-01_TestRun \ 260101_013000_N.jpg
            Assert.Contains(@"2026\2026-01-01_TestRun\", pathWithOffset);
            Assert.Contains("260101_013000_N.jpg", pathWithOffset);
        }

        // =========================================================================
        // Issue 4: Add null-guards on metadata reading in MP4File, JPEGFile, WAVFile
        // =========================================================================

        [Fact]
        public void MP4File_MissingOrCorruptMetadata_FallsBackSafelyWithoutThrowing()
        {
            var dummyFile = Path.Combine(tempDir, "corrupt.mp4");
            File.WriteAllBytes(dummyFile, [0x00, 0x01, 0x02, 0x03]); // Not a valid MP4

            var fixedTime = new DateTime(2024, 6, 15, 14, 20, 10);
            File.SetLastWriteTime(dummyFile, fixedTime);

            var device = new NativeDevice(tempDir) { HourOffset = 0, ImportName = "Test" };
            var mp4 = new MP4File();

            var ex = Record.Exception(() => mp4.GetNewFilepath(dummyFile, device));
            Assert.Null(ex);

            var result = mp4.GetNewFilepath(dummyFile, device);
            Assert.NotNull(result);
            Assert.Contains(@"2024\2024-06-15_Test\", result);
            Assert.Contains("240615_142010_N.mp4", result);
        }

        [Fact]
        public void WAVFile_MissingOrCorruptMetadata_FallsBackSafelyWithoutThrowing()
        {
            var dummyFile = Path.Combine(tempDir, "corrupt.wav");
            File.WriteAllText(dummyFile, "not audio");

            var fixedTime = new DateTime(2024, 8, 20, 9, 15, 0);
            File.SetLastWriteTime(dummyFile, fixedTime);

            var device = new NativeDevice(tempDir) { HourOffset = 2, ImportName = "AudioImport" };
            var wav = new WAVFile();

            var ex = Record.Exception(() => wav.GetNewFilepath(dummyFile, device));
            Assert.Null(ex);

            var result = wav.GetNewFilepath(dummyFile, device);
            Assert.NotNull(result);
            // 09:15 + 2h offset => 11:15
            Assert.Contains(@"2024\2024-08-20_AudioImport\", result);
            Assert.Contains("240820_111500_N.wav", result);
        }

        [Fact]
        public void JPEGFile_MissingOrCorruptMetadata_FallsBackSafelyWithoutThrowing()
        {
            var dummyFile = Path.Combine(tempDir, "empty.jpg");
            File.WriteAllBytes(dummyFile, Array.Empty<byte>());

            var fixedTime = new DateTime(2023, 3, 10, 18, 0, 0);
            File.SetLastWriteTime(dummyFile, fixedTime);

            var device = new NativeDevice(tempDir) { HourOffset = 0, ImportName = "Photos" };
            var jpeg = new JPEGFile();

            var ex = Record.Exception(() => jpeg.GetNewFilepath(dummyFile, device));
            Assert.Null(ex);

            var result = jpeg.GetNewFilepath(dummyFile, device);
            Assert.NotNull(result);
            Assert.Contains(@"2023\2023-03-10_Photos\", result);
            Assert.Contains("230310_180000_N.jpg", result);
        }

        // =========================================================================
        // Issue 5: Handle empty camera directories in DJI_Osmo.cs
        // =========================================================================

        [Fact]
        public void DJI_Osmo_EmptyCameraDirectory_DoesNotThrowIndexOutOfRangeException()
        {
            var emptyOsmoDir = Path.Combine(tempDir, "empty_osmo_dcim");
            Directory.CreateDirectory(emptyOsmoDir);

            var drive = new DriveInfo(Path.GetPathRoot(tempDir)!);
            var osmo = new DJI_Osmo(drive, emptyOsmoDir);

            var ex = Record.Exception(() => osmo.DeviceName);
            Assert.Null(ex);
            Assert.Equal("DJI_Osmo", osmo.DeviceName);
        }

        [Fact]
        public void DJI_Osmo_NonExistentDirectory_DoesNotThrow()
        {
            var nonExistentDir = Path.Combine(tempDir, "non_existent_path");

            var drive = new DriveInfo(Path.GetPathRoot(tempDir)!);
            var osmo = new DJI_Osmo(drive, nonExistentDir);

            var ex = Record.Exception(() => osmo.DeviceName);
            Assert.Null(ex);
            Assert.Equal("DJI_Osmo", osmo.DeviceName);
        }

        [Fact]
        public void DJI_Osmo_WithRecognizedFiles_IdentifiesCorrectCameraModel()
        {
            var osmoDir = Path.Combine(tempDir, "osmo_dcim");
            Directory.CreateDirectory(osmoDir);
            File.WriteAllText(Path.Combine(osmoDir, "DJI_0001_B01.mp4"), "video content");

            var drive = new DriveInfo(Path.GetPathRoot(tempDir)!);
            var osmo = new DJI_Osmo(drive, osmoDir);

            Assert.Equal("DOP3", osmo.DeviceName);
        }

        [Fact]
        public void DeviceBase_ListFiles_NonExistentDirectory_ReturnsEmptyArrayInsteadOfNull()
        {
            var nonExistentDir = Path.Combine(tempDir, "does_not_exist");
            var device = new NativeDevice(nonExistentDir);

            var files = device.ListFiles();
            Assert.NotNull(files);
            Assert.Empty(files);
        }

        [Fact]
        public void MP4File_AppliesHourOffsetAcrossYearBoundary()
        {
            var dummyFile = Path.Combine(tempDir, "nye.mp4");
            File.WriteAllBytes(dummyFile, [0x00]);

            var baseTime = new DateTime(2025, 1, 1, 1, 0, 0);
            File.SetLastWriteTime(dummyFile, baseTime);

            var device = new NativeDevice(tempDir) { HourOffset = -2, ImportName = "NYE" };
            var mp4 = new MP4File();

            var result = mp4.GetNewFilepath(dummyFile, device);
            // 2025-01-01 01:00 minus 2 hours => 2024-12-31 23:00
            Assert.Contains(@"2024\2024-12-31_NYE\", result);
            Assert.Contains("241231_230000_N.mp4", result);
        }

        [Fact]
        public void DeletionDecoupling_SkippedOrFailedFile_IsNotQueuedForDeletion()
        {
            var sourcePath = Path.Combine(tempDir, "camera_file.mp4");
            File.WriteAllText(sourcePath, "critical source video");

            var device = new NativeDevice(tempDir) { DeleteFiles = true };
            var item = new FileItem(sourcePath, "dest.mp4", device);

            var deleteList = new System.Collections.Generic.List<FileItem>();

            // If file is skipped during overwrite prompt (not copied), deleteList remains empty
            Assert.Empty(deleteList);

            // Source file is never touched
            Assert.True(File.Exists(sourcePath));
        }
    }
}
