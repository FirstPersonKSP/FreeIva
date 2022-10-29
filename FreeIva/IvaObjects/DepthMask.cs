using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva.IvaObjects
{
    // changes the shader on a model to the depth mask
    // intended to be used for "open" props
    internal class DepthMask : InternalModule
    {
        [KSPField]
        public string depthMaskTransformName = string.Empty;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            var depthMaskTransform = internalProp.FindModelTransform(depthMaskTransformName);
            if (depthMaskTransform != null)
            {
                var depthMaskRenderer = depthMaskTransform.GetComponentInChildren<Renderer>();

                Shader depthMask = Utils.GetDepthMask();
                if (depthMask != null)
                    depthMaskRenderer.material.shader = depthMask;
                depthMaskRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
            {
                Debug.LogError($"[FreeIVA] unable to find transform {depthMaskTransformName} for prop {internalProp?.propName} in {internalModel?.internalName}");
            }
        }
    }
}
