﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva.InternalModules
{
	internal class HandRail : InternalModule
	{
		[KSPField]
		public string transformName = string.Empty;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				Transform railTransform = TransformUtil.FindPropTransform(internalProp, transformName);
				if (railTransform != null)
				{
					railTransform.gameObject.layer = (int)Layers.Kerbals;

					var collider = railTransform.GetComponent<Collider>();
					if (collider == null)
					{
						string dbgName = internalProp.hasModel ? internalProp.propName : internalModel.internalName;

						var colliderNodes = node.GetNodes("Collider");
						if (colliderNodes.Length > 0)
						{
							foreach (var colliderNode in colliderNodes)
							{
								var c = ColliderUtil.CreateCollider(railTransform, colliderNode, dbgName);
								c.isTrigger = true;
							}
						}
						else
						{
							Debug.LogError($"[FreeIVA] PropBuckleButton on {dbgName} does not have a collider on transform {transformName} and no procedural colliders");
						}
					}
					else
					{
						collider.isTrigger = true;
						ColliderUtil.AddColliderVisualizer(collider);
					}
				}
				else
				{
					Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.name} could not find a transform named {transformName}");
				}
			}
		}

		public void Start()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			Transform railTransform = TransformUtil.FindPropTransform(internalProp, transformName);
			if (railTransform != null)
			{
				ClickWatcher clickWatcher = railTransform.gameObject.GetOrAddComponent<ClickWatcher>();

				clickWatcher.AddMouseDownAction(OnClick);
			}
		}

		private void OnClick()
		{
			if (KerbalIvaController.Instance.buckled)
			{
				KerbalIvaController.Instance.Unbuckle();
			}
		}
	}
}
