using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// A module that can be placed on a hatch prop.  Swaps that prop with an 'opened' version when opened
    /// </summary>
    public class PropHatch : FreeIvaHatch
    {
        [KSPField]
        public string openPropName = string.Empty;

        [KSPField]
        public Vector3 openPropPosition = Vector3.zero;

        [KSPField]
        public Vector3 openPropScale = Vector3.one;

        [KSPField]
        public Vector3 openPropRotation = Vector3.zero; // as euler angles

        public InternalProp ClosedProp => internalProp;
        public InternalProp OpenProp;

        public override void OnAwake()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            base.OnAwake();

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
                OpenProp.transform.localRotation = Quaternion.Euler(openPropRotation);
                OpenProp.transform.localPosition = openPropPosition;
                OpenProp.transform.localScale = openPropScale;
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
}
