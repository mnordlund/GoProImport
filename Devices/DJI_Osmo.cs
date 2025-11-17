using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoProImport.Devices
{
    internal class DJI_Osmo(DriveInfo DriveInfo) : Device
    {
        public DriveInfo DriveInfo { get; set; } = DriveInfo;
        public string DeviceType => "DJI Mic 2";
        public string Postfix { get; set; }

        public string outpath { get; set; } = @"d:\GoPro\";

        private const string DCIM_FOLDER = @"DJI_Audio_001";

        public static bool IsDevice(DriveInfo drive)
        {
            throw new NotImplementedException();
        }

        public FileItem[] ListFiles()
        {
            throw new NotImplementedException();
        }
    }
}
