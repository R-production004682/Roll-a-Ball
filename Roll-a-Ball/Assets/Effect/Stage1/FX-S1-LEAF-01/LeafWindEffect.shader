Shader "Roll-a-Ball/Effects/Leaf Drift"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.42, 0.72, 0.25, 0.9)
        [HDR] _EmissionColor ("Soft Emission", Color) = (0.12, 0.32, 0.12, 1)
        _EmissionStrength ("Emission Strength", Range(0, 3)) = 0.65
        _EdgeColor ("Crystal Edge Color", Color) = (0.35, 0.9, 0.75, 1)
        _EdgeStrength ("Edge Strength", Range(0, 2)) = 0.22
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "LeafDrift"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionStrength;
                half4 _EdgeColor;
                half _EdgeStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirectionWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirectionWS = GetWorldSpaceViewDir(position.positionWS);
                output.color = input.color * _BaseColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 normal = normalize(input.normalWS);
                half3 viewDirection = normalize(input.viewDirectionWS);
                half edge = pow(1 - saturate(abs(dot(normal, viewDirection))), 1.6);
                half3 color = input.color.rgb + _EmissionColor.rgb * _EmissionStrength;
                color += _EdgeColor.rgb * edge * _EdgeStrength;
                half alpha = saturate(input.color.a);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
