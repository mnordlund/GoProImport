using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoProImport
{
    class Config
    {
        public static Config instance = null;
        private const string ConfigName = "GoProImport.cfg";

        private Config()
        {
            if (!Path.Exists(ConfigName))
            {
                // No config file, create one
            }
        }

        public static Config Instance
        {
            get
            {
                instance ??= new Config();
                return instance;
            }
        }
    }
}
