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
            gp.ImportFiles(@"i:");
        }
    }
}
