using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// A PartModule used to define what additional FreeIVA objects and behaviours a part should have, such as hatches and colldiers.
    /// </summary>
    public class ModuleFreeIva : PartModule
    {

        public List<Hatch> Hatches = new List<Hatch>(); // hatches will register themselves with us

        // OnAwake should always occur before Start.
        public override void OnAwake()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;
        }

        public void Start()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT || !vessel.isActiveVessel) return; // TODO: Instantiate on vessel switch.
            if (Hatches == null)
                Debug.LogError("[FreeIVA] Startup error: Hatches null");
        }
    }
}
