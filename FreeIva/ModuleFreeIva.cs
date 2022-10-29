using FreeIva.Hatches;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// A PartModule used to define what additional FreeIVA objects and behaviours a part should have, such as hatches and colldiers.
    /// </summary>
    public class ModuleFreeIva : PartModule
    {
        [KSPField]
        public bool CopyPartCollidersToInternalColliders = false;

        public List<IHatch> Hatches = new List<IHatch>();
        public List<InternalCollider> InternalColliders = new List<InternalCollider>();
        public List<Collider> PartInternalColliders = new List<Collider>();
        public List<GameObject> PartInternalColliderObjects = new List<GameObject>();
        public List<CutParameter> Cuts = new List<CutParameter>();

        // OnAwake should always occur before Start.
        public override void OnAwake()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;

            Hatches = new List<IHatch>(PersistenceManager.instance.GetHatchesForPartInstance(part.partInfo.name));
            InternalColliders = new List<InternalCollider>(PersistenceManager.instance.GetCollidersForPartInstance(part.partInfo.name));
            Cuts = new List<CutParameter>(PersistenceManager.instance.GetCutParametersFroPartInstance(part.partInfo.name));
        }

        public void Start()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT || !vessel.isActiveVessel) return; // TODO: Instantiate on vessel switch.
            if (Hatches == null)
                Debug.LogError("[FreeIVA] Startup error: Hatches null");

            foreach (InternalCollider c in InternalColliders)
                c.Instantiate(part);

            // Instantiate internal colliders first as hatches will be instantiating their own colliders.
            foreach (Hatch h in Hatches)
                h.Instantiate(part);

            MeshCutter.Cut(part, Cuts);

            if (CopyPartCollidersToInternalColliders)
                PartCollidersToInternal();
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasNode("Hatch"))
            {
                ConfigNode[] hatchNodes = node.GetNodes("Hatch");
                foreach (var hn in hatchNodes)
                {
                    IHatch h = Hatch.LoadFromCfg(hn);
                    if (h != null)
                    {
                        Hatches.Add(h);
                    }
                }
                PersistenceManager.instance.AddHatches(part.name, Hatches);
            }
            Debug.Log("# Hatches loaded from config for part " + part.name + ": " + Hatches.Count);

            if (node.HasNode("PropHatch"))
            {
                ConfigNode[] propHatchNodes = node.GetNodes("PropHatch");
                foreach (var phn in propHatchNodes)
                {
                    PropHatch ph = PropHatch.LoadPropHatchFromCfg(phn);
                    if (ph != null)
                    {
                        Hatches.Add(ph);
                    }
                }
                PersistenceManager.instance.AddHatches(part.name, Hatches);
            }

            if (node.HasNode("MeshHatch"))
            {
                ConfigNode[] meshHatchNodes = node.GetNodes("MeshHatch");
                foreach (var mhn in meshHatchNodes)
                {
                    MeshHatch mh = MeshHatch.LoadMeshHatchFromCfg(mhn);
                    if (mh != null)
                    {
                        Hatches.Add(mh);
                    }
                }
                PersistenceManager.instance.AddHatches(part.name, Hatches);
            }
            //if (node.HasNode("PropHatchAnimated"))
            //{
            //    ConfigNode[] propHatchAnimatedNodes = node.GetNodes("PropHatchAnimated");
            //    foreach (var phan in propHatchAnimatedNodes)
            //    {
            //        PropHatchAnimated pha = PropHatchAnimated.LoadPropHatchFromCfg(phan);
            //        if (pha != null)
            //        {
            //            Hatches.Add(pha);
            //            if (pha.Collider != null)
            //                InternalColliders.Add(pha.Collider);
            //        }
            //    }
            //    PersistenceManager.instance.AddHatches(part.name, Hatches);
            //}
            Debug.Log("# Hatches loaded from config for part " + part.name + ": " + Hatches.Count);

            if (node.HasNode("InternalCollider"))
            {
                ConfigNode[] colliderNodes = node.GetNodes("InternalCollider");
                foreach (var cn in colliderNodes)
                {
                    InternalCollider ic = InternalCollider.LoadFromCfg(cn);
                    if (ic != null)
                        InternalColliders.Add(ic);
                }
                PersistenceManager.instance.AddInternalColliders(part.name, InternalColliders);
                Debug.Log("# Internal colliders loaded from config for part " + part.name + ": " + InternalColliders.Count);
            }
            else if (InternalColliders != null && InternalColliders.Count > 0)
            {
                PersistenceManager.instance.AddInternalColliders(part.name, InternalColliders);
                Debug.Log("# Internal colliders loaded from config for part " + part.name + ": " + InternalColliders.Count);
            }

            if (node.HasNode("Cut"))
            {
                ConfigNode[] cutNodes = node.GetNodes("Cut");
                foreach (ConfigNode n in cutNodes)
                {

                    CutParameter cp = CutParameter.LoadFromCfg(n);
                    if (cp != null)
                        Cuts.Add(cp);
                }
                PersistenceManager.instance.AddCutParameters(part.name, Cuts);
                Debug.Log("[FreeIVA] Cut parameters loaded from config for part " + part.name + ": " + Cuts.Count);
            }
        }

        private void PartCollidersToInternal()
        {
            var partBoxColliders = GetComponentsInChildren<BoxCollider>();
            foreach (var c in partBoxColliders)
            {
                if (c.isTrigger || c.tag != "Untagged")
                {
                    continue;
                }

                var go = Instantiate(c.gameObject);
                go.transform.parent = c.gameObject.transform.parent;
                go.layer = (int)Layers.InternalSpace;
                go.transform.position = InternalSpace.WorldToInternal(c.transform.position);
                go.transform.rotation = InternalSpace.WorldToInternal(c.transform.rotation);
                PartInternalColliderObjects.Add(go);
            }
            /*
             Structure:
            Part
                model
                    BoxCOL x 12
                        BoxCollider
             */

            /*public void OnDestroy()
            {
                /*Debug.Log("#Destroying");
                foreach (Hatch h in Hatches)
                    h.Destroy();
                Hatches.Clear();* /
            }*/
        }
    }
}
