using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// A module that can be placed on a hatch prop.  Swaps that prop with an 'opened' version when opened
    /// </summary>
    public class PropHatch : Hatch
    {
        [KSPField]
        public string openPropName = string.Empty;

        [KSPField]
        public Vector3 position = Vector3.zero;

        [KSPField]
        public Vector3 scale = Vector3.one;

        [KSPField]
        public Vector3 rotation = Vector3.zero; // as euler angles

        public InternalProp ClosedProp => internalProp;
        public InternalProp OpenProp;

        public new void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            base.Start();

            CreateOpenProp();
        }

        private void CreateOpenProp()
        {
            if (string.IsNullOrEmpty(openPropName)) return;

            OpenProp = PartLoader.GetInternalProp(openPropName);
            if (OpenProp == null)
            {
                Debug.LogError("[FreeIVA] Unable to load open prop hatch \"" + openPropName + "\" in part " + part.name);
            }
            else
            {
                Debug.Log("# Adding PropHatch to part " + part.name);
                OpenProp.propID = FreeIva.CurrentPart.internalModel.props.Count;
                OpenProp.internalModel = part.internalModel;
                
                // position the prop relative to this one, then attach it to the internal model
                OpenProp.transform.SetParent(transform, false);
                OpenProp.transform.localRotation = Quaternion.Euler(rotation);
                OpenProp.transform.localPosition = position;
                OpenProp.transform.localScale = scale;
                OpenProp.transform.SetParent(internalModel.transform, true);
                
                OpenProp.hasModel = true;
                part.internalModel.props.Add(OpenProp);
                OpenProp.gameObject.SetActive(false);
            }
        }

        public override void Open(bool open)
        {
            if (ClosedProp != null)
            {
                ClosedProp.gameObject.SetActive(!open);
            }
            else
            {
                Debug.Log("# ClosedProp was null");
            }
            if (OpenProp != null)
            {
                OpenProp.gameObject.SetActive(open);
            }
            else
            {
                Debug.Log("# OpenProp was null");
            }

            base.Open(open);
        }
    }

    // this module just contains per-placement data for the prop hatch
    public class PropHatchConfig : InternalModule
    {
        [KSPField]
        public string attachNodeId;

        public override void OnLoad(ConfigNode node)
        {
            List<KeyValuePair<Vector3, string>> hideWhenOpen = null;

            if (node.HasNode("HideWhenOpen"))
            {
                hideWhenOpen = new List<KeyValuePair<Vector3, string>>();
                ConfigNode[] hideNodes = node.GetNodes("HideWhenOpen");
                foreach (var hideNode in hideNodes)
                {
                    string propName = hideNode.GetValue("name");
                    if (propName == null)
                    {
                        Debug.LogWarning("[FreeIVA] HideWhenOpen name not found.");
                        continue;
                    }
                    
                    Vector3 propPos = Vector3.zero;
                    if (hideNode.TryGetValue("position", ref propPos))
                    {
                        hideWhenOpen.Add(new KeyValuePair<Vector3, string>(propPos, propName));
                    }
                    else
                    {
                        Debug.LogWarning($"[FreeIVA] Invalid HideWhenOpen position definition in INTERNAL {internalModel.internalName} PROP {internalProp.propName}");
                    }
                }
            }

            base.OnLoad(node);
            var propHatch = GetComponent<Hatch>();
            if (propHatch != null)
            {
                propHatch.attachNodeId = attachNodeId;
                if (hideWhenOpen != null)
                {
                    propHatch.HideWhenOpen = hideWhenOpen;
                }
            }
        }
    }
}
