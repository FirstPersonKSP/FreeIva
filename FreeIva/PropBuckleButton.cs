using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
    public class PropBuckleButton : InternalModule
    {
        [KSPField]
        public string buttonTransformName;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                Transform buttonTransform = internalProp.FindModelTransform(buttonTransformName);
                if (buttonTransform != null)
                {
                    var collider = buttonTransform.GetComponent<Collider>();
                    if (collider == null)
                    {
                        collider = CreateCollider(buttonTransform, node);
                    }
                }
            }
        }

        enum CapsuleAxis
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        Collider CreateCollider(Transform t, ConfigNode cfg)
        {
            Collider result = null;
            Vector3 center = Vector3.zero, boxDimensions = Vector3.zero;
            float radius = 0, height = 0;
            CapsuleAxis axis = CapsuleAxis.X;
            string colliderShape = string.Empty;
            
            if (!cfg.TryGetValue("colliderShape", ref colliderShape))
            {
                Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.name} does not have a collider in the model and does not have a colliderShape field");
            }
            else switch (colliderShape)
            {
                case "Capsule":
                    if (cfg.TryGetValue("colliderCenter", ref center) &&
                        cfg.TryGetValue("colliderRadius", ref radius) &&
                        cfg.TryGetValue("colliderHeight", ref height) &&
                        cfg.TryGetEnum("colliderAxis", ref axis, CapsuleAxis.X))
                    {
                        var collider = t.gameObject.AddComponent<CapsuleCollider>();
                        collider.radius = radius;
                        collider.height = height;
                        collider.center = center;
                        collider.direction = (int)axis;
                        result = collider;
                    }
                    else
                    {
                        Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: capsule shape requires colliderCenter, colliderRadius, colliderHeight, and colliderAxis fields");
                    }
                    break;
                case "Box":
                    if (cfg.TryGetValue("colliderCenter", ref center) &&
                        cfg.TryGetValue("colliderDimensions", ref boxDimensions))
                    {
                        var collider = t.gameObject.AddComponent<BoxCollider>();
                        collider.center = center;
                        collider.size = boxDimensions;
                        result = collider;
                    }
                    else
                    {
                        Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: box shape requires colliderCenter and colliderDimensions fields");
                    }
                    break;
                case "Sphere":
                    if (cfg.TryGetValue("colliderCenter", ref center) &&
                        cfg.TryGetValue("colliderRadius", ref radius))
                    {
                        var collider = t.gameObject.AddComponent<SphereCollider>();
                        collider.center = center;
                        collider.radius = radius;
                        result = collider;
                    }
                    else
                    {
                        Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: sphere shape requires colliderCenter and colliderRadius fields");
                    }
                    break;
                default:
                    Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName} has invalid colliderShape '{colliderShape}");
                    break;
            }

            return result;
        }

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            Transform buttonTransform = internalProp.FindModelTransform(buttonTransformName);
            if (buttonTransform != null)
            {
                ClickWatcher clickWatcher = buttonTransform.gameObject.GetOrAddComponent<ClickWatcher>();

                clickWatcher.AddMouseDownAction(OnClick);
            }
            else
            {
                Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName} could not find transform named {buttonTransformName}");
            }
        }

        private void OnClick()
        {
            KerbalIvaController.Instance.Unbuckle();
        }
    }
}
