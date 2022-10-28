using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    public class PropHatch : InternalModule
    {
        [KSPField]
        public string openPropName = string.Empty;

        [KSPField]
        public Vector3 position = Vector3.zero;

        [KSPField]
        public Vector3 scale = Vector3.one;

        [KSPField]
        public Vector3 rotation = Vector3.zero; // as euler angles

        [KSPField]
        public string HatchOpenSoundFile = string.Empty;

        [KSPField]
        public string HatchCloseSoundFile = string.Empty;

        // these fields are populated by PropHatchConfig, per-placement
        public string attachNodeId;
        public List<KeyValuePair<Vector3, string>> HideWhenOpen;

        PropHatchInstance hatchInstance;

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                hatchInstance = PropHatchInstance.Create(this);
                part.GetComponent<ModuleFreeIva>().Hatches.Add(hatchInstance);
            }
        }
    }

    /// <summary>
    /// A hatch which is based on one or more IVA prop objects. Assumes the prop for the hatch in the closed state is already present.
    /// </summary>
    public class PropHatchInstance : Hatch
    {
        public static PropHatchInstance Create(PropHatch propHatch)
        {
            var hatchInstance = ScriptableObject.CreateInstance<PropHatchInstance>();
            hatchInstance.Instantiate(propHatch);
            return hatchInstance;
        }

        private PropHatch _propHatch;

        public override Vector3 WorldPosition
        {
            get
            {
                if (ClosedProp != null)
                    return ClosedProp.transform.position;
                return Vector3.zero;
            }
        }

        public InternalProp ClosedProp => _propHatch.internalProp;
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

        public override void Instantiate(Part p)
        {
            throw new InvalidOperationException("PropHatch can't be instantiaed");
        }

        private void Instantiate(PropHatch propHatch)
        {
            _propHatch = propHatch;

            if (propHatch.HideWhenOpen != null)
            {
                HideWhenOpen = propHatch.HideWhenOpen;
            }

            AttachNodeId = propHatch.attachNodeId;

            Part = propHatch.part;
            Debug.Log("# Instantiating prop hatch for part " + Part);
            if (IvaGameObject != null)
            {
                Debug.LogError("[FreeIVA] Hatch has already been instantiated.");
                return;
            }

            CreateOpenProp();
            IvaGameObject = ClosedProp.gameObject;

            SetupAudio();
        }

        public override IHatch Clone()
        {
            throw new InvalidOperationException("PropHatch can't be cloned");
        }

        // Find the prop in the IVA. If not present, spawn it.
        private void CreateOpenProp()
        {
            if (string.IsNullOrEmpty(_propHatch.openPropName)) return;

            OpenProp = PartLoader.GetInternalProp(_propHatch.openPropName);
            if (OpenProp == null)
            {
                Debug.LogError("[FreeIVA] Unable to load open prop hatch \"" + _propHatch.openPropName + "\" in part " + this.Part.name);
            }
            else
            {
                Debug.Log("# Adding PropHatch to part " + this.Part.name);
                OpenProp.propID = FreeIva.CurrentPart.internalModel.props.Count;
                OpenProp.internalModel = this.Part.internalModel;
                OpenProp.transform.parent = this.Part.internalModel.transform;
                OpenProp.hasModel = true;
                this.Part.internalModel.props.Add(OpenProp);
                OpenProp.transform.rotation = _propHatch.transform.rotation * Quaternion.Euler(_propHatch.rotation);
                OpenProp.transform.position = _propHatch.transform.position + _propHatch.transform.rotation * _propHatch.position;
                OpenProp.transform.localScale = _propHatch.scale;
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
            var propHatch = GetComponent<PropHatch>();
            if (propHatch != null)
            {
                propHatch.attachNodeId = attachNodeId;
                propHatch.HideWhenOpen = hideWhenOpen;
            }
        }
    }
}
