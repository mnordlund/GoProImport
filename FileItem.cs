using System;
using System.IO;


namespace GoProImport
{
    internal class FileItem
    {
        public static string DstPath { get; set; }
        public string OriginalPath { get; set; }
        public string NewPath { get; set; }
        public long Size { get; set; }
        public Devices.DeviceBase Device { get; set; }

        public string DestinationFullPath => Path.Combine(DstPath, NewPath);

        public bool FileExists
        {
            get
            {
                return File.Exists(DestinationFullPath);
            }
        }

        // TODO Create function to pretty print sizes
        public string SizeString => (Size / Math.Pow(1024, 2)).ToString("0.00") + "MB";

        public FileItem(string originalPath, string newPath, Devices.DeviceBase device = null)
        {
            OriginalPath = originalPath;
            NewPath = newPath;
            Device = device;

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

        public bool CopyFile()
        {
            try
            {
                var fullNewPath = DestinationFullPath;
                var dir = Path.GetDirectoryName(fullNewPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(OriginalPath, fullNewPath, true);
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
