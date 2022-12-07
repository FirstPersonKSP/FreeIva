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
	public class KerbalIvaController : MonoBehaviour
	{
		public static GameObject KerbalIva;
		public static SphereCollider KerbalCollider; // this may eventually change to a Capsule
		public static Rigidbody KerbalRigidbody;
		// TODO: Vary this by kerbal stats and equipment carried: 45kg for a kerbal, 94kg with full jetpack and parachute.
		public static float KerbalMass = 1000f * 0.03125f; // From persistent file for EVA kerbal. Use PhysicsGlobals.KerbalCrewMass instead?

		public static SphereCollider KerbalFeetCollider;
		/*public static GameObject KerbalFeet;

        public static PhysicMaterial KerbalFeetPhysics;
        public static Rigidbody KerbalFeetRigidbody;
        public static Renderer KerbalFeetRenderer;*/

#if Experimental
		public static GameObject KerbalWorldSpace;
		public static Collider KerbalWorldSpaceCollider;
		public static PhysicMaterial KerbalWorldSpacePhysics;
		public static Rigidbody KerbalWorldSpaceRigidbody;
		public static Renderer KerbalWorldSpaceRenderer;
#endif

		public static bool WearingHelmet { get; private set; }
		public bool buckled = true;
		public bool cameraPositionLocked = false;
		public Quaternion previousRotation = new Quaternion();
		public static ProtoCrewMember ActiveKerbal;
		public InternalSeat OriginalSeat = null;
		public InternalSeat TargetedSeat = null;
		public static bool Gravity = true;
		public static bool EnablePhysics = true;
#if Experimental
		public static bool CanHoldItems = false;
#endif
		public static Vector3 flightForces;

		private CameraManager.CameraMode _lastCameraMode = CameraManager.CameraMode.Flight;
		private Vector3 _previousPos = Vector3.zero;
		private bool _changingCurrentIvaCrew = false;

		public static KerbalIvaController _instance;
		public static KerbalIvaController Instance { get { return _instance; } }

		public delegate void GetInputDelegate(ref IVAInput input);
		public static GetInputDelegate GetInput = GetKeyboardInput;

		void Start()
		{
			CreateCameraCollider();
			SetCameraToSeat();

			GameEvents.OnCameraChange.Add(OnCameraChange);
			_instance = this;
		}

		void OnDestroy()
		{
			GameEvents.OnCameraChange.Remove(OnCameraChange);
			_instance = null;
		}

		public void Update()
		{
			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
			{
				if (ActiveKerbal != null) // Switching away from IVA.
				{
					InputLockManager.RemoveControlLock("FreeIVA");

					// Return the kerbal to its original seat.
					TargetedSeat = ActiveKerbal.seat;
					var targetPart = FreeIva.FindPartWithEmptySeat(FreeIva.CurrentPart);
					if (targetPart != null)
					{
						// disabling this for now, because it's so different from the behavior when pressing C
						// but it works, if we want to bring it back
						// TargetedSeat = PropBuckleButton.FindClosestSeat(targetPart.internalModel, KerbalIva.transform.position, float.MaxValue);
					}
					if (!buckled)
						Buckle();
				}
			}
			else
			{
				// In IVA.
				if (_lastCameraMode != CameraManager.CameraMode.IVA)
				{
					// Switching to IVA.
					FreeIva.EnableInternals();
					UpdateActiveKerbal();//false);
					SetCameraToSeat();
					FreeIva.SetRenderQueues(FreeIva.CurrentPart);
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

				// TODO: Doesn't get mouse input when in FixedUpdate.
				// Split this out to flags set in Update and acted upon in FixedUpdate.
				// note from JonnyOThan: don't do that, because FixedUpdate happens before Update
				// well, ideally we should get input before FixedUpdate and then apply forces in FixedUpdate
				input = new IVAInput();
				GetInput(ref input);
				ApplyInput(input);

				if (!buckled)
				{
					InternalCamera.Instance.ManualReset(false);
					InternalCamera.Instance.transform.localPosition = Vector3.zero;
					InternalCamera.Instance.transform.localRotation = Quaternion.identity;
					previousRotation = InternalCamera.Instance.transform.rotation;
					// Normally the InternalCamera's transform is copied to the FlightCamera at the end of InternalCamera.Update, which will have happened right before this component updates.
					// So we need to make sure the latest internal camera rotation gets copied to the flight camera.
					FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
					FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
				}


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

		CameraManager.CameraMode _previousCameraMode = CameraManager.CameraMode.Flight;
		public void OnCameraChange(CameraManager.CameraMode cameraMode)
		{
			if (cameraMode != CameraManager.CameraMode.IVA && _previousCameraMode == CameraManager.CameraMode.IVA)
			{
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
		public Vector3 GetFlightAccelerationWorldSpace()
		{
			Vector3 gravityAccel = FlightGlobals.getGeeForceAtPosition(KerbalIva.transform.position);
			Vector3 centrifugalAccel = FlightGlobals.getCentrifugalAcc(KerbalIva.transform.position, FreeIva.CurrentPart.orbit.referenceBody);
			Vector3 coriolisAccel = FlightGlobals.getCoriolisAcc(FreeIva.CurrentPart.vessel.rb_velocity + Krakensbane.GetFrameVelocityV3f(), FreeIva.CurrentPart.orbit.referenceBody);

			return gravityAccel + centrifugalAccel + coriolisAccel;
		}

		public Vector3 GetFlightAccelerationInternalSpace()
		{
			Vector3 accelWorldSpace = GetFlightAccelerationWorldSpace();

			float magnitude = accelWorldSpace.magnitude;
			Quaternion direction = Quaternion.LookRotation(accelWorldSpace);
			Quaternion internalDirection = InternalSpace.WorldToInternal(direction);
			return internalDirection * Vector3.forward * magnitude;
		}

		public void FixedUpdate()
		{
			//FallthroughCheck();
			if (!buckled)
			{
				UpdateOrientation(input.RotationInputEuler);
				UpdatePosition(input.MovementThrottle, input.Jump);
			}

			input.Jump = false;
		}

		private void FallthroughCheck()
		{
			if (FreeIva.CurrentPart != null && FreeIva.CurrentPart.Rigidbody != null)
			{
				var velocityRelativeToPart = (FreeIva.CurrentPart.Rigidbody.velocity - KerbalRigidbody.velocity).magnitude;
				if (velocityRelativeToPart > Settings.MaxVelocityRelativeToPart)
				{
					buckled = true;
					InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;
					CameraManager.Instance.SetCameraFlight();
					KerbalIva.GetComponentCached<SphereCollider>(ref KerbalCollider);
					KerbalCollider.enabled = false;
					HideCurrentKerbal(false);
					DisablePartHighlighting(false);
					InputLockManager.RemoveControlLock("FreeIVA");

					Gravity = false;
					KerbalCollider.enabled = true;
					ScreenMessages.PostScreenMessage("It was all just a dream...", 3f, ScreenMessageStyle.LOWER_CENTER);
				}
			}
		}

		private void CreateCameraCollider()
		{
			KerbalIva = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			KerbalIva.name = "Kerbal collider";
			KerbalIva.GetComponentCached<SphereCollider>(ref KerbalCollider);
			KerbalCollider.enabled = false;
			KerbalCollider.material.staticFriction = 0.0f;
			KerbalCollider.material.dynamicFriction = 0.0f;
			KerbalCollider.material.bounciness = 0.0f;
			KerbalCollider.material.frictionCombine = PhysicMaterialCombine.Minimum;
			KerbalCollider.material.bounceCombine = PhysicMaterialCombine.Minimum;
			KerbalRigidbody = KerbalIva.AddComponent<Rigidbody>();
			KerbalRigidbody.useGravity = false;
			KerbalRigidbody.mass = KerbalMass;
			KerbalRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
			// Rotating the object would offset the rotation of the controls from the camera position.
			KerbalRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			KerbalCollisionTracker = KerbalIva.AddComponent<IvaCollisionTracker>();
			KerbalCollider.isTrigger = false;
			KerbalIva.layer = (int)Layers.Kerbals; //KerbalCollider.layer = (int)Layers.Kerbals; 2021-02-26
			var renderer = KerbalIva.GetComponent<Renderer>();
			renderer.enabled = false;

			KerbalCollider.radius = Settings.NoHelmetSize;

			KerbalFeetCollider = KerbalIva.AddComponent<SphereCollider>();
			KerbalFeetCollider.enabled = false;
			KerbalFeetCollider.isTrigger = false;
			KerbalFeetCollider.radius = Settings.NoHelmetSize * 0.9f;
			KerbalFeetCollider.center = new Vector3(0, -Settings.NoHelmetSize, 0);

			/*KerbalFeet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            KerbalFeet.name = "Kerbal feet collider";
            KerbalFeet.GetComponentCached<Collider>(ref KerbalFeetCollider);
            KerbalFeetPhysics = KerbalFeetCollider.material;
            KerbalFeetRigidbody = KerbalFeet.AddComponent<Rigidbody>();
            KerbalFeetRigidbody.useGravity = false;
            KerbalFeetRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            
            KerbalFeet.GetComponentCached<Renderer>(ref KerbalFeetRenderer);
            KerbalFeetRenderer.enabled = false;*/

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

		private void SetCameraToSeat()
		{
			if (InternalCamera.Instance == null)
			{
				Debug.LogError("InternalCamera was null");
				Debug.Log("Searching for camera: " + InternalCamera.FindObjectOfType<Camera>());
				return;
			}
			KerbalIva.transform.position = InternalCamera.Instance.transform.position; //forward;// FlightCamera.fetch.transform.forward;
																					   //previousRotation = InternalCamera.Instance.transform.rotation;// *Quaternion.AngleAxis(-90, InternalCamera.Instance.transform.right);
			previousRotation = Quaternion.AngleAxis(-90, InternalCamera.Instance.transform.right) * InternalCamera.Instance.transform.localRotation; // Fixes "unbuckling looking at feet"
		}

		public struct IVAInput
		{
			public Vector3 MovementThrottle;
			public Vector3 RotationInputEuler;
			public bool Unbuckle;
			public bool Buckle;
			public bool ToggleCameraLock;
			public bool ToggleHatch;
			public bool ToggleFarHatch;
			public bool Jump;
		}

		IVAInput input;

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

			if (!Instance.buckled && !FreeIva.Paused)
			{
				if (!Instance.cameraPositionLocked)
				{
					// TODO: add key controls for turning
					input.RotationInputEuler.z = GetKeyInputAxis(Settings.RollCCWKey, Settings.RollCWKey);
				}

				// camera
				input.RotationInputEuler.x = InternalCamera.Instance.currentPitch;
				input.RotationInputEuler.y = InternalCamera.Instance.currentRot;

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

			if (input.Unbuckle)
			{
				Unbuckle();
			}
			else if (input.Buckle)
			{
				Buckle();
			}

			if (!buckled && !FreeIva.Paused)
			{
				TargetSeats();
				TargetHatches(input.ToggleHatch, input.ToggleFarHatch);
				if (_lastCameraMode != CameraManager.CameraMode.IVA)
					InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, "FreeIVA");
			}
		}

		//private bool _reseatingCrew = false;
		public void Buckle()
		{
			if (TargetedSeat == null)
				return;

			Debug.Log(ActiveKerbal.name + " is entering seat " + TargetedSeat.transform.name + " in part " + FreeIva.CurrentPart);

			InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;

			buckled = true;
			MoveKerbalToSeat(ActiveKerbal, TargetedSeat);
			KerbalIva.GetComponentCached<SphereCollider>(ref KerbalCollider);
			KerbalCollider.enabled = false;
			KerbalFeetCollider.enabled = false;
			HideCurrentKerbal(false);
			DisablePartHighlighting(false);
			InputLockManager.RemoveControlLock("FreeIVA");
			//ActiveKerbal.flightLog.AddEntry("Buckled");
			ScreenMessages.PostScreenMessage("Buckled", 1f, ScreenMessageStyle.LOWER_CENTER);
		}

		public void ReturnToSeat()
		{
			// some of this stuff should probably get moved to a common function
			KerbalIva.GetComponentCached<SphereCollider>(ref KerbalCollider);
			KerbalCollider.enabled = false;
			KerbalFeetCollider.enabled = false;
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

		public void MoveKerbalToSeat(ProtoCrewMember crewMember, InternalSeat newSeat)
		{
			var oldSeat = crewMember.seat;
			var sourceModel = oldSeat.internalModel;
			var destModel = newSeat.internalModel;

			// remove the kerbal from their old seat in a non-destructive way
			oldSeat.kerbalRef = null;
			sourceModel.UnseatKerbalAt(oldSeat);

			// transferring seats in the same part
			if (sourceModel == destModel)
			{
				destModel.SitKerbalAt(crewMember, newSeat);
			}
			else
			{
				sourceModel.part.RemoveCrewmember(crewMember);
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

			Quaternion cameraRotationSurface = Quaternion.Inverse(FlightGlobals.GetFoR(FoRModes.SRF_NORTH)) * InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

			currentRelativeOrientation = cameraRotationSurface.eulerAngles;
			currentRelativeOrientation.z = 0; // roll

			if (currentRelativeOrientation.x > 180)
			{
				currentRelativeOrientation.x -= 360;
			}
			if (currentRelativeOrientation.x > 90 || currentRelativeOrientation.x < -90)
			{
				currentRelativeOrientation.y += 180;
				currentRelativeOrientation.x = Mathf.Clamp(currentRelativeOrientation.x, -90, 90);
			}

			previousRotation = InternalCamera.Instance.transform.rotation;
			InternalCamera.Instance.ManualReset(false);

			_previousPos = Vector3.zero;
			FreeIva.EnableInternals();
			UpdateActiveKerbal();
			FreeIva.SetRenderQueues(FreeIva.CurrentPart);
			FreeIva.InitialPart = FreeIva.CurrentPart;
			OriginalSeat = ActiveKerbal.seat;

			HideCurrentKerbal(true);

			KerbalIva.transform.position = ActiveKerbal.KerbalRef.eyeTransform.position;
			KerbalIva.transform.rotation = previousRotation;
			// The Kerbal's eye transform is the InternalCamera's parent normally, not InternalSpace.Instance as previously thought.
			InternalCamera.Instance.transform.parent = KerbalIva.transform;
			InternalCamera.Instance.transform.localPosition = Vector3.zero;
			InternalCamera.Instance.transform.localRotation = Quaternion.identity;

			InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, "FreeIVA");
			//ActiveKerbal.flightLog.AddEntry("Unbuckled");
			ScreenMessages.PostScreenMessage("Unbuckled", 1f, ScreenMessageStyle.LOWER_CENTER);
			KerbalCollider.enabled = true;
			KerbalFeetCollider.enabled = Gravity;
			buckled = false;

			DisablePartHighlighting(true);
		}

		private void UpdateActiveKerbal()
		{
			ActiveKerbal = CameraManager.Instance.IVACameraActiveKerbal.protoCrewMember;
			if (ActiveKerbal.KerbalRef != null && ActiveKerbal.KerbalRef.InPart != null)
				FreeIva.UpdateCurrentPart(ActiveKerbal.KerbalRef.InPart);
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

		// TODO: Replace this with clickable interaction colliders.
		public bool IsTargeted(Vector3 position)
		{
			float distance = Vector3.Distance(position, InternalCamera.Instance.transform.position);
			float angle = Vector3.Angle(position - InternalCamera.Instance.transform.position, InternalCamera.Instance.transform.forward);

			if (angle > 90)
				return false;

			float radius = (distance * Mathf.Sin(angle * Mathf.Deg2Rad)) / Mathf.Sin((90 - angle) * Mathf.Deg2Rad);
			return (radius <= Settings.ObjectInteractionRadius);
		}

		// TODO: Replace this with clickable interaction colliders.
		public void TargetSeats()
		{
			float closestDistance = Settings.MaxInteractDistance;
			TargetedSeat = null;
			if (FreeIva.CurrentPart.internalModel == null)
				return;

			for (int i = 0; i < FreeIva.CurrentPart.internalModel.seats.Count; i++)
			{
				if (FreeIva.CurrentPart.internalModel.seats[i].taken && !FreeIva.CurrentPart.internalModel.seats[i].crew.Equals(ActiveKerbal))
					continue;

				if (FreeIva.CurrentPart.internalModel.seats[i].seatTransform == null)
					continue; // Some parts were originally designed to have more seats, but later had their transforms removed without changing the seat count.

				if (IsTargeted(FreeIva.CurrentPart.internalModel.seats[i].seatTransform.position))
				{
					float distance = Vector3.Distance(FreeIva.CurrentPart.internalModel.seats[i].seatTransform.position,
						InternalCamera.Instance.transform.position);
					if (distance < closestDistance)
					{
						TargetedSeat = FreeIva.CurrentPart.internalModel.seats[i];
						closestDistance = distance;
					}
				}
			}

			if (TargetedSeat != null)
				ScreenMessages.PostScreenMessage("Enter seat [" + Settings.UnbuckleKey + "]", 0.1f, ScreenMessageStyle.LOWER_CENTER);
		}

		void ConsiderHatch(ref FreeIvaHatch targetedHatch, ref float closestDistance, FreeIvaHatch newHatch)
		{
			if (newHatch.enabled && IsTargeted(newHatch.WorldPosition))
			{
				float distance = Vector3.Distance(newHatch.WorldPosition, InternalCamera.Instance.transform.position);
				if (distance < closestDistance)
				{
					targetedHatch = newHatch;
					closestDistance = distance;
				}
			}
		}

		void TargetHatchesInPart(Part part, ref FreeIvaHatch targetedHatch, ref float closestDistance)
		{
			InternalModuleFreeIva ivaModule = InternalModuleFreeIva.GetForModel(part.internalModel);
			while (ivaModule != null)
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

			if (FreeIva.CurrentInternalModuleFreeIva != null && FreeIva.CurrentInternalModuleFreeIva.Hatches.Count != 0)
			{
				foreach (FreeIvaHatch h in FreeIva.CurrentInternalModuleFreeIva.Hatches)
				{
					ConsiderHatch(ref targetedHatch, ref closestDistance, h);

					// if this hatch is open, consider other hatches in the adjacent part
					if (h.IsOpen && h.ConnectedHatch != null)
					{
						TargetHatchesInPart(h.ConnectedHatch.part, ref targetedHatch, ref closestDistance);
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
					else if (targetedHatch.attachNodeId != string.Empty)
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

		bool UseRelativeMovement()
		{
			// eventually we might want to include flying in atmosphere, etc
			return FlightGlobals.ActiveVessel.LandedOrSplashed;
		}

		public Vector3 currentRelativeOrientation;
        private IvaCollisionTracker KerbalCollisionTracker;

        public void UpdateOrientation(Vector3 rotationInput)
		{
			/*Vector3 gForce = FlightGlobals.getGeeForceAtPosition(FlightCamera.fetch.transform.position);
            Vector3 gForceInternal = InternalSpace.WorldToInternal(gForce);

            
            Quaternion cameraRotation = InternalCamera.Instance.transform.rotation;


            previousRotation = cameraRotation;
            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);*/

			Vector3 angularSpeed = Time.fixedDeltaTime * new Vector3(
				rotationInput.x * Settings.PitchSpeed,
				rotationInput.y * Settings.YawSpeed,
				rotationInput.z * Settings.RollSpeed);

			if (UseRelativeMovement())
			{
				currentRelativeOrientation += angularSpeed;
				currentRelativeOrientation.z = 0;

				currentRelativeOrientation.x = Mathf.Clamp(currentRelativeOrientation.x, -90, 90);
				currentRelativeOrientation.y = currentRelativeOrientation.y % 360;

				KerbalIva.transform.rotation = InternalSpace.WorldToInternal(FlightGlobals.GetFoR(FoRModes.SRF_NORTH) * Quaternion.Euler(currentRelativeOrientation));
			}
			else
			{
				

				Quaternion rotYaw = Quaternion.AngleAxis(angularSpeed.y, previousRotation * Vector3.up);
				Quaternion rotPitch = Quaternion.AngleAxis(angularSpeed.x, previousRotation * Vector3.right);// *Quaternion.Euler(0, 90, 0);
				Quaternion rotRoll = Quaternion.AngleAxis(angularSpeed.z, previousRotation * Vector3.forward);

				KerbalIva.transform.rotation = rotRoll * rotPitch * rotYaw * previousRotation;
			}
		}

		float GetMaxDeltaSpeed(bool accelerating, bool isGrounded)
		{
			float result;
			if (accelerating)
			{
				result = Settings.MaxAcceleration;
			}
			else
			{
				result = isGrounded ? Settings.MaxDecelerationGrounded : Settings.MaxDecelerationWeightless;
			}

			return result * Time.fixedDeltaTime;
		}

		List<ContactPoint> contactPoints = new List<ContactPoint>();


		bool GetGroundPlane(Vector3 gravity, out Plane plane)
		{
			Vector3 up = -Vector3.Normalize(gravity);
			float cosWalkableSlope = Mathf.Cos(Mathf.Deg2Rad * Settings.WalkableSlope);

			Vector3 accumulatedPosition = Vector3.zero;
			Vector3 accumulatedNormal = Vector3.zero;
			int contactPointCount = 0;

			foreach (var collision in KerbalCollisionTracker.Collisions)
			{
				if (collision.contactCount > contactPoints.Capacity)
				{
					contactPoints.Capacity = collision.contactCount;
				}

				contactPoints.Clear();

				collision.GetContacts(contactPoints);

				foreach (var contactPoint in contactPoints)
				{
					if (Vector3.Dot(contactPoint.normal, up) >= cosWalkableSlope)
					{
						accumulatedNormal += contactPoint.normal;
						accumulatedPosition += contactPoint.point;
						++contactPointCount;

						Debug.DrawRay(contactPoint.point, contactPoint.normal, Color.red, 0, true);
					}
				}
			}

			if (contactPointCount > 0)
			{
				accumulatedNormal.Normalize();
				accumulatedPosition /= contactPointCount;

				plane = new Plane(accumulatedNormal, accumulatedPosition);
				return true;
			}

			plane = new Plane();
			return false;
		}

		private void UpdatePosition(Vector3 movementThrottle, bool jump)
		{
			if (!EnablePhysics) return;

			Vector3 desiredLocalSpeed = new Vector3(
				movementThrottle.x * Settings.HorizontalSpeed,
				movementThrottle.y * Settings.VerticalSpeed,
				movementThrottle.z * Settings.ForwardSpeed);

			Vector3 flightAccel = GetFlightAccelerationInternalSpace();
			bool useGroundSystem = UseRelativeMovement();
			bool tryingToMove = desiredLocalSpeed != Vector3.zero || jump;

			Quaternion orientation = useGroundSystem
				// take the yaw angle but nothing else (maintain global up)
				? orientation = InternalSpace.WorldToInternal(FlightGlobals.GetFoR(FoRModes.SRF_NORTH) * Quaternion.Euler(0, currentRelativeOrientation.y, 0))
				: previousRotation;

			// Make the movement relative to the camera rotation.
			Vector3 desiredWorldVelocity = orientation * desiredLocalSpeed;
			bool grounded= false;

			if (useGroundSystem)
			{
				grounded = GetGroundPlane(flightAccel, out Plane groundPlane);

				// for now, allow free movement vertically
				if (movementThrottle.y == 0 && Gravity)
				{
					float gravityScale = grounded ? 0.1f : 1f;
					KerbalRigidbody.AddForce(gravityScale * flightAccel, ForceMode.Acceleration);
				}

				if (grounded)
				{
					// rotate the desired world velocity along the ground plane
					float desiredSpeed = desiredWorldVelocity.magnitude;
					desiredWorldVelocity = Vector3.ProjectOnPlane(desiredWorldVelocity, groundPlane.normal);
					desiredWorldVelocity = desiredWorldVelocity.normalized * desiredSpeed;

					if (jump)
					{
						// Jump in the opposite direction to gravity.
						KerbalRigidbody.AddForce(-flightAccel.normalized * Settings.JumpForce, ForceMode.VelocityChange);
					}
				}

			}

			KerbalIva.GetComponentCached<Rigidbody>(ref KerbalRigidbody);
			
			Vector3 velocityDelta = desiredWorldVelocity - KerbalRigidbody.velocity;

			// if we're not on the ground, don't change velocity in the vertical direction
			if (useGroundSystem && !grounded && movementThrottle.y == 0)
			{
				velocityDelta = Vector3.ProjectOnPlane(velocityDelta, flightAccel.normalized);
			}

			float desiredDeltaSpeed = velocityDelta.magnitude;
			float maxDeltaSpeed = GetMaxDeltaSpeed(tryingToMove, useGroundSystem);
			if (desiredDeltaSpeed > maxDeltaSpeed)
			{
				velocityDelta = velocityDelta.normalized * maxDeltaSpeed;
			}

			if (KerbalRigidbody.velocity.magnitude < 0.02f && !tryingToMove && desiredDeltaSpeed < maxDeltaSpeed && (!useGroundSystem || grounded))
			{
				KerbalRigidbody.Sleep();
			}
			else
			{
				KerbalRigidbody.AddForce(velocityDelta, ForceMode.VelocityChange);
			}

#if Experimental
			// Move the world space collider.
			KerbalWorldSpaceCollider.GetComponentCached<Rigidbody>(ref KerbalWorldSpaceRigidbody);
			//KerbalWorldSpaceRigidbody.MovePosition(KerbalCollider.transform.localPosition);
			KerbalWorldSpaceRigidbody.MovePosition(InternalSpace.InternalToWorld(KerbalCollider.transform.localPosition));
#endif
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


		public static void HelmetOn()
		{
			WearingHelmet = true;
			KerbalCollider.radius = Settings.HelmetSize;
		}

		public static void HelmetOff()
		{
			WearingHelmet = false;
			KerbalCollider.radius = Settings.NoHelmetSize;
		}
	}
}
