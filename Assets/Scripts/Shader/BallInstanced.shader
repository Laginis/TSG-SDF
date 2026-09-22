Shader "Custom/BallInstanced"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 color : COLOR;
            };

            StructuredBuffer<float4> _BallPositions;
            float _BallRadius;
            static const float GOLDEN_RATIO_CONJUGATE = 0.61803398875;

            half3 HSVToRGB(float h, float s, float v)
            {
                float3 rgb = abs(frac(h + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
                rgb = saturate(rgb - 1.0);
                return v * lerp(float3(1.0, 1.0, 1.0), rgb, s);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 ballPosition =  _BallPositions[input.instanceID].xyz;
                float3 worldPosition = ballPosition + input.positionOS * _BallRadius;

                output.positionCS = TransformWorldToHClip(worldPosition);

                float hue = frac(input.instanceID * GOLDEN_RATIO_CONJUGATE);

                output.color = HSVToRGB(hue, 0.8, 1.0);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(input.color, 1);
            }

            ENDHLSL
        }

        // Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            StructuredBuffer<float4> _BallPositions;
            float _BallRadius;

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 ballPosition = _BallPositions[input.instanceID].xyz;
                float3 worldPosition = ballPosition + input.positionOS * _BallRadius;

                output.positionCS = TransformWorldToHClip(worldPosition);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}