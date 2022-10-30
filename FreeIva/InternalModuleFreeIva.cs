using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
    internal class InternalModuleFreeIva : InternalModule
    {
        [KSPField]
        public string shellColliderName = string.Empty;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (shellColliderName != string.Empty)
            {
                var transform = internalProp.FindModelTransform(shellColliderName);
                if (transform != null)
                {
                    foreach (var meshCollider in transform.GetComponentsInChildren<MeshCollider>())
                    {
                        meshCollider.convex = false;
                    }
                }
            }
        }
    }
}
