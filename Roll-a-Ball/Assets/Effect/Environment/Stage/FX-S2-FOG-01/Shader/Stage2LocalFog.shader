Shader "Roll-a-Ball/Effects/Stage2 Local Fog"
{
    Properties
    {
        _FogColor ("Fog color", Color) = (0.38,0.46,0.49,1)
        _FogParameters ("Density, start, end, opacity limit", Vector) = (0.10,12,36,0.45)
        _NoiseParameters ("Scale, strength, height falloff, samples", Vector) = (0.3,0.65,2,16)
        _NoiseVelocity ("Noise velocity", Vector) = (0.055,0.005,0.025,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" "DisableBatching"="True" }
        Pass
        {
            Name "LocalForestFog"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _FogParameters;
                float4 _NoiseParameters;
                float4 _NoiseVelocity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            // 箱の裏面から範囲を描画し、カメラが箱内に入っても同じ積分を使う
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // 1オクターブの滑らかな3Dノイズ。時間による乱数の再生成はしない
            float Noise(float3 p)
            {
                float3 cell = floor(p);
                float3 f = frac(p);
                f = f * f * (3 - 2 * f);
                float low = lerp(lerp(Hash(cell), Hash(cell + float3(1,0,0)), f.x),
                    lerp(Hash(cell + float3(0,1,0)), Hash(cell + float3(1,1,0)), f.x), f.y);
                float high = lerp(lerp(Hash(cell + float3(0,0,1)), Hash(cell + float3(1,0,1)), f.x),
                    lerp(Hash(cell + float3(0,1,1)), Hash(cell + float3(1,1,1)), f.x), f.y);
                return lerp(low, high, f.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
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
                if (end <= start || _FogParameters.x <= 0)
                {
                    return 0;
                }
                int steps = clamp((int)_NoiseParameters.w, 8, 24);
                float stepSize = (end - start) / steps;
                float opticalDepth = 0;
                // 固定の中点を使い、静止画の粒状感とフレーム毎のちらつきを避ける
                [loop]
                for (int s = 0; s < steps; s++)
                {
                    float t = start + (s + 0.5) * stepSize;
                    float3 positionWS = origin + direction * t;
                    float3 positionOS = originOS + directionOS * t;
                    float3 edge = smoothstep(0, 0.14, 0.5 - abs(positionOS));
                    float height = exp(-max(0, positionOS.y + 0.5) * _NoiseParameters.z);
                    float distanceFade = smoothstep(_FogParameters.y, _FogParameters.z, t);
                    float noise = Noise((positionWS - _Time.y * _NoiseVelocity.xyz) * _NoiseParameters.x);
                    float variation = lerp(1, 0.35 + noise * 1.3, _NoiseParameters.y);
                    opticalDepth += edge.x * edge.y * edge.z * height * distanceFade * variation * stepSize;
                }
                float opacity = min(_FogParameters.w, 1 - exp(-opticalDepth * _FogParameters.x));
                return half4(_FogColor.rgb, opacity);
            }
            ENDHLSL
        }
    }
}
