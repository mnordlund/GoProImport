using System;
using System.IO;
using MetadataExtractor;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace GoProImport
{
    class GoPro
    {
        public string outpath { get; set; } = @"e:\Foto video\martin\GoPro\";

        public string[] FindDrives()
        {
            var drives = new List<string>();
            foreach(var drive in DriveInfo.GetDrives())
            {
                if(Path.Exists(Path.Combine(drive.Name, @"DCIM\100GOPRO")))
                {
                    Console.WriteLine($"Found drive: {drive.Name}");
                    drives.Add(drive.Name);
                }
            }

            return drives.ToArray();
        }
        public void ImportFiles(string drive)
        {
            var path = Path.Combine(drive, @"DCIM\100GOPRO");
            var fileList = new List<FileItem>();

            if(!Path.Exists(path))
            {
                Console.WriteLine($"ERROR: Path '{path}' does not exist!");
                return;
            }
            string[] mp4Files = System.IO.Directory.GetFiles(path, "*.mp4");
            string[] jpegFiles = System.IO.Directory.GetFiles(path, "*.jpg");

            var camera = GetCamera(drive);

            Console.WriteLine("Parsing Photos:");
            foreach(string file in jpegFiles)
            {
                var dest = Path.Combine(outpath, GetNewJPEGFilepath(file, camera));

                if (System.IO.File.Exists(dest))
                {
                    // TODO Check if this is the same file or a different one (check the original name in the header of dest.
                    Console.WriteLine($"File {dest} already exists.");
                }
                else
                {
                    if (!System.IO.Directory.Exists(Path.GetDirectoryName(dest)))
                    {
                        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    }
                    fileList.Add(new FileItem(file, dest));
                }
                //listFileTags(file);
            }

            Console.WriteLine("Parsing Videos:");
            foreach (string file in mp4Files)
            {
                var dest = Path.Combine(outpath, GetNewMP4Filepath(file, camera));

                // TODO Include Proxy file if it exists.
                if (System.IO.File.Exists(dest))
                {
                    // TODO Check if this is the same file or a different one (check the original name in the header of dest.
                    Console.WriteLine($"File {dest} already exists.");
                }
                else
                {
                    if(!System.IO.Directory.Exists(Path.GetDirectoryName(dest)))
                    {
                        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    }
                    fileList.Add(new FileItem(file, dest));
                }
                //listFileTags(file);
            }

            var totalSize = fileList.Sum((fi) => fi.Size);

            Console.WriteLine($"Files found: {fileList.Count} Total size {(totalSize/Math.Pow(1024, 3)).ToString("0.00")} GB");

            Console.WriteLine("Copy files? (y/n): ");
            var reply = Console.ReadLine();
            if (reply.Trim().ToLower() == "y")
            {
                Console.Write("Copying files...");
                var count = 0;
                var progress = new[]{'|', '/', '-', '\\'};
                Console.CursorVisible = false;

                foreach(var item in fileList)
                {
                    Console.Write("\b" + progress[(count++) % progress.Length]);
                    File.Copy(item.OriginalPath, item.NewPath,true);
                }

                Console.WriteLine("\bDone!");
                Console.CursorVisible = true;

            }
        }

        private void listFileTags(string filepath)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filepath);

            foreach(var dir in dirs)
            foreach (var tag in dir.Tags)
                {
                    Console.WriteLine($"\t{dir.Name} - {tag.Name}: {tag.Description}");
                }
        }

        private string GetNewJPEGFilepath(string filepath, string camstr)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filepath);


            var exifIFD0 = dirs.OfType<MetadataExtractor.Formats.Exif.ExifIfd0Directory>().FirstOrDefault();


            var dateTime = exifIFD0.GetDateTime(MetadataExtractor.Formats.Exif.ExifIfd0Directory.TagDateTime);

            var timestamp = dateTime.ToString("yyMMddHHmmss");

            var year = dateTime.ToString("yyyy");

            var date = dateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}\";

            return $"{path}{timestamp}_{camstr}.jpg";
        }

        private string GetNewMP4Filepath(string filepath, string camstr)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filepath);

            var qtmheader = dirs.OfType<MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            var qttheader = dirs.OfType<MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory>().FirstOrDefault();

            var dateTime = qtmheader.GetDateTime(MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory.TagCreated);

            var timestamp = dateTime.ToString("yyMMddHHmmss");
            var res = GetResolutionString(qttheader.GetInt32(MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagWidth), qttheader.GetInt32(MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagHeight));

            // TODO Find out FPS of file and append it to the name.

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
