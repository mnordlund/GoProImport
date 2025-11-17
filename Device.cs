using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoProImport
{
    internal interface Device
    {
        public static bool IsDevice(DriveInfo drive) => throw new NotImplementedException();
        public DriveInfo DriveInfo { get; set; }
        public String DeviceType { get; }
        public string Postfix { get; set; }
        public FileItem[] ListFiles();
    }
}
