using System.IO;

namespace GoProImport.Devices
{
    internal class DJI_Mic : IDevice
    {
        public override string DeviceType => "DJI Mic 2";

        public override string DeviceName => "DM2";

        public override string DCIMFolder => @"DJI_Audio_001";
        public DJI_Mic(DriveInfo DriveInfo) : base(DriveInfo) { }


        public static new bool IsDevice(DriveInfo drive)
        {
            return Path.Exists(Path.Combine(drive.Name, new DJI_Mic(drive).DCIMFolder));
        }
    }
}
