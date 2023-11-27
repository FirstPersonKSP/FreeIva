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

		[SerializeField]
		public string[] subtypes;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			subtypes = node.GetValues("subtype");
		}

		public override void OnAwake()
		{
			base.OnAwake();

			if (!HighLogic.LoadedSceneIsFlight) return;

			var b9psModule = B9PS_ModuleB9PartSwitch.Create(internalProp, moduleID);

			if (b9psModule == null || subtypes.IndexOf(b9psModule.CurrentSubtypeName()) == -1)
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

		// This module can destroy itself so that it doesn't consume resources after it's done its job
		// this is done in update because all the InternalModel functions like OnAwake, OnUpdate etc are inside a loop over the prop's internalModules, and we can't remove items in the middle of that.
		// I had tried changing the OnAwake function above to be unity's Awake method instead, but at that point the prop has not been fully initialized (it doesn't get set up in the prefab, only after it's spawned)
		void Update()
		{
			internalProp.internalModules.Remove(this);
			Component.Destroy(this);
		}
	}
}
