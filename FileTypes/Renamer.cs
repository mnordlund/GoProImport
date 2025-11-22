using GoProImport.Devices;
using MetadataExtractor.Formats.Mpeg;
using System.IO;


namespace GoProImport.FileTypes
{
    internal class Renamer
    {
        private IFiletype[] Filetypes = [new MP4File(), new WAVFile(), new JPEGFile()];

        public string GetNewFilename(string filename, IDevice device)
        {

            var extension = Path.GetExtension(filename).ToLower();

            foreach(var filetype in Filetypes)
            {
                if (extension.Equals(filetype.Extension))
                {
                    return filetype.GetNewFilepath(filename, device);
                }
            }

            return null;
        }
    }
}
