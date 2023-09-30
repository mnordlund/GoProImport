using System;
using System.IO;
using MetadataExtractor;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GoProImport
{
    class GoPro
    {
        public string outpath { get; set; } = @"e:\Foto video\martin\GoPro\";
        public void ImportFiles(string drive)
        {
            var path = Path.Combine(drive, @"DCIM\100GOPRO");
            string[] files = System.IO.Directory.GetFiles(path, "*.mp4");

            var camera = GetCamera(drive);

            foreach(string file in files)
            {
                Console.WriteLine($"{file} -> {Path.Combine(outpath,GetNewFilepath(file, camera))}");

                // TODO Include Proxy file if it exists.
                File.Copy(file, Path.Combine(outpath, GetNewFilepath(file, camera)));
                //parseMp4(file);
            }
        }

        private void parseMp4(string filepath)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filepath);

            foreach(var dir in dirs)
            foreach (var tag in dir.Tags)
                {
                    Console.WriteLine($"\t{dir.Name} - {tag.Name}: {tag.Description}");
                }
        }

        private string GetNewFilepath(string filepath, string camstr)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filepath);

            var qtmheader = dirs.OfType<MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            var qttheader = dirs.OfType<MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory>().FirstOrDefault();

            var dateTime = qtmheader.GetDateTime(MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory.TagCreated);

            var timestamp = dateTime.ToString("yyMMddHHmmss");
            var res = GetResolutionString(qttheader.GetInt32(MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagWidth), qttheader.GetInt32(MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagHeight));

            var year = dateTime.ToString("yyyy");

            var date = dateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}\";

            return $"{path}{timestamp}_{camstr}_{res}.mp4";
        }

        private string GetResolutionString(int width, int height)
        {
            if (5312 == width)
                return "53K";
            if (3840 == width)
                return "4K";
            if (2704 == width)
                return "27K";
            if (1920 == width)
                return "HD";

            return "";
        }

        private string GetCamera(string drive)
        {
            var filename = Path.Combine(drive, @"MISC\version.txt");

            var caminfo = JObject.Parse(File.ReadAllText(filename));

            var camstr = caminfo.Value<string>("camera type");

            if (camstr.StartsWith("HERO12"))
                return "H12";

            // TODO Recognize other cameras.
            return "H8";
        }
    }
}
