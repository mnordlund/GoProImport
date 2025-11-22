using GoProImport.Devices;
using MetadataExtractor;
using System.Linq;

namespace GoProImport.FileTypes
{
    internal class WAVFile : IFiletype
    {
        public string Extension => ".wav";

        public string GetNewFilepath(string filename, IDevice device)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filename);

            var fileheader = dirs.OfType<MetadataExtractor.Formats.FileSystem.FileMetadataDirectory>().FirstOrDefault();
            var dateTime = fileheader.GetDateTime(MetadataExtractor.Formats.FileSystem.FileMetadataDirectory.TagFileModifiedDate);

            var timestamp = dateTime.AddHours(device.HourOffset).ToString("yyMMdd_HHmmss");

            var year = dateTime.ToString("yyyy");

            var date = dateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}_{device.ImportName}\";

            return $"{path}{timestamp}_{device.DeviceName}.wav";
        }
    }
}
