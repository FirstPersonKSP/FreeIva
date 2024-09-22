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
        Pass {}
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
        Pass {}
    }
}