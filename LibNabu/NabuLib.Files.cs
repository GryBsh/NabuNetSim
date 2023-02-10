using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu
{
    public partial class NabuLib
    {
        public static string SafeFileName(string name)
        {
            foreach (var bad in Path.GetInvalidFileNameChars())
                name = name.Replace(bad, '_');
            return name;
        }
    }
}
