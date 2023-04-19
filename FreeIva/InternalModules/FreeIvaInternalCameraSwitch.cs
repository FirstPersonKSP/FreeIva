using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
	internal class FreeIvaInternalCameraSwitch : InternalCameraSwitch
	{
		static HashSet<Collider> allCameraSwitchColliders = new HashSet<Collider>();

		public static void SetCameraSwitchesEnabled(bool enabled)
		{
			foreach (var collider in allCameraSwitchColliders)
			{
				collider.enabled = enabled;
			}
		}

		Collider collider;

		public override void OnAwake()
		{
			base.OnAwake();
			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				if (colliderTransform != null)
				{
					// stock camera colliders are typically on layer 16, which we use for colliders that can block the kerbal
					// So we change all the camera volumes to layer 20 here, which won't block the kerbal but can be clicked on
					colliderTransform.gameObject.layer = (int)Layers.InternalSpace;
				}
			}
			else if (colliderTransform != null)
			{
				collider = colliderTransform.GetComponent<Collider>();

				if (collider != null)
				{
					allCameraSwitchColliders.Add(collider);
				}
			}
		}

		void OnDestroy()
		{
			if (!object.ReferenceEquals(collider, null))
			{
				allCameraSwitchColliders.Remove(collider);
			}
		}
	}
}
