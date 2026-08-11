Shader "Hidden/LensFocusBlur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #pragma target 3.5

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _Intensity;
        float _BlurStrength;
        float _EdgeSoftness;
        float _RadialFalloff;
        float _RadialGradientPower;

        TEXTURE2D_X(_LensFocusBlurTexture);
        TEXTURE2D_X(_LensFocusMaskTexture);
        TEXTURE2D_X(_LensFocusRadialMaskTexture);

        half4 GaussianBlur(float2 uv, float2 direction)
        {
            half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0) * 0.227027h;
            color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + direction * 1.384615, 0) * 0.316216h;
            color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - direction * 1.384615, 0) * 0.316216h;
            color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + direction * 3.230769, 0) * 0.070270h;
            color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - direction * 3.230769, 0) * 0.070270h;
            return color;
        }

        half4 FragBlurHorizontal(Varyings input) : SV_Target
        {
            return GaussianBlur(
                input.texcoord,
                float2(_BlitTexture_TexelSize.x * _BlurStrength, 0.0));
        }

        half4 FragBlurVertical(Varyings input) : SV_Target
        {
            return GaussianBlur(
                input.texcoord,
                float2(0.0, _BlitTexture_TexelSize.y * _BlurStrength));
        }

        float SampleMask(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(
                _LensFocusMaskTexture,
                sampler_LinearClamp,
                saturate(uv),
                0).r;
        }

        float FeatheredLensMask(float2 uv)
        {
            float center = SampleMask(uv);
            if (_EdgeSoftness <= 0.01)
                return center;

            float2 radius = _EdgeSoftness / _ScreenParams.xy;
            float2 halfRadius = radius * 0.5;

            float mask = center * 4.0;
            mask += SampleMask(uv + float2(halfRadius.x, 0.0));
            mask += SampleMask(uv - float2(halfRadius.x, 0.0));
            mask += SampleMask(uv + float2(0.0, halfRadius.y));
            mask += SampleMask(uv - float2(0.0, halfRadius.y));
            mask += SampleMask(uv + float2(radius.x, radius.y));
            mask += SampleMask(uv + float2(radius.x, -radius.y));
            mask += SampleMask(uv + float2(-radius.x, radius.y));
            mask += SampleMask(uv - float2(radius.x, radius.y));
            return saturate(mask / 12.0);
        }

        half4 FragComposite(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            half4 source = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
            half4 blurred = SAMPLE_TEXTURE2D_X_LOD(_LensFocusBlurTexture, sampler_LinearClamp, uv, 0);
            float lensMask = FeatheredLensMask(uv);
            float radialMask = SAMPLE_TEXTURE2D_X_LOD(
                _LensFocusRadialMaskTexture,
                sampler_LinearClamp,
                uv,
                0).r;
            float radialSharpness = pow(saturate(radialMask * 4.0), _RadialGradientPower);
            float sharpness = max(lensMask, radialSharpness);
            float blurAmount = (1.0 - sharpness) * saturate(_Intensity);
            return lerp(source, blurred, blurAmount);
        }
        ENDHLSL

        Pass
        {
            Name "LensFocusBlurHorizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurHorizontal
            ENDHLSL
        }

        Pass
        {
            Name "LensFocusBlurVertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurVertical
            ENDHLSL
        }

        Pass
        {
            Name "LensFocusBlurComposite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            ENDHLSL
        }

        Pass
        {
            Name "LensFocusBlurMask"
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma vertex VertMask
            #pragma fragment FragMask

            struct MaskAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct MaskVaryings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            MaskVaryings VertMask(MaskAttributes input)
            {
                MaskVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 FragMask(MaskVaryings input) : SV_Target
            {
                return half4(1.0, 1.0, 1.0, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "LensFocusRadialHorizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragRadialHorizontal

            half4 FragRadialHorizontal(Varyings input) : SV_Target
            {
                return GaussianBlur(
                    input.texcoord,
                    float2(_BlitTexture_TexelSize.x * _RadialFalloff, 0.0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "LensFocusRadialVertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragRadialVertical

            half4 FragRadialVertical(Varyings input) : SV_Target
            {
                return GaussianBlur(
                    input.texcoord,
                    float2(0.0, _BlitTexture_TexelSize.y * _RadialFalloff));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
