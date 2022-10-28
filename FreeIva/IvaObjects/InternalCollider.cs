using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
    public class InternalCollider : IvaObject
    {
        public enum Type
        {
            Sphere,
            Capsule,
            Cylinder,
            Cube,
            Plane,
            Quad,
            Mesh
        }

        public Type ColliderType = Type.Cube;
        public string Model = string.Empty;

        public bool ColliderEnabled
        {
            get { return _colliderEnabled; }
            set
            {
                _colliderEnabled = value;
                foreach (var collider in IvaGameObjectColliders)
                {
                    collider.enabled = _colliderEnabled;
                }
            }
        }
        bool _colliderEnabled;

        public bool Visible
        {
            get
            {
                Renderer r = IvaGameObjectRenderer;
                if (r != null)
                {
                    return r.enabled;
                }
                else
                {
                    Debug.LogWarning("[FreeIVA] Tried get visibility of null renderer for " + IvaGameObject);
                }
                return false;
            }
            set
            {
                if (!AlwaysVisible)
                {
                    if (IvaGameObject != null)
                    {
                        if (IvaGameObjectRenderer != null)
                        {
                            IvaGameObjectRenderer.enabled = value;
                        }
                        else
                        {
#if DEBUG
                            Debug.LogWarning("[FreeIVA] Tried to change visibility of null renderer for " + IvaGameObject);
#endif
                        }
                    }
                    else
                    {
#if DEBUG
                        Debug.LogWarning("[FreeIVA] Tried to change visibility of null IvaGameObject");
#endif
                    }
                }
            }
        }
        public bool AlwaysVisible = false;

        //public override void OnLoad(ConfigNode node)
        //{
        //    LoadFromCfg(node);
        //    Instantiate(this.part);
        //}

        public InternalCollider()
        {

        }

        public InternalCollider(string name, Type colliderType, bool alwaysVisible, Vector3 localPosition, Vector3 scale, Quaternion rotation, string model)
        {
            Name = name;
            ColliderType = colliderType;
            AlwaysVisible = alwaysVisible;
            LocalPosition = localPosition;
            Scale = scale;
            Rotation = rotation;
            Model = model;
        }

        public InternalCollider Clone()
        {
            return new InternalCollider(Name, ColliderType, AlwaysVisible, LocalPosition, Scale, Rotation, Model);
        }

        public override void Instantiate(Part p)
        {
            Debug.Log("# Creating internal collider " + Name + " for part " + p);
            if (IvaGameObject != null)
            {
                // TODO: Prevents multiple copies of the same part from having colliders.
                Debug.LogError("[FreeIVA] InternalCollider " + Name + " has already been instantiated.");
                return;
            }

            // These values will be cleared on creating the object.
            Vector3 scale = Scale;
            Vector3 localPosition = LocalPosition;
            Quaternion rotation = Rotation;

            if (ColliderType == Type.Mesh)
            {
                var modelPrefab = GameDatabase.Instance.GetModelPrefab(Model);
                foreach (var meshCollider in modelPrefab.GetComponentsInChildren<MeshCollider>())
                {
                    meshCollider.convex = false;
                }
                IvaGameObject = GameObject.Instantiate(modelPrefab);
                IvaGameObject.SetActive(true);
            }
            else
            {
                IvaGameObject = GameObject.CreatePrimitive((PrimitiveType)ColliderType);
            }
            IvaGameObjectColliders = IvaGameObject.GetComponentsInChildren<Collider>();
            ColliderEnabled = true;

            //IvaGameObject.collider.isTrigger = true;
            if (p.internalModel == null)
            {
                Debug.Log($"# Creating blank InternalModel for {p.name}");
                p.internalModel = new InternalModel();
            }

            if (p.internalModel != null)
                IvaGameObject.transform.parent = p.internalModel.transform;
            IvaGameObject.SetLayerRecursive((int)Layers.Kerbals);
            IvaGameObject.transform.localScale = scale;
            IvaGameObject.transform.localPosition = localPosition;
            IvaGameObject.transform.localRotation = rotation;
            IvaGameObject.name = Name;
            foreach (var collider in IvaGameObjectColliders)
            {
                collider.material.bounciness = 0;
            }
            //FixedJoint joint = IvaGameObject.AddComponent<FixedJoint>();
            //joint.connectedBody = p.collider.rigidbody;
            if (IvaGameObject.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = IvaGameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;

                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            if (AlwaysVisible)
            {
                // Change the colour from the default white.
                IvaGameObjectRenderer.material.color = Color.grey;
            }
            else if (IvaGameObjectRenderer != null)
            {
                IvaGameObjectRenderer.enabled = false;
            }
        }

        public void Enable(bool enabled)
        {
            if (IvaGameObject != null)
            {
                foreach (var collider in IvaGameObjectColliders)
                {
                    collider.enabled = enabled;
                }
            }
        }

        public static void HideAllColliders()
        {
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                ModuleFreeIva mfi = p.GetModule<ModuleFreeIva>();
                if (mfi != null)
                {
                    for (int i = 0; i < mfi.InternalColliders.Count; i++)
                    {
                        if (!mfi.InternalColliders[i].AlwaysVisible)
                            mfi.InternalColliders[i].Visible = false;
                    }
                }
            }
        }

        public static InternalCollider LoadFromCfg(ConfigNode node)
        {
            Vector3 position = Vector3.zero;
            if (!node.HasValue("type"))
            {
                Debug.LogWarning("[FreeIVA] InternalCollider type not found: Skipping collider.");
                return null;
            }
            InternalCollider internalCollider = new InternalCollider();
            string typeString = node.GetValue("type");
            if (!Enum.TryParse(typeString, out internalCollider.ColliderType))
            {
                Debug.LogWarning($"[FreeIVA] invalid internal collider type {typeString}");
            }

            if (internalCollider.ColliderType == Type.Mesh)
            {
                internalCollider.Model = node.GetValue("model");
            }

            if (node.HasValue("name"))
            {
                internalCollider.Name = node.GetValue("name");
            }

            if (node.HasValue("alwaysVisible"))
            {
                internalCollider.AlwaysVisible = bool.Parse(node.GetValue("alwaysVisible"));
            }

            if (node.HasValue("position"))
            {
                string posString = node.GetValue("position");
                string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid collider position definition \"" + posString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    internalCollider.LocalPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Collider position not found: Skipping collider.");
                return null;
            }

            if (node.HasValue("scale"))
            {
                string scaleString = node.GetValue("scale");
                string[] s = scaleString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid collider scale definition \"" + scaleString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    internalCollider.Scale = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Collider scale not found: Skipping collider.");
                return null;
            }

            if (node.HasValue("rotation"))
            {
                string rotationString = node.GetValue("rotation");
                string[] s = rotationString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length == 3)
                {
                    internalCollider.Rotation = Quaternion.Euler(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                }
                else if (s.Length == 4)
                {
                    internalCollider.Rotation = new Quaternion(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                }
                else
                {
                    Debug.LogWarning("[FreeIVA] Invalid collider rotation definition \"" + rotationString + "\": Must be in the form x, y, z or w, x, y, z.");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Collider rotation not found: Using defaults.");
                internalCollider.Rotation = Quaternion.identity;
            }
            return internalCollider;
        }
    }
}
