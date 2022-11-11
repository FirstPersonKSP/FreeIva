using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal class DeleteInternalObject : InternalModule
	{
		public static void DeleteObjects(InternalProp prop, ConfigNode node)
		{
			foreach (var objectName in node.GetValues("objectName"))
			{
				var transforms = prop.FindModelTransforms(objectName);

				if (transforms.Length == 0)
				{
					var typeContext = prop.hasModel ? "prop" : "internal";
					var nameContext = prop.hasModel ? prop.propName : prop.internalModel.internalName;

					Debug.LogError($"[FreeIva] no transform named {objectName} found in {typeContext} {nameContext}");
				}
				else
				{
					foreach (var t in transforms)
					{
						GameObject.Destroy(t.gameObject);
					}
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			DeleteObjects(internalProp, node);
		}
	}
}
