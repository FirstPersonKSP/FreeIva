using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// A hatch which is based on one or more IVA prop objects. Assumes the prop for the hatch in the closed state is already present.
    /// </summary>
    public class PropHatch : Hatch
    {
        public override Vector3 WorldPosition
        {
            get
            {
                if (ClosedProp != null)
                    return ClosedProp.transform.position;
                return Vector3.zero;
            }
        }

        public InternalProp ClosedProp;
        public InternalProp OpenProp;

        public override bool IsOpen
        {
            get
            {
                if (ClosedProp != null)
                    return !ClosedProp.isActiveAndEnabled;
                else
                    return false;
            }
        }

        public string ClosedPropName { get; set; }
        public string OpenPropName { get; set; }
        public int ClosedPropIndex { get; set; }

        public PropHatch() { }

        public PropHatch(string name, string attachNodeId, Vector3 localPosition, Vector3 scale, Quaternion rotation,
            List<KeyValuePair<Vector3, string>> hideWhenOpen, InternalCollider collider,
            string closedPropName, string openPropName, int closedPropIndex)
        {
            Name = name;
            AttachNodeId = attachNodeId;
            LocalPosition = localPosition;
            Scale = scale;
            Rotation = rotation;
            HideWhenOpen = hideWhenOpen;
            Collider = collider;
            ClosedPropName = closedPropName;
            OpenPropName = openPropName;
            ClosedPropIndex = closedPropIndex;
        }

        public override void Instantiate(Part p)
        {
            Part = p;
            Debug.Log("# Instantiating prop hatch for part " + p);
            if (IvaGameObject != null)
            {
                Debug.LogError("[FreeIVA] Hatch has already been instantiated.");
                return;
            }

            GetProp();
            IvaGameObject = ClosedProp.gameObject;
            IvaGameObject.layer = (int)Layers.InternalSpace;
            PropHatches.Add(this);
            if (Collider != null)
            {
                Collider.Instantiate(p);
                ModuleFreeIva mfi = p.GetModule<ModuleFreeIva>();
                if (mfi != null)
                {
                    mfi.InternalColliders.Add(Collider);
                }
            }

            SetupAudio();
        }

        public override IHatch Clone()
        {
            return new PropHatch(Name, AttachNodeId, LocalPosition, Scale, Rotation, new List<KeyValuePair<Vector3, string>>(HideWhenOpen), Collider?.Clone(),
                ClosedPropName, OpenPropName, ClosedPropIndex);
        }

        // Find the prop in the IVA. If not present, spawn it.
        private void GetProp()
        {
            // Find the existing prop.
            InternalProp closedHatch = this.Part.internalModel.props[ClosedPropIndex];
            if (closedHatch == null)
            {
                // Spawn a new prop.
                closedHatch = PartLoader.GetInternalProp(this.ClosedPropName);
                if (closedHatch == null)
                {
                    Debug.LogError("[FreeIVA] Unable to load closed prop hatch \"" + this.ClosedPropName + "\" in part " + this.Part.name);
                }
                else
                {
                    closedHatch.propID = FreeIva.CurrentPart.internalModel.props.Count;
                    closedHatch.internalModel = this.Part.internalModel;
                    closedHatch.transform.parent = this.Part.internalModel.transform;
                    closedHatch.transform.gameObject.layer = (int)Layers.InternalSpace;
                    closedHatch.hasModel = true;
                    this.Part.internalModel.props.Add(closedHatch);
                    closedHatch.transform.localRotation = this.Rotation;
                    closedHatch.transform.localPosition = this.LocalPosition;
                }
            }
            else if (closedHatch.name != ClosedPropName)
            {
                Debug.LogError($"[FreeIVA] Prop at closedPropIndex {ClosedPropIndex} didn't match expected closedPropName {ClosedPropName}.");
            }

            ClosedProp = closedHatch;

            InternalProp openHatch = PartLoader.GetInternalProp(this.OpenPropName);
            if (openHatch == null)
            {
                Debug.LogError("[FreeIVA] Unable to load open prop hatch \"" + this.OpenPropName + "\" in part " + this.Part.name);
            }
            else
            {
                Debug.Log("# Adding PropHatch to part " + this.Part.name);
                openHatch.propID = FreeIva.CurrentPart.internalModel.props.Count;
                openHatch.internalModel = this.Part.internalModel;
                openHatch.transform.parent = this.Part.internalModel.transform;
                openHatch.hasModel = true;
                this.Part.internalModel.props.Add(openHatch);
                openHatch.transform.rotation = this.Rotation;
                openHatch.transform.localPosition = this.LocalPosition;
                OpenProp = openHatch;
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
            HideOnOpen(open);
            FreeIva.SetRenderQueues(FreeIva.CurrentPart);

            if (Collider != null)
                Collider.Enable(!open);

            if (open)
            {
                if (HatchOpenSound != null && HatchOpenSound.audio != null)
                    HatchOpenSound.audio.Play();
            }
            else
            {
                if (HatchCloseSound != null && HatchCloseSound.audio != null)
                    HatchCloseSound.audio.Play();
            }
        }

        public static List<PropHatch> PropHatches = new List<PropHatch>();

        private static bool HatchInitialised(InternalProp prop)
        {
            foreach (PropHatch p in PropHatches)
            {
                if (p.ClosedProp != null && p.ClosedProp.Equals(prop))
                    return true;
            }
            return false;
        }

        public static PropHatch LoadPropHatchFromCfg(ConfigNode node)
        {
            Vector3 position = Vector3.zero;
            Vector3 scale = Vector3.one;
            if (!node.HasValue("closedPropIndex"))
            {
                Debug.LogWarning("[FreeIVA] Prop hatch closedPropIndex not found: Skipping hatch.");
                return null;
            }
            PropHatch propHatch = new PropHatch();
            propHatch.ClosedPropIndex = int.Parse(node.GetValue("closedPropIndex"));

            if (node.HasValue("closedPropName"))
                propHatch.ClosedPropName = node.GetValue("closedPropName");

            if (node.HasValue("openPropName"))
                propHatch.OpenPropName = node.GetValue("openPropName");

            if (node.HasValue("attachNodeId"))
                propHatch.AttachNodeId = node.GetValue("attachNodeId");

            if (node.HasValue("position"))
            {
                string posString = node.GetValue("position");
                string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid prop hatch position definition \"" + posString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    propHatch.LocalPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] PropHatch does not have a position");
            }
            Debug.Log("# PropHatch position: " + propHatch.LocalPosition);

            if (node.HasValue("scale"))
            {
                string scaleString = node.GetValue("scale");
                string[] s = scaleString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid prop hatch scale definition \"" + scaleString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    propHatch.Scale = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }

            if (node.HasValue("rotation"))
            {
                string rotationString = node.GetValue("rotation");
                string[] s = rotationString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid prop hatch rotation definition \"" + rotationString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    propHatch.Rotation = Quaternion.Euler(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }

            if (node.HasValue("HatchOpenSoundFile"))
            {
                propHatch.HatchOpenSoundFile = node.GetValue("HatchOpenSoundFile");
            }
            if (node.HasValue("HatchCloseSoundFile"))
            {
                propHatch.HatchCloseSoundFile = node.GetValue("HatchCloseSoundFile");
            }

            if (node.HasNode("HideWhenOpen"))
            {
                ConfigNode[] hideNodes = node.GetNodes("HideWhenOpen");
                foreach (var hideNode in hideNodes)
                {
                    if (!hideNode.HasValue("name"))
                    {
                        Debug.LogWarning("[FreeIVA] HideWhenOpen name not found.");
                        continue;
                    }
                    string propName = hideNode.GetValue("name");

                    if (hideNode.HasValue("position"))
                    {
                        string posString = hideNode.GetValue("position");
                        string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length != 3)
                        {
                            Debug.LogWarning("[FreeIVA] Invalid HideWhenOpen position definition \"" + posString + "\": Must be in the form x, y, z.");
                            continue;
                        }
                        else
                        {
                            Vector3 propPos = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
                            propHatch.HideWhenOpen.Add(new KeyValuePair<Vector3, string>(propPos, propName));
                        }
                    }

                }
            }

            if (node.HasNode("InternalCollider"))
            {
                ConfigNode hatchColliderNode = node.GetNode("InternalCollider");
                if (hatchColliderNode != null)
                    propHatch.Collider = InternalCollider.LoadFromCfg(hatchColliderNode);
            }
            return propHatch;
        }
    }
}
