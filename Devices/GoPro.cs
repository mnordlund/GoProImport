using System.IO;
using Newtonsoft.Json.Linq;


namespace GoProImport.Devices
{
    class GoPro : IDevice
    {
        public static new bool IsDevice(DriveInfo drive) => Path.Exists(Path.Combine(drive.Name, new GoPro(drive).DCIMFolder));
        public override string DeviceType => "GoPro";
        //public override DriveInfo DriveInfo { get; set; } = DriveInfo;

        public override string DeviceName => GetCamera();

        public override string DCIMFolder => @"DCIM\100GOPRO";

        public GoPro(DriveInfo DriveInfo) : base(DriveInfo) { }

        private string GetCamera()
        {
            var filename = Path.Combine(DriveInfo.Name, @"MISC\version.txt");

            var caminfo = JObject.Parse(File.ReadAllText(filename));

            var camstr = caminfo.Value<string>("camera type");

            if (camstr.StartsWith("HERO12"))
                return "H12";
            else if (camstr.StartsWith("HERO8"))
            {
                return "H8";
            }
            else if (camstr.StartsWith("HERO7"))
            {
                return "H7";
            }

            return "GP";
        }
    }
}
