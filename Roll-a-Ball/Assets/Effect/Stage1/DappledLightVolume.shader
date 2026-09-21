Shader "Roll-a-Ball/Effects/Dappled Light Volume"
{
    Properties
    {
        _SunPositionWS ("Sun position and enabled", Vector) = (0,10,0,0)
        _Medium ("Density, scattering, anisotropy, samples", Vector) = (0.035,0.16,0.2,96)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" "DisableBatching"="True" }
        Pass
        {
            Name "LocalSingleScattering"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest Always
            Blend One One
            HLSLPROGRAM
            #pragma target 4.5
            // 初回のEditor表示で巨大なシアンの代替描画が出ることを防ぐ
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SunPositionWS;
                float4 _Medium;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation int lightIndex : TEXCOORD0;
            };

            // ライト一覧の順序はカメラごとに変わるため固定indexを保存しない
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.lightIndex = -1;
                #if USE_CLUSTER_LIGHT_LOOP
                    uint count = URP_FP_PROBES_BEGIN + URP_FP_DIRECTIONAL_LIGHTS_COUNT;
                #else
                    uint count = GetAdditionalLightsCount();
                #endif
                for (uint i = 0; i < count; i++)
                {
                    #if USE_CLUSTER_LIGHT_LOOP
                        int index = i;
                    #else
                        int index = GetPerObjectLightIndex(i);
                    #endif
                    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                        float4 position = _AdditionalLightsBuffer[index].position;
                    #else
                        float4 position = _AdditionalLightsPosition[index];
                    #endif
                    if (position.w > 0.5 && distance(position.xyz, _SunPositionWS.xyz) < 0.001)
                    {
                        output.lightIndex = index;
                        break;
                    }
                }
                return output;
            }

            // 決定的なディザで積分の帯を分散させ、時間方向のちらつきを避ける
            float Dither(float2 pixel)
            {
                return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (input.lightIndex < 0 || _SunPositionWS.w < 0.5)
                {
                    return 0;
                }
                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;
                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    float nearDepth = 1;
                    float farDepth = 0.00001;
                #else
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                    float nearDepth = UNITY_NEAR_CLIP_VALUE;
                    float farDepth = 0.99999;
                #endif
                float3 origin = ComputeWorldSpacePosition(uv, nearDepth, UNITY_MATRIX_I_VP);
                float3 farWS = ComputeWorldSpacePosition(uv, farDepth, UNITY_MATRIX_I_VP);
                float3 direction = normalize(farWS - origin);
                float3 surface = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float surfaceDistance = max(0, dot(surface - origin, direction));
                float3 originOS = TransformWorldToObject(origin);
                float3 directionOS = mul((float3x3)unity_WorldToObject, direction);
                float3 safeDirection = (step(0, directionOS) * 2 - 1) * max(abs(directionOS), 0.000001);
                float3 a = (-0.5 - originOS) / safeDirection;
                float3 b = (0.5 - originOS) / safeDirection;
                float3 entry = min(a, b);
                float3 leave = max(a, b);
                float start = max(0, max(entry.x, max(entry.y, entry.z)));
                float end = min(surfaceDistance, min(leave.x, min(leave.y, leave.z)));
                if (end <= start)
                {
                    return 0;
                }
                int steps = clamp((int)_Medium.w, 24, 96);
                float stepSize = (end - start) / steps;
                float offset = Dither(input.positionCS.xy);
                float transmittance = 1;
                float3 radiance = 0;
                float g = _Medium.z;
                [loop]
                for (int s = 0; s < steps; s++)
                {
                    float t = start + (s + offset) * stepSize;
                    float3 positionWS = origin + direction * t;
                    float3 positionOS = originOS + directionOS * t;
                    float3 edge = smoothstep(0, 0.12, 0.5 - abs(positionOS));
                    float upperFade = 1 - smoothstep(-0.05, 0.5, positionOS.y);
                    float extinction = _Medium.x * edge.x * edge.y * edge.z * upperFade;
                    float sliceOpacity = 1 - exp(-extinction * stepSize);
                    Light light = GetAdditionalPerObjectLight(input.lightIndex, positionWS);
                    float3 cookie = 1;
                    #if defined(_LIGHT_COOKIES)
                        cookie = SampleAdditionalLightCookie(input.lightIndex, positionWS);
                    #endif
                    if (light.distanceAttenuation * max(cookie.r, max(cookie.g, cookie.b)) > 0.00001)
                    {
                        float shadow = AdditionalLightRealtimeShadow(input.lightIndex, positionWS, light.direction);
                        float cosine = dot(light.direction, direction);
                        float phase = (1 - g * g) / pow(max(0.01, 1 + g * g - 2 * g * cosine), 1.5);
                        radiance += transmittance * sliceOpacity * light.color * light.distanceAttenuation
                            * shadow * cookie * phase * _Medium.y;
                    }
                    transmittance *= 1 - sliceOpacity;
                }
                return half4(radiance, 0);
            }
            ENDHLSL
        }
    }
}
