using System;
using UnityEngine;

namespace FreeIva
{
	/// <summary>
	/// A hatch which is based on an IVA prop object with animation support for opening and closing.
	/// </summary>
	/// <remarks>Rewrite of Hatch et al. These need to be merged/replaced at some point.</remarks>
	public class PropHatchAnimated : InternalModule //Hatch
	{
		public string openAnimationName;
		public string unlockAnimationName;
		public Animation unlockAnimation;
		public Animation openAnimation;
		//public string triggerTransform;
		public string unlockTriggerTransform;
		public string openTriggerTransform;
		public bool animationStartsOpened;
		public bool animationStartsUnlocked;
		public bool invertOpenState;
		public bool invertLockedState;
		private bool _isOpen = false;
		private bool _isLocked = false;

		//private GameObject depthMaskObject;
		private Renderer depthMaskRenderer;
		//private Collider depthMaskCollider;

		public string depthMaskPositionText;
		private Vector3 depthMaskPosition;
		public string depthMaskScaleText;
		private Vector3 depthMaskScale;
		public string depthMaskRotationText;
		private Quaternion depthMaskRotation;

		public override void OnLoad(ConfigNode node)
		{
			if (node.HasValue(nameof(openAnimationName)))
			{
				node.TryGetValue("openAnimationName", ref openAnimationName);
			}
			else
			{
				Debug.LogError($"[FreeIVA] PropHatchAnimated: No {nameof(openAnimationName)} found.");
				return;
			}

			if (node.HasValue(nameof(unlockAnimationName)))
			{
				node.TryGetValue(nameof(unlockAnimationName), ref unlockAnimationName);
			}
			else
			{
				Debug.LogError($"[FreeIVA] PropHatchAnimated: No {nameof(unlockAnimationName)} found.");
				return;
			}

			if (node.HasValue(nameof(openTriggerTransform)))
			{
				node.TryGetValue(nameof(openTriggerTransform), ref openTriggerTransform);
			}
			else
			{
				Debug.LogError($"[FreeIVA] PropHatchAnimated: No {nameof(openTriggerTransform)} found.");
				return;
			}

			if (node.HasValue(nameof(unlockTriggerTransform)))
			{
				unlockTriggerTransform = node.GetValue(nameof(unlockTriggerTransform));
			}

			if (node.HasValue(nameof(animationStartsOpened)))
			{
				node.TryGetValue(nameof(animationStartsOpened), ref animationStartsOpened);
			}

			if (node.HasValue(nameof(animationStartsUnlocked)))
			{
				node.TryGetValue(nameof(animationStartsUnlocked), ref animationStartsUnlocked);
			}

			if (node.HasValue(nameof(invertOpenState)))
			{
				node.TryGetValue(nameof(invertOpenState), ref invertOpenState);
			}

			if (node.HasValue(nameof(invertLockedState)))
			{
				node.TryGetValue(nameof(invertLockedState), ref invertLockedState);
			}

			if (node.HasValue(nameof(HatchOpenSoundFile)))
			{
				node.TryGetValue(nameof(HatchOpenSoundFile), ref HatchCloseSoundFile);
			}
			if (node.HasValue(nameof(HatchCloseSoundFile)))
			{
				node.TryGetValue(nameof(HatchCloseSoundFile), ref HatchCloseSoundFile);
			}

			if (node.HasNode("depthMask"))
			{
				LoadDepthMask(node);
			}
		}

