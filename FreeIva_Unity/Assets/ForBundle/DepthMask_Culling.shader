Shader "DepthMask_Culling"
{
    SubShader
    {
    	Tags
        {
            "Queue" = "Background"
            "LightMode" = "Deferred"
        }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Back
        ColorMask 0
        Pass
        {
            Offset 0.25,0.25
        }
    }

    SubShader
    {
    	Tags
        {
            "Queue" = "Background"
            "LightMode" = "ForwardBase"
        }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Back
        ColorMask 0
        Pass
        {
            Offset 0.25,0.25
        }
    }
}