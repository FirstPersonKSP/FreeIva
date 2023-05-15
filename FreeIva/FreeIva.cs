using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

/* Quick list
 * 
 * 2019:
 *   Multiple parts -> colliders only show for the first part.
 *   PropHatch spawns in root part, not current part - Fixed
 *   Colliders drifting apart over time???
 *   World space colliders pushes vessels apart and destroys them.
 * 
 * ModuleFreeIva's OnStart currently only operates on the active vessel. Other craft switched to will have uninitialised hatches.
 * Fix issues with loading, restarting etc.
 * Finish colliders for the rest of the parts.
 * Fix render queue to view through mutliple parts and windows.
 * Fix startup states for hatches, hide colliders.
 * Set up visible filler colliders.
 * Split cfg files out to individual parts.
 * Move cfg entries to the Internal rather than the Part.
 * 
 * 2021-02-25, KSP 1.11.1
 * IVA locations aren't affected by physics flexing between parts, always located relative to the root part.
 * 
 * Roadmap:
 * 1. Get gravity working, with legs.
 *      Some drifting of vertical over time?
 *      Capsule collider needs to be oriented correctly (freeze axes) and replaced with sphere for zero-gravity.
 *      Camera height needs to be raised from centre of capsule.
 * 2. Get multiple instances of the same part working.
 * 3. Get hatches and colliders set up for the stock parts.
 * 4. Documentation for users and modellers.
 * 5. Release!
 * - Mask models for stock parts: Need a mask similar to the stock ones, but with holes only for windows and where hatches can open.
 *      Hatch masks should probably be a separate mesh - Add transparent windows to them while doing this?
 *      C:\Games\Kerbal Space Program\GameData\Squad\Spaces\OverlayMasks
 * - IVA crew transfer.
 * - Climbing, mobility etc.
 * - IVA to EVA (open external hatches).
 * - IVAs for traversable stock parts.
 * - Buckle/unbuckle sound effects.
 * - Ladders/handles
 * - Disable window zoom camera interaction (click and physics).
 * - Collide with terrain or go on EVA when leaving the vessel (switch to world space physics).
 * - Fix or disallow changing the active kerbal with V.
 * - Persist hatch states.
 * - Test in Editor (VAB/SPH)?
 * - Key binding to toggle helmet. Visible helmet with animation?
 * - Mesh switcher: Add doorways when external nodes have approprate parts attached.
 */

// Assistance received from: egg
namespace FreeIva
{
	/// <summary>
	/// Main controller for FreeIva behaviours.
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class FreeIva : MonoBehaviour
	{
		public static Part CurrentPart => CurrentInternalModuleFreeIva?.part;
		public static GameObject SelectedObject = null;
		public static InternalModuleFreeIva CurrentInternalModuleFreeIva
		{
			get; private set;
		}

		public void Start()
		{
			GuiUtils.DrawGui =
#if DEBUG
			true;
#else
            false;
#endif

			Paused = false;
			GameEvents.onGamePause.Add(OnPause);
			GameEvents.onGameUnpause.Add(OnUnPause);
			GameEvents.onVesselWasModified.Add(OnVesselWasModified);
			GameEvents.onSameVesselDock.Add(OnSameVesselDockingChange);
			GameEvents.onSameVesselUndock.Add(OnSameVesselDockingChange);
			GameEvents.onCrewOnEva.Add(OnCrewOnEva);

			Settings.LoadSettings();
			SetRenderQueues(FlightGlobals.ActiveVessel.rootPart);

			Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.InternalSpace);
			Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.Kerbals, false);

