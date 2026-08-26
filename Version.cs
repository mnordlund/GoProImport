namespace GoProImport
{
    internal class Version
    {
        private const int major = 0;
        private const int minor = 1;
        private const int patch = 1;
        private const int build = 0;

        public static string VersionString => $"{major}.{minor}.{patch} b{build}";
        public static void WriteVersion()
        {
            System.Console.WriteLine($"GoPro import v{VersionString}");
        }
    }
}
