using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	/// <summary>
	/// Module to be placed in a prop that can deactivate it based on whether a certain b9ps subtype is selected
	/// </summary>
	internal class B9PSConditionalProp : InternalModule
	{
		[KSPField]
		new public string moduleID = string.Empty;

		[KSPField]
		public string subtype = string.Empty;

		public override void OnAwake()
		{
			base.OnAwake();

			if (!HighLogic.LoadedSceneIsFlight) return;

			var b9psModule = B9PS_ModuleB9PartSwitch.Create(part, moduleID);

			if (b9psModule == null || subtype != b9psModule.CurrentSubtypeName())
			{
				for (int i = 0; i < internalProp.internalModules.Count; i++)
				{
					if (internalProp.internalModules[i] is FreeIvaHatch hatch)
					{
						hatch.enabled = false;
					}
				}

				internalProp.gameObject.SetActive(false);
			}
		}
	}
}
