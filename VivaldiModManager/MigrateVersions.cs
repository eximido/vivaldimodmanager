using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VivaldiModManager
{
    public class MigrateVersions
    {
        public string version { get; set; }
        public string modsDir { get; set; }
        public string modsPersistentDir { get; set; }
        public bool Selected { get; set; }
    }
}
