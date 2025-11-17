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
        static void Main(string[] args)
        {
            // TODO Parse arguments to get drive/path and out path etc.
            /*
             * List of suggested arguments
             * -d <directory> import from given directory
             * -info <file> list all meta information on the given file
             */
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
            var postfix = Console.ReadLine().Trim().Replace(' ', '_');

            List<FileItem> fileList = new List<FileItem>();
            foreach (var device in deviceList)
            {
                device.Postfix = postfix;
                fileList.AddRange(device.ListFiles());
            }

            foreach (var file in fileList)
            {
                Console.WriteLine(file.OriginalPath +" => " + file.NewPath + " " + file.SizeString);
                if (file.FileExists)
                {
                    Console.WriteLine($"File '{file.NewPath}' already exists, will be overwritten.");
                }
            }

            var totalSize = fileList.Sum((fi) => fi.Size);

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
    }
}
