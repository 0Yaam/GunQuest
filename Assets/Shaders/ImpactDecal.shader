Shader "GunQuest/Impact Decal"
{
    Properties
    {
        _Color("Scorch colour", Color) = (0.025,0.02,0.018,0.9)
        _Fade("Fade", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _Fade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                half crater = 1.0h - smoothstep(0.18h, 0.48h, radius);
                half soot = (1.0h - smoothstep(0.32h, 0.92h, radius)) * 0.42h;
                half cracks = pow(saturate(cos(angle * 7.0 + radius * 18.0)), 18.0h) *
                    smoothstep(0.24h, 0.42h, radius) * (1.0h - smoothstep(0.55h, 0.95h, radius));
                half alpha = saturate(crater + soot + cracks * 0.7h) * _Color.a * _Fade;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
