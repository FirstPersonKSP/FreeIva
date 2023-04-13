using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	public class PhysicalProp : InternalModule
	{
		[KSPField]
		public bool isSticky = false; // when released, if this is overlapping another collider then it will attach to it

		[KSPField]
		public string placeSound = string.Empty;

		[KSPField]
		public string transformName = string.Empty;

		[KSPField]
		public Vector3 heldScale = Vector3.one;
		[KSPField]
		public Vector3 heldRotation = new Vector3(90, 225, 0);
		[KSPField]
		public Vector3 heldPosition = new Vector3(0.3f, -0.1f, 0.3f);
		[KSPField]
		public float throwSpeed = 1.0f;

		[SerializeField]
		CollisionTracker m_collisionTracker;

		[SerializeField]
		protected Interaction m_interaction;
		public bool HasInteraction => m_interaction != null;

		Rigidbody m_rigidBody;
		InternalModuleFreeIva m_freeIvaModule;

		[SerializeField]
		protected AudioSource m_audioSource;
		[SerializeReference]
		AudioClip m_grabAudioClip;
		[SerializeReference]
		AudioClip m_stickAudioClip;
		[SerializeReference]
		AudioClip m_impactAudioClip;

		[SerializeField]
		protected Collider m_collider;
		protected GameObject rigidBodyObject => m_collider.gameObject;

		[SerializeField]
		ClickWatcher m_clickWatcher;

		Vector3 originalScale;
		bool m_applyGravity = false;
		public bool IsGrabbed { get; private set; }

		internal static PhysicalProp HeldProp { get; private set; }

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				var colliderNode = node.GetNode("Collider");
				if (colliderNode != null)
				{
					string dbgName = internalProp.hasModel ? internalProp.propName : internalModel.internalName;

					Transform colliderTransform;
					var transformName = colliderNode.GetValue("parentTransformName");
					if (transformName != null)
					{
						colliderTransform = TransformUtil.FindPropTransform(internalProp, transformName);
					}
					else
					{
						colliderTransform = internalProp.hasModel ? transform : internalModel.transform;
					}

					if (colliderTransform != null)
					{
						m_collider = ColliderUtil.CreateCollider(colliderTransform, colliderNode, dbgName);
					}
				}
				else if (transformName != string.Empty)
				{
					var colliderTransform = TransformUtil.FindPropTransform(internalProp, transformName);
					if (colliderTransform != null)
					{
						m_collider = colliderTransform.GetComponent<Collider>();
					}
				}
				else
				{
					m_collider = GetComponentInChildren<Collider>();
				}

				if (m_collider != null)
				{
					m_collider.isTrigger = true;

					m_collider.gameObject.layer = 16; // needs to be 16 to bounce off shell colliders, at least while moving.  Not sure if we want it interacting with the player.

					m_collisionTracker = m_collider.gameObject.AddComponent<CollisionTracker>();
					m_collisionTracker.PhysicalProp = this;

					m_clickWatcher = m_collider.gameObject.AddComponent<ClickWatcher>();
				}
				else
				{
					Debug.LogError($"PhysicalProp: prop {internalProp.propName} does not have a collider");
				}

				if (m_collider != null)
				{
					// setup audio
					m_grabAudioClip = LoadAudioClip(node, "grabSound");
					m_stickAudioClip = LoadAudioClip(node, "stickSound");
					m_impactAudioClip = LoadAudioClip(node, "impactSound");

					if (m_grabAudioClip != null || m_stickAudioClip != null || m_impactAudioClip != null)
					{
						m_audioSource = m_collider.gameObject.AddComponent<AudioSource>();
						m_audioSource.volume = GameSettings.SHIP_VOLUME;
						m_audioSource.minDistance = 2;
						m_audioSource.maxDistance = 10;
						m_audioSource.playOnAwake = false;
						m_audioSource.spatialize = true;
					}

					// setup interactions (requires a collider)
					var interactionNode = node.GetNode("Interaction");
					if (interactionNode != null)
					{
						CreateInteraction(interactionNode);
					}
				}
			}
		}

		#region static stuff
		static Dictionary<string, TypeInfo> x_interactionTypes;

		static PhysicalProp()
		{
			x_interactionTypes = new Dictionary<string, TypeInfo>();
			foreach (var assembly in AssemblyLoader.loadedAssemblies)
			{
				try
				{
					assembly.TypeOperation(type =>
					{
						if (type.IsSubclassOf(typeof(Interaction)))
						{
							x_interactionTypes.Add(type.Name, type.GetTypeInfo());
						}
					});
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			}
		}

		public void CreateInteraction(ConfigNode interactionNode)
		{
			var name = interactionNode.GetValue("name");

			if (x_interactionTypes.TryGetValue(name, out var typeInfo))
			{
				m_interaction = (Interaction)gameObject.AddComponent(typeInfo.AsType());
				m_interaction.PhysicalProp = this;
				m_interaction.OnLoad(interactionNode);
			}
			else
			{
				Debug.LogError($"PROP {internalProp.propName}: No PhysicalProp.Interaction named {name} exists");
			}
		}
		#endregion

		protected void Start()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			if (m_clickWatcher != null)
			{
				m_clickWatcher.AddMouseUpAction(OnPropMouseUp);
			}
		}

		protected void OnDestroy()
		{
			if (HeldProp == this)
			{
				HeldProp = null;
			}
		}

		public void StartInteraction()
		{
			if (m_interaction) m_interaction.StartInteraction();
		}

		public void StopInteraction()
		{
			if (m_interaction) m_interaction.StopInteraction();
		}

		public void ThrowProp()
		{
			if (HeldProp != null)
			{
				HeldProp.Release(InternalCamera.Instance._camera.transform.forward * throwSpeed, Vector3.zero);
				HeldProp.transform.localScale = HeldProp.originalScale;
				HeldProp = null;
			}
		}

		private void OnPropMouseUp()
		{
			if (!IsGrabbed)
			{
				ThrowProp();

				HeldProp = this;

				originalScale = rigidBodyObject.transform.localScale;

				rigidBodyObject.transform.SetParent(InternalCamera.Instance._camera.transform, true);
				rigidBodyObject.transform.localPosition = heldPosition;
				rigidBodyObject.transform.localScale = heldScale;
				rigidBodyObject.transform.localRotation = Quaternion.Euler(heldRotation);
				Grab();
			}
		}

		public AudioClip LoadAudioClip(ConfigNode node, string key)
		{
			string clipUrl = node.GetValue(key);
			if (clipUrl == null) return null;

			AudioClip result = GameDatabase.Instance.GetAudioClip(clipUrl);

			if (result == null)
			{
				Debug.LogError($"Failed to find audio clip {clipUrl} for prop {internalProp.propName}");
			}

			return result;
		}

		public void PlayAudioClip(AudioClip clip, float volume, float pitch)
		{
			if (clip == null) return;
			m_audioSource.PlayOneShot(clip);
		}

		public void PlayAudioClip(AudioClip clip)
		{
			PlayAudioClip(clip, GameSettings.SHIP_VOLUME, 1.0f);
		}

		public void StartAudioLoop(AudioClip clip)
		{
			if (clip == null) return;
			m_audioSource.clip = clip;
			m_audioSource.loop = true;
			m_audioSource.volume = GameSettings.SHIP_VOLUME;
			m_audioSource.pitch = 1.0f;
			m_audioSource.Play();
		}

		public void StopAudioLoop()
		{
			m_audioSource.loop = false;
			m_audioSource.Stop();
		}

		public void OnImpact(float magnitude)
		{
			if (m_interaction != null)
			{
				m_interaction.OnImpact(magnitude);
			}

			// TOD: maybe randomize a bit?
			// m_audioSource.pitch = UnityEngine.Random.Range(-0.2f, 0.2f);
			float volume = Mathf.InverseLerp(1.0f, 5.0f, magnitude);
			if (volume == 0) return;

			PlayAudioClip(m_impactAudioClip, volume, UnityEngine.Random.Range(0.8f, 1.2f));
		}


		public void Stick(Vector3 position)
		{
			m_freeIvaModule = FreeIva.CurrentInternalModuleFreeIva;

			rigidBodyObject.transform.SetParent(m_freeIvaModule.Centrifuge?.IVARotationRoot ?? m_freeIvaModule.internalModel.transform, true);
			rigidBodyObject.transform.position = position;

			if (m_interaction)
			{
				m_interaction.OnRelease();
			}

			if (m_rigidBody != null)
			{
				Component.Destroy(m_rigidBody);
				m_rigidBody = null;
			}

			m_applyGravity = false;

			m_collider.isTrigger = true;
			m_collider.enabled = true;
			IsGrabbed = false;

			HeldProp = null;

			PlayStickyFeedback();
		}

		protected void Release(Vector3 linearVelocity, Vector3 angularVelocity)
		{
			// are we sticking to something?
			if (isSticky && m_collisionTracker.ContactCollider != null)
			{
				Stick(transform.position);
				return;
			}

			m_freeIvaModule = FreeIva.CurrentInternalModuleFreeIva;

			rigidBodyObject.transform.SetParent(m_freeIvaModule.Centrifuge?.IVARotationRoot ?? m_freeIvaModule.internalModel.transform, true);

			if (m_interaction)
			{
				m_interaction.OnRelease();
			}

			if (m_rigidBody == null)
			{
				m_rigidBody = rigidBodyObject.AddComponent<Rigidbody>();
			}

			m_collider.isTrigger = false;
			m_collider.enabled = true;

			m_rigidBody.isKinematic = true;
			m_rigidBody.useGravity = false;


			m_rigidBody.isKinematic = false;
			m_rigidBody.WakeUp();

			m_rigidBody.velocity = linearVelocity;
			m_rigidBody.angularVelocity = angularVelocity;

			// total hack? - apply reaction velocity in zero-g
			if (!KerbalIvaAddon.Instance.buckled && !KerbalIvaAddon.Instance.KerbalIva.UseRelativeMovement())
			{
				// TODO: should probably have some idea of how much mass this thing is
				KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody.WakeUp();
				KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody.velocity += -linearVelocity * 0.7f;
			}

			m_applyGravity = true;

			m_collider.enabled = true;
			IsGrabbed = false;

			// TODO: switch back to kinematic when it comes to rest (or not? it's fun to kick around)
		}

		protected void Grab()
		{
			// disable the collider so it doesn't push us around - or possibly we can just use Physics.IgnoreCollision
			if (isSticky)
			{
				m_collider.isTrigger = true;
			}
			else
			{
				m_collider.enabled = false;
			}

			if (m_rigidBody != null)
			{
				m_rigidBody.isKinematic = true;
				m_applyGravity = false;
			}

			if (m_interaction)
			{
				m_interaction.OnGrab();
			}

			PlayAudioClip(m_grabAudioClip);
			IsGrabbed = true;
		}

		void FixedUpdate()
		{
			if (m_applyGravity)
			{
				var accel = KerbalIvaAddon.GetInternalSubjectiveAcceleration(m_freeIvaModule, rigidBodyObject.transform.position);
				m_rigidBody.AddForce(accel, ForceMode.Acceleration);
			}
		}

		internal class CollisionTracker : MonoBehaviour
		{
			public PhysicalProp PhysicalProp;
			public Collider ContactCollider;

			void FixedUpdate()
			{
				ContactCollider = null;
				enabled = false;
			}

			void OnCollisionEnter(Collision other)
			{
				PhysicalProp.OnImpact(other.relativeVelocity.magnitude);
			}

			void OnTriggerExit(Collider other)
			{
				if (other == ContactCollider)
				{
					PhysicalProp.PlayStickyFeedback();
				}
			}

			void OnTriggerEnter(Collider other)
			{
				if (other.gameObject.layer == 16 && !other.isTrigger && PhysicalProp.IsGrabbed && !ColliderIsKerbalIva(other))
				{
					PhysicalProp.PlayStickyFeedback();
					ContactCollider = other;
					enabled = true;
				}
			}

			void OnTriggerStay(Collider other)
			{
				// how do we determine if this is a part of the iva shell?
				if (other.gameObject.layer == 16 && !other.isTrigger && PhysicalProp.IsGrabbed && !ColliderIsKerbalIva(other))
				{
					ContactCollider = other;
					enabled = true;
				}
			}

			static bool ColliderIsKerbalIva(Collider collider)
			{
				return collider.attachedRigidbody == KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody;
			}
		}

		protected virtual void PlayStickyFeedback()
		{
			if (isSticky)
			{
				PlayAudioClip(m_stickAudioClip);
			}
		}

		public class Interaction : MonoBehaviour
		{
			public PhysicalProp PhysicalProp;
			public bool IsInteracting { get; protected set; }

			public virtual void OnLoad(ConfigNode interactionNode) { }

			public virtual void OnGrab() { }
			public virtual void OnRelease() { }
			public virtual void OnImpact(float magnitude) { }

			public virtual void StartInteraction() {  IsInteracting = true; }
			public virtual void StopInteraction() { IsInteracting = false; }
		}

		public class InteractionBreakable : Interaction
		{
			[SerializeReference] AudioClip breakSound;
			[SerializeField] float breakSpeed = 4;
			[SerializeField] ParticleSystem m_particleSystem;

			public override void OnLoad(ConfigNode interactionNode)
			{
				breakSound = PhysicalProp.LoadAudioClip(interactionNode, nameof(breakSound));

				interactionNode.TryGetValue(nameof(breakSpeed), ref breakSpeed);

				string particleSystemName = interactionNode.GetValue(nameof(particleSystemName));
				if (particleSystemName != null)
				{
					var particlePrefab = AssetLoader.Instance.GetGameObject(particleSystemName);
					if (particlePrefab != null)
					{
						var particleObject = GameObject.Instantiate(particlePrefab);

						particleObject.layer = 20;
						particleObject.transform.SetParent(PhysicalProp.m_collider.transform, false);
						particleObject.transform.localPosition = PhysicalProp.m_collider.bounds.center;

						m_particleSystem = particleObject.GetComponent<ParticleSystem>();
					}
				}
			}

			public override void OnImpact(float magnitude)
			{
				if (magnitude > breakSpeed)
				{
					var freeIvaModule = FreeIva.CurrentInternalModuleFreeIva;

					m_particleSystem.transform.SetParent(freeIvaModule.Centrifuge?.IVARotationRoot ?? freeIvaModule.internalModel.transform, true);
					m_particleSystem.Play();

					if (breakSound != null)
					{
						var audioSource = Utils.CloneComponent(PhysicalProp.m_audioSource, m_particleSystem.gameObject);

						audioSource.PlayOneShot(breakSound);
					}

					GameObject.Destroy(PhysicalProp.rigidBodyObject);
				}
			}
		}

		public class InteractionSqueak : Interaction
		{
			[SerializeReference] AudioClip m_squeakSound;

			public override void OnLoad(ConfigNode interactionNode)
			{
				base.OnLoad(interactionNode);
				m_squeakSound = PhysicalProp.LoadAudioClip(interactionNode, "squeakSound");
			}

			public override void OnGrab()
			{
				PhysicalProp.PlayAudioClip(m_squeakSound);
			}

			public override void StartInteraction()
			{
				PhysicalProp.PlayAudioClip(m_squeakSound);
			}
		}

		public class InteractionExtinguisher : Interaction
		{
			[SerializeField] Vector3 thrustVector;
			[SerializeField] Vector3 thrustPosition;
			public Transform thrustTransform;
			public AudioClip m_audioClip;
			[SerializeField] ParticleSystem m_particleSystem;

			bool m_playingSound;

			public override void OnLoad(ConfigNode interactionNode)
			{
				base.OnLoad(interactionNode);

				string transformName = interactionNode.GetValue("thrustTransformName");
				thrustTransform = TransformUtil.FindPropTransform(PhysicalProp.internalProp, transformName);
				enabled = false;

				interactionNode.TryGetValue(nameof(thrustPosition), ref thrustPosition);

				if (!interactionNode.TryGetValue(nameof(thrustVector), ref thrustVector))
				{
					Debug.LogError($"PROP {PhysicalProp.internalProp.propName} InteractionExtinguisher could not parse key {nameof(thrustVector)}");
				}

				m_audioClip = PhysicalProp.LoadAudioClip(interactionNode, "sound");

				string particleSystemName = string.Empty;
				if (interactionNode.TryGetValue(nameof(particleSystemName), ref particleSystemName))
				{
					var particlePrefab = AssetLoader.Instance.GetGameObject(particleSystemName);
					if (particlePrefab != null)
					{
						var particleObject = GameObject.Instantiate(particlePrefab);

						particleObject.layer = 20;
						particleObject.transform.SetParent(thrustTransform, false);
						particleObject.transform.localPosition = thrustPosition;
						particleObject.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, -thrustVector);

						m_particleSystem = particleObject.GetComponent<ParticleSystem>();
					}
				}
			}

			public override void OnGrab()
			{
				enabled = true;
				m_particleSystem.gameObject.SetActive(true);
			}

			public override void OnRelease()
			{
				enabled = false;
			}

			void FixedUpdate()
			{
				if (IsInteracting)
				{
					Vector3 accelerationVector = thrustTransform.TransformVector(thrustVector);

					KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody.WakeUp();
					KerbalIvaAddon.Instance.KerbalIva.KerbalRigidbody.AddForceAtPosition(accelerationVector, thrustTransform.position, ForceMode.Acceleration);

					if (!m_playingSound)
					{
						PhysicalProp.StartAudioLoop(m_audioClip);
						m_playingSound = true;
						m_particleSystem.Play();
					}
				}
				else
				{
					if (m_playingSound)
					{
						PhysicalProp.StopAudioLoop();
						m_playingSound = false;
						m_particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
					}
				}
			}
		}

		public class InteractionCamera : Interaction
		{
			[SerializeReference] AudioClip m_shutterSound;

			public override void OnLoad(ConfigNode interactionNode)
			{
				base.OnLoad(interactionNode);

				m_shutterSound = PhysicalProp.LoadAudioClip(interactionNode, "shutterSound");
			}

			public override void StartInteraction()
			{
				PhysicalProp.PlayAudioClip(m_shutterSound);

				string screenshotDir = Application.platform == RuntimePlatform.OSXPlayer
					? Path.Combine(Application.dataPath, "../../Screenshots")
					: Path.Combine(Application.dataPath, "../Screenshots");
				string screenshotFileName = Path.GetRandomFileName();
				string screenshotPath = Path.ChangeExtension(Path.Combine(screenshotDir, screenshotFileName), ".png");

				ScreenCapture.CaptureScreenshot(screenshotPath, ScreenCapture.StereoScreenCaptureMode.BothEyes);
			}
		}

		public class InteractionFlashlight : Interaction
		{
			[SerializeReference] AudioClip m_buttonSound;
			[SerializeField] Material m_lensMaterial;
			[SerializeField] Light m_light;

			static int EMISSIVE_COLOR_PROPERTY_ID = Shader.PropertyToID("_EmissiveColor");

			public override void OnLoad(ConfigNode interactionNode)
			{
				base.OnLoad(interactionNode);

				m_buttonSound = PhysicalProp.LoadAudioClip(interactionNode, "buttonSound");
				m_lensMaterial = TransformUtil.FindPropTransform(PhysicalProp.internalProp, interactionNode.GetValue("lensObjectName"))?.GetComponent<MeshRenderer>()?.material;
				m_light = TransformUtil.FindPropTransform(PhysicalProp.internalProp, interactionNode.GetValue("lightObjectName"))?.GetComponent<Light>();

				if (m_light != null)
				{
					m_light.enabled = false;
				}

				if (m_lensMaterial != null)
				{
					m_lensMaterial.SetColor(EMISSIVE_COLOR_PROPERTY_ID, Color.black);
				}
			}
			public override void StartInteraction()
			{
				PhysicalProp.PlayAudioClip(m_buttonSound);

				if (m_light != null)
				{
					m_light.enabled = !m_light.enabled;

					if (m_lensMaterial != null)
					{
						m_lensMaterial.SetColor(EMISSIVE_COLOR_PROPERTY_ID, m_light.enabled ? Color.white : Color.black);
					}
				}
			}
		}
	}
}
