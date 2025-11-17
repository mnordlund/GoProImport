using MetadataExtractor.Formats.Photoshop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoProImport.Devices
{
    internal class DJI_Mic(DriveInfo DriveInfo) : Device
    {
        public DriveInfo DriveInfo { get; set; } = DriveInfo;
        public string DeviceType => "DJI Mic 2";
        public string Postfix { get; set; }

        public string outpath { get; set; } = @"d:\GoPro\";

        private const string DCIM_FOLDER = @"DJI_Audio_001";


        public static bool IsDevice(DriveInfo drive)
        {
            return Path.Exists(Path.Combine(drive.Name, DCIM_FOLDER));
        }

        public FileItem[] ListFiles()
        {
            var path = Path.Combine(DriveInfo.Name, DCIM_FOLDER);
            var fileList = new List<FileItem>();

            if (!Path.Exists(path))
            {
                Console.WriteLine($"ERROR: Path '{path}' does not exist!");
                return null;
            }

            string[] wavFiles = System.IO.Directory.GetFiles(path, "*.wav");

            foreach(var wavFile in wavFiles )
            {
                // TODO Read date from file name.
                var dest = Path.Combine(outpath, GetNewWAVFilePath(wavFile));

                fileList.Add(new FileItem(wavFile, dest));
            }

            return fileList.ToArray();
        }

        private string GetNewWAVFilePath(string filepath)
        {
            var filename = Path.GetFileName(filepath);
            var spl_filename = filename.Split('_', '.');
            var datestring = spl_filename[2];
            var timestring = spl_filename[3];

            var year = datestring.Substring(0, 4);
            var month = datestring.Substring(4, 2);
            var day = datestring.Substring(6, 2);

            var path = @$"{year}\{year}-{month}-{day}_{Postfix}\";

            return $"{path}{year.Substring(2)}{month}{day}{timestring}_DM2.wav";
        }
    }
}
