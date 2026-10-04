Shader "GunQuest/Forest Foliage"
{
    Properties
    {
        _BaseMap("Leaf colour", 2D) = "white" {}
        _AlphaMap("Leaf silhouette", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _HueVariation("Stand variation", Color) = (0.58,0.76,0.38,0.32)
        _SubsurfaceColor("Backlight colour", Color) = (0.62,0.88,0.34,1)
        _Cutoff("Cutout", Range(0,1)) = 0.4
        _Wind("Wind amplitude", Range(0,0.2)) = 0.025
        _WindSpeed("Wind speed", Range(0,4)) = 1.15
        _Smoothness("Leaf smoothness", Range(0,1)) = 0.16
        _SubsurfaceStrength("Backlight strength", Range(0,1)) = 0.28
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_AlphaMap); SAMPLER(sampler_AlphaMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half4 _HueVariation;
        half4 _SubsurfaceColor;
        half _Cutoff, _Wind, _WindSpeed, _Smoothness, _SubsurfaceStrength;
        CBUFFER_END
        struct Attributes { float4 vertex : POSITION; float3 normal : NORMAL; float4 tangent : TANGENT; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; float4 tangent : TEXCOORD2; float2 uv : TEXCOORD3; float fog : TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID };
        Varyings Vert(Attributes v)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o);
            float3 world = TransformObjectToWorld(v.vertex.xyz);
            float branchWeight = saturate(abs(v.vertex.y) * 0.16 + v.uv.y * 0.7);
            float gust = sin(_Time.y * _WindSpeed + world.x * 0.55 + world.z * 0.31) * 0.68 +
                sin(_Time.y * (_WindSpeed * 1.73) + world.z * 1.13) * 0.32;
            world.xz += float2(gust, gust * 0.37) * _Wind * branchWeight;
            o.world = world; o.pos = TransformWorldToHClip(world);
            o.normal = TransformObjectToWorldNormal(v.normal);
            o.tangent = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w * GetOddNegativeScale());
            o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.fog = ComputeFogFactor(o.pos.z);
            return o;
        }
        void Cutout(Varyings i) { clip(SAMPLE_TEXTURE2D(_AlphaMap,sampler_AlphaMap,i.uv).r - _Cutoff); }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            half4 Frag(Varyings i, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); Cutout(i);
                half3 n = normalize(i.normal) * IS_FRONT_VFACE(facing,1,-1);
                half3 tangent = normalize(i.tangent.xyz);
                half3 bitangent = cross(n,tangent) * i.tangent.w;
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv));
                n = normalize(mul(normalTS,half3x3(tangent,bitangent,n)));
                InputData input = (InputData)0;
                input.positionWS = i.world; input.normalWS = n;
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.world);
                input.shadowCoord = TransformWorldToShadowCoord(i.world);
                input.bakedGI = SampleSH(n);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos);
                input.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                half3 leaf = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                float2 stand = floor(i.world.xz * 0.11);
                half variation = frac(sin(dot(stand,float2(12.9898,78.233))) * 43758.5453);
                half3 standTint = lerp(half3(1,1,1), _HueVariation.rgb, variation * _HueVariation.a);
                surface.albedo = leaf * _BaseColor.rgb * standTint;
                surface.smoothness = _Smoothness; surface.specular = 0.025; surface.occlusion = 1; surface.alpha = 1;
                surface.normalTS = normalTS;
                Light sun = GetMainLight(input.shadowCoord);
                half transmission = pow(saturate(dot(-sun.direction, input.viewDirectionWS)), 2.0h) *
                    saturate(dot(-n, sun.direction) * 0.75h + 0.25h) * sun.distanceAttenuation;
                half skyWrap = (0.055h + saturate(n.y * 0.5h + 0.5h) * 0.055h);
                surface.emission = surface.albedo * skyWrap + _SubsurfaceColor.rgb * surface.albedo *
                    transmission * _SubsurfaceStrength * lerp(0.45h, 1.0h, sun.shadowAttenuation);
                half4 colour = UniversalFragmentPBR(input,surface);
                colour.rgb = MixFog(colour.rgb,i.fog); return colour;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Depth
            #pragma multi_compile_instancing
            half4 Depth(Varyings i) : SV_Target { Cutout(i); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Depth
            #pragma multi_compile_instancing
            half4 Depth(Varyings i) : SV_Target { Cutout(i); return 0; }
            ENDHLSL
        }
    }
}
