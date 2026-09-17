using System;
using System.IO;


namespace GoProImport
{
    internal class FileItem
    {
        private static string _defaultDstPath;
        public static string DstPath
        {
            get => _defaultDstPath;
            set => _defaultDstPath = value;
        }

        private string _destinationPath;
        public string DestinationPath
        {
            get => _destinationPath ?? DstPath;
            set => _destinationPath = value;
        }

        public string OriginalPath { get; set; }
        public string NewPath { get; set; }
        public long Size { get; set; }
        public Devices.DeviceBase Device { get; set; }

        public string DestinationFullPath => Path.Combine(DestinationPath ?? string.Empty, NewPath);

        public bool FileExists
        {
            get
            {
                return File.Exists(DestinationFullPath);
            }
        }

        // Pretty print sizes
        public string SizeString => CopyProgressTracker.FormatSize(Size);

        public FileItem(string originalPath, string newPath, Devices.DeviceBase device = null, string destinationPath = null)
        {
            OriginalPath = originalPath;
            NewPath = newPath;
            Device = device;
            _destinationPath = destinationPath;

            Size = File.Exists(originalPath) ? new FileInfo(originalPath).Length : 0;
        }

        public bool VerifyIntegrity()
        {
            var fullNewPath = DestinationFullPath;
            if (!File.Exists(fullNewPath) || !File.Exists(OriginalPath))
            {
                return false;
            }

            var destInfo = new FileInfo(fullNewPath);
            var srcInfo = new FileInfo(OriginalPath);
            return destInfo.Length == srcInfo.Length;
        }

        public bool CopyFile(Action<long> onBytesCopied = null, int bufferSize = 4 * 1024 * 1024)
        {
            try
            {
                var fullNewPath = DestinationFullPath;
                var dir = Path.GetDirectoryName(fullNewPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    using (var sourceStream = new FileStream(OriginalPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
                    using (var destStream = new FileStream(fullNewPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan))
                    {
                        int bytesRead;
                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            destStream.Write(buffer, 0, bytesRead);
                            onBytesCopied?.Invoke(bytesRead);
                        }
                    }
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }

                try
                {
                    if (File.Exists(OriginalPath))
                    {
                        File.SetLastWriteTime(fullNewPath, File.GetLastWriteTime(OriginalPath));
                    }
                }
                catch
                {
                    // Non-fatal if timestamp preservation is not supported
                }

                return VerifyIntegrity();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying '{OriginalPath}' to '{DestinationFullPath}': {ex.Message}");
                return false;
            }
        }

        public void DeleteOriginal()
        {
            File.Delete(OriginalPath);
        }
    }
}
