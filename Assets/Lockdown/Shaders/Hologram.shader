// transparent hologram with scan lines, rim glow and glitch bands
Shader "Lockdown/Hologram"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.25, 0.2, 1)
        _MainTex ("Pattern", 2D) = "white" {}
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _LineDensity ("Line Density", Float) = 90
        _LineSpeed ("Line Speed", Float) = 1.5
        _Alpha ("Alpha", Range(0, 1)) = 0.75
        _Glitch ("Glitch", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _RimPower;
                half _LineDensity;
                half _LineSpeed;
                half _Alpha;
                half _Glitch;
            CBUFFER_END

            float hash21(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 pos = v.positionOS.xyz;
                // horizontal glitch shove in rare bands
                float band = floor(pos.y * 12.0 + _Time.y * 3.0);
                float shove = step(0.93, hash21(float2(band, floor(_Time.y * 8.0)))) * _Glitch * 0.03;
                pos.x += shove;
                VertexPositionInputs p = GetVertexPositionInputs(pos);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(abs(dot(normalize(i.normalWS), viewDir))), _RimPower);
                float lines = 0.55 + 0.45 * sin((i.positionWS.y * _LineDensity - _Time.y * _LineSpeed) * 6.2831);
                float flicker = 0.85 + 0.15 * sin(_Time.y * 37.0) * sin(_Time.y * 11.0);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half a = saturate((0.25 + rim * 1.4) * lines * flicker * tex.a) * _Alpha;
                return half4(_Color.rgb * tex.rgb * (1.0 + rim * 2.0), a);
            }
            ENDHLSL
        }
    }
}
