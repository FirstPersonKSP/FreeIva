using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// Base class for IVA objects created by FreeIVA.
    /// </summary>
    /// TODO: Rename to IvaObjectBase
    public class IvaObject : ScriptableObject, IIvaObject
    {
        public string Name { get; set; }

        public GameObject IvaGameObject { get; protected set; } = null;
        public Collider[] IvaGameObjectColliders;
        private Renderer _ivaGameObjectRenderer;
        public Renderer IvaGameObjectRenderer {
            get
            {
                if (IvaGameObject != null)
                {
                    return IvaGameObject.GetComponentCached(ref _ivaGameObjectRenderer);
                }
                return null;
            }
            set => _ivaGameObjectRenderer = value;
        }
        private Rigidbody _ivaGameObjectRigidbody;
        public Rigidbody IvaGameObjectRigidbody {
            get
            {
                if (IvaGameObject != null)
                {
                    return IvaGameObject.GetComponentCached(ref _ivaGameObjectRigidbody);
                }
                return null;
            }
            set => _ivaGameObjectRigidbody = value;
        }
        private Vector3 _scale = Vector3.zero;
        public virtual Vector3 Scale
        {
            get
            {
                if (IvaGameObject != null)
                    return IvaGameObject.transform.localScale;
                return _scale;
            }
            set
            {
                if (IvaGameObject != null)
                    IvaGameObject.transform.localScale = value;
                _scale = value;
            }
        }
        private Vector3 _position = Vector3.zero;
        public virtual Vector3 LocalPosition
        {
            get
            {
                if (IvaGameObject != null)
                    return IvaGameObject.transform.localPosition;
                return _position;
            }
            set
            {
                if (IvaGameObject != null)
                    IvaGameObject.transform.localPosition = value;
                _position = value;
            }
        }

        private Quaternion _rotation = Quaternion.identity;
        public virtual Quaternion Rotation
        {
            get
            {
                if (IvaGameObject != null)
                {
                    IvaGameObject.transform.localRotation = _rotation;
                    return IvaGameObject.transform.localRotation;
                }
                return _rotation;
            }
            set
            {
                if (IvaGameObject != null)
                    IvaGameObject.transform.localRotation = value;
                _rotation = value;
            }
        }

        public virtual void Instantiate(Part p) { }
    }
}
