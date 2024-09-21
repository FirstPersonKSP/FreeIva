Shader "DepthMask_Culling" {
    SubShader {
    	Tags { "Queue" = "Background" "LightMode" = "Deferred" }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Back
        ColorMask 0
        Pass
        {
            Stencil
            {
                Ref 0
                WriteMask 128
                Comp Always
                Pass Replace
            }
        }
    }

    SubShader {
    	Tags { "Queue" = "Background" "LightMode" = "ForwardBase" }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Back
        ColorMask 0
        Pass
        {
            Stencil
            {
                Ref 0
                WriteMask 128
                Comp Always
                Pass Replace
            }
        }
    }
}