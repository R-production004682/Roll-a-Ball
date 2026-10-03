Shader "Roll-a-Ball/Effects/PlayerFracture"
{
    Properties
    {
        _BaseColor ("Outer surface", Color) = (0.5,0.5,0.5,1)
        _InteriorColor ("Broken interior", Color) = (0.06,0.3,0.38,1)
        [HDR] _CrackColor ("Crack light", Color) = (0.3,1.5,1.8,1)
        _CrackGlow ("Crack light intensity", Range(0,2)) = 0
        _Visibility ("Visibility", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "FractureSurface"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _InteriorColor;
                half4 _CrackColor;
                half _CrackGlow;
                half _Visibility;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half fog : TEXCOORD2;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Opaque のまま細かい欠片をディザで消し、重なる断面のソートを安定させる
                float noise = frac(52.9829189 * frac(dot(floor(input.positionCS.xy), float2(0.06711056,0.00583715))));
                clip(_Visibility - noise - 0.001);
                half3 normal = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 surface = lerp(_BaseColor.rgb, _InteriorColor.rgb, input.color.r);
                half diffuse = saturate(dot(normal, light.direction));
                half3 ambient = max(SampleSH(normal), half3(0.16,0.2,0.23));
                half3 color = surface * (ambient + light.color * diffuse * light.shadowAttenuation);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half specular = pow(saturate(dot(normal, normalize(light.direction + view))), 36);
                color += specular * light.color * lerp(0.12,0.35,input.color.r);
                half edge = smoothstep(0.82,0.99,input.color.g);
                color += _CrackColor.rgb * edge * _CrackGlow;
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
