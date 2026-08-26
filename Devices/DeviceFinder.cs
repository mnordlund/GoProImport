using System.Collections.Generic;
using System.IO;

namespace GoProImport.Devices
{
    internal class DeviceFinder
    {
        public static DeviceBase[] ListDevices()
        {
            var drives = new List<DeviceBase>();

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

        public static DeviceBase GetDevice(DriveInfo drive)
        {
            if (GoPro.IsDevice(drive))
            {
                return new GoPro(drive);
            }
            else if (DJI_Mic.IsDevice(drive))
            {
                return new DJI_Mic(drive);
            }
            else if (DJI_Osmo.IsDevice(drive))
            {
                return new DJI_Osmo(drive);
            }

            return null;
        }
    }
}
