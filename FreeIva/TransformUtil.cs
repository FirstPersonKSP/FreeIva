using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal static class TransformUtil
	{
		static Transform FindNthTransform(Transform current, string name, ref int n)
		{
			if (current.name == name)
			{
				if (n == 0)
				{
					return current;
				}
				else
				{
					--n;
				}
			}

			for (int i = 0; i < current.childCount; ++i)
			{
				var t = FindNthTransform(current.GetChild(i), name, ref n);
				if (t != null) return t;
			}

			return null;
		}

		static Transform FindModelTransform(Transform root, string transformName, string contextType, string contextName, bool emitError)
		{
			int commaIndex = transformName.IndexOf(',');
			int index = 0;
			string trimmedName = transformName;

			if (commaIndex != -1)
			{
				if (!int.TryParse(transformName.Substring(commaIndex + 1), out index))
				{
					Debug.LogError($"[FreeIva] failed to parse index from transform {transformName} in {contextType} {contextName}");
					return null;
				}
				trimmedName = transformName.Substring(0, commaIndex);
			}

			var transform = FindNthTransform(root, trimmedName, ref index);
			
			if (transform == null && emitError)
			{
				Debug.LogError($"[FreeIva] could not find transform {transformName} in {contextType} {contextName}");
			}

			return transform;
		}

		public static Transform FindInternalModelTransform(InternalModel model, string transformName, bool emitError = true)
		{
			return FindModelTransform(model.transform, transformName, "internal", model.internalName, emitError);
		}

		public static Transform FindPropTransform(InternalProp prop, string transformName, bool emitError = true)
		{
			if (string.IsNullOrEmpty(transformName)) return null;

			if (prop.hasModel)
			{
				return FindModelTransform(prop.transform, transformName, "prop", prop.propName, emitError);
			}
			else
			{
				return FindInternalModelTransform(prop.internalModel, transformName, emitError);
			}
		}

		public static Transform FindPartTransform(Part part, string transformName, bool emitError = true)
		{
			return FindModelTransform(part.transform.Find("model"), transformName, "part", part.partInfo.name, emitError);
		}

		public static Transform FindModelFile(Transform root, string modelFileName)
		{
			var objectName = modelFileName + "(Clone)";

			var modelObject = root.Find("model");

			for (int i = 0; i < modelObject.childCount; i++)
			{
				var childObject = modelObject.GetChild(i);
				if (childObject.name == objectName)
				{
					return childObject;
				}
			}

			Debug.LogError($"[FreeIva] Could not find model file '{modelFileName}' in '{root.name}'");
			return null;
		}
	}
}
