// additive laser beam with a hot core and drifting noise
Shader "Lockdown/LaserBeam"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.1, 0.1, 1)
        _Intensity ("Intensity", Float) = 3
        _CoreWidth ("Core Width", Range(0.02, 0.6)) = 0.18
        _NoiseScale ("Noise Scale", Float) = 18
        _ScrollSpeed ("Scroll Speed", Float) = 6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

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
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _CoreWidth;
                half _NoiseScale;
                half _ScrollSpeed;
            CBUFFER_END

            // cheap value noise
            float hash(float n) { return frac(sin(n) * 43758.5453); }
            float noise1(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(hash(i), hash(i + 1.0), f);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // distance from beam center across the strip
                float across = abs(i.uv.y - 0.5) * 2.0;
                float core = saturate(1.0 - across / _CoreWidth);
                float glow = pow(saturate(1.0 - across), 3.0);
                float along = dot(i.positionWS, float3(1.0, 0.7, 1.3));
                float n = noise1(along * _NoiseScale - _Time.y * _ScrollSpeed);
                float flicker = 0.75 + 0.25 * n;
                half3 col = _Color.rgb * glow * flicker + half3(1, 1, 1) * core * core;
                return half4(col * _Intensity * i.color.a, 1);
            }
            ENDHLSL
        }
    }
}
