using System;
using System.IO;

namespace GoProImport
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO Parse arguments to get drive/path and out path etc.
            var gp = new GoPro();
            var driveArray = gp.FindDrives();
            if(driveArray.Length == 0 ) 
            {
                Console.WriteLine("No drives found.");
                return;
            }
            foreach (var drive in driveArray)
            {
                gp.ImportFiles(drive);
            }
        }
    }
}
