Shader "DepthMask_Culling" {
    SubShader {
    	Tags { "Queue" = "Background" }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Back
        ColorMask 0
        Pass {}
    }
}