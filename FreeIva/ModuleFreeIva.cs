using FreeIva.Hatches;
using System.Collections.Generic;
using UnityEngine;

// Assistance received from: blowfish, stupid_chris
namespace FreeIva
{
    /// <summary>
    /// A PartModule used to define what additional FreeIVA objects and behaviours a part should have, such as hatches and colldiers.
    /// </summary>
    public class ModuleFreeIva : PartModule
    {
        public bool CanIva = true;
        public List<IHatch> Hatches = new List<IHatch>();
        public List<InternalCollider> InternalColliders = new List<InternalCollider>();

        public void Start()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT || !vessel.isActiveVessel) return; // TODO: Instantiate on vessel switch.
            if (Hatches == null)
                Debug.LogError("[FreeIVA] Startup error: Hatches null");
            foreach (Hatch h in Hatches)
                h.Instantiate(part);

            foreach (InternalCollider c in InternalColliders)
                c.Instantiate(part);
        }

        public override void OnAwake()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;
            //Debug.Log("# PartInfo name " + part.partInfo.name);

            var hatchTemplates = PersistenceManager.instance.GetPartHatches(part.partInfo.name);
            Hatches = new List<IHatch>();
            foreach (var hatch in hatchTemplates)
            {
                Hatches.Add(hatch);//.Clone());
            }
            InternalColliders = new List<InternalCollider>(PersistenceManager.instance.GetPartColliders(part.partInfo.name));
        }

        /*public override void OnSave(ConfigNode node)
        {
            Debug.Log("# OnSave " + node);
            return;
            base.OnSave(node);
            Debug.Log("# Saving hatches");
            ConfigNode hatchNode = new ConfigNode("Hatch");
            foreach (Hatch h in Hatches)
            {
                hatchNode.AddValue("attachNodeId", h.AttachNodeId);
                hatchNode.AddValue("position", h.Position.x + ", " +  h.Position.y + ", " + h.Position.z);
                hatchNode.AddValue("scale", h.Scale.x + ", " + h.Scale.y + ", " + h.Scale.z);
            }
            Debug.Log("# Adding hatch node " + hatchNode);
            node.AddNode(hatchNode);
        }*/

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("CanIva"))
                CanIva = bool.Parse(node.GetValue("CanIva"));

            if (node.HasNode("Hatch"))
            {
                ConfigNode[] hatchNodes = node.GetNodes("Hatch");
                foreach (var hn in hatchNodes)
                {
                    IHatch h = Hatch.LoadFromCfg(hn);
                    if (h != null)
                    {
                        Hatches.Add(h);
                        if (h.Collider != null)
                            InternalColliders.Add(h.Collider);
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
                        if (ph.Collider != null)
                            InternalColliders.Add(ph.Collider);
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
                        if (mh.Collider != null)
                            InternalColliders.Add(mh.Collider);
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
        }

        /*public void OnDestroy()
        {
            /*Debug.Log("#Destroying");
            foreach (Hatch h in Hatches)
                h.Destroy();
            Hatches.Clear();* /
        }*/
    }
}
