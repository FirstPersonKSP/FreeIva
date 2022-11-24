using KSP.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FreeIva
{
	/// <summary>
	/// Base class for hatches (points where kerbals can exit or enter a part during IVA).
	/// Manages attachment node, audio, and connections to other hatches
	/// </summary>
	public class FreeIvaHatch : InternalModule
	{
		// ----- fields set in prop config

		[KSPField]
		public string hatchOpenSoundFile = "FreeIva/Sounds/HatchOpen";

		[KSPField]
		public string hatchCloseSoundFile = "FreeIva/Sounds/HatchClose";

		[KSPField]
		public string handleTransformName = string.Empty;

		[KSPField]
		public string doorTransformName = string.Empty;

		[KSPField]
		public string tubeTransformName = string.Empty;

		[KSPField]
		public string cutoutTransformName = string.Empty;

		[KSPField]
		public string blockedPropName = string.Empty;

		// ----- the following fields are set via HatchConfig, so that they can be different per placement of the prop

		// The name of the part attach node this hatch is positioned on, as defined in the part.cfg's "node definitions".
		// e.g. top => node_stack_top.  Do not include the prefixes (they are stripped out during loading in the stock code).
		public string attachNodeId;

		// some docking ports don't use AttachNodes (inline ports, inflatable airlock, shielded)
		public string dockingPortNodeName = string.Empty;

		public string requiredAnimationName = string.Empty;

		[SerializeReference]
		public ObjectsToHide HideWhenOpen;

		public string cutoutTargetTransformName = string.Empty;

		public string airlockName = string.Empty;

		public float tubeExtent = 0;

		public bool hideDoorWhenConnected = false;

		// -----

		[Serializable]
		public struct ObjectToHide
		{
			public string name;
			public Vector3 position;
		}

		// this bullshit brought to you by the Unity serialization system
		public class ObjectsToHide : ScriptableObject
		{
			public List<ObjectToHide> objects = new List<ObjectToHide>();
		}

		Transform m_doorTransform;
		ModuleDockingNode m_dockingNodeModule;
		ModuleAnimateGeneric m_animationModule;
		InternalProp m_blockedProp;

		// Where the GameObject is located. Used for basic interaction targeting (i.e. when to show the "Open hatch?" prompt).
		public virtual Vector3 WorldPosition => transform.position;

		private FreeIvaHatch _connectedHatch = null;
		// The other hatch that this one is connected or docked to, if present.
		public FreeIvaHatch ConnectedHatch => _connectedHatch;

		public FXGroup HatchOpenSound = null;
		public FXGroup HatchCloseSound = null;

		public bool IsOpen { get; private set; }

		public bool CanEVA { get; private set; }

		public override void OnLoad(ConfigNode node)
		{
			// is this module placed directly in an INTERNAL node?
			if (internalModel != null)
			{
				Vector3 position = Vector3.zero;
				if (node.TryGetValue("position", ref position))
				{
					transform.localPosition = position;
				}

				Quaternion rotation = Quaternion.identity;
				if (node.TryGetValue("rotation", ref rotation))
				{
					transform.localRotation = rotation;
				}
			}

			foreach (var reparentNode in node.GetNodes("Reparent"))
			{
				ReparentUtil.Reparent(internalProp, reparentNode);
			}

			var handleTransform = TransformUtil.FindPropTransform(internalProp, handleTransformName);
			if (handleTransform != null)
			{
				handleTransform.gameObject.layer = (int)Layers.InternalSpace;

				foreach (var collidernode in node.GetNodes("HandleCollider"))
				{
					ColliderUtil.CreateCollider(handleTransform, collidernode, internalProp.propName);
				}
			}

			var doorTransform = TransformUtil.FindPropTransform(internalProp, doorTransformName);

			if (doorTransform != null)
			{
				doorTransform.gameObject.layer = (int)Layers.Kerbals;

				foreach (var colliderNode in node.GetNodes("DoorCollider"))
				{
					ColliderUtil.CreateCollider(doorTransform, colliderNode, internalProp.propName);
				}
			}
			else if (doorTransformName != string.Empty)
			{
				Debug.LogError($"[FreeIva] doorTransform {doorTransformName} not found in {internalProp.propName}");
			}
		}
		public override void OnAwake()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			Debug.Log($"# Creating hatch {internalProp.propName} for part {part.partInfo.name}");

			HatchOpenSound = SetupAudio(hatchOpenSoundFile, "HatchOpen");
			HatchCloseSound = SetupAudio(hatchCloseSoundFile, "HatchClose");

			if (handleTransformName != string.Empty)
			{
				var handleTransform = TransformUtil.FindPropTransform(internalProp, handleTransformName);
				if (handleTransform != null)
				{
					var clickWatcher = handleTransform.gameObject.AddComponent<ClickWatcher>();
					clickWatcher.AddMouseDownAction(OnHandleClick);
				}
			}

			// if the cutout didn't get removed at load time, do it now
			if (cutoutTransformName != string.Empty)
			{
				var cutoutTransform = TransformUtil.FindPropTransform(internalProp, cutoutTransformName, false);
				if (cutoutTransform != null)
				{
					GameObject.Destroy(cutoutTransform.gameObject);
				}
			}

			m_doorTransform = TransformUtil.FindPropTransform(internalProp, doorTransformName);

			if (dockingPortNodeName != string.Empty)
			{
				foreach (var module in part.Modules.OfType<ModuleDockingNode>())
				{
					if (module.nodeTransformName == dockingPortNodeName)
					{
						m_dockingNodeModule = module;
						break;
					}
				}

				if (m_dockingNodeModule == null)
				{
					Debug.LogError($"[FreeIva] No ModuleDockingNode with nodeTransformName '{dockingPortNodeName}' found in part {part.partInfo.name} for prop {internalProp.propName} in internal {internalModel.internalName}");
				}
			}

			if (requiredAnimationName != string.Empty)
			{
				foreach (var module in part.modules.OfType<ModuleAnimateGeneric>())
				{
					if (module.animationName == requiredAnimationName)
					{
						m_animationModule = module;
						break;
					}
				}

				if (m_animationModule != null)
				{
					m_animationModule.OnStop.Add(OnAnimationModuleStop);
					m_animationModule.OnMoving.Add(OnAnimationModuleMoving);
				}
				else
				{
					Debug.LogError($"[FreeIva] No ModuleAnimateGeneric with animationName '{requiredAnimationName}' found in part {part.partInfo.name} for prop {internalProp.propName} in internal {internalModel.internalName}");
				}
			}

			var internalModule = InternalModuleFreeIva.GetForModel(internalModel);
			if (internalModule == null)
			{
				Debug.LogError($"[FreeIva] no InternalModuleFreeIva instance registered for internal {internalModel.internalName} for hatch prop {internalProp.propName}");
				return;
			}
			internalModule.Hatches.Add(this);

			RefreshConnection();
		}

		void OnDestroy()
		{
			if (m_animationModule != null)
			{
				m_animationModule.OnStop.Remove(OnAnimationModuleStop);
				m_animationModule.OnMoving.Remove(OnAnimationModuleMoving);
			}
		}

		private void OnAnimationModuleMoving(float data0, float data1)
		{
			Open(false);
		}

		private void OnAnimationModuleStop(float data)
		{
			if (data == 1.0f && hideDoorWhenConnected)
			{
				Open(true);
			}
		}

		public bool IsBlockedByAnimation(bool checkConnectedHatch = true)
		{
			if (checkConnectedHatch && _connectedHatch != null && _connectedHatch.IsBlockedByAnimation(false))
			{
				return true;
			}

			return m_animationModule != null && m_animationModule.GetState().normalizedTime != 1.0f;
		}

		void SetTubeScale()
		{
			// scale tube appropriately
			var tubeTransform = TransformUtil.FindPropTransform(internalProp, tubeTransformName);
			if (tubeTransform != null)
			{
				float tubeScale = 0;

				// if we're connected to another hatch, find the midpoint between our attach nodes - this is for passthrough
				// for parts that are directly connected to each other, the attach nodes are in the same place
				// but with passthrough, we need to find a point to meet
				if (_connectedHatch != null)
				{
					var otherTubeTransform = TransformUtil.FindPropTransform(_connectedHatch.internalProp, _connectedHatch.tubeTransformName);

					// if the other transform doesn't have a tube, then we scale ours to reach the other prop's origin
					if (otherTubeTransform == null)
					{
						tubeScale = Vector3.Distance(tubeTransform.position, _connectedHatch.transform.position);
					}
					else
					{
						// otherwise we find the midpoint
						tubeScale = Vector3.Distance(tubeTransform.position, otherTubeTransform.position) * 0.5f;
					}
				}
				else
				{
					// try to determine tube length from attach node
					var myAttachNode = part.FindAttachNode(attachNodeId);
					if (tubeExtent == 0 && myAttachNode != null)
					{
						tubeExtent = Vector3.Dot(myAttachNode.originalPosition, myAttachNode.originalOrientation);
					}

					if (tubeExtent != 0)
					{
						Vector3 tubePositionInIVA = internalModel.transform.InverseTransformPoint(tubeTransform.position);
						Vector3 tubeDownVectorWorld = tubeTransform.rotation * Vector3.down;
						Vector3 tubeDownVectorIVA = internalModel.transform.InverseTransformVector(tubeDownVectorWorld);

						float tubePositionOnAxis = Vector3.Dot(tubeDownVectorIVA, tubePositionInIVA);
						tubeScale = tubeExtent - tubePositionOnAxis;

						// TODO: what if the prop itself is scaled?
					}
				}

				if (tubeScale <= 0)
				{
					GameObject.Destroy(tubeTransform.gameObject);
				}
				else
				{
					tubeTransform.localScale = new Vector3(1.0f, tubeScale, 1.0f);
				}
			}
		}

		public void RefreshConnection()
		{
			// start a coroutine so that all the hatches have been initialized
			gameObject.SetActive(true);
			StartCoroutine(CheckForConnection());
		}

		IEnumerator CheckForConnection()
		{
			yield return null;

			while (vessel.packed)
			{
				yield return null;
			}

			if (_connectedHatch == null)
			{
				_connectedHatch = FindConnectedHatch();
			}

			bool useBlockedProp = false;

			if (_connectedHatch != null)
			{
				CanEVA = false;
				_connectedHatch._connectedHatch = this;
			}
			else
			{
				CanEVA = airlockName != string.Empty;

				var attachNode = part.FindAttachNode(attachNodeId);
				var attachedPart = attachNode?.attachedPart;
				if (attachedPart != null)
				{
					var attachedIvaModule = attachedPart.GetModule<ModuleFreeIva>();
					CanEVA = attachedIvaModule == null ? false : attachedIvaModule.doesNotBlockEVA;
				}

				useBlockedProp = !CanEVA && blockedPropName != string.Empty;
			}

			if (useBlockedProp && m_blockedProp == null)
			{
				m_blockedProp = PropHatch.CreateProp(blockedPropName, internalProp);
				if (m_blockedProp == null)
				{
					blockedPropName = string.Empty; // clear this so that we'll get a message about the hatch being blocked
				}
			}

			if (m_blockedProp != null)
			{
				gameObject.SetActive(!useBlockedProp);
				m_blockedProp.gameObject.SetActive(useBlockedProp);

				if (!useBlockedProp)
				{
					internalModel.props.Remove(m_blockedProp);
					GameObject.Destroy(m_blockedProp);
					m_blockedProp = null;
				}
			}

			if (_connectedHatch != null && hideDoorWhenConnected)
			{
				Open(true, false);
				HideOnOpen(true, true);
				_connectedHatch.HideOnOpen(true, true);
				if (m_doorTransform != null)
				{
					// right now this is redundant, but eventually doors will animate open instead of disappearing
					m_doorTransform.gameObject.SetActive(false);
				}
				if (_connectedHatch.m_doorTransform != null)
				{
					_connectedHatch.m_doorTransform.gameObject.SetActive(false);
				}

				enabled = false;
				_connectedHatch.enabled = false;
			}

			SetTubeScale();
		}

		private void OnHandleClick()
		{
			ToggleHatch();
		}

		private FreeIvaHatch FindConnectedHatch()
		{
			// these variables are set in the below loop
			AttachNode otherNode = null;
			InternalModuleFreeIva otherIvaModule = null;

			// if we're docked, find the hatch on the other side (these don't use AttachNodes)
			if (m_dockingNodeModule != null && m_dockingNodeModule.state.Contains("Docked"))
			{
				ModuleDockingNode otherDockingNode = m_dockingNodeModule.otherNode;
				otherIvaModule = InternalModuleFreeIva.GetForModel(otherDockingNode.part.internalModel);
				if (otherIvaModule != null)
				{
					foreach (var otherHatch in otherIvaModule.Hatches)
					{
						if (otherHatch.m_dockingNodeModule == otherDockingNode)
						{
							return otherHatch;
						}
					}
				}

				return null;
			}

			AttachNode currentNode = part.FindAttachNode(attachNodeId);
			if (currentNode == null) return null;

			// find the Iva module and attachnode that this hatch connects to, possibly through a chain of passthrough parts
			Part currentPart = part;
			Part otherPart = currentNode.attachedPart;
			while (otherPart != null)
			{
				// note: FindOpposingNode doesn't seem to work because currentNode.owner seems to be null
				otherNode = otherPart.FindAttachNodeByPart(currentPart);

				// if we found an internal module, we're done
				otherIvaModule = InternalModuleFreeIva.GetForModel(otherPart.internalModel);
				if (otherIvaModule != null) break;

				// if there's a part module, see if it supports passthrough for this node
				var ivaPartModule = otherPart.GetModule<ModuleFreeIva>();
				if (ivaPartModule == null) return null;

				string passThroughNodeName = null;
				if (otherNode.id == ivaPartModule.passThroughNodeA)
				{
					passThroughNodeName = ivaPartModule.passThroughNodeB;
				}
				else if (otherNode.id == ivaPartModule.passThroughNodeB)
				{
					passThroughNodeName = ivaPartModule.passThroughNodeA;
				}
				else
				{
					// no passthrough; we're done
					return null;
				}

				// get the node on the far side of the other part
				currentNode = otherPart.FindAttachNode(passThroughNodeName);

				if (currentNode == null)
				{
					Debug.LogError($"[FreeIva] node {passThroughNodeName} wasn't found in part {otherPart.partInfo.name}");
					return null;
				}

				currentPart = otherPart;
				otherPart = currentNode.attachedPart;
			}

			if (otherIvaModule == null) return null;

			// look for a hatch that is on the node we're connected to
			foreach (var otherHatch in otherIvaModule.Hatches)
			{
				if (otherHatch.attachNodeId == otherNode.id)
				{
					return otherHatch;
				}
			}

			return null;
		}

		public FXGroup SetupAudio(string soundFile, string id)
		{
			FXGroup result = null;
			if (!string.IsNullOrEmpty(soundFile))
			{
				result = new FXGroup(id);
				result.audio = gameObject.AddComponent<AudioSource>(); // TODO: if we deactivate this object when the hatch opens, do we need to put the sound on a different object?
				result.audio.dopplerLevel = 0f;
				result.audio.Stop();
				result.audio.clip = GameDatabase.Instance.GetAudioClip(soundFile);
				result.audio.loop = false;
			}

			return result;
		}

		public void ToggleHatch()
		{
			Open(!IsOpen);
		}

		public virtual void Open(bool open, bool allowSounds = true)
		{
			var connectedHatch = ConnectedHatch;

			if (open && IsBlockedByAnimation())
			{
				// can't do anything.
			}
			// if we're trying to open a door to space, just go EVA
			else if (connectedHatch == null && open)
			{
				GoEVA();
			}
			else
			{
				if (m_doorTransform != null)
				{
					m_doorTransform.gameObject.SetActive(!open);
				}

				HideOnOpen(open, false);

				if (open != IsOpen)
				{
					if (allowSounds)
					{
						var sound = open ? HatchOpenSound : HatchCloseSound;
						if (sound != null && sound.audio != null)
							sound.audio.Play();
					}
				}

				IsOpen = open;

				// automatically toggle the far hatch too
				if (connectedHatch != null && connectedHatch.IsOpen != open)
				{
					connectedHatch.Open(open, allowSounds);
					FreeIva.SetRenderQueues(FreeIva.CurrentPart);
				}
			}
		}

		public virtual void HideOnOpen(bool open, bool permanent)
		{
			if (HideWhenOpen == null) return;

			MeshRenderer[] meshRenderers = internalModel.GetComponentsInChildren<MeshRenderer>();
			foreach (var hideProp in HideWhenOpen.objects)
			{
				bool found = false;

				foreach (MeshRenderer mr in meshRenderers)
				{
					if (mr.name.Equals(hideProp.name) && mr.transform != null)
					{
						float error = Vector3.Distance(mr.transform.localPosition, hideProp.position);
						if (error < 0.15)
						{
							Debug.Log("# Toggling " + mr.name);

							if (permanent)
							{
								GameObject.Destroy(mr.gameObject);
							}
							else
							{
								mr.gameObject.SetActive(!open);
							}
							found = true;
							break;
						}
					}
				}

				if (!found)
				{
					Debug.LogError($"[FreeIva] HideWhenOpen - could not find meshrenderer named {hideProp.name} in model {internalModel.internalName}");
				}
			}
		}

		static Transform FindAirlock(Part part, string airlockName)
		{
			if (!string.IsNullOrEmpty(airlockName))
			{
				var childTransform = TransformUtil.FindPartTransform(part, airlockName);

				if (childTransform != null && childTransform.CompareTag("Airlock"))
				{
					return childTransform;
				}
				else if (childTransform == null)
				{
					Debug.LogError($"[FreeIva] could not find airlock transform named {airlockName} on part {part.partInfo.name}");
				}
				else
				{
					Debug.LogError($"[FreeIva] found airlock transform named {airlockName} on part {part.partInfo.name} but it doesn't have the 'Airlock' tag");
				}
			}

			return part.airlock;
		}

		KerbalEVA SpawnEVA(ProtoCrewMember pCrew, Part airlockPart, Transform fromAirlock)
		{
			var flightEVA = FlightEVA.fetch;

			Part crewPart = pCrew.KerbalRef.InPart;

			if (FlightEVA.HatchIsObstructed(part, fromAirlock)) // NOTE: stock code also checks hatchInsideFairing
			{
				ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_111978"), 5f, ScreenMessageStyle.UPPER_CENTER);
				return null;
			}
			flightEVA.overrideEVA = false;
			GameEvents.onAttemptEva.Fire(pCrew, crewPart, fromAirlock);
			if (flightEVA.overrideEVA)
			{
				return null;
			}

			// at this point we're *definitely* going EVA
			// manipulate the crew assignments to make this work.
			if (crewPart != airlockPart)
			{
				crewPart.RemoveCrewmember(pCrew);

				++airlockPart.CrewCapacity;
				airlockPart.AddCrewmember(pCrew);
				pCrew.KerbalRef.InPart = airlockPart;
				--airlockPart.CrewCapacity;
			}

			flightEVA.pCrew = pCrew;
			flightEVA.fromPart = airlockPart;
			flightEVA.fromAirlock = fromAirlock;
			return flightEVA.onGoForEVA();
		}

		public bool GoEVA()
		{
			float acLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);
			bool evaUnlocked = GameVariables.Instance.UnlockedEVA(acLevel);
			bool evaPossible = GameVariables.Instance.EVAIsPossible(evaUnlocked, vessel);

			Kerbal kerbal = CameraManager.Instance.IVACameraActiveKerbal;

			if (kerbal != null && evaPossible && HighLogic.CurrentGame.Parameters.Flight.CanEVA)
			{
				// var kerbalEVA = FlightEVA.fetch.spawnEVA(kerbal.protoCrewMember, kerbal.InPart, FindAirlock(kerbal.InPart, airlockName), true);
				var kerbalEVA = SpawnEVA(kerbal.protoCrewMember, part, FindAirlock(part, airlockName));

				if (kerbalEVA != null)
				{
					CameraManager.Instance.SetCameraFlight();
					return true;
				}
			}

			return false;
		}

		public static void InitialiseAllHatchesClosed()
		{
			foreach (Part p in FlightGlobals.ActiveVessel.Parts)
			{
				InternalModuleFreeIva mfi = InternalModuleFreeIva.GetForModel(p.internalModel);
				if (mfi != null)
				{
					foreach (var hatch in mfi.Hatches)
					{
						hatch.Open(false);
					}
				}
			}
		}
	}
}
