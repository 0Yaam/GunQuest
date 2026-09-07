Shader "GunQuest/Forest Foliage"
{
    Properties
    {
        _BaseMap("Leaf colour", 2D) = "white" {}
        _AlphaMap("Leaf silhouette", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _Cutoff("Cutout", Range(0,1)) = 0.4
        _Wind("Wind amplitude", Range(0,0.2)) = 0.025
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
        half _Cutoff, _Wind;
        CBUFFER_END
        struct Attributes { float4 vertex : POSITION; float3 normal : NORMAL; float4 tangent : TANGENT; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; float4 tangent : TEXCOORD2; float2 uv : TEXCOORD3; float fog : TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID };
        Varyings Vert(Attributes v)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o);
            float3 world = TransformObjectToWorld(v.vertex.xyz);
            world.x += sin(_Time.y * 1.2 + world.x * 0.7 + world.z * 0.3) * _Wind * saturate(v.vertex.y * 0.8);
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
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
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb * _BaseColor.rgb;
                surface.smoothness = 0.12; surface.occlusion = 1; surface.alpha = 1;
                surface.normalTS = normalTS;
                Light sun = GetMainLight(input.shadowCoord);
                // Thin leaves transmit a restrained amount of backlight, not neon self-illumination.
                surface.emission = surface.albedo * sun.color * saturate(dot(-n,sun.direction)) * sun.shadowAttenuation * 0.12;
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
