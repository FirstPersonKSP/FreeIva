using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal static class ReparentUtil
	{
		public static void Reparent(InternalProp prop, ConfigNode node)
		{
			string childTransformName = string.Empty;
			string parentTransformName = string.Empty;

			if (node.TryGetValue("childTransformName", ref childTransformName) &&
				node.TryGetValue("parentTransformName", ref parentTransformName))
			{
				var childTransform = TransformUtil.FindPropTransform(prop, childTransformName);
				var parentTransform = TransformUtil.FindPropTransform(prop, parentTransformName);

				if (parentTransformName == String.Empty)
				{
					parentTransform = prop.transform;
				}

				if (childTransform == null)
				{
					Debug.LogError($"[FreeIva] Reparent node in prop {prop.name} failed to find '{childTransformName}'");
				}
				if (parentTransform == null)
				{
					Debug.LogError($"[FreeIva] Reparent node in prop {prop.name} failed to find '{parentTransformName}'");
				}

				if (childTransform != null && parentTransform != null)
				{
					childTransform.SetParent(parentTransform, true);
				}
			}
			else
			{
				Debug.LogError($"[FreeIva] Reparent node in prop {prop.propName} requires 'childTransformName' and 'parentTransformName' values");
			}
		}
	}
}
