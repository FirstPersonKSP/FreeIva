using UnityEngine;

namespace FreeIva
{
	/// <summary>
	/// Allow setting a CFG entry to have a mesh in a part internal always be hidden.
	/// </summary>
	public class HideInternalMesh : InternalModule
	{
		public override void OnLoad(ConfigNode node)
		{
			string meshName = string.Empty;
			int meshIndex = -1;

			Debug.Log("#HideInternalMesh OnLoad: " + node);
			if (node.HasValue("meshName"))
			{
				meshName = node.GetValue("meshName");
			}
			if (node.HasValue("meshIndex"))
			{
				int.TryParse(node.GetValue("meshIndex"), out meshIndex);
			}


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

			Component.Destroy(meshRenderers[meshIndex]);
		}
	}
}
