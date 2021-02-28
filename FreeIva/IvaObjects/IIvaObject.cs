using UnityEngine;

namespace FreeIva
{
    public interface IIvaObject
    {
        string Name { get; set; }
        Vector3 LocalPosition { get; set; }
        Vector3 Scale { get; set; }
        Quaternion Rotation { get; set; }
        GameObject IvaGameObject { get; }
        Renderer IvaGameObjectRenderer { get; set; }
        Rigidbody IvaGameObjectRigidbody { get; set; }
    }
}
