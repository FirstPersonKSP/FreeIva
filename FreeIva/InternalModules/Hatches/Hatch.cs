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
		public static readonly string AIRLOCK_TAG = "Airlock";

		// ----- fields set in prop config

		[KSPField]
		public string hatchOpenSoundFile = "FreeIva/Sounds/HatchOpen";

		[KSPField]
		public string hatchCloseSoundFile = "FreeIva/Sounds/HatchClose";

		[KSPField]
		public string doorTransformName = string.Empty;

		[KSPField]
		public string tubeTransformName = string.Empty;
		public Transform tubeTransform = null;

		[KSPField]
		public string cutoutTransformName = string.Empty;

		[KSPField]
		public string blockedPropName = string.Empty;

		[KSPField]
		public bool isEvaHatch = false;

		[KSPField]
		public string openAnimationName = string.Empty;

		// if the hatch has a window, we need to make sure we can't see other parts' internals through it when it's not connected to anything
		public MeshRenderer m_windowRenderer;

		// ----- the following fields are set via HatchConfig, so that they can be different per placement of the prop

		// The name of the part attach node this hatch is positioned on, as defined in the part.cfg's "node definitions".
		// e.g. top => node_stack_top.  Do not include the prefixes (they are stripped out during loading in the stock code).
		[KSPField]
		public string attachNodeId;

		// some docking ports don't use AttachNodes (inline ports, inflatable airlock, shielded)
		public string dockingPortNodeName = string.Empty;

		public bool requireDeploy = false;

		[SerializeReference]
		public ObjectsToHide HideWhenOpen;

		[KSPField]
		public string cutoutTargetTransformName = string.Empty;

		[KSPField]
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
		// specifically: you can't serialize a list of a custom struct, but you can serialize a reference to a class containing one
		public class ObjectsToHide : ScriptableObject
		{
			public List<ObjectToHide> objects = new List<ObjectToHide>();
		}

		[SerializeField]
		Transform[] m_handleTransforms;
		public Transform[] HandleTransforms => m_handleTransforms;

		[SerializeField]
		Transform m_doorTransform;
		ModuleDockingNode m_dockingNodeModule;
		InternalProp m_blockedProp;

		private FreeIvaHatch _connectedHatch = null;
		// The other hatch that this one is connected or docked to, if present.
		public FreeIvaHatch ConnectedHatch => _connectedHatch;

		public FXGroup HatchOpenSound = null;
		public FXGroup HatchCloseSound = null;

		public Animation m_animationComponent;
		public bool HasAnimation => m_animationComponent != null;

		public enum State
		{
			Closed,
			Opening,
			Open,
			Closing
		}

		public State CurrentState { get; private set; }

		public bool DesiredOpen { get; private set; }

		public bool IsOpen => CurrentState == State.Open;

		public bool CanEVA { get; private set; }
		float openAnimationLimit => CanEVA ? 0.5f : 1.0f;

		static Shader[] x_windowShaders = null;

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

			var deleteObjectsNode = node.GetNode("DeleteInternalObject");
			if (deleteObjectsNode != null)
			{
				DeleteInternalObject.DeleteObjects(internalProp, deleteObjectsNode);
			}

			foreach (var reparentNode in node.GetNodes("Reparent"))
			{
				ReparentUtil.Reparent(internalProp, reparentNode);
			}

			// handle transforms
			{
				var handleTransformNames = node.GetValuesList("handleTransformName");
				var handleTransformList = new List<Transform>(handleTransformNames.Count);
				foreach (var handleTransformName in handleTransformNames)
				{
					var handleTransform = TransformUtil.FindPropTransform(internalProp, handleTransformName);
					if (handleTransform != null)
					{
						handleTransformList.Add(handleTransform);
						handleTransform.gameObject.layer = (int)Layers.InternalSpace; // make sure these can be clicked and don't block the player
					}
				}
				m_handleTransforms = handleTransformList.ToArray();

				if (m_handleTransforms.Length > 0)
				{
					foreach (var collidernode in node.GetNodes("HandleCollider"))
					{
						ColliderUtil.CreateCollider(m_handleTransforms[0], collidernode, internalProp.propName);
					}
				}
			}

			// door transform
			{
				m_doorTransform = TransformUtil.FindPropTransform(internalProp, doorTransformName);

				if (m_doorTransform != null)
				{
					m_doorTransform.gameObject.layer = (int)Layers.Kerbals;

					foreach (var colliderNode in node.GetNodes("DoorCollider"))
					{
						ColliderUtil.CreateCollider(m_doorTransform, colliderNode, internalProp.propName);
					}
				}
				else if (doorTransformName != string.Empty)
				{
					Debug.LogError($"[FreeIva] doorTransform {doorTransformName} not found in {internalProp.propName}");
				}
			}

			// window transforms
			{
				string windowTransformName = node.GetValue(nameof(windowTransformName));
				if (windowTransformName != null)
				{
					var windowTransform = TransformUtil.FindPropTransform(internalProp, windowTransformName);
					m_windowRenderer = windowTransform?.GetComponentInChildren<MeshRenderer>();
					if (m_windowRenderer != null)
					{
						m_windowRenderer.material.renderQueue = InternalModuleFreeIva.WINDOW_RENDER_QUEUE;
					}
				}

				// try to find a window to manage
				// note this is similar to what we do for internal modules except we consider depth masks to be windows
				if (x_windowShaders == null)
				{
					x_windowShaders = new Shader[]
					{
						Shader.Find("KSP/Alpha/Translucent Specular"),
						Shader.Find("KSP/Alpha/Translucent"),
						Shader.Find("KSP/Alpha/Unlit Transparent"),
						Shader.Find("DepthMask")
					};
				}

				if (windowTransformName == null)
				{
					foreach (var renderer in internalProp.gameObject.GetComponentsInChildren<MeshRenderer>())
					{
						if (x_windowShaders.Contains(renderer.material.shader))
						{
							m_windowRenderer = renderer;
							renderer.material.renderQueue = InternalModuleFreeIva.WINDOW_RENDER_QUEUE;
							break;
						}
					}
				}
			}

			tubeTransform = TransformUtil.FindPropTransform(internalProp, tubeTransformName);
			if (tubeTransform != null)
			{
				foreach (var renderer in tubeTransform.GetComponentsInChildren<MeshRenderer>())
				{
					renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
				}
			}

			if (openAnimationName != string.Empty)
			{
				var animators = internalProp.FindModelAnimators(openAnimationName);
				m_animationComponent = animators.FirstOrDefault();
				if (m_animationComponent == null)
				{
					Debug.LogError($"[FreeIva] could not find animation named {openAnimationName} in prop {internalProp.propName}");
				}
			}
		}
		public override void OnAwake()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			Debug.Log($"# Creating hatch {internalProp.propName} for part {part.partInfo.name}");

			HatchOpenSound = SetupAudio(hatchOpenSoundFile, "HatchOpen");
			HatchCloseSound = SetupAudio(hatchCloseSoundFile, "HatchClose");

			foreach (var handleTransform in m_handleTransforms)
			{
				var clickWatcher = handleTransform.gameObject.AddComponent<ClickWatcher>();
				clickWatcher.AddMouseDownAction(OnHandleClick);
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

			var internalModule = InternalModuleFreeIva.GetForModel(internalModel);
			if (internalModule == null)
			{
				Debug.LogError($"[FreeIva] no InternalModuleFreeIva instance registered for internal {internalModel.internalName} for hatch prop {internalProp.propName}");
			}
			else
			{
				internalModule.Hatches.Add(this);

				if (requireDeploy)
				{
					internalModule.NeedsDeployable = true;
				}
			}

			RefreshConnection();
		}

		public bool IsBlockedByAnimation(bool checkConnectedHatch = true)
		{
			// check the other hatch first (non-recursively)
			if (checkConnectedHatch && _connectedHatch != null && _connectedHatch.IsBlockedByAnimation(false))
			{
				return true;
			}

			if (requireDeploy)
			{
				var internalModule = InternalModuleFreeIva.GetForModel(internalModel);
				var deployable = internalModule?.Deployable;
				if (deployable == null || !deployable.IsDeployed)
				{
					return true;
				}
			}

			return false;
		}

		static bool PartsAreConnected(Part partA, Part partB)
		{
			return partA.parent == partB || partB.parent == partA;
		}

		void SetTubeScale()
		{
			// scale tube appropriately
			if (tubeTransform != null)
			{
				float tubeScale = 0;
				tubeTransform.localScale = Vector3.one;
				Vector3 tubeDownVectorWorld = tubeTransform.TransformVector(Vector3.down);

				// if we're connected to another hatch, find the midpoint between the hatches
				if (_connectedHatch != null)
				{
					var otherTubeTransform = TransformUtil.FindPropTransform(_connectedHatch.internalProp, _connectedHatch.tubeTransformName);

					// if the other transform doesn't have a tube, then we scale ours to reach the other prop's origin
					// NOTE: we used to scale the tube to only reach the attachnode, which might avoid some edge cases but this is simpler and also handles docking ports and parts that were offset after attaching
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

				tubeTransform.gameObject.SetActive(tubeScale > 0);

				if (tubeScale > 0)
				{
					tubeScale /= tubeDownVectorWorld.magnitude;
					tubeTransform.localScale = new Vector3(1.0f, tubeScale, 1.0f);
				}
			}
		}

		public void RefreshConnection()
		{
			if (enabled)
			{
				// start a coroutine so that all the hatches have been initialized
				gameObject.SetActive(true);
				StartCoroutine(CheckForConnection());
			}
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
				CanEVA = airlockName != string.Empty || isEvaHatch;

				var attachNode = part.FindAttachNode(attachNodeId);

				if (attachNode == null && attachNodeId != string.Empty)
				{
					Debug.LogError($"[FreeIva] INTERNAL '{internalModel.internalName}' contains PROP '{internalProp.propName}' with attachNodeId '{attachNodeId}' but it was not found in PART '{part.partInfo.name}'");
				}

				var attachedPart = attachNode?.attachedPart;
				if (attachedPart != null)
				{
					var attachedIvaModule = attachedPart.GetModule<ModuleFreeIva>();
					CanEVA = attachedIvaModule == null ? false : attachedIvaModule.doesNotBlockEVA;
				}

				// don't use blocked props for docking port hatches, because they could become unblocked
				useBlockedProp = (attachNodeId != string.Empty && dockingPortNodeName == string.Empty) && !CanEVA && blockedPropName != string.Empty;
			}

			if (useBlockedProp && m_blockedProp == null)
			{
				m_blockedProp = CreateProp(blockedPropName, internalProp);
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
				SetOpened(true, false);
				HideOnOpen(true, true);

				if (_connectedHatch.m_doorTransform != null)
				{
					// Do we still want to do this? I think not...or at least turn it into a another config param
					// _connectedHatch.m_doorTransform.gameObject.SetActive(false);
				}

				enabled = false;
				//_connectedHatch.enabled = false;
			}

			// if we have a connection, or this is just some internal hatch with no functionality, we want to be able to see internals beyond the window, so set the draw order later
			if (m_windowRenderer != null)
			{
				if (ConnectedHatch != null || (dockingPortNodeName == string.Empty && attachNodeId == string.Empty && airlockName == string.Empty))
				{
					m_windowRenderer.material.renderQueue = 3000; // typical for transparent geometry
				}
				else
				{
					m_windowRenderer.material.renderQueue = InternalModuleFreeIva.WINDOW_RENDER_QUEUE;
				}
			}

			SetTubeScale();
		}

		private void OnHandleClick()
		{
			var interaction = GetInteraction();
			if (InteractionAllowed(interaction))
			{
				SetDesiredOpen(!DesiredOpen);
			}
			else
			{
				KerbalIvaAddon.PostHatchInteractionMessage(interaction);
			}
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

			while (otherIvaModule != null)
			{
				// look for a hatch that is on the node we're connected to
				foreach (var otherHatch in otherIvaModule.Hatches)
				{
					if (otherHatch.attachNodeId == otherNode.id)
					{
						return otherHatch;
					}
				}

				otherIvaModule = InternalModuleFreeIva.GetForModel(otherIvaModule.SecondaryInternalModel);
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
				result.audio.playOnAwake = false;
			}

			return result;
		}

		private void PlaySounds(bool open)
		{
			var sound = open ? HatchOpenSound : HatchCloseSound;
			if (sound != null && sound.audio != null)
				sound.audio.Play();
		}

		public enum Interaction
		{
			Open,
			Close,
			Blocked,
			Locked,
			EVA,
		}

		public Interaction GetInteraction()
		{
			if (IsBlockedByAnimation())
			{
				return Interaction.Locked;
			}
			else if (this.CanEVA)
			{
				return Interaction.EVA;
			}
			else if (ConnectedHatch == null && (attachNodeId != string.Empty || dockingPortNodeName != string.Empty))
			{
				return Interaction.Blocked;
			}
			else
			{
				return DesiredOpen ? Interaction.Close : Interaction.Open;
			}
		}

		public static bool InteractionAllowed(Interaction interaction)
		{
			switch (interaction)
			{
			case Interaction.Locked:
			case Interaction.Blocked:
				return false;
			default:
				return true;
			}
		}

		public void SetDesiredOpen(bool open)
		{
			if (!InteractionAllowed(GetInteraction()))
			{
				open = false;
			}

			DesiredOpen = open;
			if (HasAnimation)
			{
				if (m_animationCoroutine == null)
				{
					m_animationCoroutine = StartCoroutine(AnimationUpdate());
				}
			}
			else
			{
				SetOpened(open, true);
			}
		}

		void SetAnimationState(State newState)
		{
			var animState = m_animationComponent[openAnimationName];

			switch (newState)
			{
			case State.Closed:
				animState.normalizedTime = 0;
				m_animationComponent.Stop();
				m_animationCoroutine = null;
				break;

			case State.Opening:
				animState.speed = 1.0f;
				m_animationComponent.Play();
				break;

			case State.Closing:
				animState.speed = -1.0f;
				m_animationComponent.Play();
				break;

			case State.Open:
				animState.normalizedTime = openAnimationLimit;
				m_animationComponent.Stop();
				m_animationCoroutine = null;
				break;
			}

			CurrentState = newState;
		}

		Coroutine m_animationCoroutine;
		IEnumerator AnimationUpdate()
		{
			var animState = m_animationComponent[openAnimationName];
			m_animationComponent.wrapMode = WrapMode.ClampForever;

			while (true)
			{
				switch (CurrentState)
				{
				case State.Closed:
					if (DesiredOpen)
					{
						animState.normalizedTime = 0.0f;
						SetAnimationState(State.Opening);
						PlaySounds(DesiredOpen);
						SetDoorCollidersEnabled(false);
					}
					else
					{
						SetAnimationState(State.Closed);
						yield break;
					}
					break;

				case State.Opening:
					if (!DesiredOpen)
					{
						SetAnimationState(State.Closing);
					}
					else if (animState.normalizedTime >= openAnimationLimit)
					{
						SetAnimationState(State.Open);
						SetOpened(true, false);
						yield break;
					}
					break;

				case State.Closing:
					if (DesiredOpen)
					{
						SetAnimationState(State.Opening);
					}
					else if (animState.normalizedTime <= 0.0f)
					{
						SetAnimationState(State.Closed);
						SetOpened(false, false);
						yield break;
					}
					break;

				case State.Open:
					if (!DesiredOpen)
					{
						animState.normalizedTime = openAnimationLimit;
						SetAnimationState(State.Closing);
						PlaySounds(DesiredOpen);
					}
					else
					{
						SetAnimationState(State.Open);
						yield break;
					}
					break;
				}

				yield return null;
			}
		}

		void SetDoorCollidersEnabled(bool enabled)
		{
			foreach (var collider in m_doorTransform.GetComponentsInChildren<Collider>())
			{
				if (collider.gameObject.layer == (int)Layers.Kerbals)
				{
					collider.enabled = enabled;
				}
			}
		}

		public void SetOpened(bool open, bool allowSounds = true)
		{
			Open(open, allowSounds);
		}

		// leaving this here for the benefit of kerbalvr so it can be called directly (I think)
		protected virtual void Open(bool open, bool allowSounds = true)
		{
			var connectedHatch = ConnectedHatch;
			var interaction = GetInteraction();

			if (!InteractionAllowed(interaction))
			{
				open = false;
			}
			
			// if we're trying to open a door to space, just go EVA (what in the world would it mean if open == false?)
			if (open && interaction == Interaction.EVA)
			{
				GoEVA();
			}
			else
			{
				if (m_doorTransform != null)
				{
					if (HasAnimation && !hideDoorWhenConnected)
					{
						SetDoorCollidersEnabled(!open);
						SetAnimationState(open ? State.Open : State.Closed);
					}
					else
					{
						m_doorTransform.gameObject.SetActive(!open);
					}
				}

				HideOnOpen(open, false);

				if (open != IsOpen && allowSounds && !HasAnimation)
				{
					PlaySounds(open);
				}

				DesiredOpen = open;
				CurrentState = open ? State.Open : State.Closed;

				// automatically toggle the far hatch too
				if (connectedHatch != null && connectedHatch.IsOpen != open && !connectedHatch.HasAnimation)
				{
					connectedHatch.SetOpened(open, allowSounds);
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

				if (childTransform != null && childTransform.CompareTag(AIRLOCK_TAG))
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

		// NOTE: this code has been copied to RPM - should also be updated there if we make any changes
		KerbalEVA SpawnEVA(ProtoCrewMember pCrew, Part airlockPart, Transform fromAirlock)
		{
			var flightEVA = FlightEVA.fetch;

			Part crewPart = KerbalIvaAddon.GetPartContainingCrew(pCrew);

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

		public void GoEVA()
		{
			StartCoroutine(GoEVA_Coroutine());
		}

		private IEnumerator GoEVA_Coroutine()
		{
			float acLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);
			bool evaUnlocked = GameVariables.Instance.UnlockedEVA(acLevel);
			bool evaPossible = GameVariables.Instance.EVAIsPossible(evaUnlocked, vessel);

			Kerbal kerbal = CameraManager.Instance.IVACameraActiveKerbal;

			if (kerbal != null && evaPossible && HighLogic.CurrentGame.Parameters.Flight.CanEVA)
			{
				part.hatchObstructionCheckOutwardDistance = 0.5f;
				var kerbalEVA = SpawnEVA(kerbal.protoCrewMember, part, FindAirlock(part, airlockName));

				if (kerbalEVA != null)
				{
					CameraManager.Instance.SetCameraFlight();

					yield return null;

					// wait for kerbal to be ready
					while (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.packed ||
						FlightGlobals.ActiveVessel.evaController != gameObject.GetComponent<KerbalEVA>())
					{
						yield return null;
					}

					yield return null;

					ThroughTheEyes.EnterFirstPerson();
				}
			}
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
						hatch.SetOpened(false);
					}
				}
			}
		}

		static InternalProp CreateProp(string propName, InternalProp atProp)
		{
			return CreateProp(propName, atProp, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		static InternalProp CreateProp(string propName, InternalProp atProp, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			InternalProp result = PartLoader.GetInternalProp(propName);
			if (result == null)
			{
				Debug.LogError("[FreeIVA] Unable to load open prop hatch \"" + propName + "\" in internal " + atProp.internalModel.internalName);
			}
			else
			{
				result.propID = atProp.internalModel.props.Count;
				result.internalModel = atProp.internalModel;

				// position the prop relative to this one, then attach it to the internal model
				result.transform.SetParent(atProp.transform, false);
				result.transform.localRotation = localRotation;
				result.transform.localPosition = localPosition;
				result.transform.localScale = localScale;
				result.transform.SetParent(atProp.internalModel.transform, true);

				result.hasModel = true;
				atProp.internalModel.props.Add(result);
			}

			return result;
		}
	}
}
