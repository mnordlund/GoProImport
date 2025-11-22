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
        public bool FileExists
        {
            get
            {
                return File.Exists(Path.Combine(DstPath,NewPath));
            }
        }

        // TODO Create function to pretty print sizes
        public string SizeString => (Size / Math.Pow(1024, 2)).ToString("0.00") + "MB";

        public FileItem(string originalPath, string newPath)
        {
            OriginalPath = originalPath;
            NewPath = newPath;

            Size = new FileInfo(originalPath).Length;
        }

        public void CopyFile()
        {
            var fullNewPath = Path.Combine(DstPath,NewPath);
            if(!Path.Exists(Path.GetDirectoryName(fullNewPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullNewPath));
            }
            File.Copy(OriginalPath, fullNewPath, true);
        }
    }
}
