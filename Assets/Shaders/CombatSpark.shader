Shader "GunQuest/Combat Spark"
{
    Properties { _Intensity("Intensity", Range(0,10)) = 4 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+45" }
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
                float2 p = input.uv * 2.0 - 1.0;
                half radial = saturate(1.0h - length(p));
                half streak = saturate(1.0h - min(abs(p.x), abs(p.y)) * 3.2h) * saturate(1.0h - length(p) * 0.72h);
                half energy = max(pow(radial, 3.0h), streak * 0.42h) * input.color.a;
                return half4(input.color.rgb * energy * _Intensity, energy);
            }
            ENDHLSL
        }
    }
}
