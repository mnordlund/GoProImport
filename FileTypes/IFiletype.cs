using GoProImport.Devices;

namespace GoProImport.FileTypes
{
    internal interface IFiletype
    {
        public string Extension { get; }
        public string GetNewFilepath(string filename, IDevice device);
    }
}
