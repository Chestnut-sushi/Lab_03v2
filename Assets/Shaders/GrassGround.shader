Shader "Custom/GrassGround"
{
    // Tiled meadow for the ground around the temple. UVs come from world XZ, so separate meshes
    // (courtyard floor + outer ring) line up. Lit by the main light + ambient like the temple,
    // but instead of the grey scene fog it fades into the pale mist of the mountain backdrop.
    Properties
    {
        _MainTex ("Grass", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _TileSize ("Tile size (m)", Float) = 4
        _MistColor ("Mist colour", Color) = (0.737, 0.795, 0.825, 1)
        _MistDistance ("Mist distance (m)", Float) = 200
        _MistEnd ("Fully misted at (m)", Float) = 460
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
                float _TileSize;
                half4 _MistColor;
                float _MistDistance;
                float _MistEnd;
            CBUFFER_END

            struct Attributes { float4 pos : POSITION; };
            struct Varyings { float4 cs : SV_POSITION; float3 ws : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.ws = TransformObjectToWorld(i.pos.xyz);
                o.cs = TransformWorldToHClip(o.ws);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.ws.xz / _TileSize;
                half3 near = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                half3 macro = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv / 11.3 + 0.37).rgb;
                half3 albedo = near * lerp(0.72h, 1.28h, saturate(macro.g * 2.2h - 0.35h)) * _Tint.rgb;

                Light l = GetMainLight();
                half3 light = SampleSH(half3(0, 1, 0)) + l.color * saturate(l.direction.y);
                half3 c = albedo * light;

                float d = length(i.ws.xz - _WorldSpaceCameraPos.xz);
                float f = 1.0 - exp(-pow(d / _MistDistance, 1.5));
                f = max(f, smoothstep(_MistEnd - 80.0, _MistEnd, d));
                c = lerp(c, _MistColor.rgb, saturate(f));
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
