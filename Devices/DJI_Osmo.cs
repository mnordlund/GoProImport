using System;
using System.IO;

namespace GoProImport.Devices
{
    internal class DJI_Osmo : DeviceBase
    {
        public static new bool IsDevice(DriveInfo drive) => Directory.Exists(Path.Combine(drive.Name, new DJI_Osmo(drive).DCIMFolder));
        public override string DeviceType => "DJI Osmo";

        public override string DeviceName => GetCamera();

        private readonly string customDcimFolder = null;
        public override string DCIMFolder => customDcimFolder ?? @"DCIM\DJI_001";

        public DJI_Osmo(DriveInfo DriveInfo, string customDcimFolder = null) : base(DriveInfo) 
        { 
            this.customDcimFolder = customDcimFolder;
        }

        private string devName = null;

        private string GetCamera()
        {
            if (devName != null) return devName;

            // TODO Make camera array readable from file
            var cameras = new[] { ("B01", "DOP3"), ("C001", "DOA6") };
            var path = Path.IsPathRooted(DCIMFolder) ? DCIMFolder : Path.Combine(DriveInfo.Name, DCIMFolder);

            if (!Directory.Exists(path))
            {
                return "DJI_Osmo";
            }

            var files = Directory.GetFiles(path);
            if (files.Length == 0)
            {
                return "DJI_Osmo";
            }

            var file = Path.GetFileNameWithoutExtension(files[0]);

            foreach (var camera in cameras)
            {
                if(file.EndsWith(camera.Item1))
                {
                    devName = camera.Item2;
                    return devName;
                }
            }
            // TODO No camera found, present instructions to add camera.
            var scheme = file.Length >= 4 ? file[^4..] : file;
            Console.WriteLine($"Unknown {DeviceType} camera. Filenaming scheme is {scheme}.");
            Console.WriteLine("Update DJI_Osmo.cs to add this camera.");
            Console.WriteLine("Enter camera name to use for current import:");
            devName = Console.ReadLine();
            return devName;
        }
    }
}
