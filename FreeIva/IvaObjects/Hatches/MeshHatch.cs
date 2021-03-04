using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva.Hatches
{
    /// <summary>
    /// A hatch with a custom mesh.
    /// </summary>
    public class MeshHatch : Hatch
    {
        private MeshRenderer _closedRenderer = null;
        public MeshRenderer ClosedRenderer
        {
            get
            {
                if (_closedRenderer == null && IvaGameObject != null)
                {
                    Debug.Log("# Getting ClosedRenderer...");
                    _closedRenderer = IvaGameObject.GetComponentInChildren<MeshRenderer>();
                }
                return _closedRenderer;
            }
        }

        public override bool IsOpen
        {
            get
            {
                if (ClosedRenderer != null)
                    return !ClosedRenderer.enabled;
                else
                    return false;
            }
        }
        public string ClosedMeshName { get; set; }

        public override void Instantiate(Part p)
        {
            Part = p;
            Debug.Log("# Instantiating mesh hatch for part " + p);
            if (IvaGameObject != null)
            {
                Debug.LogError("[FreeIVA] Hatch has already been instantiated.");
                return;
            }
            
            IvaGameObject = new GameObject();
            if (p.internalModel == null)
                p.CreateInternalModel(); // TODO: Detect this in an event instead.
            IvaGameObject.transform.parent = p.internalModel.transform;
            IvaGameObject.layer = (int)Layers.InternalSpace;
            
            IvaGameObject.name = Name;
            SetupAudio();
        }


        public override void Open(bool open)
        {
            if (ClosedRenderer != null)
                ClosedRenderer.enabled = !open;

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

        public static MeshHatch LoadMeshHatchFromCfg(ConfigNode node)
        {
            Vector3 position = Vector3.zero;
            Vector3 scale = Vector3.one;
            MeshHatch meshHatch = new MeshHatch();

            if (node.HasValue("meshName"))
                meshHatch.ClosedMeshName = node.GetValue("meshName");

            if (node.HasValue("attachNodeId"))
                meshHatch.AttachNodeId = node.GetValue("attachNodeId");

            if (node.HasValue("position"))
            {
                string posString = node.GetValue("position");
                string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid mesh hatch position definition \"" + posString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    meshHatch.LocalPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] MeshHatch does not have a position");
            }
            Debug.Log("# MeshHatch position: " + meshHatch.LocalPosition);

            if (node.HasValue("scale"))
            {
                string scaleString = node.GetValue("scale");
                string[] s = scaleString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid mesh hatch scale definition \"" + scaleString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    meshHatch.Scale = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }

            if (node.HasValue("rotation"))
            {
                string rotationString = node.GetValue("rotation");
                string[] s = rotationString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid mesh hatch rotation definition \"" + rotationString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    meshHatch.Rotation = Quaternion.Euler(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }

            if (node.HasValue("HatchOpenSoundFile"))
            {
                meshHatch.HatchOpenSoundFile = node.GetValue("HatchOpenSoundFile");
            }
            if (node.HasValue("HatchCloseSoundFile"))
            {
                meshHatch.HatchCloseSoundFile = node.GetValue("HatchCloseSoundFile");
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
                            meshHatch.HideWhenOpen.Add(new KeyValuePair<Vector3, string>(propPos, propName));
                        }
                    }

                }
            }

            if (node.HasNode("InternalCollider"))
            {
                ConfigNode hatchColliderNode = node.GetNode("InternalCollider");
                if (hatchColliderNode != null)
                    meshHatch.Collider = InternalCollider.LoadFromCfg(hatchColliderNode);
            }
            return meshHatch;
        }
    }
}
