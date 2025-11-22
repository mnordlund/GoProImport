using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoProImport.Devices
{
    internal class DJI_Osmo : IDevice
    {
        public static new bool IsDevice(DriveInfo drive) => Path.Exists(Path.Combine(drive.Name, new DJI_Osmo(drive).DCIMFolder));
        public override string DeviceType => "DJI Osmo";

        public override string DeviceName => "DOP3";

        public override string DCIMFolder => @"DCIM\DJI_001";

        public DJI_Osmo(DriveInfo DriveInfo) : base(DriveInfo) { }
    }
}
