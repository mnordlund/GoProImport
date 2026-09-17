using GoProImport.FileTypes;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoProImport.Devices
{
    internal abstract class DeviceBase(DriveInfo DriveInfo)
    {
        public static bool IsDevice(DriveInfo drive) => throw new NotImplementedException();
        public DriveInfo DriveInfo { get; set; } = DriveInfo;
        public abstract String DeviceType { get; }
        public abstract String DeviceName { get; }
        public string ImportName { get; set; }
        public int HourOffset { get; set; } = 0;
        public bool DeleteFiles { get; set; } = false;

        public abstract string DCIMFolder { get; }

        public FileItem[] ListFiles()
        {
            var path = Path.IsPathRooted(DCIMFolder) ? DCIMFolder : Path.Combine(DriveInfo.Name, DCIMFolder);
            var fileList = new List<FileItem>();

            if (!Directory.Exists(path))
            {
                Console.WriteLine($"ERROR: Path '{path}' does not exist!");
                return Array.Empty<FileItem>();
            }

            var files = Directory.GetFiles(path);
            var renamer = new Renamer();

            foreach (var file in files)
            {
                var newName = renamer.GetNewFilename(file, this);
                if (newName != null)
                {
                    fileList.Add(new FileItem(file, newName, this));
                }
            }

            return fileList.ToArray();
        }
    }
}
