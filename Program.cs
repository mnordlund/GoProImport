using GoProImport.Devices;
using MetadataExtractor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoProImport
{
    internal class Program
    {
        static string Usage = @$"
Usage:
    -help (-h) Show this help
    -version -v Show version
    -info -i <file> Show file information on file <file>
    -out -o <dir> Use <dir> as output directory
    -device -d <dir> Use <dir> as device to import
";
        internal static void Main(string[] args)
        {
            // Default configuration
            // TODO Read from a config file
            var DstPath = @"D:\GoPro";

            DeviceBase[] devices = null;
            // Parse arguments

            if (args.Length > 0)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-h":
                        case "-help":
                        case "--help":
                            Version.WriteVersion();
                            Console.WriteLine(Usage);
                            return;
                        case "-v":
                        case "-version":
                        case "--version":
                            Console.WriteLine($"GoPro Import Version {Version.VersionString}");
                            Console.WriteLine("By: Martin Nordlund (martin@mnordlund.se)");
                            return;
                        case "-i":
                        case "-info":
                            if (i + 1 < args.Length)
                            {
                                listFileTags(args[++i]);
                            }
                            else
                            {
                                Console.WriteLine("Error: Missing file argument for -info / -i.");
                                Console.WriteLine(Usage);
                            }
                            return;
                        case "-o":
                        case "-out":
                            if (i + 1 < args.Length)
                            {
                                DstPath = args[++i];
                                Console.WriteLine($"Destination set to: {DstPath}");
                            }
                            else
                            {
                                Console.WriteLine("Error: Missing directory argument for -out / -o.");
                                Console.WriteLine(Usage);
                                return;
                            }
                            break;
                        case "-d":
                        case "-device":
                            if (i + 1 < args.Length)
                            {
                                var deviceFolder = args[++i];
                                Console.WriteLine($"Using folder {deviceFolder} as native device");
                                devices = [new NativeDevice(deviceFolder)];
                            }
                            else
                            {
                                Console.WriteLine("Error: Missing directory argument for -device / -d.");
                                Console.WriteLine(Usage);
                                return;
                            }
                            break;
                        default:
                            Console.WriteLine($"Unknown option: {args[i]}");
                            Console.WriteLine(Usage);
                            return;
                    }
                }
            }

            Version.WriteVersion();

            FileItem.DstPath = DstPath;

            if (devices == null)
            {
                devices = DeviceFinder.ListDevices();
            }

            if(devices.Length == 0 ) 
            {
                Console.WriteLine("No drives found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Drives found: ");
            foreach (var device in devices)
            {
                Console.WriteLine($"{device.DeviceType} => {device.DriveInfo.Name}");
            }

            Console.WriteLine("Do you want to name the import? (Enter to skip):");
            var importName = Console.ReadLine()?.Trim().Replace(' ', '_') ?? string.Empty;

            List<FileItem> fileList = new List<FileItem>();
            foreach (var device in devices)
            {
                device.ImportName = importName;
                var files = device.ListFiles();
                if (files != null)
                {
                    fileList.AddRange(files);
                }
            }

            var overwrite = "n";

            for(int i = fileList.Count - 1; i >= 0; i--)
            {
                if (fileList[i].FileExists)
                {
                    if (overwrite.Equals("never", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Skipping existing file: {fileList[i].OriginalPath} => {fileList[i].NewPath}");
                        fileList.RemoveAt(i);
                        continue;
                    }

                    if (!overwrite.Equals("a", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"File '{Path.Combine(DstPath, fileList[i].NewPath)}' already exists, overwrite (y/n/a/never)");
                        overwrite = Console.ReadLine()?.Trim().ToLower() ?? "n";
                        if (!overwrite.Equals("y", StringComparison.OrdinalIgnoreCase) && !overwrite.Equals("a", StringComparison.OrdinalIgnoreCase))
                        {
                            fileList.RemoveAt(i);
                        }
                    }
                }
            }

            if (0 == fileList.Count)
            {
                Console.WriteLine("No files found");
                return;
            }

            foreach(var file in fileList)
            {
                Console.WriteLine($"{file.OriginalPath} => {Path.Combine(DstPath, file.NewPath)} {file.SizeString}");
            }

            var totalSize = fileList.Sum((fi) => fi.Size);

            Console.WriteLine($"Files found: {fileList.Count} Total size {CopyProgressTracker.FormatSize(totalSize)}");

            Console.WriteLine("Copy files? (y/n): ");

            var reply = Console.ReadLine();
            if (reply != null && reply.Trim().ToLower() == "y")
            {
                CopyProgressTracker.SafeSetCursorVisibility(false);

                var tracker = new CopyProgressTracker(totalSize, fileList.Count);
                var concurrentDeleteList = new ConcurrentBag<FileItem>();

                // Group files by distinct source device for concurrent multi-device transfer
                var deviceGroups = fileList
                    .GroupBy(f => f.Device != null ? f.Device.DriveInfo.Name : (Path.GetPathRoot(f.OriginalPath) ?? "Default"))
                    .ToList();

                var copyTasks = deviceGroups.Select(group => Task.Run(() =>
                {
                    var firstItem = group.First();
                    var device = firstItem.Device;
                    string deviceName = device?.DeviceType ?? Path.GetPathRoot(firstItem.OriginalPath) ?? "Device";
                    long deviceTotalBytes = group.Sum(f => f.Size);
                    int deviceFileIndex = 1;
                    int deviceTotalFiles = group.Count();

                    tracker.RegisterDevice(group.Key, deviceName, deviceTotalFiles, deviceTotalBytes);

                    foreach (var item in group)
                    {
                        var fileName = Path.GetFileName(item.NewPath);
                        tracker.OnFileStart(group.Key, fileName, item.Size, deviceFileIndex);

                        var success = item.CopyFile(bytesRead =>
                        {
                            tracker.OnBytesCopied(group.Key, bytesRead);
                        });

                        if (success)
                        {
                            if (item.Device != null && item.Device.DeleteFiles)
                            {
                                concurrentDeleteList.Add(item);
                            }
                            tracker.OnFileCompleted(group.Key, fileName, true);
                        }
                        else
                        {
                            Console.WriteLine($"WARNING: Failed to copy or verify file: {item.OriginalPath}");
                            tracker.OnFileCompleted(group.Key, fileName, false);
                        }

                        deviceFileIndex++;
                    }

                    tracker.OnDeviceCompleted(group.Key);
                })).ToArray();

                Task.WhenAll(copyTasks).GetAwaiter().GetResult();
                tracker.Finish();

                CopyProgressTracker.SafeSetCursorVisibility(true);

                var deleteList = concurrentDeleteList.ToList();

                if (deleteList.Count > 0)
                {
                    Console.WriteLine("Files marked for deletion:");

                    foreach(var file in deleteList)
                    {
                        Console.WriteLine($"{file.OriginalPath}");
                    }
                    Console.WriteLine($"Delete {deleteList.Count} files? (y/n): ");

                    reply = Console.ReadLine();
                    if (reply != null && reply.Trim().ToLower() == "y")
                    {
                        Console.Write("Deleting files...");

                        int deletedCount = 0;
                        foreach(var file in deleteList)
                        {
                            if (file.VerifyIntegrity())
                            {
                                file.DeleteOriginal();
                                deletedCount++;
                            }
                            else
                            {
                                Console.WriteLine($"\nSkipping deletion of '{file.OriginalPath}': destination missing or size mismatch!");
                            }
                        }

                        Console.WriteLine($" Done! Deleted {deletedCount} of {deleteList.Count} files.");
                    }
                }
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
