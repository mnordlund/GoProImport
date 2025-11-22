using GoProImport.Devices;
using MetadataExtractor;
using System;
using System.Linq;

namespace GoProImport.FileTypes
{
    internal class MP4File : IFiletype
    {
        public string Extension => ".mp4";

        public string GetNewFilepath(string filename, IDevice device)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filename);

            var qtmheader = dirs.OfType<MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            var qttheader = dirs.OfType<MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory>().FirstOrDefault();
            var fileheader = dirs.OfType<MetadataExtractor.Formats.FileSystem.FileMetadataDirectory>().FirstOrDefault();

            var dateTime = fileheader.GetDateTime(MetadataExtractor.Formats.FileSystem.FileMetadataDirectory.TagFileModifiedDate);

            var timestamp = dateTime.AddHours(device.HourOffset).ToString("yyMMdd_HHmmss");
            var res = GetResolutionString(qttheader.GetInt32(MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagWidth), qttheader.GetInt32(MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagHeight));

            var year = dateTime.ToString("yyyy");

            var date = dateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}_{device.ImportName}\";

            return $"{path}{timestamp}_{device.DeviceName}_{res}.mp4";
        }

        private static string GetResolutionString(int width, int height)
        {
            // TODO Make sure resolutions are accurate
            var resString = string.Empty;
            var max = width > height ? width : height;

            if (max == height)
            {
                resString += "V";
            }
            else if(((int[])[3000, 2028, 4648, 3360]).Contains(width) || width == height)
            {
                resString += "SQ";
            }

            switch (max)
            {
                case 5312:
                    resString += "5K";
                    break;
                case 3840:
                case 4000:
                    resString += "4K";
                    break;
                case 3072:
                    resString += "3K";
                    break;
                case 2704:
                case 2720:
                    resString += "2K";
                    break;
                case 1920:
                    resString += "HD";
                    break;
                default:
                    break;
            }

            return resString;
        }
    }
}
