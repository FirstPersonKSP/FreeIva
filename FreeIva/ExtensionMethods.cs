using System.Collections.Generic;

namespace FreeIva
{
    public static class ExtensionMethods
    {
        //public static ModuleFreeIva GetModuleFreeIva<PartModule>(this Part p)
        //{
        //    if (p == null || p.Modules == null) return null;
        //    List<ModuleFreeIva> mfil = p.Modules.GetModules<ModuleFreeIva>();
        //    if (mfil == null || mfil.Count == 0)
        //        return null;
        //    return mfil[0];
        //}

        public static T GetModule<T>(this Part p) where T : PartModule
        {
            if (p == null || p.Modules == null) return default(T);
            List<T> mfil = p.Modules.GetModules<T>();
            if (mfil == null || mfil.Count == 0)
                return default(T);
            return mfil[0];
        }
    }
}
