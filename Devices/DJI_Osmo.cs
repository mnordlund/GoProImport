using System;
using System.IO;

namespace GoProImport.Devices
{
    internal class DJI_Osmo : DeviceBase
    {
        public static new bool IsDevice(DriveInfo drive) => Path.Exists(Path.Combine(drive.Name, new DJI_Osmo(drive).DCIMFolder));
        public override string DeviceType => "DJI Osmo";

        public override string DeviceName => GetCamera();

        public override string DCIMFolder => @"DCIM\DJI_001";

        public DJI_Osmo(DriveInfo DriveInfo) : base(DriveInfo) { }

        private string? devName = null;

        private string GetCamera()
        {
            if (devName != null) return devName;

            // TODO Make camera array readable from file
            var cameras = new[] { ("B01", "DOP3"), ("C001", "DOA6") };
            var path = Path.Combine(DriveInfo.Name, DCIMFolder);
            var file = Path.GetFileNameWithoutExtension(Directory.GetFiles(path)[0]);

            foreach (var camera in cameras)
            {
                if(file.EndsWith(camera.Item1))
                {
                    devName = camera.Item2;
                    return devName;
                }
            }
            // TODO No camera found, present instructions to add camera.
            Console.WriteLine($"Unknown {DeviceType} camera. Filenaming scheme is {file[^4..]}.");
            Console.WriteLine("Update DJI_Osmo.cs to add this camera.");
            Console.WriteLine("Enter camera name to use for current import:");
            devName = Console.ReadLine();
            return devName;
        }
    }
}
