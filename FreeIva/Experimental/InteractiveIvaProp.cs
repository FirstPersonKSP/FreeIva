#if Experimental
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
    public class InteractiveIvaProp : InternalModule
    {
        public List<IvaAction> IvaActions;

        public override void OnLoad(ConfigNode configNode)
        {
            foreach (ConfigNode node in configNode.nodes)
            {
                if (node.name == "ACTION")
                {
                    // TODO
                }
            }
        }

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            foreach (IvaAction action in IvaActions)
            {
                // Events
                if (!String.IsNullOrEmpty(action.TriggerTransform))
                {
                    Transform transform = internalProp.FindModelTransform(action.TriggerTransform);
                    if (transform != null)
                    {
                        GameObject unlockTriggerObject = transform.gameObject;
                        if (unlockTriggerObject != null)
                        {
                            ClickWatcher clickWatcher = unlockTriggerObject.GetComponent<ClickWatcher>();
                            if (clickWatcher == null)
                            {
                                clickWatcher = unlockTriggerObject.AddComponent<ClickWatcher>();
                                clickWatcher.AddMouseDownAction(() => action.Toggle());
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("[FreeIVA] InteractiveIvaProp: Unable to find triggerTransform \"" + action.TriggerTransform + 
                            "\" for action \"" + action.Name + "\".");
                    }
                }

                // Animations
                Animation[] animations = internalProp.FindModelAnimators(action.AnimationName);
                if (animations == null || animations.Length == 0)
                {
                    Debug.LogError("[FreeIVA] InteractiveIvaProp: animationName " + action.AnimationName + " was not found for action \"" + action.Name + "\".");
                    return;
                }
                action.Animation = animations[0];
                AnimationState animationState = action.Animation[action.AnimationName];
                if (action.InvertState)
                    action.IsActive = !action.IsActive;
                if (action.ReverseAnimation)
                    animationState.speed = -animationState.speed;

                action.SetupAudio(internalProp.gameObject);
            }
        }
    }

    public class IvaAction
    {
        public string Name { get; set; }
        public string AnimationName { get; set; }
        public string TriggerTransform { get; set; }
        public bool ReverseAnimation { get; set; }
        public bool InvertState { get; set; }
        public bool IsActive { get; internal set; }
        public Animation Animation { get; set; }
        public FXGroup ActivateSound { get; set; }
        public string ActivateSoundFile { get; set; }
        public FXGroup DeactivateSound { get; set; }
        public string DeactivateSoundFile { get; set; }

        public void Toggle()
        {
            Activate(!IsActive);
        }

        public void Activate(bool activate)
        {
            IsActive = !IsActive;

            AnimationState animationState = Animation[AnimationName];
            if (IsActive ^ animationState.speed > 0)
                animationState.speed = -animationState.speed;
            if (animationState.speed > 0)
            {
                animationState.normalizedTime = 0f;
            }
            else
            {
                animationState.normalizedTime = 1f;
            }
            Animation.clip = animationState.clip; // This animation player could be used for multiple clips.
            Animation.enabled = true;
            Animation.wrapMode = WrapMode.Once;
            Animation.Play();

            FreeIva.SetRenderQueues(FreeIva.CurrentPart);

            if (IsActive)
            {
                if (ActivateSound != null && ActivateSound.audio != null)
                {
                    ActivateSound.audio.Play();
                }
            }
            else
            {
                if (DeactivateSound != null && DeactivateSound.audio != null)
                {
                    DeactivateSound.audio.Play();
                }
            }
        }
        
        public void SetupAudio(GameObject gameObject)
        {
            ActivateSound = new FXGroup("HatchOpen");
            ActivateSound.audio = gameObject.AddComponent<AudioSource>();
            ActivateSound.audio.dopplerLevel = 0f;
            ActivateSound.audio.Stop();
            ActivateSound.audio.clip = GameDatabase.Instance.GetAudioClip(ActivateSoundFile);
            ActivateSound.audio.loop = false;

            DeactivateSound = new FXGroup("HatchClose");
            DeactivateSound.audio = gameObject.AddComponent<AudioSource>();
            DeactivateSound.audio.dopplerLevel = 0f;
            DeactivateSound.audio.Stop();
            DeactivateSound.audio.clip = GameDatabase.Instance.GetAudioClip(DeactivateSoundFile);
            DeactivateSound.audio.loop = false;
        }
    }
}
#endif
