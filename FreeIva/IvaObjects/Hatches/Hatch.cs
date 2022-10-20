using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// Base class for hatches (points where kerbals can exit or enter a part during IVA).
    /// </summary>
    public class Hatch : IvaObject, IHatch
    {
        // Where the GameObject is located. Used for basic interaction targeting (i.e. when to show the "Open hatch?" prompt).
        public virtual Vector3 WorldPosition
        {
            get
            {
                if (IvaGameObject != null)
                    return IvaGameObject.transform.position;
                return Vector3.zero;
            }
        }

        private Quaternion _rotation = Quaternion.identity;
        private bool _cylinderWasNull = true;
        public override Quaternion Rotation
        {
            get
            {
                if (IvaGameObject != null)
                {
                    if (_cylinderWasNull)
                    {
                        IvaGameObject.transform.localRotation = _rotation;
                        _cylinderWasNull = false;
                    }
                    return IvaGameObject.transform.localRotation;
                }
                return _rotation;
            }
            set
            {
                if (IvaGameObject != null)
                    IvaGameObject.transform.localRotation = value;
                _rotation = value;
            }
        }

        public InternalCollider Collider { get; set; }

        // The name of the part attach node this hatch is positioned on, as defined in the part.cfg's "node definitions".
        // e.g. node_stack_top
        public string AttachNodeId { get; set; }

        private IHatch _connectedHatch = null;
        // The other hatch that this one is connected or docked to, if present.
        public IHatch ConnectedHatch
        {
            get
            {
                if (_connectedHatch == null)
                    GetConnectedHatch();
                return _connectedHatch;
            }
        }

        public Part HatchPart => Part;

        private AttachNode _hatchNode;
        // The part attach node this hatch is positioned on.
        public AttachNode HatchNode
        {
            get
            {
                if (_hatchNode == null)
                    _hatchNode = GetHatchNode(AttachNodeId);
                return _hatchNode;
            }
        }

        public string HatchOpenSoundFile { get; set; } = "FreeIva/Sounds/HatchOpen";
        public string HatchCloseSoundFile { get; set; } = "FreeIva/Sounds/HatchClose";
        public FXGroup HatchOpenSound = null;
        public FXGroup HatchCloseSound = null;

        public virtual bool IsOpen
        {
            get
            {
                if (IvaGameObject == null ||  IvaGameObjectRenderer == null)
                    return false;
                return IvaGameObjectRenderer.enabled;
            }
        }
        public List<KeyValuePair<Vector3, string>> HideWhenOpen { get; set; } = new List<KeyValuePair<Vector3, string>>();
        public Part Part;

        public Hatch() {}

        public Hatch(string name, string attachNodeId, Vector3 localPosition, Vector3 scale, Quaternion rotation, List<KeyValuePair<Vector3, string>> hideWhenOpen, InternalCollider collider)
        {
            Name = name;
            AttachNodeId = attachNodeId;
            LocalPosition = localPosition;
            Scale = scale;
            Rotation = rotation;
            HideWhenOpen = hideWhenOpen;
            Collider = collider;
        }

        //public override void OnLoad(ConfigNode node)
        //{
        //    Hatch h = LoadFromCfg(node);
        //    Instantiate(this.part);
        //}
        public override void Instantiate(Part p)
        {
            Part = p;
            Debug.Log("# Creating hatch for part " + p);
            if (IvaGameObject != null)
            {
                Debug.LogError("[FreeIVA] Hatch has already been instantiated.");
                return;
            }

            // These values will be cleared on creating the object.
            Vector3 scale = Scale;
            Vector3 localPosition = LocalPosition;
            Quaternion rotation = Rotation;

            IvaGameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(IvaGameObject.GetComponent<Collider>());
            if (p.internalModel == null)
                p.CreateInternalModel(); // TODO: Detect this in an event instead.
            IvaGameObject.transform.parent = p.internalModel.transform;
            IvaGameObject.layer = (int)Layers.InternalSpace;

            // Restore cleared values.
            Scale = scale;
            LocalPosition = localPosition;
            Rotation = rotation;
            IvaGameObject.transform.localScale = scale;
            IvaGameObject.transform.localPosition = localPosition;
            IvaGameObject.transform.localRotation = rotation;
            IvaGameObject.name = Name;
            if (Collider != null)
            {
                Collider.Instantiate(p);
                ModuleFreeIva mfi = p.GetModule<ModuleFreeIva>();
                if (mfi != null)
                {
                    mfi.InternalColliders.Add(Collider);
                }
            }

            Shader depthMask = Utils.GetDepthMask();
            if (depthMask != null)
                IvaGameObjectRenderer.material.shader = depthMask;
            IvaGameObjectRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            ChangeMesh(IvaGameObject);
            SetupAudio();
            //IvaGameObject.renderer.enabled = false; Gets reenabled by EnableInternals.
        }

        public virtual IHatch Clone()
        {
            return new Hatch(Name, AttachNodeId, LocalPosition, Scale, Rotation, new List<KeyValuePair<Vector3, string>>(HideWhenOpen), Collider?.Clone());
        }

        private void GetConnectedHatch()
        {
            AttachNode hatchNode = GetHatchNode(AttachNodeId);
            if (hatchNode == null) return;

            ModuleFreeIva iva = hatchNode.attachedPart.GetModule<ModuleFreeIva>();
            if (iva == null) return;
            for (int i = 0; i < iva.Hatches.Count; i++)
            {
                AttachNode otherHatchNode = iva.Hatches[i].HatchNode;
                if (otherHatchNode != null && otherHatchNode.attachedPart != null && otherHatchNode.attachedPart.Equals(Part))
                {
                    _connectedHatch = iva.Hatches[i];
                    break;
                }
            }
        }

        /// <summary>
        /// Find the part attach node this hatch is associated with.
        /// </summary>
        /// <param name="attachNodeId"></param>
        /// <returns></returns>
        private AttachNode GetHatchNode(string attachNodeId)
        {
            string nodeName = RemoveNodePrefix(attachNodeId);
            foreach (AttachNode n in Part.attachNodes)
            {
                if (n.id == nodeName)
                    return n;
            }
            return null;
        }

        private static string RemoveNodePrefix(string attachNodeId)
        {
            string nodeName;
            string prefix = @"node_stack_";
            if (attachNodeId.StartsWith(prefix))
            {
                nodeName = attachNodeId.Substring(prefix.Length, attachNodeId.Length - prefix.Length);
            }
            else
                nodeName = attachNodeId;
            return nodeName;
        }

        public static void ChangeMesh(GameObject original)
        {
            try
            {
                string modelPath = "FreeIva/Models/HatchMask";
                Debug.Log("#Changing mesh");
                GameObject hatchMask = GameDatabase.Instance.GetModel(modelPath);
                if (hatchMask != null)
                {
                    MeshFilter mfC = original.GetComponent<MeshFilter>();
                    MeshFilter mfM = hatchMask.GetComponent<MeshFilter>();
                    if (mfM == null)
                    {
                        Debug.LogError("[Free IVA] MeshFilter not found in mesh " + modelPath);
                    }
                    else
                    {
                        Mesh m = UnityEngine.Object.Instantiate(mfM.mesh);
                        mfC.mesh = m;
                        Debug.Log("#Changed mesh");
                    }
                }
                else
                    Debug.LogError("[Free IVA] HatchMask.dae not found at " + modelPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Free IVA] Error Loading mesh: " + ex.Message + ", " + ex.StackTrace);
            }
        }

        public void SetupAudio()
        {
            if (!string.IsNullOrEmpty(HatchOpenSoundFile))
            {
                HatchOpenSound = new FXGroup("HatchOpen");
                HatchOpenSound.audio = IvaGameObject.AddComponent<AudioSource>();
                HatchOpenSound.audio.dopplerLevel = 0f;
                HatchOpenSound.audio.Stop();
                HatchOpenSound.audio.clip = GameDatabase.Instance.GetAudioClip(HatchOpenSoundFile);
                HatchOpenSound.audio.loop = false;
            }
            if (!string.IsNullOrEmpty(HatchCloseSoundFile))
            {
                HatchCloseSound = new FXGroup("HatchClose");
                HatchCloseSound.audio = IvaGameObject.AddComponent<AudioSource>();
                HatchCloseSound.audio.dopplerLevel = 0f;
                HatchCloseSound.audio.Stop();
                HatchCloseSound.audio.clip = GameDatabase.Instance.GetAudioClip(HatchCloseSoundFile);
                HatchCloseSound.audio.loop = false;
            }
        }

        public void ToggleHatch()
        {
            Open(!IsOpen);
        }

        public virtual void Open(bool open)
        {
            if (IvaGameObject != null)
            {
                if (IvaGameObjectRenderer != null)
                    IvaGameObjectRenderer.enabled = open;
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

        public virtual void HideOnOpen(bool open)
        {
            MeshRenderer[] meshRenderers = Part.internalModel.GetComponentsInChildren<MeshRenderer>();
            foreach (var hideProp in HideWhenOpen)
            {
                foreach (MeshRenderer mr in meshRenderers)
                {
                    if (mr.name.Equals(hideProp.Value) && mr.transform != null)
                    {
                        float error = Vector3.Distance(mr.transform.localPosition, hideProp.Key);
                        if (error < 0.15)
                        {
                            Debug.Log("# Toggling " + mr.name);
                            mr.enabled = !open;
                            break;
                        }
                    }
                }
            }
        }

        public static void InitialiseAllHatchesClosed()
        {
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                ModuleFreeIva mfi = p.GetModule<ModuleFreeIva>();
                if (mfi != null)
                {
                    for (int i = 0; i < mfi.Hatches.Count; i++)
                    {
                        if (mfi.Hatches[i].IvaGameObject != null)
                        {
                            mfi.Hatches[i].IvaGameObjectRenderer = mfi.Hatches[i].IvaGameObject.GetComponent<Renderer>(); // TODO: Why get this every time?
                        }
                        if (mfi.Hatches[i].IvaGameObjectRenderer != null)
                        {
                            mfi.Hatches[i].IvaGameObjectRenderer.enabled = false;
                        }
                        else
                        {
                            //Debug.LogError("[FreeIVA] Hatch " + i + " renderer not found in part " + p);
                        }
                        if (mfi.Hatches[i].Collider != null)
                        {
                            mfi.Hatches[i].Collider.Enable(true);
                        }
                    }
                }
            }
        }

        public static Hatch LoadFromCfg(ConfigNode node)
        {
            if (!node.HasValue("attachNodeId"))
            {
                Debug.LogWarning("[FreeIVA] Hatch attachNodeId not found: Skipping hatch.");
                return null;
            }
            Hatch hatch = new Hatch();

            hatch.AttachNodeId = node.GetValue("attachNodeId");

            if (node.HasValue("position"))
            {
                string posString = node.GetValue("position");
                string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid hatch position definition \"" + posString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    hatch.LocalPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Hatch position not found: Skipping hatch.");
                return null;
            }

            if (node.HasValue("scale"))
            {
                string scaleString = node.GetValue("scale");
                string[] s = scaleString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid hatch scale definition \"" + scaleString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    hatch.Scale = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Hatch scale not found: Skipping hatch.");
                return null;
            }

            if (node.HasValue("rotation"))
            {
                string rotationString = node.GetValue("rotation");
                string[] s = rotationString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid hatch rotation definition \"" + rotationString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    hatch.Rotation = Quaternion.Euler(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Hatch rotation not found: Skipping hatch.");
                return null;
            }

            if (node.HasValue("HatchOpenSoundFile"))
            {
                hatch.HatchOpenSoundFile = node.GetValue("HatchOpenSoundFile");
            }
            if (node.HasValue("HatchCloseSoundFile"))
            {
                hatch.HatchCloseSoundFile = node.GetValue("HatchCloseSoundFile");
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
                    Vector3 propPos;

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
                            propPos = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[FreeIVA] Hatch position not found: Skipping hatch.");
                        continue;
                    }

                    hatch.HideWhenOpen.Add(new KeyValuePair<Vector3, string>(propPos, propName));
                }
            }

            if (node.HasNode("InternalCollider"))
            {
                ConfigNode hatchColliderNode = node.GetNode("InternalCollider");
                if (hatchColliderNode != null)
                    hatch.Collider = InternalCollider.LoadFromCfg(hatchColliderNode);
            }
            return hatch;
        }

        /*public void Destroy()
        {
            cylinder.DestroyGameObject();
        }*/

        /*public static void PositionHole()
        {
            cylinder.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            cylinder.transform.localScale = Scale;
            return;
            /*Debug.Log("Positioning cylinder from " + cylinder.transform.localPosition);
            cylinder.transform.localPosition = new Vector3(hatchX, hatchY, hatchZ);
            Debug.Log("                       to " + cylinder.transform.localPosition);
            cylinder.transform.localScale = new Vector3(hatchScaleX, hatchScaleY, hatchScaleZ);* /
        }*/
    }
}
