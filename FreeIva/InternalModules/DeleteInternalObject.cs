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
				var transform = TransformUtil.FindPropTransform(prop, objectName);

				if (transform != null)
				{
					GameObject.Destroy(transform.gameObject);
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			DeleteObjects(internalProp, node);
		}
	}
}
