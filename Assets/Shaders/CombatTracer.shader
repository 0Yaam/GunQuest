Shader "GunQuest/Combat Tracer"
{
    Properties { _Intensity("Intensity", Range(0,8)) = 3.5 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+40" }
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

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half width = saturate(1.0h - abs(input.uv.y * 2.0h - 1.0h));
                half ends = smoothstep(0.0h, 0.12h, input.uv.x) * smoothstep(0.0h, 0.18h, 1.0h - input.uv.x);
                half energy = pow(width, 2.5h) * ends * input.color.a;
                return half4(input.color.rgb * energy * _Intensity, energy);
            }
            ENDHLSL
        }
    }
}
