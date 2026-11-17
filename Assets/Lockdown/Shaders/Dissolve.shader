// lit metal panel that burns away along procedural noise with a glowing edge
Shader "Lockdown/Dissolve"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.35, 0.37, 0.4, 1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (1, 0.45, 0.1, 1)
        _EdgeWidth ("Edge Width", Range(0.005, 0.2)) = 0.06
        _Cutoff ("Dissolve", Range(0, 1.05)) = 0
        _NoiseScale ("Noise Scale", Float) = 6
        _StripeColor ("Stripe Color", Color) = (0.9, 0.7, 0.05, 1)
        _StripeFreq ("Stripe Freq", Float) = 7
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionOS : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EdgeColor;
                half _EdgeWidth;
                half _Cutoff;
                half _NoiseScale;
                half4 _StripeColor;
                half _StripeFreq;
            CBUFFER_END

            float3 hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            // smooth 3d value noise
            float vnoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash33(i).x, n100 = hash33(i + float3(1,0,0)).x;
                float n010 = hash33(i + float3(0,1,0)).x, n110 = hash33(i + float3(1,1,0)).x;
                float n001 = hash33(i + float3(0,0,1)).x, n101 = hash33(i + float3(1,0,1)).x;
                float n011 = hash33(i + float3(0,1,1)).x, n111 = hash33(i + float3(1,1,1)).x;
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float fbm(float3 p)
            {
                return vnoise(p) * 0.55 + vnoise(p * 2.03) * 0.3 + vnoise(p * 4.1) * 0.15;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = v.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // dissolve front sweeps upward with noise
                float n = fbm(i.positionWS * _NoiseScale) * 0.7 + saturate(i.positionOS.y + 0.5) * 0.3;
                float d = n - _Cutoff;
                clip(d);
                float edge = 1.0 - saturate(d / _EdgeWidth);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                // hazard stripes on the shutter
                float stripe = step(0.5, frac((i.uv.x + i.uv.y) * _StripeFreq));
                float band = step(0.82, i.uv.y) + step(i.uv.y, 0.12);
                albedo = lerp(albedo, _StripeColor.rgb * stripe + albedo * 0.2 * (1 - stripe), saturate(band));

                float3 nrm = normalize(i.normalWS);
                Light main = GetMainLight();
                half3 lit = albedo * (main.color * saturate(dot(nrm, main.direction)) + SampleSH(nrm));
                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; ++li)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    lit += albedo * l.color * l.distanceAttenuation * saturate(dot(nrm, l.direction));
                }
                #endif
                lit += _EdgeColor.rgb * edge * 4.0 * step(0.001, _Cutoff);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
}
