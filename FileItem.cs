using System;
using System.IO;


namespace GoProImport
{
    internal class FileItem
    {
        public string OriginalPath { get; set; }
        public string NewPath {  get; set; }
        public long Size { get; set; }

        public FileItem(string originalPath, string newPath)
        {
            OriginalPath = originalPath;
            NewPath = newPath;

            Size = new FileInfo(originalPath).Length;

            Console.WriteLine($"{originalPath} => {newPath} {(Size/Math.Pow(1024,2)).ToString("0.00")}MB");
        }
    }
}
