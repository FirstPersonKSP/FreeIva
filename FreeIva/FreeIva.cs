using KSP.Localization;
using KSP.UI.Screens.Flight;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace FreeIva
{
	// there's too much overlap between this class and KerbalIvaAddon.
	// Eventually this class can become fully static and contain utliity functions
	// note - a lot of stuff in here might be referenced by kerbalvr or other mods
	[KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
	public class FreeIva : MonoBehaviour
	{
		public static Part CurrentPart => CurrentInternalModuleFreeIva == null ? null : CurrentInternalModuleFreeIva.part;
		public static GameObject SelectedObject = null;
		public static InternalModuleFreeIva CurrentInternalModuleFreeIva
		{
			get; private set;
		}

		void Awake()
		{
			Paused = false;
			GameEvents.onGamePause.Add(OnPause);
			GameEvents.onGameUnpause.Add(OnUnPause);
		}

		public void Start()
		{
			GuiUtils.DrawGui =
#if DEBUG
			true;
#else
            false;
#endif
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

		static IEnumerator PostBoardCoroutine(ProtoCrewMember protoCrewMember, Collider airlockCollider, Part airlockPart)
		{
			yield return null; // we have to wait a frame so the kerbal gets set up

			if (protoCrewMember.KerbalRef == null)
			{
				Log.Error($"PostBoardCoroutine: KerbalRef is null in internal {protoCrewMember.seat.internalModel.internalName}");
				yield break;
			}

			KerbalIvaAddon.SetCameraIVA(protoCrewMember.KerbalRef);

			if (KerbalPortraitGallery.Instance?.resetCoroutine != null)
			{
				KerbalPortraitGallery.Instance.StopCoroutine(KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.resetCoroutine);
				KerbalPortraitGallery.Instance.SetActivePortraitsForVessel(airlockPart.vessel);
				EnableInternals();
			}

			FreeIvaHatch matchingHatch = null;

			for (var freeIvaModule = InternalModuleFreeIva.GetForModel(airlockPart.internalModel); freeIvaModule != null && matchingHatch == null; freeIvaModule = InternalModuleFreeIva.GetForModel(freeIvaModule.SecondaryInternalModel))
			{
				foreach (var hatch in freeIvaModule.Hatches)
				{
					if (hatch.AirlockTransform == airlockCollider.transform)
					{
						matchingHatch = hatch;
						break;
					}
				}
			}

			if (matchingHatch != null)
			{
				KerbalIvaAddon.Instance.Unbuckle(false);
				if (!KerbalIvaAddon.Instance.buckled)
				{
					Vector3 position = matchingHatch.transform.TransformPoint(matchingHatch.inwardsDirection * 0.3f);
					Vector3 hatchInwards = matchingHatch.transform.TransformDirection(matchingHatch.inwardsDirection);
					Quaternion rotation = Quaternion.LookRotation(hatchInwards, matchingHatch.internalModel.transform.up);
					KerbalIvaAddon.Instance.KerbalIva.transform.position = position;
					KerbalIvaAddon.Instance.KerbalIva.SetCameraOrientation(rotation);
				}
			}
		}

		public static void BoardPartFromAirlock(KerbalEVA kerbalEva, bool checkInventoryAndScience)
		{
			kerbalEva.On_boardPart.GoToStateOnEvent = kerbalEva.fsm.CurrentState;

			var airlockCollider = kerbalEva.currentAirlockTrigger;
			var airlockPart = kerbalEva.currentAirlockPart;
			var targetPart = FindPartWithEmptySeat(kerbalEva.currentAirlockPart);
			bool runPostBoarding = Settings.BoardingMode == BoardingMode.Always || Settings.BoardingMode == BoardingMode.FromThroughTheEyes && ThroughTheEyes.IsInFirstPerson();

			var pcm = kerbalEva.part.protoModuleCrew[0];

			if (!HighLogic.CurrentGame.Parameters.Flight.CanBoard)
			{
				ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_115948"), 5f, ScreenMessageStyle.UPPER_CENTER);
			}
			else if (checkInventoryAndScience)
			{
				kerbalEva.BoardPart(targetPart);
			}
			else
			{
				// this is a subset of BoardPart, but it won't fail if the inventory or science can't be stored (to be used by kerbalvr)
				kerbalEva.checkExperiments(airlockPart); // do we want this one?  or the part that we'll eventually be seated in?
				PopupDialog.ClearPopUps();
				kerbalEva.proceedAndBoard(targetPart);
			}

			if (runPostBoarding && pcm.seat != null)
			{
				// using the KerbalIvaAddon as the coroutine host here is pretty arbitrary; but this is a static method so we need to pick *something* and there's no static instance of this addon available
				KerbalIvaAddon.Instance.StartCoroutine(PostBoardCoroutine(pcm, airlockCollider, airlockPart));
			}
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

		public void OnDestroy()
		{
			GameEvents.onGamePause.Remove(OnPause);
			GameEvents.onGameUnpause.Remove(OnUnPause);
			InputLockManager.RemoveControlLock("FreeIVA");
		}

		public void FixedUpdate()
		{
			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA && CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.Internal)
			{
				if (CurrentInternalModuleFreeIva != null)
				{
					SetCurrentPart(null);
				}
				return;
			}

			UpdateCurrentPart();

			// prevent mouse clicks from hitting the kerbal collider or the internal shell
			// Some of the shell colliders are a little too tight and block props
			// This has a few downsides:
			// -you're able to click on things through hatches (unless they have a second collider on layer 20)
			// -any props that are on the wrong layer (16) will not be clickable
			// eventually it might be prudent to undo this change, make the kerbal a single capsule collider, and fix the shell colliders instead

			// update for Physical Props: allow clicking on layer 16 when the modifier key is held
			if (InternalCamera.Instance == null)
			{
				// certain bugs can destroy the internal camera; if that happens we shouldn't spew exceptions.
				// for example: https://github.com/KSP-KOS/KOS/issues/3095
			}
			else if (GameSettings.MODIFIER_KEY.GetKey())
			{
				InternalCamera.Instance._camera.eventMask |= (1 << (int)Layers.Kerbals);
			}
			else
			{
				InternalCamera.Instance._camera.eventMask &= ~(1 << (int)Layers.Kerbals);
			}
		}

		void LateUpdate()
		{
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

		List<InternalModuleFreeIva> possibleModules = new List<InternalModuleFreeIva>();
		Vector3 _previousCameraPosition = Vector3.zero;
		public void UpdateCurrentPart()
		{
			if (KerbalIvaAddon.Instance.buckled) return;

			if (InternalCamera.Instance == null)
			{
				Log.Error("InternalCamera was null");
				Log.Message("Searching for camera: " + InternalCamera.FindObjectOfType<Camera>());
				return;
			}

			if (_previousCameraPosition == InternalCamera.Instance.transform.position)
				return;
			//Log.Message("###########################");
			_previousCameraPosition = InternalCamera.Instance.transform.position;
			Vector3 camPos = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
			InternalModuleFreeIva newModule = null;

			// Part colliders are larger than the parts themselves and overlap.
			// Find which of the containing parts we're nearest to.
			possibleModules.Clear();
			bool currentModuleBoundsCamera = false;

			if (CurrentInternalModuleFreeIva != null && CurrentInternalModuleFreeIva.part.vessel == FlightGlobals.ActiveVessel) // e.g. on part destroyed.
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
				//Log.Message("# Zero connected parts found, checking everything.");
				foreach (Part p in FlightGlobals.ActiveVessel.parts)
				{
					GetInternalModulesBoundingCamera(p, possibleModules);
				}
			}

			if (possibleModules.Count == 0)
			{
				//Log.Message("# No potential parts found");
				return;
			}

			if (possibleModules.Count == 1)
			{
				newModule = possibleModules[0];
			}
			else if (possibleModules.Count > 1)
			{
				float minDistance = float.MaxValue;
				//Log.Message("# Checking " + possibleParts.Count + " possibilities.");
				foreach (InternalModuleFreeIva possibleModule in possibleModules)
				{
					Profiler.BeginSample("Testing possible part");
					Part pp = possibleModule.part;
					if (pp.collider == null) continue;
					// Raycast from the camera to the centre of the collider.
					// TODO: Figure out how to deal with multi-collider parts.
					Vector3 c = pp.collider.bounds.center;
					Vector3 direction = c - camPos;
					Ray ray = new Ray(camPos, direction);
					RaycastHit hitInfo;
					if (!pp.collider.Raycast(ray, out hitInfo, direction.magnitude))
					{
						//Log.Message("# Raycast missed part from inside: " + pp);
						// Ray didn't hit the collider => we are inside the collider.
						float dist = Vector3.Distance(pp.collider.bounds.center, camPos);
						if (dist < minDistance)
						{
							newModule = possibleModule;
							minDistance = dist;
						}
						/*else
							Log.Message("# Part was further away: " + minDistance + " vs part's " + dist);*/
					}
					/*else
						Log.Message("# Raycast hit part from outside: " + pp);*/
					Profiler.EndSample();
				}
			}

			if (newModule != null || !currentModuleBoundsCamera)
			{
				SetCurrentPart(newModule.internalModel);
			}

			/*else
                Log.Message("# No closest part found.");*/
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

		public static void SetCurrentPart(InternalModel newModel)
		{
			var newModule = InternalModuleFreeIva.GetForModel(newModel);

			if (newModule == null && CurrentInternalModuleFreeIva != null && CameraManager.Instance != null && CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
			{
				Log.Warning($"setting current module to null (was {CurrentInternalModuleFreeIva.internalModel.internalName}) for INTERNAL {(newModel == null ? "null" : newModel.internalName)}");
			}

			if (FreeIva.CurrentInternalModuleFreeIva != newModule)
			{
				if (CurrentInternalModuleFreeIva != null) CurrentInternalModuleFreeIva.SetIsCurrentModule(false);
				if (newModule != null) newModule.SetIsCurrentModule(true);

				CurrentInternalModuleFreeIva = newModule;
				CameraManager.Instance.activeInternalPart = CurrentPart;
			}
		}

		internal static Part GetPartContainingCrew(ProtoCrewMember crewMember)
		{
			// Kerbalref.InPart is always the part where the kerbal was last *seated*
			// most of the time, this is also the part that contains their protocrewmember.
			// but sometimes when entering a seat we don't actually transfer the kerbal between parts - specifically when the seat is in a part that doesn't have enough crew capacity e.g. science lab or inline docking port
			Part containingPart = crewMember.KerbalRef.InPart;

			if (containingPart.protoModuleCrew.Contains(crewMember))
			{
				return containingPart;
			}
			else
			{
				foreach (var part in containingPart.vessel.parts)
				{
					if (part.protoModuleCrew.Contains(crewMember))
					{
						return part;
					}
				}
			}

			return null;
		}

		internal static void MoveKerbalToSeat(ProtoCrewMember crewMember, InternalSeat newSeat)
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
				Part oldPart = GetPartContainingCrew(crewMember);
				if (oldPart != null)
				{
					// fully move the kerbal
					oldPart.RemoveCrewmember(crewMember);

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
					Log.Warning($"Kerbal {crewMember.name} did not exist in crew of source part {crewMember.KerbalRef.InPart.partInfo.name} nor anywhere on the vessel");
					destModel.SitKerbalAt(crewMember, newSeat);
				}
			}
			else
			{
				// put the kerbal in the seat without adjusting crew assignments
				// this would normally be done inside AddCrewMemberAt
				// NOTE: SitKerbalAt alters the ProtoCrewMember's seatIdx member, which is stored in the save file and used when spawning kerbals for the IVA.
				// https://github.com/FirstPersonKSP/FreeIva/issues/380#issuecomment-1952986010
				int oldSeatIndex = crewMember.seatIdx;
				destModel.SitKerbalAt(crewMember, newSeat);
				crewMember.seatIdx = oldSeatIndex;
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
		}

		public static bool PartIsProbeCore(Part part)
		{
			// the ArcReactor from Pathfinder has 0 crew capacity and a ModuleCommand, so technically it is a probe core.  but we definitely want to create the IVA like all other parts
			// Using the presence of an airlock (which freeiva adds....) as the decider here.  This feels like a pretty big hack, hopefuly it doesn't come back to bite me.
			// Another possible test here could be whether there are any hatches that are connected to attachnodes (because presumably that means the IVA was meant for use on the actual ship, not PCR)
			if (part.CrewCapacity == 0 && part.airlock == null && part.HasModuleImplementing<ModuleCommand>())
			{
				var freeIvaModule = part.FindModuleImplementing<ModuleFreeIva>();

				if (freeIvaModule == null || !freeIvaModule.forceInternalCreation)
				{
					return part.modules.GetModule("ProbeControlRoomPart") != null;
				}
			}

			return false;
		}

		static bool ShouldCreateInternals(Part part)
		{
			if (PartIsProbeCore(part))
			{
				return false;
			}

			var freeIvaModule = part.FindModuleImplementing<ModuleFreeIva>();

			if (freeIvaModule != null && freeIvaModule.requireDeploy && freeIvaModule.Deployable != null && !freeIvaModule.Deployable.IsDeployed)
			{
				return false;
			}

			return true;
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
						EnablePartInternals(p);
					}
				}

				InternalModuleFreeIva.RefreshInternals();
				InternalModuleFreeIva.RefreshDepthMasks();
			}
			catch (Exception ex)
			{
				Log.Error("Error enabling internals: " + ex.Message + ", " + ex.StackTrace);
			}
		}

		static void EnablePartInternals(Part p)
		{
			if (ShouldCreateInternals(p))
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

		static Vector3 WorldDirectionToInternal(Vector3 worldDirection)
		{
			Quaternion direction = Quaternion.LookRotation(worldDirection);
			Quaternion internalDirection = InternalSpace.WorldToInternal(direction);
			return internalDirection * Vector3.forward;
		}

		static void GetAngularVelocity(Part part, out Vector3 centerOfRotationWorld, out Vector3 angularVelocityWorld)
		{
			if (part)
			{
				Rigidbody rb = part.Rigidbody;
				Vector3 localVelocity = rb.velocity - part.vessel.rb_velocity;
				float omegaSquared = rb.angularVelocity.sqrMagnitude;

				centerOfRotationWorld = rb.position;

				if (omegaSquared > 0.0001f)
				{
					centerOfRotationWorld += Vector3.Cross(rb.angularVelocity, localVelocity) / omegaSquared;
				}

				angularVelocityWorld = rb.angularVelocity;
			}
			else
			{
				centerOfRotationWorld = FlightGlobals.ActiveVessel.CoM;
				angularVelocityWorld = FlightGlobals.ActiveVessel.angularVelocity;
			}
		}

		internal static Vector3 GetFlightAccelerationInternalSpace(Part part, Vector3 internalSpacePosition)
		{
			Vector3 accelWorldSpace;

			if (FlightGlobals.ActiveVessel.LandedOrSplashed)
			{
				accelWorldSpace = FlightGlobals.ActiveVessel.graviticAcceleration;
			}
			else
			{
				// linear acceleration
				accelWorldSpace = -FlightGlobals.ActiveVessel.perturbation;

				// angular (centrifugal) acceleration => omega^2 * r
				// this is super fake, it will apply to things that aren't in contact with the rotating part and doesn't account for coriolis, etc.
				GetAngularVelocity(part, out Vector3 centerOfRotationWorld, out Vector3 angularVelocityWorld);

				Vector3 rotationAxis = angularVelocityWorld.normalized;
				Vector3 worldSpacePosition = InternalSpace.InternalToWorld(internalSpacePosition);
				Vector3 fromAxis = Vector3.ProjectOnPlane(worldSpacePosition - centerOfRotationWorld, rotationAxis);


				float r = fromAxis.magnitude;
				float omegaSquared = angularVelocityWorld.sqrMagnitude;

				float centrifugalAccelMag = omegaSquared * r;

				if (centrifugalAccelMag > 0.01f)
				{
					accelWorldSpace += fromAxis / r * centrifugalAccelMag;
				}
			}

			float magnitude = accelWorldSpace.magnitude;

			if (magnitude <= 0.01f)
			{
				return Vector3.zero;
			}

			return WorldDirectionToInternal(accelWorldSpace) * magnitude;
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
			if (!KerbalIvaAddon.Instance.Gravity)
			{
				return Vector3.zero;
			}
			else if (ivaModule?.Centrifuge != null && ivaModule.Centrifuge.CurrentSpinRate != 0)
			{
				return GetCentrifugeAccel(ivaModule.Centrifuge, internalSpacePosition);
			}
			else if (ivaModule != null && ivaModule.customGravity != Vector3.zero)
			{
				return ivaModule.transform.TransformDirection(ivaModule.customGravity);
			}
			else
			{
				return GetFlightAccelerationInternalSpace(ivaModule?.part, internalSpacePosition);
			}
		}
	}
}
