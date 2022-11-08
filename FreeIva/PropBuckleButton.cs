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
						var colliderNodes = node.GetNodes("Collider");
						if (colliderNodes.Length > 0)
						{
							buttonTransform.gameObject.layer = (int)Layers.InternalSpace;

							foreach (var colliderNode in colliderNodes)
							{
								var c = CreateCollider(buttonTransform, colliderNode);
								AddColliderVisualizer(c);
							}
						}
						else
						{
							string dbgName = internalProp.hasModel ? internalProp.propName : internalModel.internalName;
							Debug.LogError($"[FreeIVA] PropBuckleButton on {dbgName} does not have a collider on transform {transformName} and no procedural colliders");
						}
					}
					else
					{
						AddColliderVisualizer(collider);
					}
				}
				else
				{
					Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.name} could not find a transform named {transformName}");
				}
			}
		}

		enum CapsuleAxis
		{
			X = 0,
			Y = 1,
			Z = 2
		}

		void AddColliderVisualizer(Collider collider)
		{
#if false
			if (collider is BoxCollider box)
			{
				var debugObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
				Component.Destroy(debugObject.GetComponent<Collider>());
				debugObject.transform.SetParent(collider.transform, false);
				debugObject.transform.localPosition = box.center;
				debugObject.transform.localScale = box.size;
				debugObject.layer = 20;
			}
			// TODO:
#endif
		}

		Collider CreateCollider(Transform t, ConfigNode cfg)
		{
			Collider result = null;
			Vector3 center = Vector3.zero, boxDimensions = Vector3.zero;
			float radius = 0, height = 0;
			CapsuleAxis axis = CapsuleAxis.X;
			string colliderShape = string.Empty;
			
			if (!cfg.TryGetValue("shape", ref colliderShape))
			{
				Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.name} does not have a collider in the model and does not have a shape field");
			}
			else switch (colliderShape)
			{
				case "Capsule":
					if (cfg.TryGetValue("center", ref center) &&
						cfg.TryGetValue("radius", ref radius) &&
						cfg.TryGetValue("height", ref height) &&
						cfg.TryGetEnum("axis", ref axis, CapsuleAxis.X))
					{
						var collider = t.gameObject.AddComponent<CapsuleCollider>();
						collider.radius = radius;
						collider.height = height;
						collider.center = center;
						collider.direction = (int)axis;
						result = collider;
					}
					else
					{
						Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: capsule shape requires center, radius, height, and axis fields");
					}
					break;
				case "Box":
					if (cfg.TryGetValue("center", ref center) &&
						cfg.TryGetValue("dimensions", ref boxDimensions))
					{
						var collider = t.gameObject.AddComponent<BoxCollider>();
						collider.center = center;
						collider.size = boxDimensions;
						result = collider;
					}
					else
					{
						Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: box shape requires center and dimensions fields");
					}
					break;
				case "Sphere":
					if (cfg.TryGetValue("center", ref center) &&
						cfg.TryGetValue("radius", ref radius))
					{
						var collider = t.gameObject.AddComponent<SphereCollider>();
						collider.center = center;
						collider.radius = radius;
						result = collider;
					}
					else
					{
						Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: sphere shape requires center and radius fields");
					}
					break;
				default:
					Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName} has invalid collider shape '{colliderShape}");
					break;
			}

			return result;
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
