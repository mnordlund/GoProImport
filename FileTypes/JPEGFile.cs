using GoProImport.Devices;
using MetadataExtractor;
using System;
using System.IO;
using System.Linq;

namespace GoProImport.FileTypes
{
    internal class JPEGFile : IFiletype
    {
        public string Extension => ".jpg";

        public string GetNewFilepath(string filename, DeviceBase device)
        {
            DateTime? dateTime = null;

            try
            {
                var dirs = ImageMetadataReader.ReadMetadata(filename);
                var exifIFD0 = dirs.OfType<MetadataExtractor.Formats.Exif.ExifIfd0Directory>().FirstOrDefault();
                if (exifIFD0 != null && exifIFD0.TryGetDateTime(MetadataExtractor.Formats.Exif.ExifIfd0Directory.TagDateTime, out var dt))
                {
                    dateTime = dt;
                }
                else
                {
                    var subIfd = dirs.OfType<MetadataExtractor.Formats.Exif.ExifSubIfdDirectory>().FirstOrDefault();
                    if (subIfd != null && (subIfd.TryGetDateTime(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeOriginal, out dt) ||
                                           subIfd.TryGetDateTime(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeDigitized, out dt)))
                    {
                        dateTime = dt;
                    }
                    else
                    {
                        var fileHeader = dirs.OfType<MetadataExtractor.Formats.FileSystem.FileMetadataDirectory>().FirstOrDefault();
                        if (fileHeader != null && fileHeader.TryGetDateTime(MetadataExtractor.Formats.FileSystem.FileMetadataDirectory.TagFileModifiedDate, out dt))
                        {
                            dateTime = dt;
                        }
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

            var adjustedDateTime = dateTime.Value.AddHours(device.HourOffset);
            var timestamp = adjustedDateTime.ToString("yyMMdd_HHmmss");
            var year = adjustedDateTime.ToString("yyyy");
            var date = adjustedDateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}_{device.ImportName}\";

            return $"{path}{timestamp}_{device.DeviceName}.jpg";
        }
    }
}
