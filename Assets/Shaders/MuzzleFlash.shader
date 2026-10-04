Shader "GunQuest/Muzzle Flash"
{
    Properties { _Intensity("Intensity", Range(0,8)) = 3.2 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+50" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half _Intensity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.color=input.color;
                output.uv=input.uv;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 p=input.uv*2.0-1.0;
                float radius=length(p);
                float angle=atan2(p.y,p.x);
                half core=pow(saturate(1.0-radius),3.0h);
                half rays=pow(saturate(cos(angle*5.0)*0.5+0.5),7.0h)*saturate(1.0-radius*1.15);
                half plume=pow(saturate(1.0-abs(p.y)*2.6),3.0h)*pow(saturate(1.0-abs(p.x)),2.0h)*0.34h;
                half energy=(core+rays*0.48h+plume)*input.color.a;
                return half4(input.color.rgb*energy*_Intensity,energy);
            }
            ENDHLSL
        }
    }
}