		private void LoadDepthMask(ConfigNode node)
		{
			ConfigNode depthMaskNode = node.GetNode("depthMask");

			if (depthMaskNode.HasValue("position"))
			{
				depthMaskPositionText = depthMaskNode.GetValue("position");
				string[] p = depthMaskPositionText.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
				if (p.Length != 3)
				{
					Debug.LogWarning("[FreeIVA] Invalid depthMask position definition \"" + depthMaskPositionText + "\": Must be in the form x, y, z.");
				}
				else
					depthMaskPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
			}

			if (depthMaskNode.HasValue("scale"))
			{
				depthMaskScaleText = depthMaskNode.GetValue("scale");
				string[] s = depthMaskScaleText.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
				if (s.Length != 3)
				{
					Debug.LogWarning("[FreeIVA] Invalid depthMask scale definition \"" + depthMaskScaleText + "\": Must be in the form x, y, z.");
				}
				else
					depthMaskScale = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
			}

			if (depthMaskNode.HasValue("rotation"))
			{
				depthMaskRotationText = depthMaskNode.GetValue("rotation");
				string[] s = depthMaskRotationText.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
				if (s.Length == 3)
					depthMaskRotation = Quaternion.Euler(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
				else if (s.Length == 4)
					depthMaskRotation = new Quaternion(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
				else
					Debug.LogWarning("[FreeIVA] Invalid depthMask rotation definition \"" + depthMaskRotationText + "\": Must be in the form x, y, z.");
			}
		}

		public void Start()
		{
			if (HighLogic.LoadedSceneIsFlight == false)
				return;

			IvaGameObject = internalProp.gameObject;

			// Events
			if (!string.IsNullOrEmpty(unlockTriggerTransform))
			{
				Transform unlockTransform = TransformUtil.FindPropTransform(internalProp, unlockTriggerTransform);
				if (unlockTransform != null)
				{
					GameObject unlockTriggerObject = unlockTransform.gameObject;
					if (unlockTriggerObject != null)
					{
						ClickWatcher clickWatcher = unlockTriggerObject.GetComponent<ClickWatcher>();
						if (clickWatcher == null)
						{
							clickWatcher = unlockTriggerObject.AddComponent<ClickWatcher>();
							clickWatcher.AddMouseDownAction(() => ToggleLock());
						}
					}
				}
				else
				{
					Debug.LogError("[FreeIVA] PropHatchAnimated: Unable to find unlockTriggerTransform \"" + unlockTriggerTransform + "\".");
				}
			}
			if (!string.IsNullOrEmpty(openTriggerTransform))
			{
				Transform openTransform = TransformUtil.FindPropTransform(internalProp, openTriggerTransform);
				if (openTransform != null)
				{
					GameObject openTriggerObject = openTransform.gameObject;
					if (openTriggerObject != null)
					{
						ClickWatcher clickWatcher = openTriggerObject.GetComponent<ClickWatcher>();
						if (clickWatcher == null)
						{
							clickWatcher = openTriggerObject.AddComponent<ClickWatcher>();
							clickWatcher.AddMouseDownAction(() => ToggleHatch());
						}
					}
				}
				else
				{
					Debug.LogError("[FreeIVA] PropHatchAnimated: Unable to find unlockTriggerTransform \"" + openTriggerTransform + "\".");
				}
			}

			Collider[] colliders = IvaGameObject.GetComponentsInChildren<Collider>();
			foreach (Collider collider in colliders)
			{
				collider.material.bounciness = 0;
			}

			// Animations
			Animation[] unlockAnimations = internalProp.FindModelAnimators(unlockAnimationName);
			if (unlockAnimations == null || unlockAnimations.Length == 0)
			{
				Debug.LogError("[FreeIVA] PropHatchAnimated: unlockAnimationName " + unlockAnimationName + " was not found.");
				return;
			}
			unlockAnimation = unlockAnimations[0];
			AnimationState unlockAnimationState = unlockAnimation[unlockAnimationName];
			if (invertLockedState)
				_isLocked = !_isLocked;
			if (animationStartsUnlocked)
				unlockAnimationState.speed = -unlockAnimationState.speed;


			Animation[] openAnimations = internalProp.FindModelAnimators(openAnimationName);
			if (openAnimations == null || openAnimations.Length == 0)
			{
				Debug.LogError("[FreeIVA] PropHatchAnimated: openAnimationName " + openAnimationName + " was not found.");
				return;
			}
			openAnimation = openAnimations[0];
			AnimationState openAnimationState = openAnimation[openAnimationName];
			if (invertLockedState)
				_isOpen = !_isOpen;
			if (animationStartsOpened)
				openAnimationState.speed = -openAnimationState.speed;

			/*/ Depth mask disc
             * 
             * TODO: Needs to be loaded from config properly.
             * 
            depthMaskObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            MonoBehaviour.Destroy(depthMaskObject.GetComponentCached<Collider>(ref depthMaskCollider));
            if (part.internalModel == null)
                part.CreateInternalModel(); // TODO: Detect this in an event instead.
            depthMaskObject.transform.parent = part.internalModel.transform;
            depthMaskObject.layer = (int)Layers.InternalSpace;
            depthMaskObject.transform.localScale = new Vector3(0.512f, 1, 0.512f); //depthMaskScale; TODO
            depthMaskObject.transform.localPosition = new Vector3(0, 0, 0.643f); // depthMaskPosition; TODO
            depthMaskObject.transform.localRotation = Quaternion.Euler(270, 0, 0); //depthMaskRotation; TODO
            depthMaskObject.name = "Hatch Depth Mask";
            
            Shader depthMaskShader = Utils.GetDepthMask();
            if (depthMaskShader != null)
                depthMaskObject.GetComponentCached<Renderer>(ref depthMaskRenderer).material.shader = depthMaskShader;
            depthMaskObject.GetComponentCached<Renderer>(ref depthMaskRenderer).shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ChangeMesh(depthMaskObject);*/

			//CreateDepthMask();
			SetupAudio();
		}

		/*public void CreateDepthMask()
        {
            depthMaskObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            MonoBehaviour.Destroy(depthMaskObject.GetComponent<Collider>());
            depthMaskObject.transform.parent = this.transform;
            depthMaskObject.layer = (int)Layers.InternalSpace;

            // Restore cleared values.
            depthMaskObject.transform.localPosition = depthMaskPosition;
            depthMaskObject.transform.localScale = depthMaskScale;
            depthMaskObject.transform.localRotation = depthMaskRotation;
            depthMaskObject.name = this.Name + " DepthMask";
            /*if (Collider != null)
            {
                Debug.Log("#Initialising hatch collider");
                Collider.Init(p);
            }* /

            Shader depthMask = Utils.GetDepthMask();
            if (depthMask != null)
                depthMaskObject.GetComponentCached<Renderer>(ref IvaGameObjectRenderer).material.shader = depthMask;

            ChangeMesh(depthMaskObject);
        }*/

		//public override bool IsOpen
		public bool IsOpen
		{
			get
			{
				return _isOpen;
			}
		}

		public bool IsLocked
		{
			get
			{
				return _isLocked;
			}
		}

		//public override void Open(bool open)
		public void Open(bool open)
		{
			if (open && _isLocked)
			{
				ScreenMessages.PostScreenMessage("Hatch is locked", 1f, ScreenMessageStyle.LOWER_CENTER);
				return;
			}

			if (IvaGameObject != null)
			{
				Renderer r = IvaGameObject.GetComponentCached<Renderer>(ref depthMaskRenderer);
				if (r != null)
					r.enabled = open;
			}

			AnimationState animationState = unlockAnimation[openAnimationName];
			if (open ^ animationState.speed > 0)
				animationState.speed = -animationState.speed;
			if (animationState.speed > 0)
			{
				animationState.normalizedTime = 0f;
			}
			else
			{
				animationState.normalizedTime = 1f;
			}
			unlockAnimation.clip = animationState.clip; // This animation player could be used for multiple clips.
			unlockAnimation.enabled = true;
			unlockAnimation.wrapMode = WrapMode.Once;
			unlockAnimation.Play();

			FreeIva.SetRenderQueues(FreeIva.CurrentPart);

			if (open)
				HatchOpenSound.audio.Play();
			else
				HatchCloseSound.audio.Play();

			_isOpen = !_isOpen;
		}

		public void Lock(bool lockHatch)
		{
			if (_isOpen)
			{
				return;
			}

			AnimationState animationState = unlockAnimation[unlockAnimationName];
			if (!lockHatch ^ animationState.speed > 0)
				animationState.speed = -animationState.speed;
			if (animationState.speed > 0)
			{
				animationState.normalizedTime = 0f;
			}
			else
			{
				animationState.normalizedTime = 1f;
			}
			unlockAnimation.clip = animationState.clip; // This animation player could be used for multiple clips.
			unlockAnimation.enabled = true;
			unlockAnimation.wrapMode = WrapMode.Once;
			unlockAnimation.Play();

			_isLocked = !_isLocked;
		}

		//
		// TODO: All the below are temporarily copied from Hatch!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		//       Clean these up in.
		//
		GameObject IvaGameObject;

		public static void ChangeMesh(GameObject original)
		{
			try
			{
				string modelPath = "FreeIva/Models/HatchMask";
				GameObject hatchMask = GameDatabase.Instance.GetModel(modelPath);
				if (hatchMask != null)
				{
					MeshFilter mfC = original.GetComponent<MeshFilter>();
					MeshFilter mfM = hatchMask.GetComponent<MeshFilter>();
					if (mfM == null)
					{
						Debug.LogError("[Free IVA] MeshFilter not found in mesh " + modelPath);
					}
					else
					{
						Mesh m = FreeIva.Instantiate(mfM.mesh) as Mesh;
						mfC.mesh = m;
					}
				}
				else
					Debug.LogError("[Free IVA] HatchMask.dae not found at " + modelPath);
			}
			catch (Exception ex)
			{
				Debug.LogError("[Free IVA] Error Loading mesh: " + ex.Message + ", " + ex.StackTrace);
			}
		}

		public string HatchOpenSoundFile = "FreeIva/Sounds/HatchOpen";
		public string HatchCloseSoundFile = "FreeIva/Sounds/HatchClose";
		public FXGroup HatchOpenSound = null;
		public FXGroup HatchCloseSound = null;

		public void SetupAudio()
		{
			HatchOpenSound = new FXGroup("HatchOpen");
			HatchOpenSound.audio = IvaGameObject.AddComponent<AudioSource>();
			HatchOpenSound.audio.dopplerLevel = 0f;
			HatchOpenSound.audio.Stop();
			HatchOpenSound.audio.clip = GameDatabase.Instance.GetAudioClip(HatchOpenSoundFile);
			HatchOpenSound.audio.loop = false;

			HatchCloseSound = new FXGroup("HatchClose");
			HatchCloseSound.audio = IvaGameObject.AddComponent<AudioSource>();
			HatchCloseSound.audio.dopplerLevel = 0f;
			HatchCloseSound.audio.Stop();
			HatchCloseSound.audio.clip = GameDatabase.Instance.GetAudioClip(HatchCloseSoundFile);
			HatchCloseSound.audio.loop = false;
		}

		public void ToggleHatch()
		{
			Open(!IsOpen);
		}

		public void ToggleLock()
		{
			Lock(!IsLocked);
		}

	}
}