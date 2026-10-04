Shader "GunQuest/Viewmodel Weapon"
{
    Properties
    {
        _BaseMap("Finish", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _MetallicGlossMap("Metal and gloss", 2D) = "white" {}
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _EmissionMap("Powered accents", 2D) = "black" {}
        _BaseColor("Finish tint", Color) = (0.52,0.56,0.52,1)
        _AccentColor("Accent", Color) = (0.12,1.1,0.82,1)
        _Metallic("Metallic", Range(0,1)) = 0.72
        _Smoothness("Smoothness", Range(0,1)) = 0.48
        _BumpScale("Normal strength", Range(0,2)) = 1
        _OcclusionStrength("Occlusion", Range(0,1)) = 1
        _AccentStrength("Accent strength", Range(0,5)) = 1.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _AccentColor;
            half _Metallic, _Smoothness, _BumpScale, _OcclusionStrength, _AccentStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 tangentWS:TEXCOORD2; float2 uv:TEXCOORD3; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.tangentWS = half4(normals.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 metalSample = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);
                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3 normalWS = normalize(mul(normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS)));
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(1.0h - saturate(dot(normalWS, view)), 5.0h);
                half3 powered = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.normalWS = normalWS;
                lighting.viewDirectionWS = view;
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = baseSample.rgb * _BaseColor.rgb;
                surface.metallic = saturate(metalSample.r * _Metallic);
                surface.smoothness = saturate(lerp(_Smoothness * 0.72h, _Smoothness, metalSample.a));
                surface.occlusion = lerp(1.0h, occlusion, _OcclusionStrength);
                surface.normalTS = normalTS;
                surface.alpha = 1;
                surface.emission = powered * _AccentColor.rgb * _AccentStrength +
                    _AccentColor.rgb * fresnel * 0.018h + surface.albedo * (0.045h + fresnel * 0.035h);
                return UniversalFragmentPBR(lighting, surface);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
