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
        public string transformName = string.Empty;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                Transform buttonTransform = internalProp.FindModelTransform(transformName);
                if (buttonTransform != null)
                {
                    var collider = buttonTransform.GetComponent<Collider>();
                    if (collider == null)
                    {
                        var colliderNodes = node.GetNodes("Collider");
                        if (colliderNodes.Length > 0)
                        {
                            buttonTransform.gameObject.layer = (int)Layers.InternalSpace;

                            foreach (var colliderNode in colliderNodes)
                            {
                                var c = CreateCollider(buttonTransform, colliderNode);
                                AddColliderVisualizer(c);
                            }
                        }
                        else
                        {
                            string dbgName = internalProp.hasModel ? internalProp.propName : internalModel.internalName;
                            Debug.LogError($"[FreeIVA] PropBuckleButton on {dbgName} does not have a collider on transform {transformName} and no procedural colliders");
                        }
                    }
                    else
                    {
                        AddColliderVisualizer(collider);
                    }
                }
                else
                {
                    Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.name} could not find a transform named {transformName}");
                }
            }
        }

        enum CapsuleAxis
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        void AddColliderVisualizer(Collider collider)
        {
#if false
            if (collider is BoxCollider box)
            {
                var debugObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Component.Destroy(debugObject.GetComponent<Collider>());
                debugObject.transform.SetParent(collider.transform, false);
                debugObject.transform.localPosition = box.center;
                debugObject.transform.localScale = box.size;
                debugObject.layer = 20;
            }
            // TODO:
#endif
        }

        Collider CreateCollider(Transform t, ConfigNode cfg)
        {
            Collider result = null;
            Vector3 center = Vector3.zero, boxDimensions = Vector3.zero;
            float radius = 0, height = 0;
            CapsuleAxis axis = CapsuleAxis.X;
            string colliderShape = string.Empty;
            
            if (!cfg.TryGetValue("shape", ref colliderShape))
            {
                Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.name} does not have a collider in the model and does not have a shape field");
            }
            else switch (colliderShape)
            {
                case "Capsule":
                    if (cfg.TryGetValue("center", ref center) &&
                        cfg.TryGetValue("radius", ref radius) &&
                        cfg.TryGetValue("height", ref height) &&
                        cfg.TryGetEnum("axis", ref axis, CapsuleAxis.X))
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
                        Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: capsule shape requires center, radius, height, and axis fields");
                    }
                    break;
                case "Box":
                    if (cfg.TryGetValue("center", ref center) &&
                        cfg.TryGetValue("dimensions", ref boxDimensions))
                    {
                        var collider = t.gameObject.AddComponent<BoxCollider>();
                        collider.center = center;
                        collider.size = boxDimensions;
                        result = collider;
                    }
                    else
                    {
                        Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: box shape requires center and dimensions fields");
                    }
                    break;
                case "Sphere":
                    if (cfg.TryGetValue("center", ref center) &&
                        cfg.TryGetValue("radius", ref radius))
                    {
                        var collider = t.gameObject.AddComponent<SphereCollider>();
                        collider.center = center;
                        collider.radius = radius;
                        result = collider;
                    }
                    else
                    {
                        Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName}: sphere shape requires center and radius fields");
                    }
                    break;
                default:
                    Debug.LogError($"[FreeIVA] PropBuckleButton on {internalProp.propName} has invalid collider shape '{colliderShape}");
                    break;
            }

            return result;
        }

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            Transform buttonTransform = internalProp.FindModelTransform(transformName);
            if (buttonTransform != null)
            {
                ClickWatcher clickWatcher = buttonTransform.gameObject.GetOrAddComponent<ClickWatcher>();

                clickWatcher.AddMouseDownAction(OnClick);
            }
        }

        private void OnClick()
        {
            KerbalIvaController.Instance.Unbuckle();
        }
    }
}
