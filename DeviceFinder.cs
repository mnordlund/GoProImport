using GoProImport.Devices;
using System.Collections.Generic;
using System.IO;

namespace GoProImport
{
    internal class DeviceFinder
    {
        public static Device[] ListDevices()
        {
            var drives = new List<Device>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                var ret = GetDevice(drive);

                if(ret != null)
                {
                    drives.Add(ret);
                }
            }
            return drives.ToArray();
        }

        public static Device GetDevice(DriveInfo drive)
        {
            // TODO Add more device types.
            if (GoPro.IsDevice(drive))
            {
                return new GoPro(drive);
            }
            else if (DJI_Mic.IsDevice(drive))
            {
                return new DJI_Mic(drive);
            }

            return null;
        }
    }
}
