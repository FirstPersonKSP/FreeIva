using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;
using System.Collections;
using KSP.UI;
using KSP.UI.Screens;

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
	[KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
	public class KerbalIvaAddon : MonoBehaviour
	{
		// Localisation strings
		private static string str_GravityEnabled = Localizer.Format("#FreeIVA_Message_GravityEnabled");
		private static string str_GravityDisabled = Localizer.Format("#FreeIVA_Message_GravityDisabled");
		private static string str_Unbuckle = Localizer.Format("#FreeIVA_Message_Unbuckle");
		private static string str_Buckled = Localizer.Format("#FreeIVA_Message_Buckled");
		private static string str_PartCannotUnbuckle = Localizer.Format("#FreeIVA_Message_PartCannotUnbuckle");
		private static string str_Unbuckled = Localizer.Format("#FreeIVA_Message_Unbuckled");
		private static string str_EnterSeat = Localizer.Format("#FreeIVA_Message_EnterSeat");
		private static string str_HatchLocked = Localizer.Format("#FreeIVA_Message_HatchLocked");
		private static string str_GoEVA = Localizer.Format("#FreeIVA_Message_GoEVA");
		private static string str_HatchBlocked = Localizer.Format("#FreeIVA_Message_HatchBlocked");
		private static string str_CloseHatch = Localizer.Format("#FreeIVA_Message_CloseHatch");
		private static string str_OpenHatch = Localizer.Format("#FreeIVA_Message_OpenHatch");
        public KerbalIvaController KerbalIva;
#if Experimental
		public GameObject KerbalWorldSpace;
		public Collider KerbalWorldSpaceCollider;
		public PhysicMaterial KerbalWorldSpacePhysics;
		public Rigidbody KerbalWorldSpaceRigidbody;
		public Renderer KerbalWorldSpaceRenderer;
#endif

		public bool buckled = true;
		public ProtoCrewMember ActiveKerbal => CameraManager.Instance.ivaCameraActiveKerbal?.protoCrewMember;
		public InternalSeat OriginalSeat = null;
		public InternalSeat TargetedSeat = null;
		public bool Gravity = true;
		public static Vector3 flightForces;

		private CameraManager.CameraMode _lastCameraMode = CameraManager.CameraMode.Flight;
		private bool _changingCurrentIvaCrew = false;

		public static KerbalIvaAddon _instance;
		public static KerbalIvaAddon Instance { get { return _instance; } }

		public delegate void GetInputDelegate(ref IVAInput input);
		public static GetInputDelegate GetInput = GetKeyboardInput;

		ShadowCascadeTweak cascadeTweak;

		void Start()
		{
			_instance = this;
			GameEvents.OnCameraChange.Add(OnCameraChange);
			GameEvents.OnIVACameraKerbalChange.Add(OnIVACameraKerbalChange);
			GameEvents.onVesselChange.Add(OnVesselChange);
			GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountChanged);
			GameEvents.onSameVesselDock.Add(OnSameVesselDockingChange);
			GameEvents.onSameVesselUndock.Add(OnSameVesselDockingChange);

			if (HighLogic.LoadedSceneIsEditor)
			{
				var cameraManager = gameObject.AddComponent<CameraManager>();
				cameraManager.enabled = false;
				var internalSpace = gameObject.AddComponent<InternalSpace>();
				var internalCameraObject = new GameObject("internalCamera");
				internalCameraObject.transform.SetParent(internalSpace.transform, false);
				var internalCamera = internalCameraObject.AddComponent<Camera>();
				internalCamera.clearFlags = CameraClearFlags.Depth;
				internalCamera.cullingMask = (1 << 16) | (1 << 20);
				internalCamera.depth = 3;
				internalCamera.eventMask = -65537;
				internalCamera.farClipPlane = 50;
				internalCamera.nearClipPlane = 0.01f;
				internalCameraObject.AddComponent<InternalCamera>();
				gameObject.AddComponent<CrewHatchController>();

				FlightGlobals.fetch.enabled = false;
			}

			Settings.LoadSettings();
			InternalModuleFreeIva.RefreshDepthMasks();

			Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.InternalSpace);
			Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.Kerbals, false);

			var ivaSun = InternalSpace.Instance.transform.Find("IVASun")?.GetComponent<IVASun>();
			if (ivaSun)
			{
				ivaSun.ivaLight.shadowBias = 0;
				ivaSun.ivaLight.shadowNormalBias = 0;
				ivaSun.ivaLight.shadows = LightShadows.Hard;
			}

			CreateCameraCollider();

			// cascadeTweak = InternalCamera.Instance._camera.gameObject.GetOrAddComponent<ShadowCascadeTweak>();
		}

		void OnDestroy()
		{
			GameObject.Destroy(KerbalIva);
			GameEvents.OnCameraChange.Remove(OnCameraChange);
			GameEvents.OnIVACameraKerbalChange.Remove(OnIVACameraKerbalChange);
			GameEvents.onVesselChange.Remove(OnVesselChange);
			GameEvents.onVesselPartCountChanged.Remove(OnVesselPartCountChanged);
			GameEvents.onSameVesselDock.Remove(OnSameVesselDockingChange);
			GameEvents.onSameVesselUndock.Remove(OnSameVesselDockingChange);

			_instance = null;
			KerbalIva = null;
		}

		void OnGUI()
		{
			GuiTutorial.Gui(GetInstanceID());
		}

		public void Update()
		{
			// if we just respawned the crew because we exited ProbeControlRoom, mark them all as "alive" so you don't have to wait 3 seconds to hit C to re-enter IVA
			if (_markCrewAlive)
			{
				if (FlightGlobals.ActiveVessel != null)
				{
					foreach (var pcm in FlightGlobals.ActiveVessel.crew)
					{
						if (pcm.KerbalRef != null && pcm.KerbalRef.state == Kerbal.States.NO_SIGNAL)
						{
							pcm.KerbalRef.state = Kerbal.States.ALIVE;
						}
					}
				}

				_markCrewAlive = false;
			}

			if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(Settings.ToggleGravityKey))
			{
				Gravity = !Gravity && Settings.EnableCollisions;
				ScreenMessages.PostScreenMessage(Gravity ? str_GravityEnabled : str_GravityDisabled, 1f, ScreenMessageStyle.LOWER_CENTER);
			}

			if (GameSettings.CAMERA_MODE.GetKeyDown(true))
			{
				if (!buckled)
				{
					ReturnToSeat();
				}
				else if (HighLogic.LoadedSceneIsEditor)
				{
					StopEditorIVA();
				}
			}

			// if the kerbal disappeared somehow (generally from dying in a separate part), reset the camera
			if (!buckled && ActiveKerbal.KerbalRef == null)
			{
				ReturnToSeat();
				CameraManager.Instance.SetCameraFlight();
			}

			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
			{
				GuiTutorial.Active = false;
			}
			else
			{
				// Switching to IVA.
				if (_lastCameraMode != CameraManager.CameraMode.IVA)
				{
					GuiTutorial.Active = true;

					FreeIva.EnableInternals();
					UpdateActiveKerbal();

					var freeIvaModule = FreeIva.CurrentPart.GetModule<ModuleFreeIva>();
					if (freeIvaModule != null && freeIvaModule.allowsUnbuckling)
					{
						if (buckled)
						{
							ScreenMessages.PostScreenMessage($"{str_Unbuckle} [{Settings.UnbuckleKey}]", 2f, ScreenMessageStyle.LOWER_CENTER);
						}
						else
						{
							ScreenMessages.PostScreenMessage($"{str_EnterSeat} [{GameSettings.CAMERA_MODE.primary}]", 2f, ScreenMessageStyle.LOWER_CENTER);
						}
					}
				}

				// Check if we're changing crew member using the 'V' key.
				if (GameSettings.CAMERA_NEXT.GetKeyDown())
					_changingCurrentIvaCrew = true;
				else
				{
					if (_changingCurrentIvaCrew)
					{
						UpdateActiveKerbal();
						_changingCurrentIvaCrew = false;
					}
				}

				input = new IVAInput();
				GetInput(ref input);

				JumpLatched = JumpLatched && input.Jump;
				input.Jump = input.Jump && !JumpLatched;
			}
			_lastCameraMode = CameraManager.Instance.currentCameraMode;
		}

		// This must be after the internal camera updates
		private void LateUpdate()
		{
			if (m_internalVisibilityDirty)
			{
				FreeIva.EnableInternals();
				if (KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.refreshCoroutine != null)
				{
					KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.StopCoroutine(KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.refreshCoroutine);
				}
				m_internalVisibilityDirty = false;
			}

			if (FreeIva.Paused) return;

			ApplyInput(input);

			if (!buckled)
			{
				Vector3 cameraWorldPosition = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
				Quaternion cameraWorldRotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

				// Normally the InternalCamera's transform is copied to the FlightCamera at the end of InternalCamera.Update, which will have happened right before this component updates.
				// So we need to make sure the latest internal camera rotation gets copied to the flight camera.
				FlightCamera.fetch.transform.SetPositionAndRotation(cameraWorldPosition, cameraWorldRotation);
			}
		}

		bool _markCrewAlive = false;
		CameraManager.CameraMode _previousCameraMode = CameraManager.CameraMode.Flight;
		public void OnCameraChange(CameraManager.CameraMode cameraMode)
		{
			if (cameraMode == CameraManager.CameraMode.IVA)
			{
				// this can sometimes get destroyed if it was attached to the internal (like in a centrifuge).  just handle recreating it here.
				if (KerbalIva == null)
				{
					CreateCameraCollider();
				}

				// STOP HIDING INTERNALS DAMMIT
				var ivaOverlay = KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance?.ivaOverlay;
				if (ivaOverlay != null)
				{
					ivaOverlay.onDismiss = null;
				}
				FreeIva.EnableInternals();
			}
			else if (cameraMode != CameraManager.CameraMode.IVA && _previousCameraMode == CameraManager.CameraMode.IVA)
			{
				InternalModuleFreeIva.RefreshDepthMasks();
				InternalModuleFreeIva.HideAllTubes();

				InputLockManager.RemoveControlLock("FreeIVA");

				// if ProbeControlRoom was enabled, we may have deactivated all the internal models.  The stock game doesn't expect this, so restore them
				if (cameraMode == CameraManager.CameraMode.Flight && FlightGlobals.ActiveVessel != null)
				{
					foreach (var p in FlightGlobals.ActiveVessel.parts)
					{
						if (!FreeIva.PartIsProbeCore(p) && p.internalModel != null && !p.internalModel.gameObject.activeSelf)
						{
							p.internalModel.gameObject.SetActive(true);
							p.internalModel.SpawnCrew();

							// make sure kerbals and seats get linked up
							// this is similar to logic in InternalModel.Initialize
							foreach (var pcm in p.protoModuleCrew)
							{
								if (pcm.seatIdx == -1)
								{
									pcm.seatIdx = p.internalModel.GetNextAvailableSeatIndex();
								}
								if (pcm.seatIdx != -1)
								{
									p.internalModel.AssignToSeat(pcm);
								}
							}	

							_markCrewAlive = true;
						}
					}
				}

#if Experimental
				KerbalWorldSpaceCollider.enabled = false;
#endif
				if (!buckled && ActiveKerbal != null)
				{
					ReturnToSeatInternal(false);
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

		private void OnIVACameraKerbalChange(Kerbal data)
		{
			UpdateActiveKerbal();
		}

		private void OnVesselChange(Vessel vessel)
		{
			if (!buckled)
			{
				ReturnToSeatInternal(false);
			}

			if (vessel.evaController != null)
			{
				StartCoroutine(ModifyEvaFsm(vessel.evaController));
			}
		}

		private IEnumerator ModifyEvaFsm(KerbalEVA kerbalEva)
		{
			while (!kerbalEva.Ready)
			{
				yield return null;
			}

			kerbalEva.On_boardPart.OnEvent = () => FreeIva.BoardPartFromAirlock(kerbalEva, true);
		}

		bool m_internalVisibilityDirty = false;

		private void OnVesselPartCountChanged(Vessel vessel)
		{
			m_internalVisibilityDirty = true;
		}

		private void OnSameVesselDockingChange(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> data)
		{
			if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
			{
				m_internalVisibilityDirty = true;
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
			if (KOSPropMonitor.IsLocked())
			{
				return;
			}

			if (Input.GetKeyDown(Settings.UnbuckleKey))
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

			input.SwitchTo = GameSettings.CAMERA_NEXT.GetKeyDown(true);

			if (!Instance.buckled && !FreeIva.Paused)
			{
				// TODO: add key controls for turning
				input.RotationInputEuler.z = GetKeyInputAxis(Settings.RollCCWKey, Settings.RollCWKey);

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
			else if (input.SwitchTo && !buckled)
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
		private void BuckleInternal(bool resetCamera)
		{
			if (TargetedSeat == null || (TargetedSeat.taken && TargetedSeat.crew != ActiveKerbal))
				return;

			Log.Message(ActiveKerbal.name + " is entering seat " + TargetedSeat.transform.name + " in part " + FreeIva.CurrentPart);

			buckled = true;
			FreeIva.MoveKerbalToSeat(ActiveKerbal, TargetedSeat);

			if (resetCamera)
			{
				InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;
				SetCameraIVA(ActiveKerbal.KerbalRef);
			}

			KerbalIva.gameObject.SetActive(false);
			HideCurrentKerbal(false);
			DisablePartHighlighting(false);
			FreeIvaInternalCameraSwitch.SetCameraSwitchesEnabled(true);
			InputLockManager.RemoveControlLock("FreeIVA");
			//ActiveKerbal.flightLog.AddEntry("Buckled");
			ScreenMessages.PostScreenMessage(str_Buckled, 1f, ScreenMessageStyle.LOWER_CENTER);

			PlaySeatBuckleAudio(TargetedSeat);
		}

		public void ReturnToSeat()
		{
			ReturnToSeatInternal(true);
		}

		public void Buckle()
		{
			BuckleInternal(true);
		}

		private void ReturnToSeatInternal(bool resetCamera)
		{
			if (buckled) return;

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
			BuckleInternal(resetCamera);
			ScreenMessages.PostScreenMessage(Localizer.Format("#FreeIVA_Message_KermanReturnedSeat", ActiveKerbal.name), 1f, ScreenMessageStyle.LOWER_CENTER);
		}

		public void SwitchToKerbal()
		{
			if (TargetedSeat == null || TargetedSeat.crew == ActiveKerbal)
			{
				return;
			}

			var targetKerbal = TargetedSeat.kerbalRef;
			ReturnToSeatInternal(false);

			SetCameraIVA(targetKerbal);
		}

		internal static void SetCameraIVA(Kerbal kerbal)
		{
			// SetCameraIVA will actually change to flight mode if called with the current kerbal and already in iva mode, so make sure that doesn't happen
			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA ||
				CameraManager.Instance.ivaCameraActiveKerbal != kerbal)
			{
				CameraManager.Instance.SetCameraIVA(kerbal, true);
			}

			GameEvents.OnIVACameraKerbalChange.Fire(kerbal);

			kerbal.IVAEnable(true);

			FreeIva.EnableInternals(); // SetCameraIVA also calls FlightGlobals.ActiveVessel.SetActiveInternalSpace(activeInternalPart); which will hide all other IVAs
		}

		public void Unbuckle(bool feedbackEnabled = true)
		{
			if (!buckled) return;

			UpdateActiveKerbal();

			var freeIvaModule = FreeIva.CurrentPart.GetModule<ModuleFreeIva>();
			if ((freeIvaModule && !freeIvaModule.allowsUnbuckling) || FreeIva.CurrentInternalModuleFreeIva == null)
			{
				if (feedbackEnabled)
				{
					ScreenMessages.PostScreenMessage(str_PartCannotUnbuckle, 1f, ScreenMessageStyle.LOWER_CENTER);
				}
				return;
			}

			FreeIva.EnableInternals();
			OriginalSeat = ActiveKerbal.seat;

			Gravity = Gravity && Settings.EnableCollisions;

			HideCurrentKerbal(true);

			InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.CAMERAMODES, "FreeIVA");
			//ActiveKerbal.flightLog.AddEntry("Unbuckled");
			KerbalIva.Activate(ActiveKerbal);
			buckled = false;

			// eventually might want to attach this kerbal to the rigid body (or combine their gameobjects etc) but for now this is important to signal to ProbeControlRoom that the kerbal is unbuckled
			ActiveKerbal.KerbalRef.transform.SetParent(null, true);

			if (feedbackEnabled)
			{
				PlaySeatBuckleAudio(OriginalSeat);
				ScreenMessages.PostScreenMessage(str_Unbuckled, 1f, ScreenMessageStyle.LOWER_CENTER);
			}

			DisablePartHighlighting(true);
			FreeIvaInternalCameraSwitch.SetCameraSwitchesEnabled(false);
		}

		private void UpdateActiveKerbal()
		{
			if (ActiveKerbal.KerbalRef != null && ActiveKerbal.seat != null)
			{
				FreeIva.SetCurrentPart(ActiveKerbal.seat.internalModel);
			}
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
					// Since the held prop is a child of the camera, and the camera is a child of the kerbal, make sure we
					// don't accidentally hide the held prop too.
					Transform oldHeldPropParent = PhysicalProp.HeldProp?.transform.parent;
					if (oldHeldPropParent != null)
					{
						PhysicalProp.HeldProp.transform.SetParent(null, true);
					}

					Renderer[] renderers = ActiveKerbal.KerbalRef.GetComponentsInChildren<Renderer>();
					foreach (var r in renderers)
					{
						r.enabled = !hidden;
					}

					if (oldHeldPropParent != null)
					{
						PhysicalProp.HeldProp.transform.SetParent(oldHeldPropParent, true);
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
				Log.Error("Error hiding current kerbal: " + ex.Message + ", " + ex.StackTrace);
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

		public bool IsTargeted(Transform targetTransform, Vector3 targetPosition, ref float closestDistance)
		{
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
				Vector3 targetPosition = seat.seatTransform.position + seat.seatTransform.TransformDirection(localTargetPosition); // can't use TransformPoint because the seat might have scale on it

				if (IsTargeted(seat.seatTransform, targetPosition, ref closestDistance))
				{
					TargetedSeat = seat;
				}
			}

			if (TargetedSeat != null)
			{
				// is someone else here?
				if (TargetedSeat.taken && TargetedSeat.crew != ActiveKerbal)
				{
					ScreenMessages.PostScreenMessage(Localizer.Format("#FreeIVA_Message_CamerSwitchtoKerbal", GameSettings.CAMERA_NEXT.primary, TargetedSeat.crew.name), 0.1f, ScreenMessageStyle.LOWER_CENTER);
				}
				else
				{
					ScreenMessages.PostScreenMessage(str_EnterSeat + " [" + Settings.UnbuckleKey + "]", 0.1f, ScreenMessageStyle.LOWER_CENTER);
				}
			}
		}

		void ConsiderHatch(ref FreeIvaHatch targetedHatch, ref float closestDistance, FreeIvaHatch newHatch)
		{
			if (!newHatch.enabled) return;

			if (newHatch.HandleTransforms == null || newHatch.HandleTransforms.Length == 0)
			{
				if (IsTargeted(newHatch.transform, newHatch.transform.position, ref closestDistance))
				{
					targetedHatch = newHatch;
				}
			}
			else
			{
				// should each hatch specify its own offset point? This code is mostly for the benefit of the BDB LM, where the hatch origins are at the center of the IVA
				foreach (var handleTransform in newHatch.HandleTransforms)
				{
					if (IsTargeted(newHatch.transform, handleTransform.position, ref closestDistance))
					{
						targetedHatch = newHatch;
					}
				}
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
			if (FreeIva.CurrentPart == null) return;

			FreeIvaHatch targetedHatch = null;
			float closestDistance = Settings.MaxInteractDistance;

			for (var internalModule = InternalModuleFreeIva.GetForModel(FreeIva.CurrentPart.internalModel); internalModule != null; internalModule = InternalModuleFreeIva.GetForModel(internalModule.SecondaryInternalModel))
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
				var interaction = targetedHatch.GetInteraction();

				PostHatchInteractionMessage(interaction);

				if (FreeIvaHatch.InteractionAllowed(interaction) && openHatch)
				{
					targetedHatch.SetDesiredOpen(!targetedHatch.DesiredOpen);
				}
			}
		}

		public static void PostHatchInteractionMessage(FreeIvaHatch.Interaction interaction)
		{
			ScreenMessages.PostScreenMessage(GetInteractionString(interaction), 0.1f, ScreenMessageStyle.LOWER_CENTER);
		}

		private static string GetInteractionString(FreeIvaHatch.Interaction interaction)
		{
			switch (interaction)
			{
			case FreeIvaHatch.Interaction.Locked: return str_HatchLocked;
			case FreeIvaHatch.Interaction.EVA: return str_GoEVA + " [" + Settings.OpenHatchKey + "]";
			case FreeIvaHatch.Interaction.Blocked: return str_HatchBlocked;
			case FreeIvaHatch.Interaction.Open: return str_OpenHatch + " [" + Settings.OpenHatchKey + "]";
			case FreeIvaHatch.Interaction.Close: return str_CloseHatch + " [" + Settings.OpenHatchKey + "]";
			default: return string.Empty;
			}
		}

        internal void StartEditorIVA()
        {
			StartCoroutine(StartEditorIVACoroutine());
		}

		IEnumerator StartEditorIVACoroutine()
		{
			var vessel = EditorLogic.fetch.rootPart.gameObject.GetOrAddComponent<Vessel>();
			vessel.enabled = false;
			FlightGlobals.fetch.activeVessel = vessel;
			vessel.parts = new List<Part>();
			EditorLogic.fetch.rootPart.GetComponentsInChildren<Part>(vessel.Parts);
			foreach (Part p in vessel.Parts)
			{
				p.vessel = vessel;
			}
			Component.Destroy(vessel.precalc);
			foreach (var vm in vessel.vesselModules) Component.Destroy(vm);
			vessel.vesselModules.Clear();
			ShipConstruction.ShipManifest.AssignCrewToVessel(EditorLogic.fetch.ship);

			FreeIva.EnableInternals();
			yield return null;
			var kerbal = EditorLogic.fetch.rootPart.protoModuleCrew[0].KerbalRef;
			bool oldControlPointSetting = GameSettings.IVA_RETAIN_CONTROL_POINT;
			CameraManager.Instance.SetCameraIVA_Editor(kerbal, true);
			GameEvents.onHideUI.Fire();
			UIMasterController.Instance.screenMessageCanvas.enabled = true;
			UIMainCamera.Camera.enabled = true;
			EditorPartList.Instance.gameObject.SetActive(false);
		}

		void StopEditorIVA()
		{
			CameraManager.Instance.SetCameraEditor();
			GameEvents.onShowUI.Fire();
			EditorPartList.Instance.gameObject.SetActive(true);
		}
	}
}
