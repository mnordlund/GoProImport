using System;
using System.IO;
using MetadataExtractor;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace GoProImport.Devices
{
    class GoPro(DriveInfo DriveInfo) : Device
    {
        // TODO Make this configurable
        // TODO Add backup paths where backups can be copied to.
        public string DeviceType => "GoPro";
        public DriveInfo DriveInfo { get; set; } = DriveInfo;
        public string Postfix { get; set; }
        public string outpath { get; set; } = @"d:\GoPro\";

        private const string DCIM_FOLDER = @"DCIM\100GOPRO";

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
            string[] mp4Files = System.IO.Directory.GetFiles(path, "*.mp4");
            string[] jpegFiles = System.IO.Directory.GetFiles(path, "*.jpg");

            var camera = GetCamera(DriveInfo.Name);

            foreach (string file in jpegFiles)
            {
                var dest = Path.Combine(outpath, GetNewJPEGFilepath(file, camera, Postfix));

                if (System.IO.File.Exists(dest))
                {
                    // TODO Check if this is the same file or a different one (check the original name in the header of dest.
                    Console.WriteLine($"File {dest} already exists.");
                }
                else
                {
                    fileList.Add(new FileItem(file, dest));
                }
                //listFileTags(file);
            }

            foreach (string file in mp4Files)
            {
                var dest = Path.Combine(outpath, GetNewMP4Filepath(file, camera, Postfix));

                // TODO Include Proxy file if it exists.
                if (System.IO.File.Exists(dest))
                {
                    // TODO Check if this is the same file or a different one (check the original name in the header of dest.
                    Console.WriteLine($"File {dest} already exists.");
                }
                else
                {
                    fileList.Add(new FileItem(file, dest));
                }
                //listFileTags(file);
            }

            return fileList.ToArray();
        }

        /*
        public void ImportFiles()
        {
            var path = Path.Combine(driveInfo.Name, dcimFolder);
            var fileList = new List<FileItem>();

            if(!Path.Exists(path))
            {
                Console.WriteLine($"ERROR: Path '{path}' does not exist!");
                return;
            }
            string[] mp4Files = System.IO.Directory.GetFiles(path, "*.mp4");
            string[] jpegFiles = System.IO.Directory.GetFiles(path, "*.jpg");

            Console.WriteLine("Do you want to name the import? (Enter to skip):");
            var postfix = Console.ReadLine().Trim().Replace(' ', '_');

            var camera = GetCamera(drive);

            Console.WriteLine("Parsing Photos:");
            foreach(string file in jpegFiles)
            {
                var dest = Path.Combine(outpath, GetNewJPEGFilepath(file, camera, postfix));

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
                var dest = Path.Combine(outpath, GetNewMP4Filepath(file, camera, postfix));

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
                    // TODO Write a filecopy function with progress
                    File.Copy(item.OriginalPath, item.NewPath,true);
                }

                Console.WriteLine("\bDone!");
                Console.CursorVisible = true;

            }
        }
        */

        private void listFileTags(string filepath)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filepath);

            foreach(var dir in dirs)
            foreach (var tag in dir.Tags)
                {
                    Console.WriteLine($"\t{dir.Name} - {tag.Name}: {tag.Description}");
                }
        }

        private string GetNewJPEGFilepath(string filepath, string camstr, string postfix)
        {
            var dirs = ImageMetadataReader.ReadMetadata(filepath);


            var exifIFD0 = dirs.OfType<MetadataExtractor.Formats.Exif.ExifIfd0Directory>().FirstOrDefault();


            var dateTime = exifIFD0.GetDateTime(MetadataExtractor.Formats.Exif.ExifIfd0Directory.TagDateTime);

            var timestamp = dateTime.ToString("yyMMddHHmmss");

            var year = dateTime.ToString("yyyy");

            var date = dateTime.ToString("yyyy-MM-dd");

            var path = @$"{year}\{date}_{postfix}\";

            return $"{path}{timestamp}_{camstr}.jpg";
        }

        private string GetNewMP4Filepath(string filepath, string camstr, string postfix)
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

            var path = @$"{year}\{date}_{postfix}\";

            return $"{path}{timestamp}_{camstr}_{res}.mp4";
        }

        private string GetResolutionString(int width, int height)
        {
            // TODO Make this work with square and vertical sizes.
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
