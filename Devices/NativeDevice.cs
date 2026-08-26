using System.IO;


namespace GoProImport.Devices
{
    internal class NativeDevice : DeviceBase
    {
        public override string DeviceType => "Native";

        public override string DeviceName => "N";

        public override string DCIMFolder { get; }

        public NativeDevice(string DCIMFolder) : base(new DriveInfo(Path.GetPathRoot(DCIMFolder)))
        {
            this.DCIMFolder = DCIMFolder.Substring(Path.GetPathRoot(DCIMFolder).Length);
        }
    }
}
