using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeIva.InternalModules
{
	internal class ChangeObjectLayer : InternalModule
	{
		[KSPField]
		public int layer;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			foreach (var transformName in node.GetValues("transformName"))
			{
				var transform = TransformUtil.FindPropTransform(internalProp, transformName);
				if (transform != null)
				{
					transform.gameObject.layer = layer;
				}
			}
		}
	}
}
