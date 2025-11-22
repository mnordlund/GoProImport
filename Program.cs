using GoProImport.Devices;
using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;

namespace GoProImport
{
    class Program
    {
        static string Version = "v0.1.0";

        static string Usage = @$"GoPro Import version: {Version}
Usage:
    -help (-h) Show this help
    -version -v Show version
    -info -i <file> Show file information on file <file>
    -out -o <dir> Use <dir> as output directory
";
        static void Main(string[] args)
        {
            // Default configuration
            // TODO Read from a config file
            var DstPath = @"D:\GoPro";

            // Parse arguments

            if (args.Length > 0)
            {
                for(var i  = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-h":
                        case "-help":
                            Console.WriteLine(Usage);
                            return;
                            break;
                        case "-v":
                        case "-version":
                            Console.WriteLine($"GoPro Import Version {Version}");
                            Console.WriteLine("By: Martin Nordlund (martin@mnordlund.se)");
                            return;
                            break;
                        case "-i":
                        case "-info":
                            listFileTags(args[++i]);
                            return;
                            break;
                        case "-o":
                        case "-out":
                            DstPath = args[++i];
                            break;
                        case "-d":
                        case "-device":
                            // TODO use args[++i] as device, ignore device type
                            throw new NotImplementedException();
                            break;
                    }
                }
            }

            FileItem.DstPath = DstPath;

            var deviceList = DeviceFinder.ListDevices();
            if(deviceList.Length == 0 ) 
            {
                Console.WriteLine("No drives found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Drives found: ");
            foreach (var device in deviceList)
            {
                Console.WriteLine($"{device.DeviceType} => {device.DriveInfo.Name}");
            }

            Console.WriteLine("Do you want to name the import? (Enter to skip):");
            var importName = Console.ReadLine().Trim().Replace(' ', '_');

            List<FileItem> fileList = new List<FileItem>();
            foreach (var device in deviceList)
            {
                device.ImportName = importName;
                fileList.AddRange(device.ListFiles());
            }

            var overwrite = "n";

            for(int i = fileList.Count - 1; i >= 0; i--)
            {
                if (fileList[i].FileExists)
                {
                    if (overwrite.Equals("never"))
                    {
                        fileList.RemoveAt(i);
                        continue;
                    }

                    if (!overwrite.Equals("a"))
                    {
                        Console.WriteLine($"File '{Path.Combine(DstPath, fileList[i].NewPath)}' already exists, overwrite (y/n/a/never)");
                        overwrite = Console.ReadLine();
                        if (!overwrite.Equals("y") && !overwrite.Equals("a"))
                        {
                            fileList.RemoveAt(i);
                        }
                    }
                }
            }

            if (fileList.Count == 0)
            {
                Console.WriteLine("No files found");
                return;
            }

            foreach(var file in fileList)
            {
                Console.WriteLine($"{file.OriginalPath} => {Path.Combine(DstPath, file.NewPath)} {file.SizeString}");
            }

            var totalSize = fileList.Sum((fi) => fi.Size);

            // TODO Create function to pretty print sizes
            Console.WriteLine($"Files found: {fileList.Count} Total size {(totalSize / Math.Pow(1024, 3)).ToString("0.00")} GB");

            Console.WriteLine("Copy files? (y/n): ");

            var reply = Console.ReadLine();
            if (reply.Trim().ToLower() == "y")
            {
                var progress = new string('-', 50);
                Console.CursorVisible = false;

                long bytesCopied = 0;
                int lastPercent = 0;
                int fileCount = 1;
                foreach (var item in fileList)
                {   
                    Console.WriteLine($"Copying file {fileCount} of {fileList.Count}: {Path.GetFileName(item.NewPath)}");
                    Console.WriteLine($"[{progress}]");
                    
                    item.CopyFile();
                    bytesCopied += item.Size;
                    var percent = (int)(bytesCopied * 100 / totalSize);

                    var prgChars = progress.ToCharArray();

                    for(int i = lastPercent; i < percent; i++)
                    {
                        prgChars[i/2] = '#';
                    }

                    progress = new string(prgChars);
                    lastPercent = (int)percent;
                    Console.CursorLeft = 0;
                    Console.CursorTop = Console.CursorTop - 2;
                    Console.Write(new String(' ', Console.WindowWidth));
                    Console.CursorLeft = 0;
                    fileCount++;
                }

                Console.WriteLine($"Copying done!");
                Console.WriteLine($"[{progress}]");
                Console.CursorVisible = true;


            }

            Console.WriteLine("Press any key to quit.");
            Console.ReadKey();
        }

        private static void listFileTags(string filepath)
        {
            try
            {
                var metaData = ImageMetadataReader.ReadMetadata(filepath);

                foreach (var dir in metaData)
                {
                    Console.WriteLine($"{dir.Name}");
                    Console.WriteLine("{");
                    foreach (var tag in dir.Tags)
                    {
                        Console.WriteLine($"\t{tag.Name}: {tag.Description}");
                    }
                    Console.WriteLine("}\n");
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Unable to parse file {filepath}");
            }

        }
    }
}
