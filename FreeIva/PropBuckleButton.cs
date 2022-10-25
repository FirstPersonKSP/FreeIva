using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
    public class PropBuckleButton : InternalModule
    {
        [KSPField]
        public string buttonTransformName;

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            Transform buttonTransform = internalProp.FindModelTransform(buttonTransformName);
            if (buttonTransform != null)
            {
                ClickWatcher clickWatcher = buttonTransform.gameObject.GetOrAddComponent<ClickWatcher>();

                clickWatcher.AddMouseDownAction(OnClick);
            }
            else
            {
                Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName} could not find transform named {buttonTransformName}");
            }
        }

        private void OnClick()
        {
            KerbalIvaController.Instance.Unbuckle();
        }
    }
}
