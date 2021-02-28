using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// Allow setting a CFG entry to have a mesh in a part internal always be hidden.
    /// </summary>
    public class HideInternalMesh : InternalModule
    {
        public string meshName;
        public int meshIndex = -1;

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("#HideInternalMesh OnLoad: " + node);
            if (node.HasValue("meshName"))
            {
                meshName = node.GetValue("meshName");
            }
            if (node.HasValue("meshIndex"))
            {
                int.TryParse(node.GetValue("meshIndex"), out meshIndex);
            }
        }

        public override void OnAwake()
        {
            if (meshIndex == -1) return;

            MeshRenderer[] meshRenderers = this.internalModel.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length <= meshIndex)
            {
                Debug.LogError("[FreeIVA] HideInternalMesh: Searching for mesh with index " + meshIndex + ", only " + meshRenderers.Length + " mesh available.");
                return;
            }
            if (meshRenderers[meshIndex].name != meshName)
            {
                Debug.LogError("[FreeIVA] HideInternalMesh: Searching for mesh " + meshName + " at index " + meshIndex + " but found mesh " +
                    meshRenderers[meshIndex].name + " instead. Skipping...");
                return;
            }

            meshRenderers[meshIndex].enabled = false;
        }
    }
}
