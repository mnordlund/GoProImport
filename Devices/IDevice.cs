using GoProImport.FileTypes;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoProImport.Devices
{
    internal abstract class IDevice(DriveInfo DriveInfo)
    {
        public static bool IsDevice(DriveInfo drive) => throw new NotImplementedException();
        public DriveInfo DriveInfo { get; set; } = DriveInfo;
        public abstract String DeviceType { get; }
        public abstract String DeviceName { get; }
        public string ImportName { get; set; }
        public int HourOffset { get; }

        public abstract string DCIMFolder { get; }

        public FileItem[] ListFiles()
        {
            var path = Path.Combine(DriveInfo.Name, DCIMFolder);
            var fileList = new List<FileItem>();

            if (!Path.Exists(path))
            {
                Console.WriteLine($"ERROR: Path '{path}' does not exist!");
                return null;
            }

            var files = Directory.GetFiles(path);
            var renamer = new Renamer();

            foreach (var file in files)
            {
                var newName = renamer.GetNewFilename(file, this);
                if (newName != null)
                {
                    fileList.Add(new FileItem(file, newName));
                }
            }

            return fileList.ToArray();
        }
    }
}
