// crt style monitor: scan lines, rolling bar, static and a pulsing warden eye
Shader "Lockdown/MonitorNoise"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 0.2, 0.15, 1)
        _Brightness ("Brightness", Float) = 1.4
        _Static ("Static", Range(0, 1)) = 0.25
        _Eye ("Eye", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Brightness;
                half _Static;
                half _Eye;
            CBUFFER_END

            float rnd(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv = i.uv;
                float t = _Time.y;
                float scan = 0.8 + 0.2 * sin(uv.y * 420.0);
                float roll = smoothstep(0.0, 0.08, abs(frac(uv.y - t * 0.15) - 0.5));
                float grain = rnd(floor(uv * 160.0) + floor(t * 24.0));
                // eye: ring plus pupil that looks around
                float2 c = uv - 0.5;
                c.x *= 1.6;
                float r = length(c);
                float2 look = float2(sin(t * 0.7), cos(t * 0.53)) * 0.05;
                float pupil = 1.0 - smoothstep(0.05, 0.065, length(c - look));
                float ring = smoothstep(0.02, 0.0, abs(r - 0.22)) + smoothstep(0.01, 0.0, abs(r - 0.3)) * 0.5;
                float pulse = 0.7 + 0.3 * sin(t * 2.0);
                float eye = saturate(ring * pulse + pupil) * _Eye;
                float vign = saturate(1.2 - r * 1.4);
                half3 col = _Tint.rgb * (eye * 1.6 + 0.08) * scan * (0.7 + 0.3 * roll) * vign;
                col += grain * _Static * 0.35;
                return half4(col * _Brightness, 1);
            }
            ENDHLSL
        }
    }
}