			var ivaSun = InternalSpace.Instance.transform.Find("IVASun").GetComponent<IVASun>();
			ivaSun.ivaLight.shadowBias = 0;
			ivaSun.ivaLight.shadowNormalBias = 0;
			ivaSun.ivaLight.shadows = LightShadows.Hard;
		}

		private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> data)
		{
			StartCoroutine(ModifyEvaFsm(data.to));
		}


		internal static Part FindPartWithEmptySeat(Part sourcePart)
		{
			if (sourcePart.protoModuleCrew.Count < sourcePart.CrewCapacity)
			{
				return sourcePart;
			}

			// search for a nearby part that we could sit in
			HashSet<Part> visitedParts = new HashSet<Part>();
			Queue<Part> partQueue = new Queue<Part>();
			partQueue.Enqueue(sourcePart);
			visitedParts.Add(sourcePart);

			Action<Part> EnqueuePart = (Part part) =>
			{
				// NOTE: checking for a FreeIva module doesn't necessarily mean they are traversible by hatches (could be surface-attached, etc)
				// We'd need to spawn all the internal models to figure out if the hatches were connected
				if (!visitedParts.Contains(part) && part.HasModuleImplementing<ModuleFreeIva>())
				{
					partQueue.Enqueue(part);
					visitedParts.Add(part);
				}
			};

			while (partQueue.Count > 0)
			{
				Part part = partQueue.Dequeue();
				if (part.protoModuleCrew.Count < part.CrewCapacity)
				{
					return part;
				}

				if (part.parent != null)
				{
					EnqueuePart(part.parent);
				}

				foreach (var childPart in part.children)
				{
					EnqueuePart(childPart);
				}
			}

			// failure case: return original part so the user gets a message about the module being full
			return sourcePart;
		}

		private IEnumerator ModifyEvaFsm(Part kerbalPart)
		{
			var kerbalEva = kerbalPart.GetModule<KerbalEVA>();

			while (!kerbalEva.Ready)
			{
				yield return null;
			}

			kerbalEva.On_boardPart.OnEvent = delegate
			{
				kerbalEva.On_boardPart.GoToStateOnEvent = kerbalEva.fsm.CurrentState;

				var targetPart = FindPartWithEmptySeat(kerbalEva.currentAirlockPart);

				kerbalEva.BoardPart(targetPart);
			};
		}

		public static bool Paused = false;
		public void OnPause()
		{
			Paused = true;
		}

		public void OnUnPause()
		{
			Paused = false;
		}

		private void OnVesselWasModified(Vessel vessel)
		{
			if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
			{
				m_internalVisibilityDirty = true;
			}
		}

		private void OnSameVesselDockingChange(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> data)
		{
			OnVesselWasModified(data.to.vessel);
			if (data.from.vessel != data.to.vessel)
			{
				OnVesselWasModified(data.from.vessel);
			}
		}

		public void OnDestroy()
		{
			GameEvents.onGamePause.Remove(OnPause);
			GameEvents.onGameUnpause.Remove(OnUnPause);
			GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
			GameEvents.onSameVesselDock.Remove(OnSameVesselDockingChange);
			GameEvents.onSameVesselUndock.Remove(OnSameVesselDockingChange);
			GameEvents.onCrewOnEva.Remove(OnCrewOnEva);
			InputLockManager.RemoveControlLock("FreeIVA");
		}

		public void FixedUpdate()
		{
			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA && CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.Internal) return;

			UpdateCurrentPart();

			// prevent mouse clicks from hitting the kerbal collider or the internal shell
			// Some of the shell colliders are a little too tight and block props
			// This has a few downsides:
			// -you're able to click on things through hatches (unless they have a second collider on layer 20)
			// -any props that are on the wrong layer (16) will not be clickable
			// eventually it might be prudent to undo this change, make the kerbal a single capsule collider, and fix the shell colliders instead

			// update for Physical Props: allow clicking on layer 16 when the modifier key is held
			if (GameSettings.MODIFIER_KEY.GetKey())
			{
				InternalCamera.Instance._camera.eventMask |= (1 << (int)Layers.Kerbals);
			}
			else
			{
				InternalCamera.Instance._camera.eventMask &= ~(1 << (int)Layers.Kerbals);
			}
		}

		bool m_internalVisibilityDirty = false;

		void LateUpdate()
		{
			if (m_internalVisibilityDirty)
			{
				EnableInternals();
				KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.StopCoroutine(KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.refreshCoroutine);
				m_internalVisibilityDirty = false;
			}

			if (PhysicalProp.HeldProp != null)
			{
				if (Input.GetMouseButtonDown(0))
				{
					if (GameSettings.MODIFIER_KEY.GetKey())
					{
						if (PhysicalProp.HeldProp.isSticky)
						{
							Ray ray = InternalCamera.Instance._camera.ScreenPointToRay(Input.mousePosition);
							if (Physics.Raycast(ray, out RaycastHit hit, 2f, InternalCamera.Instance._camera.eventMask, QueryTriggerInteraction.Collide))
							{
								PhysicalProp.HeldProp.Stick(hit.point + hit.normal * 0.005f);
							}
						}
						else
						{
							PhysicalProp.ThrowProp();
						}
					}
					else
					{
						// TODO: it would be nice to somehow figure out if we were clicking on UI or another prop or something
						PhysicalProp.HeldProp.StartInteraction();
					}
				}

				if (Input.GetMouseButtonUp(0) && !GameSettings.MODIFIER_KEY.GetKey())
				{
					PhysicalProp.HeldProp.StopInteraction();
				}
			}
		}

		public static int DepthMaskQueue = 999;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="activePart">The part that the IVA player is currently inside.</param>
		public static void SetRenderQueues(Part activePart)
		{
			InternalModuleFreeIva.RefreshDepthMasks();
			return;
		}

		List<InternalModuleFreeIva> possibleModules = new List<InternalModuleFreeIva>();
		Vector3 _previousCameraPosition = Vector3.zero;
		public void UpdateCurrentPart()
		{
			if (KerbalIvaAddon.Instance.buckled) return;

			if (InternalCamera.Instance == null)
			{
				Debug.LogError("InternalCamera was null");
				Debug.Log("Searching for camera: " + InternalCamera.FindObjectOfType<Camera>());
				return;
			}

			if (_previousCameraPosition == InternalCamera.Instance.transform.position)
				return;
			//Debug.Log("###########################");
			_previousCameraPosition = InternalCamera.Instance.transform.position;
			Vector3 camPos = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
			InternalModuleFreeIva newModule = null;

			// Part colliders are larger than the parts themselves and overlap.
			// Find which of the containing parts we're nearest to.
			possibleModules.Clear();
			bool currentModuleBoundsCamera = false;

			if (CurrentPart != null) // e.g. on part destroyed.
			{
				currentModuleBoundsCamera = InternalModuleBoundsCamera(CurrentInternalModuleFreeIva);
				if (currentModuleBoundsCamera)
				{
					possibleModules.Add(CurrentInternalModuleFreeIva);
				}
				else
				{
					GetInternalModulesBoundingCamera(CurrentPart, possibleModules);
				}
				
				// Check all attached parts.
				if (CurrentPart.parent != null)
				{
					GetInternalModulesBoundingCamera(CurrentPart.parent, possibleModules);
				}
				foreach (Part c in CurrentPart.children)
				{
					GetInternalModulesBoundingCamera(c, possibleModules);
				}
			}
			if (possibleModules.Count == 0)
			{
				//Debug.Log("# Zero connected parts found, checking everything.");
				foreach (Part p in FlightGlobals.ActiveVessel.parts)
				{
					GetInternalModulesBoundingCamera(p, possibleModules);
				}
			}

			if (possibleModules.Count == 0)
			{
				//Debug.Log("# No potential parts found");
				return;
			}

			if (possibleModules.Count == 1)
			{
				newModule = possibleModules[0];
			}
			else if (possibleModules.Count > 1)
			{
				float minDistance = float.MaxValue;
				//Debug.Log("# Checking " + possibleParts.Count + " possibilities.");
				foreach (InternalModuleFreeIva possibleModule in possibleModules)
				{
					Profiler.BeginSample("Testing possible part");
					Part pp = possibleModule.part;
					// Raycast from the camera to the centre of the collider.
					// TODO: Figure out how to deal with multi-collider parts.
					Vector3 c = pp.collider.bounds.center;
					Vector3 direction = c - camPos;
					Ray ray = new Ray(camPos, direction);
					RaycastHit hitInfo;
					if (!pp.collider.Raycast(ray, out hitInfo, direction.magnitude))
					{
						//Debug.Log("# Raycast missed part from inside: " + pp);
						// Ray didn't hit the collider => we are inside the collider.
						float dist = Vector3.Distance(pp.collider.bounds.center, camPos);
						if (dist < minDistance)
						{
							newModule = possibleModule;
							minDistance = dist;
						}
						/*else
							Debug.Log("# Part was further away: " + minDistance + " vs part's " + dist);*/
					}
					/*else
						Debug.Log("# Raycast hit part from outside: " + pp);*/
					Profiler.EndSample();
				}
			}

			if (newModule != null || !currentModuleBoundsCamera)
			{
				SetCurrentPart(newModule);
			}

			/*else
                Debug.Log("# No closest part found.");*/
			// Keep the last part we were inside as the current part: We could be transitioning between hatches.
			// TODO: Idendify/store when we are outside all parts (EVA from IVA?).
		}

		static bool InternalModuleBoundsCamera(InternalModuleFreeIva ivaModule)
		{
			Vector3 localPosition = ivaModule.internalModel.transform.InverseTransformPoint(InternalCamera.Instance.transform.position);
			return ivaModule.ShellColliderBounds.Contains(localPosition);
		}

		public static void GetInternalModulesBoundingCamera(Part p, List<InternalModuleFreeIva> modules)
		{
			Profiler.BeginSample("PartBoundsCamera");

			if (p.internalModel != null && p.internalModel.isActiveAndEnabled)
			{
				for (var ivaModule = InternalModuleFreeIva.GetForModel(p.internalModel); ivaModule != null; ivaModule = InternalModuleFreeIva.GetForModel(ivaModule.SecondaryInternalModel))
				{
					if (InternalModuleBoundsCamera(ivaModule))
					{
						modules.Add(ivaModule);
						break; // do we want to break here? Centrifuges typically have the rotating bit as the first model
					}
				}
			}

			Profiler.EndSample();
		}

		public static void SetCurrentPart(InternalModuleFreeIva newModule)
		{
			if (FreeIva.CurrentInternalModuleFreeIva != newModule)
			{
				CurrentInternalModuleFreeIva = newModule;
				CameraManager.Instance.activeInternalPart = CurrentPart;
			}
		}

		public static bool PartIsProbeCore(Part part)
		{
			return part.CrewCapacity == 0 && part.HasModuleImplementing<ModuleCommand>();
		}

		public static void EnableInternals()
		{
			try
			{
				if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA && PartIsProbeCore(CameraManager.Instance.activeInternalPart))
				{
					InternalModel currentInternal = CameraManager.Instance.activeInternalPart.internalModel;

					foreach (Part p in FlightGlobals.ActiveVessel.parts)
					{
						if (p != currentInternal.part && p.internalModel != null)
						{
							p.internalModel.DespawnCrew();
							p.internalModel.gameObject.SetActive(false);
						}
					}

					currentInternal.gameObject.SetActive(true);
					currentInternal.SetVisible(true);
				}
				else
				{
					foreach (Part p in FlightGlobals.ActiveVessel.parts)
					{
						if (!PartIsProbeCore(p))
						{
							if (p.internalModel == null)
							{
								p.CreateInternalModel();
								if (p.internalModel != null)
								{
									p.internalModel.Initialize(p);
									p.internalModel.SpawnCrew();
								}
							}

							if (p.internalModel != null)
							{
								p.internalModel.gameObject.SetActive(true);
								p.internalModel.SetVisible(true);
							}
						}
					}
				}

				InternalModuleFreeIva.RefreshInternals();
				InternalModuleFreeIva.RefreshDepthMasks();
			}
			catch (Exception ex)
			{
				Debug.LogError("[FreeIVA] Error enabling internals: " + ex.Message + ", " + ex.StackTrace);
			}
		}
	}
}
