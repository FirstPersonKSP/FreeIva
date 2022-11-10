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
					colliderTransform.gameObject.layer = (int)Layers.InternalSpace;
				}
			}
		}
	}
}
