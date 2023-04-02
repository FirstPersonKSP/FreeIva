namespace FreeIva
{
	internal class FreeIvaInternalCameraSwitch : InternalCameraSwitch
	{
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
			else
			{
				// We also don't want to respond to double-click events while unbuckled, so override the stock event handler here
				var internalButton = colliderTransform.GetComponent<InternalButton>();
				if (internalButton != null)
				{
					internalButton.onDoubleTap = Button_OnDoubleTap;
				}
			}
		}

		new public void Button_OnDoubleTap()
		{
			if (KerbalIvaAddon.Instance.buckled)
			{
				base.Button_OnDoubleTap();
			}
		}
	}
}
