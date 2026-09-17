using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoProImport;
using GoProImport.Devices;
using Xunit;

namespace GoProImport.Tests
{
    public class Section2PerformanceAndUXTests : IDisposable
    {
        private readonly string tempDir;

        public Section2PerformanceAndUXTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "GoProImport_Sec2_" + Guid.NewGuid().ToString("N"));
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
        // Task 1: Stream-based chunked copying with live progress & transfer speed
        // =========================================================================

        [Fact]
        public void ChunkedCopy_ReportsProgressInChunksAndPreservesContent()
        {
            var sourcePath = Path.Combine(tempDir, "large_source.bin");
            var destDir = Path.Combine(tempDir, "dest");
            FileItem.DstPath = destDir;

            // Generate 10 MB file
            const int totalSize = 10 * 1024 * 1024;
            var testData = new byte[totalSize];
            new Random(42).NextBytes(testData);
            File.WriteAllBytes(sourcePath, testData);

            var item = new FileItem(sourcePath, "large_dest.bin");

            // Use 2 MB buffer size
            const int bufferSize = 2 * 1024 * 1024;
            long reportedBytes = 0;
            int callbackCount = 0;

            var copied = item.CopyFile(bytesRead =>
            {
                reportedBytes += bytesRead;
                callbackCount++;
            }, bufferSize: bufferSize);

            Assert.True(copied);
            Assert.Equal(totalSize, reportedBytes);
            // With 10 MB and 2 MB buffer, should have 5 chunks
            Assert.Equal(5, callbackCount);

            var destBytes = File.ReadAllBytes(item.DestinationFullPath);
            Assert.Equal(testData, destBytes);
        }

        [Fact]
        public void CopyProgressTracker_FormatSize_FormatsCorrectUnits()
        {
            Assert.Equal("500 B", CopyProgressTracker.FormatSize(500));
            Assert.Equal("1.50 KB", CopyProgressTracker.FormatSize(1536));
            Assert.Equal("10.00 MB", CopyProgressTracker.FormatSize(10 * 1024 * 1024));
            Assert.Equal("4.50 GB", CopyProgressTracker.FormatSize((long)(4.5 * 1024 * 1024 * 1024)));
        }

        [Fact]
        public void CopyProgressTracker_GenerateProgressBar_ProducesExpectedFormatting()
        {
            var bar0 = CopyProgressTracker.GenerateProgressBar(0, 20);
            Assert.Equal("[" + new string('-', 20) + "]", bar0);

            var bar50 = CopyProgressTracker.GenerateProgressBar(50, 20);
            Assert.Equal("[" + new string('#', 10) + new string('-', 10) + "]", bar50);

            var bar100 = CopyProgressTracker.GenerateProgressBar(100, 20);
            Assert.Equal("[" + new string('#', 20) + "]", bar100);

            // Clamped
            var barOver = CopyProgressTracker.GenerateProgressBar(150, 20);
            Assert.Equal("[" + new string('#', 20) + "]", barOver);
        }

        // =========================================================================
        // Task 2: Safe console cursor updates
        // =========================================================================

        [Fact]
        public void SafeCursorUpdates_WhenRedirected_DoesNotThrowIOException()
        {
            var originalOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);

