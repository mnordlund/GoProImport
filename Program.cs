using System;
using System.IO;

namespace GoProImport
{
    class Program
    {
        static void Main(string[] args)
        {
            var gp = new GoPro();
            gp.ImportFiles(@"i:");
        }
    }
}
