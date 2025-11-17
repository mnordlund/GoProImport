using System;
using System.IO;


namespace GoProImport
{
    internal class FileItem
    {
        public string OriginalPath { get; set; }
        public string NewPath { get; set; }
        public long Size { get; set; }
        public bool FileExists
        {
            get
            {
                return File.Exists(NewPath);
            }
        }

        public string SizeString => (Size / Math.Pow(1024, 2)).ToString("0.00") + "MB";

        public FileItem(string originalPath, string newPath)
        {
            OriginalPath = originalPath;
            NewPath = newPath;

            Size = new FileInfo(originalPath).Length;
        }

        public void CopyFile()
        {
            if(!Path.Exists(Path.GetDirectoryName(NewPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(NewPath));
            }
            File.Copy(OriginalPath, NewPath, true);
        }
    }
}
