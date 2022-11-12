using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public class PropBuckleButton : InternalModule
	{
		[KSPField]
		public string transformName = string.Empty;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				Transform buttonTransform = internalProp.FindModelTransform(transformName);
				if (buttonTransform != null)
				{
					var collider = buttonTransform.GetComponent<Collider>();
					if (collider == null)
					{
						string dbgName = internalProp.hasModel ? internalProp.propName : internalModel.internalName;

						var colliderNodes = node.GetNodes("Collider");
						if (colliderNodes.Length > 0)
						{
							buttonTransform.gameObject.layer = (int)Layers.InternalSpace;

							foreach (var colliderNode in colliderNodes)
							{
								var c = ColliderUtil.CreateCollider(buttonTransform, colliderNode, dbgName);
								ColliderUtil.AddColliderVisualizer(c);
							}
						}
						else
						{
							Debug.LogError($"[FreeIVA] PropBuckleButton on {dbgName} does not have a collider on transform {transformName} and no procedural colliders");
						}
					}
					else
					{
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

			Transform buttonTransform = internalProp.FindModelTransform(transformName);
			if (buttonTransform != null)
			{
				ClickWatcher clickWatcher = buttonTransform.gameObject.GetOrAddComponent<ClickWatcher>();

				clickWatcher.AddMouseDownAction(OnClick);
			}
		}

		// since props aren't usually directly associated with InternalSeats, we have to search for a seat that is near this prop
		// only consider seats within this range
		static float MaxRangeFromProp = 2f;

		// how far away the kerbal can be from the seat transform to board it when clicking
		static float MaxRangeFromKerbal = 1f;

		InternalSeat FindClosestSeat(InternalModel model, Vector3 position)
		{
			float bestDist = MaxRangeFromProp;
			InternalSeat result = null;
			foreach (var seat in model.seats)
			{
				float dist = Vector3.Distance(seat.seatTransform.position, position);
				if (dist < bestDist)
				{
					bestDist = dist;
					result = seat;
				}
			}

			if (result.taken && result.crew != KerbalIvaController.ActiveKerbal)
			{
				result = null;
			}

			return result;
		}

		private void OnClick()
		{
			if (KerbalIvaController.Instance.buckled)
			{
				// TODO: what if this buckle is on a different seat than the one you're in?
				KerbalIvaController.Instance.Unbuckle();
			}
			else
			{
				var seat = FindClosestSeat(internalModel, internalProp.transform.position);
				if (seat != null)
				{
					Vector3 seatEyePosition = seat.seatTransform.position + seat.seatTransform.rotation * seat.kerbalEyeOffset;
					float distanceFromKerbal = Vector3.Distance(InternalCamera.Instance.transform.position, seatEyePosition);

					Debug.Log("[FreeIva] PropBuckleButton clicked while unbuckled; distance = " + distanceFromKerbal.ToString());

					if (distanceFromKerbal < MaxRangeFromKerbal)
					{
						KerbalIvaController.Instance.TargetedSeat = seat;
						KerbalIvaController.Instance.Buckle();
					}
				}
			}
		}
	}
}
