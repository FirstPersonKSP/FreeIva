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
		public static EventData<Part> OnIvaPartChanged = new EventData<Part>("OnIvaPartChanged");
		public static Part InitialPart;
		public static Part CurrentPart;
		public static GameObject SelectedObject = null;
		private static InternalModuleFreeIva _currentInternalModuleFreeIva = null;
		public static InternalModuleFreeIva CurrentInternalModuleFreeIva
		{
			get
			{
				if (_currentInternalModuleFreeIva == null || _currentInternalModuleFreeIva.part != CurrentPart)
				{
					_currentInternalModuleFreeIva = InternalModuleFreeIva.GetForModel(CurrentPart.internalModel);
					return _currentInternalModuleFreeIva;
				}
				return _currentInternalModuleFreeIva;
			}
		}

		public void Start()
		{
			CurrentPart = FlightGlobals.ActiveVessel.rootPart;
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
			OnIvaPartChanged.Add(IvaPartChanged);
			SetRenderQueues(FlightGlobals.ActiveVessel.rootPart);

			Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.InternalSpace);
			Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.Kerbals, false);
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
			if (vessel == FlightGlobals.ActiveVessel)
			{
				EnableInternals();
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
			OnIvaPartChanged.Remove(IvaPartChanged);
			InputLockManager.RemoveControlLock("FreeIVA");
		}

		public void FixedUpdate()
		{
			UpdateCurrentPart();
		}

		public static int DepthMaskQueue = 999;
		private static HashSet<Part> visibleParts = new HashSet<Part>();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="activePart">The part that the IVA player is currently inside.</param>
		public static void SetRenderQueues(Part activePart)
		{
			Profiler.BeginSample("SetRenderQueues");

			//visibleParts.Clear();
			//GetVisibleParts(CurrentPart, visibleParts);

			for (int i = 0; i < FlightGlobals.ActiveVessel.Parts.Count; i++)
			{
				Part p = FlightGlobals.ActiveVessel.Parts[i];
				//bool partVisible = visibleParts.Contains(p);

				if (p.internalModel != null)
				{
					Renderer[] renderers = p.internalModel.GetComponentsInChildren<Renderer>();
					foreach (var r in renderers)
					{
						foreach (var m in r.materials)
						{
							// Geometry is rendered at 2000. The depth mask will be rendered over it at 1999.
							// Render the next visible area (behind the depth mask) before it, over the top of it, at 1998.
							if (m.shader.name.Contains("DepthMask") || r.name == "HatchDoor" || r.name == "hatchCombing" || r.name == "mk2CrewCabinExtHatchCut")
							{
								m.renderQueue = DepthMaskQueue;
							}
							else
							{
								m.renderQueue = DepthMaskQueue - 1;

								//if (p == activePart || !partVisible)
								//    m.renderQueue = 2000; // Hide the part the player is inside, and parts with closed hatches.
								//else
								//    m.renderQueue = DepthMaskQueue - 1; //1998;
							}
						}
					}
				}
			}

			Profiler.EndSample();
		}

		/// <summary>
		/// Gets a list of parts that have
		/// </summary>
		/// <returns></returns>
		private static void GetVisibleParts(Part part, HashSet<Part> visibleParts)
		{
			InternalModuleFreeIva iva = InternalModuleFreeIva.GetForModel(part.internalModel);
			if (iva != null)
			{
				if (!visibleParts.Contains(part))
					visibleParts.Add(part);

				for (int i = 0; i < iva.Hatches.Count; i++)
				{
					FreeIvaHatch h = iva.Hatches[i];

					// TODO: Hatches can have windows in them, and parts may have windows facing against connected parts.
					var canSeeMoreIvaThroughTheHatch = h.IsOpen && h.ConnectedHatch != null && h.ConnectedHatch.IsOpen && h.ConnectedHatch.part != null;
					if (canSeeMoreIvaThroughTheHatch && !visibleParts.Contains(h.ConnectedHatch.part))
					{
						GetVisibleParts(h.ConnectedHatch.part, visibleParts);
					}
				}
			}
		}

		Vector3 _previousCameraPosition = Vector3.zero;
		public void UpdateCurrentPart()
		{
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
			Part lastPart = CurrentPart;

			// Part colliders are larger than the parts themselves and overlap.
			// Find which of the containing parts we're nearest to.
			List<Part> possibleParts = new List<Part>();

			if (CurrentPart != null) // e.g. on part destroyed.
			{
				if (PartBoundsCamera(CurrentPart))
				{
					//Debug.Log("# Adding previous currentpart.");
					possibleParts.Add(CurrentPart);
				}
				// Check all attached parts.
				if (CurrentPart.parent != null && PartBoundsCamera(CurrentPart.parent))
				{
					//Debug.Log("# Adding parent " + CurrentPart.parent);
					possibleParts.Add(CurrentPart.parent);
				}
				foreach (Part c in CurrentPart.children)
				{
					if (PartBoundsCamera(c))
					{
						//Debug.Log("# Adding child " + c);
						possibleParts.Add(c);
					}
				}
			}
			if (possibleParts.Count == 0)
			{
				//Debug.Log("# Zero connected parts found, checking everything.");
				foreach (Part p in FlightGlobals.ActiveVessel.parts)
				{
					if (PartBoundsCamera(p))
					{
						//Debug.Log("# Adding vessel part " + p);
						possibleParts.Add(p);
					}
				}
			}

			if (possibleParts.Count == 0)
			{
				//Debug.Log("# No potential parts found");
				return;
			}

			if (possibleParts.Count == 1)
			{
				//Debug.Log("# Only one part found: " + possibleParts[0]);
				CurrentPart = possibleParts[0];
				if (CurrentPart != lastPart)
					OnIvaPartChanged.Fire(CurrentPart);
				/*else
                    Debug.Log("# Same part as before: " + CurrentPart + " at " + CurrentPart.transform.position);*/
				return;
			}

			float minDistance = float.MaxValue;
			Part closestPart = null;
			//Debug.Log("# Checking " + possibleParts.Count + " possibilities.");
			foreach (Part pp in possibleParts)
			{
				Profiler.BeginSample("Testing possible part");
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
						closestPart = pp;
						minDistance = dist;
					}
					/*else
                        Debug.Log("# Part was further away: " + minDistance + " vs part's " + dist);*/
				}
				/*else
                    Debug.Log("# Raycast hit part from outside: " + pp);*/
				Profiler.EndSample();
			}
			if (closestPart != null)
			{
				Profiler.BeginSample("OnIvaPartChanged");
				//Debug.Log("# New closest part found: " + closestPart);
				CurrentPart = closestPart;
				if (CurrentPart != lastPart)
					OnIvaPartChanged.Fire(CurrentPart);
				Profiler.EndSample();
				/*else
                    Debug.Log("# Same part as before: " + CurrentPart + " at " + CurrentPart.transform.position);*/
			}
			/*else
                Debug.Log("# No closest part found.");*/
			// Keep the last part we were inside as the current part: We could be transitioning between hatches.
			// TODO: Idendify/store when we are outside all parts (EVA from IVA?).
		}

		public static bool PartBoundsCamera(Part p)
		{
			Profiler.BeginSample("PartBoundsCamera");
			var part = GameObjectBoundsCamera(p.gameObject);
			Profiler.EndSample();
			return part;
		}

		private static bool GameObjectBoundsCamera(GameObject go)
		{
			// The transform containing the mesh can be buried several levels deep.
			int childCount = go.transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				Transform child = go.transform.GetChild(i);
				if (child.name != "main camera pivot" && child.GetComponent<Part>() == null)
				{
					GameObject goc = child.gameObject;
					MeshFilter[] meshc = goc.GetComponents<MeshFilter>();
					for (int m = 0; m < meshc.Length; m++)
					{
						Bounds meshBounds = meshc[m].mesh.bounds;
						if (meshBounds != null)
						{
							Vector3 camPos = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
							// Bounds are relative to the transform position, not the world.
							camPos -= goc.transform.position;

							if (meshBounds.Contains(camPos))
								return true;
						}
					}
					bool foundGrandChild = GameObjectBoundsCamera(goc);
					if (foundGrandChild)
						return true;
				}
			}
			return false;
		}

		public static void UpdateCurrentPart(Part newCurrentPart)
		{
			if (FreeIva.CurrentPart != newCurrentPart)
			{
				CurrentPart = newCurrentPart;
				OnIvaPartChanged.Fire(CurrentPart);
			}
		}

		public void IvaPartChanged(Part newPart)
		{
			SetRenderQueues(newPart);
		}

		public static void EnableInternals()
		{
			try
			{
				foreach (Part p in FlightGlobals.ActiveVessel.parts)
				{
					var ivaModule = p.GetModule<ModuleFreeIva>();

					if (ivaModule == null || ivaModule.autoCreateInternals)
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
							p.internalModel.SetVisible(true);
						}
					}
				}

				InternalModuleFreeIva.RefreshInternals();
			}
			catch (Exception ex)
			{
				Debug.LogError("[FreeIVA] Error enabling internals: " + ex.Message + ", " + ex.StackTrace);
			}
		}
	}
}
