Shader "Hidden/TransparentChromaKey"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "TransparentChromaKey"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _KeyColor;
            float _GlowThreshold;

            float GetLuminance(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float depth = SampleSceneDepth(input.texcoord);

                #if UNITY_REVERSED_Z
                    float hasGeometry = step(0.0001, depth);
                #else
                    float hasGeometry = 1.0 - step(0.9999, depth);
                #endif

                float glowVisible = step(_GlowThreshold, GetLuminance(color.rgb));
                float keepScene = saturate(max(hasGeometry, glowVisible));

                return lerp(_KeyColor, color, keepScene);
            }
            ENDHLSL
        }
    }
}