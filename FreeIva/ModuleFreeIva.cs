using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// This class is currently empty, but in the future it may contain data to persist hatch state etc across saves.
    /// </summary>
    public class ModuleFreeIva : PartModule
    {
        [KSPField]
        public string passThroughNodeA;
        [KSPField]
        public string passThroughNodeB;
    }
}