            try
            {
                // In redirected stdout, cursor manipulations must be safely bypassed
                var ex1 = Record.Exception(() => CopyProgressTracker.SafeSetCursorVisibility(false));
                Assert.Null(ex1);

                var ex2 = Record.Exception(() => CopyProgressTracker.SafeSetCursorVisibility(true));
                Assert.Null(ex2);

                using var tracker = new CopyProgressTracker(1024 * 1024, 1);
                tracker.RegisterDevice("dev1", "TestDevice", 1, 1024 * 1024);
                tracker.OnFileStart("dev1", "file.mp4", 1024 * 1024, 1);
                tracker.OnBytesCopied("dev1", 512 * 1024);
                tracker.OnFileCompleted("dev1", "file.mp4", true);
                tracker.OnDeviceCompleted("dev1");
                tracker.Finish();

                var output = sw.ToString();
                Assert.Contains("TestDevice", output);
                Assert.Contains("file.mp4", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        // =========================================================================
        // Task 3: Parallel multi-device copy support
        // =========================================================================

        [Fact]
        public async Task ParallelMultiDeviceCopy_CopiesFromDistinctDevicesConcurrently()
        {
            var dev1Dir = Path.Combine(tempDir, "dev1_dcim");
            var dev2Dir = Path.Combine(tempDir, "dev2_dcim");
            Directory.CreateDirectory(dev1Dir);
            Directory.CreateDirectory(dev2Dir);

            var destDir = Path.Combine(tempDir, "dest_parallel");
            FileItem.DstPath = destDir;

            // Create files on device 1 (e.g. GoPro)
            var dev1Files = new List<FileItem>();
            var dev1 = new NativeDevice(dev1Dir) { DeleteFiles = false };
            for (int i = 1; i <= 3; i++)
            {
                var filePath = Path.Combine(dev1Dir, $"GP_{i}.mp4");
                File.WriteAllBytes(filePath, new byte[1024 * 1024]); // 1 MB
                dev1Files.Add(new FileItem(filePath, $"dev1/GP_{i}.mp4", dev1));
            }

            // Create files on device 2 (e.g. DJI Mic)
            var dev2Files = new List<FileItem>();
            var dev2 = new NativeDevice(dev2Dir) { DeleteFiles = true };
            for (int i = 1; i <= 3; i++)
            {
                var filePath = Path.Combine(dev2Dir, $"MIC_{i}.wav");
                File.WriteAllBytes(filePath, new byte[512 * 1024]); // 512 KB
                dev2Files.Add(new FileItem(filePath, $"dev2/MIC_{i}.wav", dev2));
            }

            var allFiles = dev1Files.Concat(dev2Files).ToList();
            long totalSize = allFiles.Sum(f => f.Size);

            using var tracker = new CopyProgressTracker(totalSize, allFiles.Count);
            var concurrentDeleteList = new ConcurrentBag<FileItem>();

            // Group by distinct device
            var deviceGroups = allFiles
                .GroupBy(f => f.Device?.DCIMFolder ?? f.OriginalPath)
                .ToList();

            Assert.Equal(2, deviceGroups.Count);

            var copyTasks = deviceGroups.Select(group => Task.Run(() =>
            {
                var device = group.First().Device;
                string deviceName = device?.DeviceType ?? "TestDevice";
                tracker.RegisterDevice(group.Key, deviceName, group.Count(), group.Sum(f => f.Size));

                int fileIndex = 1;
                foreach (var item in group)
                {
                    var fileName = Path.GetFileName(item.NewPath);
                    tracker.OnFileStart(group.Key, fileName, item.Size, fileIndex);

                    var success = item.CopyFile(bytesRead =>
                    {
                        tracker.OnBytesCopied(group.Key, bytesRead);
                    });

                    if (success)
                    {
                        if (item.Device != null && item.Device.DeleteFiles)
                        {
                            concurrentDeleteList.Add(item);
                        }
                        tracker.OnFileCompleted(group.Key, fileName, true);
                    }
                    else
                    {
                        tracker.OnFileCompleted(group.Key, fileName, false);
                    }

                    fileIndex++;
                }

                tracker.OnDeviceCompleted(group.Key);
            })).ToArray();

            await Task.WhenAll(copyTasks);
            tracker.Finish();

            // All destination files must exist and verify integrity
            foreach (var file in allFiles)
            {
                Assert.True(File.Exists(file.DestinationFullPath));
                Assert.True(file.VerifyIntegrity());
            }

            // Only device 2 has DeleteFiles = true (3 files)
            Assert.Equal(3, concurrentDeleteList.Count);
            Assert.All(concurrentDeleteList, item => Assert.Equal(dev2, item.Device));
        }
    }
}
