using FreeIva;
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

		[SerializeField] AudioSource m_audioSource;

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				Transform buttonTransform = TransformUtil.FindPropTransform(internalProp, transformName);
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
					Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName} could not find a transform named {transformName}");
				}

				string soundName = node.GetValue("soundName");
				if (soundName != null && buttonTransform != null)
				{
					var buckleSound = GameDatabase.Instance.GetAudioClip(soundName);

					if (buckleSound != null)
					{
						m_audioSource = buttonTransform.gameObject.AddComponent<AudioSource>();
						m_audioSource.playOnAwake = false;
						m_audioSource.clip = buckleSound;
					}
					else
					{
						Debug.LogError($"[FreeIva] PropBuckleButton on {internalProp.propName} could not find audio clip {soundName}");
					}
				}
			}
		}

		public void Start()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			Transform buttonTransform = TransformUtil.FindPropTransform(internalProp, transformName);
			if (buttonTransform != null)
			{
				ClickWatcher clickWatcher = buttonTransform.gameObject.GetOrAddComponent<ClickWatcher>();

				clickWatcher.AddMouseDownAction(OnClick);
			}
		}

		// since props aren't usually directly associated with InternalSeats, we have to search for a seat that is near this prop
		// only consider seats within this range
		const float MaxRangeFromProp = 2f;

		// how far away the kerbal can be from the seat transform to board it when clicking
		const float MaxRangeFromKerbal = 1f;

		internal static InternalSeat FindClosestSeat(InternalModel model, Vector3 position, float maxRange = MaxRangeFromProp)
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

			return result;
		}

		public void PlayBuckleSound()
		{
			if (m_audioSource != null)
			{
				m_audioSource.PlayOneShot(m_audioSource.clip);
			}
		}

		public void OnClick()
		{
			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
			{
				var seat = FindClosestSeat(internalModel, internalProp.transform.position);
				if (seat != null && seat.kerbalRef != null)
				{
					CameraManager.Instance.SetCameraIVA(seat.kerbalRef, true);
					KerbalIvaAddon.Instance.Unbuckle();
				}
			}
			else if (KerbalIvaAddon.Instance.buckled)
			{
				// TODO: what if this buckle is on a different seat than the one you're in?
				KerbalIvaAddon.Instance.Unbuckle();
			}
			else
			{
				var seat = FindClosestSeat(internalModel, internalProp.transform.position);
				if (seat != null && (!seat.taken || seat.crew == KerbalIvaAddon.Instance.ActiveKerbal))
				{
					Vector3 seatEyePosition = seat.seatTransform.position + seat.seatTransform.rotation * seat.kerbalEyeOffset;
					float distanceFromKerbal = Vector3.Distance(InternalCamera.Instance.transform.position, seatEyePosition);

					Debug.Log("[FreeIva] PropBuckleButton clicked while unbuckled; distance = " + distanceFromKerbal.ToString());

					if (distanceFromKerbal < MaxRangeFromKerbal)
					{
						KerbalIvaAddon.Instance.TargetedSeat = seat;
						KerbalIvaAddon.Instance.Buckle();
					}
				}
			}
		}
	}
}
