using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    public interface IHatch : IIvaObject
    {
        Vector3 WorldPosition { get; }
        string AttachNodeId { get; set; }
        List<KeyValuePair<Vector3, string>> HideWhenOpen { get; set; }
        InternalCollider Collider { get; set; }
        bool IsOpen { get; }
        IHatch ConnectedHatch { get; }
        Part HatchPart { get; }
        AttachNode HatchNode { get; }

        void Open(bool open);
        void ToggleHatch();
    }
}
