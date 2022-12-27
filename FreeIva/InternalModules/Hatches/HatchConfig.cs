using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	// this module just contains per-placement data for hatches.
	public class HatchConfig : InternalModule
	{
		public override void OnLoad(ConfigNode node)
		{
			var propHatch = GetComponent<FreeIvaHatch>();

			if (propHatch == null)
			{
				if (internalProp.hasModel)
				{
					Debug.Log($"[FreeIva] HatchConfig in internal {internalModel.internalName} placed in prop {internalProp.propName} that does not have a FreeIvaHatch module");
				}
				else
				{
					Debug.LogError($"[FreeIva] HatchConfig in internal {internalModel.internalName} was placed outside of a PROP node");
				}
				return;
			}

			node.TryGetValue(nameof(FreeIvaHatch.attachNodeId), ref propHatch.attachNodeId);
			node.TryGetValue(nameof(FreeIvaHatch.airlockName), ref propHatch.airlockName);
			node.TryGetValue(nameof(FreeIvaHatch.tubeExtent), ref propHatch.tubeExtent);
			node.TryGetValue(nameof(FreeIvaHatch.hideDoorWhenConnected), ref propHatch.hideDoorWhenConnected);
			node.TryGetValue(nameof(FreeIvaHatch.dockingPortNodeName), ref propHatch.dockingPortNodeName);
			node.TryGetValue(nameof(FreeIvaHatch.cutoutTargetTransformName), ref propHatch.cutoutTargetTransformName);
			node.TryGetValue(nameof(FreeIvaHatch.requireDeploy), ref propHatch.requireDeploy);

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

			var freeIvaModule = internalModel.GetComponentInChildren<InternalModuleFreeIva>();
				
			// it's possible that this prop is before the FreeIva module in the INTERNAL node.
			// skip adding the cut for now, and the module will pick it up in its OnLoad function
			if (freeIvaModule != null)
			{
				freeIvaModule.AddPropCut(propHatch);
			}
		}
	}
}
