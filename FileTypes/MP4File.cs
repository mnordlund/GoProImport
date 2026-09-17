using GoProImport.Devices;
using MetadataExtractor;
using System;
using System.IO;
using System.Linq;

namespace GoProImport.FileTypes
{
    internal class MP4File : IFiletype
    {
        public string Extension => ".mp4";

        public string GetNewFilepath(string filename, DeviceBase device)
        {
            DateTime? dateTime = null;
            int width = 0;
            int height = 0;

            try
            {
                var dirs = ImageMetadataReader.ReadMetadata(filename);

                var qttheader = dirs.OfType<MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory>().FirstOrDefault();
                if (qttheader != null)
                {
                    qttheader.TryGetInt32(MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagWidth, out width);
                    qttheader.TryGetInt32(MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagHeight, out height);
                }

                var fileheader = dirs.OfType<MetadataExtractor.Formats.FileSystem.FileMetadataDirectory>().FirstOrDefault();
                if (fileheader != null && fileheader.TryGetDateTime(MetadataExtractor.Formats.FileSystem.FileMetadataDirectory.TagFileModifiedDate, out var dt))
                {
                    dateTime = dt;
                }
                else
                {
                    var qtmheader = dirs.OfType<MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory>().FirstOrDefault();
                    if (qtmheader != null && qtmheader.TryGetDateTime(MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory.TagCreated, out var qtmDt))
                    {
                        dateTime = qtmDt;
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to file system timestamp below if metadata parsing fails
            }

            if (!dateTime.HasValue)
            {
                dateTime = File.Exists(filename) ? File.GetLastWriteTime(filename) : DateTime.Now;
            }

            // TODO Use date created together with timezone from file modified to set timestamp.
            // TODO Possibly depending on if using native device or not
            var adjustedDateTime = dateTime.Value.AddHours(device.HourOffset);
            var timestamp = adjustedDateTime.ToString("yyMMdd_HHmmss");
            var res = GetResolutionString(width, height);
            var resPart = string.IsNullOrEmpty(res) ? string.Empty : $"_{res}";

            var year = adjustedDateTime.ToString("yyyy");

            var date = adjustedDateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}_{device.ImportName}\";

            return $"{path}{timestamp}_{device.DeviceName}{resPart}.mp4";
        }

        private static string GetResolutionString(int width, int height)
        {
            if (width <= 0 && height <= 0)
            {
                return string.Empty;
            }

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
