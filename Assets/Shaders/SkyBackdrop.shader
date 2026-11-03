Shader "Custom/SkyBackdrop"
{
    // Unlit equirectangular backdrop for a big inverted dome around the scene.
    // Ignores fog and lights so it looks the same whatever the temple lighting does.
    // Horizontal: full turn. Vertical: elevation angle from _EMin to _EMax degrees (clamped outside).
    Properties
    {
        _MainTex ("Panorama", 2D) = "white" {}
        _EMin ("Bottom elevation (deg)", Float) = -24
        _EMax ("Top elevation (deg)", Float) = 42
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _EMin;
                float _EMax;
                half4 _Tint;
            CBUFFER_END

            struct Attributes { float4 pos : POSITION; };
            struct Varyings { float4 cs : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.cs = TransformObjectToHClip(i.pos.xyz);
                o.dir = i.pos.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float u = atan2(d.x, d.z) * 0.15915494 + 0.5;
                float e = degrees(asin(clamp(d.y, -1.0, 1.0)));
                float v = saturate((e - _EMin) / (_EMax - _EMin));
                half3 c = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, float2(u, v), 0).rgb;
                return half4(c * _Tint.rgb, 1);
            }
            ENDHLSL
        }
    }
}
