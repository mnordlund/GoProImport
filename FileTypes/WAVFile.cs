using GoProImport.Devices;
using MetadataExtractor;
using System;
using System.IO;
using System.Linq;

namespace GoProImport.FileTypes
{
    internal class WAVFile : IFiletype
    {
        public string Extension => ".wav";

        public string GetNewFilepath(string filename, DeviceBase device)
        {
            DateTime? dateTime = null;

            try
            {
                var dirs = ImageMetadataReader.ReadMetadata(filename);
                var fileheader = dirs.OfType<MetadataExtractor.Formats.FileSystem.FileMetadataDirectory>().FirstOrDefault();
                if (fileheader != null && fileheader.TryGetDateTime(MetadataExtractor.Formats.FileSystem.FileMetadataDirectory.TagFileModifiedDate, out var dt))
                {
                    dateTime = dt;
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

            var adjustedDateTime = dateTime.Value.AddHours(device.HourOffset);
            var timestamp = adjustedDateTime.ToString("yyMMdd_HHmmss");
            var year = adjustedDateTime.ToString("yyyy");
            var date = adjustedDateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}_{device.ImportName}\";

            return $"{path}{timestamp}_{device.DeviceName}.wav";
        }
    }
}
