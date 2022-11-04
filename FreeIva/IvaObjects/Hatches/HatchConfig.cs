using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
    // this module just contains per-placement data for hatches.  It's named badly because it's no longer specific to PropHatch
    public class PropHatchConfig : InternalModule
    {
        public override void OnLoad(ConfigNode node)
        {
            var propHatch = GetComponent<FreeIvaHatch>();

            if (propHatch == null)
            {
                if (internalProp.hasModel)
                {
                    Debug.Log($"[FreeIva] PropHatchConfig in internal {internalModel.internalName} placed in prop {internalProp.propName} that does not have a FreeIvaHatch module");
                }
                else
                {
                    Debug.LogError($"[FreeIva] PropHatchConfig in internal {internalModel.internalName} was placed outside of a PROP node");
                }
                return;
            }

            node.TryGetValue("attachNodeId", ref propHatch.attachNodeId);
            node.TryGetValue("airlockName", ref propHatch.airlockName);
            node.TryGetValue("tubeExtent", ref propHatch.tubeExtent);
            node.TryGetValue("hideDoorWhenConnected", ref propHatch.hideDoorWhenConnected);

            ConfigNode[] hideNodes = node.GetNodes("HideWhenOpen");
            if (hideNodes != null && hideNodes.Length > 0)
            {
                propHatch.HideWhenOpen = ScriptableObject.CreateInstance<FreeIvaHatch.ObjectsToHide>();

                foreach (var hideNode in hideNodes)
                {
                    FreeIvaHatch.ObjectToHide objectToHide = new FreeIvaHatch.ObjectToHide();

                    objectToHide.name = hideNode.GetValue("name");
                    if (objectToHide.name == null)
                    {
                        Debug.LogWarning("[FreeIVA] HideWhenOpen name not found.");
                        continue;
                    }

                    if (hideNode.TryGetValue("position", ref objectToHide.position))
                    {
                        propHatch.HideWhenOpen.objects.Add(objectToHide);
                    }
                    else
                    {
                        Debug.LogWarning($"[FreeIVA] Invalid HideWhenOpen position definition in INTERNAL {internalModel.internalName} PROP {internalProp.propName}");
                    }
                }
            }

            var cutoutTargetTransformName = node.GetValue("cutoutTargetTransformName");
            if (propHatch.cutoutTransformName != string.Empty && cutoutTargetTransformName != null)
            {
                var freeIvaModule = internalModel.GetComponentInChildren<InternalModuleFreeIva>();
                freeIvaModule.AddPropCut(cutoutTargetTransformName, propHatch);
            }
        }
    }
}
