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

					Vector3 v = Vector3.zero;
					if (node.TryGetValue("localPosition", ref v))
					{
						childTransform.localPosition = v;
					}

					if (node.TryGetValue("localRotation", ref v))
					{
						childTransform.localRotation = Quaternion.Euler(v);
					}

					Quaternion localRotation = Quaternion.identity;
					if (node.TryGetValue("localRotation", ref localRotation))
					{
						childTransform.localRotation = localRotation;
					}

					if (node.TryGetValue("localScale", ref v))
					{
						childTransform.localScale = v;
					}
				}
			}
			else
			{
				Debug.LogError($"[FreeIva] Reparent node in prop {prop.propName} requires 'childTransformName' and 'parentTransformName' values");
			}
		}
	}
}
