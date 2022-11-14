using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal class DisableCollider : InternalModule
	{
		public static void DisableColliders(InternalProp prop, ConfigNode node)
		{
			foreach (var colliderName in node.GetValues("colliderName"))
			{
				var colliderTransform = TransformUtil.FindPropTransform(prop, colliderName);
				if (colliderTransform != null)
				{
					var collider = colliderTransform.GetComponent<Collider>();
					if (collider != null)
					{
						Component.Destroy(collider);
					}
					else if (prop.hasModel)
					{
						Debug.LogError($"[FreeIva] DisableCollider: no collider found on transform {colliderName} in prop {prop.propName}");
					}
					else
					{
						Debug.LogError($"[FreeIva] DisableCollider: no collider found on transform {colliderName} in internal {prop.internalModel.internalName}");
					}
				}
				else if (prop.hasModel)
				{
					Debug.LogError($"[FreeIva] DisableCollider: no transform named {colliderName} in prop {prop.propName}");
				}
				else
				{
					Debug.LogError($"[FreeIva] DisableCollider: no transform named {colliderName} in internal {prop.internalModel.internalName}");
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			DisableColliders(internalProp, node);
		}

		void Start()
		{
			enabled = false;
		}
	}
}
