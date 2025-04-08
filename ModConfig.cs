using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DeliverySaver
{
    internal static class ModConfig
    {
        public static string ModRootFile {
            get => Path.Combine(Application.dataPath, "..", "Mods", "DeliverySaver");
        }

        public static string GetFilePath(string fileName)
        {
            return Path.Combine(ModRootFile, fileName);
        }
    }
}
