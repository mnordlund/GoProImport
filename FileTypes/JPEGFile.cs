using GoProImport.Devices;
using MetadataExtractor;
using System.Linq;

namespace GoProImport.FileTypes
{
    internal class JPEGFile : IFiletype
    {
        public string Extension => ".jpg";

        public string GetNewFilepath(string filename, IDevice device)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filename);


            var exifIFD0 = dirs.OfType<MetadataExtractor.Formats.Exif.ExifIfd0Directory>().FirstOrDefault();


            var dateTime = exifIFD0.GetDateTime(MetadataExtractor.Formats.Exif.ExifIfd0Directory.TagDateTime);

            var timestamp = dateTime.ToString("yyMMdd_HHmmss");

            var year = dateTime.AddHours(device.HourOffset).ToString("yyyy");

            var date = dateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}_{device.ImportName}\";

            return $"{path}{timestamp}_{device.DeviceName}.jpg";
        }
    }
}
