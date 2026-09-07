Shader "GunQuest/Forest Creek"
{
    Properties
    {
        _BaseColor("Deep water",Color)=(0.045,0.095,0.082,1)
        _Smoothness("Smoothness",Range(0,1))=0.92
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Smoothness;
            CBUFFER_END
            struct Attributes { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 pos:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;float fog:TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.world=TransformObjectToWorld(v.vertex.xyz);o.pos=TransformWorldToHClip(o.world);
                o.uv=v.uv;o.fog=ComputeFogFactor(o.pos.z);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.world.xz;
                float flow=_Time.y;
                float dx=cos(p.x*9.3+p.y*5.2-flow*2.1)*.032+cos(p.x*14-p.y*5-flow*3.6)*.021;
                float dz=cos(p.x*3.1+p.y*11.4-flow*1.4)*.028+sin(p.x*11+p.y*13-flow*2.8)*.018;
                half3 normal=normalize(float3(dx,1,dz));
                float2 screen=GetNormalizedScreenSpaceUV(i.pos);
                float sceneDepth=LinearEyeDepth(SampleSceneDepth(screen),_ZBufferParams);
                float waterDepth=-TransformWorldToView(i.world).z;
                float thickness=max(0,sceneDepth-waterDepth);
                float edge=saturate(min(i.uv.y,1-i.uv.y)*8);
                float fresnel=pow(1-saturate(dot(normal,GetWorldSpaceNormalizeViewDir(i.world))),4);
                InputData input=(InputData)0;input.positionWS=i.world;input.normalWS=normal;
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);input.shadowCoord=TransformWorldToShadowCoord(i.world);
                input.bakedGI=SampleSH(normal);input.normalizedScreenSpaceUV=screen;input.shadowMask=1;
                SurfaceData surface=(SurfaceData)0;surface.albedo=_BaseColor.rgb;surface.smoothness=_Smoothness;
                surface.specular=.04;surface.occlusion=1;surface.alpha=1;surface.normalTS=half3(0,0,1);
                half4 colour=UniversalFragmentPBR(input,surface);
                colour.rgb=MixFog(colour.rgb,i.fog);
                colour.a=saturate(thickness*2)*edge*lerp(.48,.90,fresnel);
                return colour;
            }
            ENDHLSL
        }
    }
}
