using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
	/* Stock EVA setup:
        KerbalEVA is on layer 17 (EVA).
        capsuleCollider:
            height 0.47
            radius: 0.12
        helmetAndHeadCollider: Sphere
            radius: 0.27
    */

	/// <summary>
	/// Character controller for IVA movement.
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KerbalIvaAddon : MonoBehaviour
	{
		public KerbalIvaController KerbalIva;
#if Experimental
		public GameObject KerbalWorldSpace;
		public Collider KerbalWorldSpaceCollider;
		public PhysicMaterial KerbalWorldSpacePhysics;
		public Rigidbody KerbalWorldSpaceRigidbody;
		public Renderer KerbalWorldSpaceRenderer;
#endif

		public bool buckled = true;
		public bool cameraPositionLocked = false;
		public ProtoCrewMember ActiveKerbal;
		public InternalSeat OriginalSeat = null;
		public InternalSeat TargetedSeat = null;
		public bool Gravity = true;
#if Experimental
		public static bool CanHoldItems = false;
#endif
		public static Vector3 flightForces;

		private CameraManager.CameraMode _lastCameraMode = CameraManager.CameraMode.Flight;
		private bool _changingCurrentIvaCrew = false;

		public static KerbalIvaAddon _instance;
		public static KerbalIvaAddon Instance { get { return _instance; } }

		public delegate void GetInputDelegate(ref IVAInput input);
		public static GetInputDelegate GetInput = GetKeyboardInput;

		void Start()
		{
			CreateCameraCollider();

			GameEvents.OnCameraChange.Add(OnCameraChange);
			_instance = this;
		}

		void OnDestroy()
		{
			GameObject.Destroy(KerbalIva);
			GameEvents.OnCameraChange.Remove(OnCameraChange);
			_instance = null;
			KerbalIva = null;
		}

		void OnGUI()
		{
			GuiTutorial.Gui(GetInstanceID());
		}

		public void Update()
		{
			if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(Settings.ToggleGravityKey))
			{
				Gravity = !Gravity && Settings.EnableCollisions;
				ScreenMessages.PostScreenMessage("[FreeIva] Gravity " + (Gravity ? "Enabled" : "Disabled"), 1f, ScreenMessageStyle.LOWER_CENTER);
				KerbalIva.KerbalFeetCollider.enabled = KerbalIva.UseRelativeMovement();
			}

			if (!buckled && GameSettings.CAMERA_MODE.GetKeyDown(true))
			{
				// Return the kerbal to its original seat.
				TargetedSeat = ActiveKerbal.seat;

				if (FreeIva.CurrentPart != null)
				{
					var targetPart = FreeIva.FindPartWithEmptySeat(FreeIva.CurrentPart);
					if (targetPart != null)
					{
						// disabling this for now, because it's so different from the behavior when pressing C
						// but it works, if we want to bring it back
						// TargetedSeat = PropBuckleButton.FindClosestSeat(targetPart.internalModel, KerbalIva.transform.position, float.MaxValue);
					}
				}

				Buckle();
			}

			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
			{
				ActiveKerbal = null;
				FreeIva.CurrentPart = null;
				GuiTutorial.Active = false;
			}
			else
			{
				// In IVA.
				if (_lastCameraMode != CameraManager.CameraMode.IVA)
				{
					GuiTutorial.Active = true;

					// Switching to IVA.
					FreeIva.EnableInternals();
					UpdateActiveKerbal();//false);
					FreeIva.SetRenderQueues(FreeIva.CurrentPart);

					var freeIvaModule = FreeIva.CurrentPart.GetModule<ModuleFreeIva>();
					if (freeIvaModule != null && freeIvaModule.allowsUnbuckling)
					{
						ScreenMessages.PostScreenMessage($"Unbuckle [{Settings.UnbuckleKey}]", 2f, ScreenMessageStyle.LOWER_CENTER);
					}
				}

				// Check if we're changing crew member using the 'V' key.
				if (GameSettings.CAMERA_NEXT.GetKeyDown())
					_changingCurrentIvaCrew = true;
				else
				{
					if (_changingCurrentIvaCrew)
					{
						UpdateActiveKerbal();//false);
						_changingCurrentIvaCrew = false;
					}
				}

				input = new IVAInput();
				GetInput(ref input);

				JumpLatched = JumpLatched && input.Jump;
				input.Jump = input.Jump && !JumpLatched;

				/*FreeIva.InitialPart.Events.Clear();
                if (_transferStart != 0 && Planetarium.GetUniversalTime() > (0.25 + _transferStart))
                {
                    FlightGlobals.ActiveVessel.SpawnCrew();
                    GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
                    InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;
                    _transferStart = 0;
                }*/

			}
			_lastCameraMode = CameraManager.Instance.currentCameraMode;
		}

		// This must be after the internal camera updates
		private void LateUpdate()
		{
			if (FreeIva.Paused) return;

			ApplyInput(input);

			if (!buckled)
			{
				// Normally the InternalCamera's transform is copied to the FlightCamera at the end of InternalCamera.Update, which will have happened right before this component updates.
				// So we need to make sure the latest internal camera rotation gets copied to the flight camera.
				FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
				FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
			}
		}

		CameraManager.CameraMode _previousCameraMode = CameraManager.CameraMode.Flight;
		public void OnCameraChange(CameraManager.CameraMode cameraMode)
		{
			if (cameraMode != CameraManager.CameraMode.IVA && _previousCameraMode == CameraManager.CameraMode.IVA)
			{
				InternalModuleFreeIva.RefreshDepthMasks();

				InputLockManager.RemoveControlLock("FreeIVA");

#if Experimental
				KerbalWorldSpaceCollider.enabled = false;
#endif
				if (!buckled && ActiveKerbal != null)
				{
					ReturnToSeat();
				}
			}
#if Experimental
			if (cameraMode == CameraManager.CameraMode.IVA)
			{
				//KerbalWorldSpaceCollider.enabled = true;
			}
#endif
			_previousCameraMode = cameraMode;
		}

		// Returns (8.018946, 0.0083341, -5.557827) while clamped to the runway.
		public static Vector3 GetFlightAccelerationWorldSpace()
		{
			//Vector3 gravityAccel = FlightGlobals.getGeeForceAtPosition(KerbalIva.transform.position);
			//Vector3 centrifugalAccel = FlightGlobals.getCentrifugalAcc(KerbalIva.transform.position, FreeIva.CurrentPart.orbit.referenceBody);
			//Vector3 coriolisAccel = FlightGlobals.getCoriolisAcc(FreeIva.CurrentPart.vessel.rb_velocity + Krakensbane.GetFrameVelocityV3f(), FreeIva.CurrentPart.orbit.referenceBody);

			// this should be the linear acceleration of the vessel itself
			// TODO: add centrifugal forces from the vessel rotating?
			Vector3 result = -FlightGlobals.ActiveVessel.perturbation;
			
			return result.sqrMagnitude <= 1e-4f ? Vector3.zero : result;
			//return gravityAccel + centrifugalAccel + coriolisAccel;
		}

		public static Vector3 GetFlightAccelerationInternalSpace()
		{
			// TODO: need to subtract the centrifugal acceleration caused by the ship's movement
			// ideally for an object in orbit, this function should return a zero vector
			// wait, but something in a ballistic arc should also be weightless...

			Vector3 accelWorldSpace = GetFlightAccelerationWorldSpace();

			float magnitude = accelWorldSpace.magnitude;
			Quaternion direction = Quaternion.LookRotation(accelWorldSpace);
			Quaternion internalDirection = InternalSpace.WorldToInternal(direction);
			return internalDirection * Vector3.forward * magnitude;
		}

		public static Vector3 GetCentrifugeAccel(ICentrifuge centrifuge, Vector3 internalSpacePosition)
		{
			float omega = Mathf.Deg2Rad * Mathf.Abs(centrifuge.CurrentSpinRate);
			if (omega == 0) return Vector3.zero;

			// for now, we'll assume that the centrifuge is spinning around the "top/bottom" axis - Z in local IVA space
			Transform rotationRoot = centrifuge.IVARotationRoot;
			Vector3 localKerbalPosition = rotationRoot.InverseTransformPoint(internalSpacePosition);
			localKerbalPosition.z = 0;

			// centripetal acceleration = omega^2 * r
			Vector3 localAcceleration = omega * omega * localKerbalPosition;

			return rotationRoot.TransformVector(localAcceleration);
		}

		public static Vector3 GetInternalSubjectiveAcceleration(InternalModuleFreeIva ivaModule, Vector3 internalSpacePosition)
		{
			if (!Instance.Gravity)
			{
				return Vector3.zero;
			}
			else if (ivaModule.Centrifuge != null && ivaModule.Centrifuge.CurrentSpinRate != 0)
			{
				return GetCentrifugeAccel(ivaModule.Centrifuge, internalSpacePosition);
			}
			else if (ivaModule.customGravity != Vector3.zero)
			{
				return ivaModule.customGravity;
			}
			else
			{
				return GetFlightAccelerationInternalSpace();
			}
		}

		public void FixedUpdate()
		{
			if (!buckled)
			{
				KerbalIva.DoFixedUpdate(input);
			}

			input.Jump = false;
		}

		private void CreateCameraCollider()
		{
			KerbalIva = new GameObject("KerbalIvaController").AddComponent<KerbalIvaController>();
			KerbalIva.gameObject.SetActive(false);

#if Experimental
			KerbalWorldSpace = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			KerbalWorldSpace.name = "Kerbal World Space collider";
			KerbalWorldSpace.GetComponentCached<Collider>(ref KerbalWorldSpaceCollider);
			KerbalWorldSpaceCollider.enabled = false; // TODO: Makes vessel explode.
			KerbalWorldSpacePhysics = KerbalWorldSpaceCollider.material;
			KerbalWorldSpaceRigidbody = KerbalWorldSpace.AddComponent<Rigidbody>();
			KerbalWorldSpaceRigidbody.useGravity = false;
			KerbalWorldSpaceRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

			WorldCollisionTracker KerbalWorldCollisionTracker = KerbalWorldSpace.AddComponent<WorldCollisionTracker>();
#if DEBUG
			KerbalWorldSpace.AddComponent<IvaCollisionPrinter>();
#endif
			KerbalWorldCollisionTracker.Initialise(KerbalRigidbody);

			KerbalWorldSpaceCollider.isTrigger = false;
			KerbalWorldSpace.layer = (int)Layers.LocalScenery;
			KerbalWorldSpaceCollider.transform.parent = KerbalCollider.transform;
			KerbalWorldSpace.transform.localScale = new Vector3(Settings.HelmetSize, Settings.HelmetSize, Settings.HelmetSize);
			KerbalWorldSpace.transform.localPosition = new Vector3(0, 0, 0.2f);
			KerbalWorldSpace.GetComponentCached<Renderer>(ref KerbalWorldSpaceRenderer);
			KerbalWorldSpaceRenderer.enabled = true;
#endif
		}

		public struct IVAInput
		{
			public Vector3 MovementThrottle;
			public Vector3 RotationInputEuler;
			public bool Unbuckle;
			public bool Buckle;
			public bool SwitchTo;
			public bool ToggleCameraLock;
			public bool ToggleHatch;
			public bool ToggleFarHatch;
			public bool Jump;
			public bool ToggleCrouch;
		}

		IVAInput input;
		public bool JumpLatched;
		public bool ToggleCrouchLatched;

		private static float GetKeyInputAxis(KeyCode positive, KeyCode negative)
		{
			return (Input.GetKey(positive) ? 1.0f : 0.0f) + (Input.GetKey(negative) ? -1.0f : 0.0f);
		}

		private static void GetKeyboardInput(ref IVAInput input)
		{
			if (Input.GetKeyDown(Settings.UnbuckleKey))
			{
				if (Input.GetKey(Settings.ModifierKey))
				{
					input.ToggleCameraLock = true;
				}
				else
				{
					if (Instance.buckled)
					{
						input.Unbuckle = true;
					}
					else
					{
						input.Buckle = true;
					}
				}
			}

			input.SwitchTo = GameSettings.CAMERA_NEXT.GetKeyDown(true);

			if (!Instance.buckled && !FreeIva.Paused)
			{
				if (!Instance.cameraPositionLocked)
				{
					// TODO: add key controls for turning
					input.RotationInputEuler.z = GetKeyInputAxis(Settings.RollCCWKey, Settings.RollCWKey);
				}

				// movement
				{
					input.MovementThrottle.z = GetKeyInputAxis(Settings.ForwardKey, Settings.BackwardKey);
					input.MovementThrottle.x = GetKeyInputAxis(Settings.RightKey, Settings.LeftKey);
					input.MovementThrottle.y = GetKeyInputAxis(Settings.UpKey, Settings.DownKey);

					input.MovementThrottle.Normalize();
				}

				input.Jump = Input.GetKey(Settings.JumpKey);

				if (Input.GetKeyDown(Settings.OpenHatchKey))
				{
					input.ToggleFarHatch = Input.GetKey(Settings.ModifierKey);
					input.ToggleHatch = !input.ToggleFarHatch;
				}

				input.ToggleCrouch = Input.GetKeyDown(Settings.CrouchKey);
			}
		}

		private void ApplyInput(IVAInput input)
		{
			if (input.ToggleCameraLock)
			{
				cameraPositionLocked = !cameraPositionLocked;
				ScreenMessages.PostScreenMessage(cameraPositionLocked ? "Camera locked" : "Camera unlocked",
						1f, ScreenMessageStyle.LOWER_CENTER);
			}

			if (input.ToggleCrouch && !ToggleCrouchLatched)
			{
				KerbalIva.targetCrouchFraction = 1.0f - KerbalIva.targetCrouchFraction;
			}

			ToggleCrouchLatched = input.ToggleCrouch;

			if (input.Unbuckle)
			{
				Unbuckle();
			}
			else if (input.Buckle)
			{
				Buckle();
			}
			else if (input.SwitchTo)
			{
				SwitchToKerbal();
			}

			if (!buckled && !FreeIva.Paused)
			{
				KerbalIva.UpdateOrientation(input.RotationInputEuler);
				TargetSeats();
				TargetHatches(input.ToggleHatch, input.ToggleFarHatch);
			}
		}

		void PlaySeatBuckleAudio(InternalSeat seat)
		{
			// this sucks: the buckle props aren't actually directly connected to the seat object
			// because in most IVAs, the seat is a standalone prop and the InternalSeat module is just placed directly in the internal
			// maybe we should fix that on loading or something?
			foreach (var buckleButton in seat.internalModel.GetComponentsInChildren<PropBuckleButton>())
			{
				if (Vector3.Distance(buckleButton.transform.position, seat.seatTransform.position) < 2f)
				{
					buckleButton.PlayBuckleSound();
					break;
				}
			}
		}


		//private bool _reseatingCrew = false;
		public void Buckle()
		{
			if (TargetedSeat == null || (TargetedSeat.taken && TargetedSeat.crew != ActiveKerbal))
				return;

			Debug.Log(ActiveKerbal.name + " is entering seat " + TargetedSeat.transform.name + " in part " + FreeIva.CurrentPart);

			InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;

			buckled = true;
			MoveKerbalToSeat(ActiveKerbal, TargetedSeat);
			KerbalIva.gameObject.SetActive(false);
			KerbalIva.KerbalCollisionTracker.CurrentInternalModel = TargetedSeat.internalModel;
			HideCurrentKerbal(false);
			DisablePartHighlighting(false);
			InputLockManager.RemoveControlLock("FreeIVA");
			//ActiveKerbal.flightLog.AddEntry("Buckled");
			ScreenMessages.PostScreenMessage("Buckled", 1f, ScreenMessageStyle.LOWER_CENTER);

			PlaySeatBuckleAudio(TargetedSeat);
		}

		public void ReturnToSeat()
		{
			// some of this stuff should probably get moved to a common function
			KerbalIva.gameObject.SetActive(false);
			buckled = true;
			DisablePartHighlighting(false);
			InputLockManager.RemoveControlLock("FreeIVA");

			// the original seat should still be pointing at this kerbal.  If not, they probably went EVA or something
			if (OriginalSeat.crew == null)
			{
				return;
			}

			TargetedSeat = OriginalSeat;
			Buckle();
			ScreenMessages.PostScreenMessage(ActiveKerbal.name + " returned to their seat.", 1f, ScreenMessageStyle.LOWER_CENTER);
		}

		void SwitchToKerbal()
		{
			if (buckled || TargetedSeat == null || TargetedSeat.crew == ActiveKerbal)
			{
				return;
			}

			var targetKerbal = TargetedSeat.kerbalRef;
			ReturnToSeat();

			CameraManager.Instance.SetCameraIVA(targetKerbal, true);
			targetKerbal.IVAEnable(true);
		}

		public void MoveKerbalToSeat(ProtoCrewMember crewMember, InternalSeat newSeat)
		{
			var oldSeat = crewMember.seat;
			var sourceModel = oldSeat.internalModel; // note: this might not be the part where the protocrewmember actually exists.
			var destModel = newSeat.internalModel;

			// remove the kerbal from their old seat in a non-destructive way
			oldSeat.kerbalRef = null;
			sourceModel.UnseatKerbalAt(oldSeat); // does not mess with protocrewmember assignments

			// transferring seats in the same part
			if (sourceModel.part == destModel.part)
			{
				destModel.SitKerbalAt(crewMember, newSeat);
			}
			else if (destModel.part.protoModuleCrew.Count < destModel.part.CrewCapacity)
			{
				bool removedCrewFromOldPart = false;

				// fully move the kerbal
				if (crewMember.KerbalRef.InPart.protoModuleCrew.Contains(crewMember))
				{
					crewMember.KerbalRef.InPart.RemoveCrewmember(crewMember);
					removedCrewFromOldPart = true;
				}
				else
				{
					foreach (var part in sourceModel.vessel.parts)
					{
						if (part.protoModuleCrew.Contains(crewMember))
						{
							part.RemoveCrewmember(crewMember);
							removedCrewFromOldPart = true;
							break;
						}
					}
				}

				if (removedCrewFromOldPart)
				{
					destModel.part.AddCrewmemberAt(crewMember, destModel.seats.IndexOf(newSeat));

					// suppress the portrait system's response to this message because it messes with internal model visibility
					bool removePortraitEventHandler = !KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.portraitContainer.isActiveAndEnabled;
					if (removePortraitEventHandler)
					{
						GameEvents.onCrewTransferred.Remove(KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.onCrewTransferred);
					}
					GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(crewMember, sourceModel.part, destModel.part));
					if (removePortraitEventHandler)
					{
						GameEvents.onCrewTransferred.Add(KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.onCrewTransferred);
					}
					Vessel.CrewWasModified(sourceModel.part.vessel, destModel.part.vessel);
				}
				else
				{
					Debug.LogWarning($"[FreeIva] Kerbal {crewMember.name} did not exist in crew of source part {crewMember.KerbalRef.InPart.partInfo.name} nor anywhere on the vessel");
					destModel.SitKerbalAt(crewMember, newSeat);
				}
			}
			else
			{
				// put the kerbal in the seat without adjusting crew assignments
				// this would normally be done inside AddCrewMemberAt
				destModel.SitKerbalAt(crewMember, newSeat);
			}

			// this is basically InternalSeat.SpawnCrew but without creating the new kerbal (because we already have one)
			{
				var kerbal = crewMember.KerbalRef;
				// Kerbal kerbal = ProtoCrewMember.Spawn(crew);
				kerbal.transform.parent = newSeat.seatTransform;
				kerbal.transform.localPosition = newSeat.kerbalOffset;
				kerbal.transform.localScale = Vector3.Scale(kerbal.transform.localScale, newSeat.kerbalScale);
				kerbal.transform.localRotation = Quaternion.identity;
				kerbal.InPart = destModel.part;
				kerbal.ShowHelmet(newSeat.allowCrewHelmet);
				newSeat.kerbalRef = kerbal;
			}

			// SetCameraIVA will actually change to flight mode if called with the current kerbal and already in iva mode
			// to prevent this, forcibly change the current camera mode (this will emit an extra mode changed event, but it should be fine)
			CameraManager.Instance.currentCameraMode = CameraManager.CameraMode.Internal;
			CameraManager.Instance.SetCameraIVA(crewMember.KerbalRef, false);
			GameEvents.OnIVACameraKerbalChange.Fire(newSeat.kerbalRef);

			FreeIva.EnableInternals(); // SetCameraIVA also calls FlightGlobals.ActiveVessel.SetActiveInternalSpace(activeInternalPart); which will hide all other IVAs
			FreeIva.SetRenderQueues(newSeat.part);
		}

		public void Unbuckle()
		{
			if (!buckled) return;

			var freeIvaModule = FreeIva.CurrentPart.GetModule<ModuleFreeIva>();
			if (freeIvaModule != null && !freeIvaModule.allowsUnbuckling)
			{
				ScreenMessages.PostScreenMessage("Cannot unbuckle in this part", 1f, ScreenMessageStyle.LOWER_CENTER);
				return;
			}

			FreeIva.EnableInternals();
			UpdateActiveKerbal();
			FreeIva.SetRenderQueues(FreeIva.CurrentPart);
			OriginalSeat = ActiveKerbal.seat;

			Gravity = Gravity && Settings.EnableCollisions;

			HideCurrentKerbal(true);

			InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.CAMERAMODES, "FreeIVA");
			//ActiveKerbal.flightLog.AddEntry("Unbuckled");
			ScreenMessages.PostScreenMessage("Unbuckled", 1f, ScreenMessageStyle.LOWER_CENTER);
			KerbalIva.Activate(ActiveKerbal);
			buckled = false;

			// eventually might want to attach this kerbal to the rigid body (or combine their gameobjects etc) but for now this is important to signal to ProbeControlRoom that the kerbal is unbuckled
			ActiveKerbal.KerbalRef.transform.SetParent(null, true);

			PlaySeatBuckleAudio(OriginalSeat);

			DisablePartHighlighting(true);
		}

		private void UpdateActiveKerbal()
		{
			ActiveKerbal = CameraManager.Instance.IVACameraActiveKerbal.protoCrewMember;
			if (ActiveKerbal.KerbalRef != null && ActiveKerbal.KerbalRef.InPart != null)
				FreeIva.UpdateCurrentPart(ActiveKerbal.seat.part);
			return;
		}

		/// <summary>
		/// Hides or unhides the kerbal IVA model of the currently controlled crew member.
		/// </summary>
		/// <param name="hidden"></param>
		public void HideCurrentKerbal(bool hidden)
		{
			try
			{
				// Set the visible/invisible state for each component of the kerbal model.
				// This won't unhide a helmet hidden elsewhere.
				if (ActiveKerbal != null)
				{
					Renderer[] renderers = ActiveKerbal.KerbalRef.GetComponentsInChildren<Renderer>();
					foreach (var r in renderers)
					{
						r.enabled = !hidden;
					}
				}

				if (!hidden)
				{
					// hides the head renderers
					ActiveKerbal.KerbalRef.IVAEnable();
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("[FreeIVA] Error hiding current kerbal: " + ex.Message + ", " + ex.StackTrace);
			}
		}

		public void DisablePartHighlighting(bool isDisabled)
		{
			int partCount = FlightGlobals.ActiveVessel.parts.Count;
			for (int i = 0; i < partCount; i++)
			{
				if (isDisabled)
					FlightGlobals.ActiveVessel.parts[i].highlightType = Part.HighlightType.Disabled;
				else
				{
					FlightGlobals.ActiveVessel.parts[i].highlightType = Part.HighlightType.OnMouseOver;
				}
			}
		}

		bool ClearLineOfSight(Vector3 from, Transform target, Vector3 targetPosition)
		{
			// first test a direct ray
			if (Physics.Linecast(from, targetPosition, out RaycastHit raycastHit, 1 << (int)Layers.Kerbals, QueryTriggerInteraction.Ignore))
			{
				// if we hit the thing we're trying to reach, success
				if (raycastHit.transform.IsChildOf(target))
				{
					return true;
				}
				// if we hit something other than the source object, it's obstructed
				else if (!raycastHit.transform.IsChildOf(KerbalIva.transform))
				{
					return false;
				}

				// the ray hit ourselves - test a new ray from the hit point
				Vector3 toHit = Vector3.Normalize(raycastHit.point - from) * 0.01f;
				if (Physics.Linecast(raycastHit.point + toHit, targetPosition, out raycastHit, 1 << (int)Layers.Kerbals, QueryTriggerInteraction.Ignore))
				{
					if (raycastHit.transform.IsChildOf(target))
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}

		public bool IsTargeted(Transform targetTransform, Vector3 localPosition, ref float closestDistance)
		{
			Vector3 targetPosition = targetTransform.TransformPoint(localPosition);

			Vector3 toTarget = targetPosition - InternalCamera.Instance.transform.position;
			float distance = toTarget.magnitude;
			float angle = Vector3.Angle(toTarget, InternalCamera.Instance.transform.forward);

			if (angle > 90 || distance > closestDistance)
				return false;

			float radius = (distance * Mathf.Sin(angle * Mathf.Deg2Rad)) / Mathf.Sin((90 - angle) * Mathf.Deg2Rad);
			if (radius <= Settings.ObjectInteractionRadius)
			{
				if (ClearLineOfSight(InternalCamera.Instance.transform.position, targetTransform, targetPosition))
				{
					closestDistance = distance;
					return true;
				}
				else
				{
					return false;
				}
			}

			return false;
		}

		public void TargetSeats()
		{
			float closestDistance = Settings.MaxInteractDistance;
			TargetedSeat = null;
			if (FreeIva.CurrentPart?.internalModel == null)
				return;

			foreach (var seat in FreeIva.CurrentPart.internalModel.seats)
			{
				if (seat.seatTransform == null)
					continue; // Some parts were originally designed to have more seats, but later had their transforms removed without changing the seat count.

				// target a position halfway between the origin of the seat and the kerbal eye position
				// the seat origin is often close to or underneath the shell collider, and the eye position is well above the "center" of the seat
				Vector3 localTargetPosition = KerbalIva.ActiveKerbal.KerbalRef.eyeInitialPos * 0.5f;

				if (IsTargeted(seat.seatTransform, localTargetPosition, ref closestDistance))
				{
					TargetedSeat = seat;
				}
			}

			if (TargetedSeat != null)
			{
				// is someone else here?
				if (TargetedSeat.taken && TargetedSeat.crew != ActiveKerbal)
				{
					ScreenMessages.PostScreenMessage($"[{GameSettings.CAMERA_NEXT.primary}] Switch to {TargetedSeat.crew.name}", 0.1f, ScreenMessageStyle.LOWER_CENTER);
				}
				else
				{
					ScreenMessages.PostScreenMessage("Enter seat [" + Settings.UnbuckleKey + "]", 0.1f, ScreenMessageStyle.LOWER_CENTER);
				}
			}
		}

		void ConsiderHatch(ref FreeIvaHatch targetedHatch, ref float closestDistance, FreeIvaHatch newHatch)
		{
			if (newHatch.enabled && IsTargeted(newHatch.transform, Vector3.zero, ref closestDistance))
			{
				targetedHatch = newHatch;
			}
		}

		void TargetHatchesInPart(Part part, ref FreeIvaHatch targetedHatch, ref float closestDistance)
		{
			InternalModuleFreeIva ivaModule = InternalModuleFreeIva.GetForModel(part.internalModel);
			while (ivaModule != null && ivaModule.isActiveAndEnabled)
			{
				foreach (FreeIvaHatch h in ivaModule.Hatches)
				{
					ConsiderHatch(ref targetedHatch, ref closestDistance, h);
				}

				ivaModule = InternalModuleFreeIva.GetForModel(ivaModule.SecondaryInternalModel);
			}
		}

		public void TargetHatches(bool openHatch, bool openFarHatch)
		{
			FreeIvaHatch targetedHatch = null;
			float closestDistance = Settings.MaxInteractDistance;

			for (var internalModule = FreeIva.CurrentInternalModuleFreeIva; internalModule != null; internalModule = InternalModuleFreeIva.GetForModel(internalModule.SecondaryInternalModel))
			{
				if (internalModule.isActiveAndEnabled)
				{
					foreach (FreeIvaHatch h in internalModule.Hatches)
					{
						ConsiderHatch(ref targetedHatch, ref closestDistance, h);

						// if this hatch is open, consider other hatches in the adjacent part
						if (h.IsOpen && h.ConnectedHatch != null)
						{
							TargetHatchesInPart(h.ConnectedHatch.part, ref targetedHatch, ref closestDistance);
						}
					}
				}
			}

			// try neighboring parts
			if (targetedHatch == null)
			{
				if (FreeIva.CurrentPart.parent != null)
				{
					TargetHatchesInPart(FreeIva.CurrentPart.parent, ref targetedHatch, ref closestDistance);
				}

				foreach (Part child in FreeIva.CurrentPart.children)
				{
					TargetHatchesInPart(child, ref targetedHatch, ref closestDistance);
				}
			}

			if (targetedHatch != null)
			{
				bool canOpenHatch = false;

				if (targetedHatch.IsBlockedByAnimation())
				{
					ScreenMessages.PostScreenMessage("Hatch is locked",
							0.1f, ScreenMessageStyle.LOWER_CENTER);
				}
				else if (targetedHatch.ConnectedHatch == null)
				{
					if (targetedHatch.CanEVA)
					{
						ScreenMessages.PostScreenMessage("Go EVA [" + Settings.OpenHatchKey + "]", 0.1f, ScreenMessageStyle.LOWER_CENTER);

						if (openHatch)
						{
							targetedHatch.GoEVA();
						}
					}
					else if (targetedHatch.attachNodeId != string.Empty || targetedHatch.dockingPortNodeName != string.Empty)
					{
						ScreenMessages.PostScreenMessage("Hatch is blocked", 0.1f, ScreenMessageStyle.LOWER_CENTER);
					}
					else
					{
						canOpenHatch = true;
					}
				}
				else
				{
					canOpenHatch = true;
				}
				
				if (canOpenHatch)
				{
					ScreenMessages.PostScreenMessage((targetedHatch.IsOpen ? "Close" : "Open") + " hatch [" + Settings.OpenHatchKey + "]",
						0.1f, ScreenMessageStyle.LOWER_CENTER);

					if (openHatch)
						targetedHatch.ToggleHatch();
				}
			}
		}

		

#if Experimental
		private static Transform _oldParent = null;
		private static Transform HeldItem = null;
		public static bool HoldingItem { get; private set; }
		public static void HoldItem(Transform t)
		{
			if (!CanHoldItems) return;

			if (HoldingItem)
				HeldItem.transform.parent = _oldParent;

			_oldParent = t.transform.parent;
			HeldItem = t;
			HeldItem.transform.parent = InternalCamera.Instance.transform;
			HoldingItem = true;
		}

		public static void DropHeldItem()
		{
			if (HeldItem != null && HeldItem.transform != null)
				HeldItem.transform.parent = _oldParent;
		}
#endif

		public void OnCollisionEnter(Collision collision)
		{
			//Debug.Log("# OnCollisionEnter " + name + " with " + collision.gameObject + " layer " + collision.gameObject.layer);
			ScreenMessages.PostScreenMessage("OnCollisionEnter " + name + " with " + collision.gameObject + " layer " + collision.gameObject.layer,
				1f, ScreenMessageStyle.LOWER_CENTER);
		}

		public void OnCollisionStay(Collision collision)
		{
			//Debug.Log("# OnCollisionStay " + collision.gameObject + " with " + collision.transform);
			ScreenMessages.PostScreenMessage("OnCollisionStay " + collision.gameObject + " with " + collision.transform + " layer " + collision.gameObject.layer,
				1f, ScreenMessageStyle.LOWER_CENTER);
		}

		public void OnCollisionExit(Collision collision)
		{
			//Debug.Log("# OnCollisionExit " + collision.gameObject + " with " + collision.transform);
			ScreenMessages.PostScreenMessage("OnCollisionExit " + collision.gameObject + " with " + collision.transform + " layer " + collision.gameObject.layer,
				1f, ScreenMessageStyle.LOWER_CENTER);
		}
	}
}
