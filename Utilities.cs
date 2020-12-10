using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnhollowerRuntimeLib.XrefScans;

namespace VRC_MenuOverrender
{
    static class Utilities
    {
        public static bool checkXref(this MethodBase m, string match)
        {
            try
            {
                return XrefScanner.XrefScan(m).Any(
                    instance => instance.Type == XrefType.Global && instance.ReadAsObject() != null && instance.ReadAsObject().ToString()
                                   .Equals(match, StringComparison.OrdinalIgnoreCase));
            } catch { } // ignored

            return false;
        }

        public static T Cast<T>(this object o)
        {
            return (T)o;
        }
    }
}
