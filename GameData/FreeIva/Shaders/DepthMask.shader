Shader "FreeIVA/DepthMask"
{
	SubShader
	{
		Tags {"Queue" = "Geometry-1" }
		Lighting Off
		Pass
		{
			// First value is offset towards the camera, second is offset along surface normal
			Offset -1, -1
			ZWrite On
			ZTest LEqual
			ColorMask 0
		}
	}
}
