Shader "Custom/Desktop Screenshot Background"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _GreenLight ("Green Light", 2D) = "white" {}
        _RedLight ("Red Light", 2D) = "white" {}
        _LightMultiplyStrength ("Light Multiply Strength", Range(0, 1)) = 1
        _LightEdgeSoftness ("Light Edge Softness", Range(0.001, 1)) = 0.45
        _LightNoiseStrength ("Light Noise Strength", Range(0, 1)) = 0.08
        _Darkness ("Darkness", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0, 1)) = 0.5
        [Toggle] _FlickerBandingEnabled ("Flicker Banding Enabled", Float) = 0
        _FlickerBandingStrength ("Flicker Banding Strength", Range(0, 1)) = 1
        _FlickerBandColor ("Flicker Band Color", Color) = (1, 1, 1, 0.07)
        _FlickerBandCount ("Flicker Band Count", Range(1, 12)) = 2.15
        _FlickerBandWidth ("Flicker Band Width", Range(0.01, 0.95)) = 0.799
        _FlickerBandSoftness ("Flicker Band Softness", Range(0.001, 0.5)) = 0.274
        _FlickerBandSpeed ("Flicker Band Speed", Range(-3, 3)) = 0.8
        _FlickerBandTilt ("Rolling Shutter Tilt", Range(-1, 1)) = 0
        _FlickerBandHorizontalDistortion ("Flicker Band Horizontal Distortion", Range(0, 0.15)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry-100"
        }

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_GreenLight);
            SAMPLER(sampler_GreenLight);
            TEXTURE2D(_RedLight);
            SAMPLER(sampler_RedLight);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _GreenLight_ST;
                float4 _RedLight_ST;
                float _LightMultiplyStrength;
                float _LightEdgeSoftness;
                float _LightNoiseStrength;
                float _Darkness;
                float _Saturation;
                float _FlickerBandingEnabled;
                float _FlickerBandingStrength;
                float4 _FlickerBandColor;
                float _FlickerBandCount;
                float _FlickerBandWidth;
                float _FlickerBandSoftness;
                float _FlickerBandSpeed;
                float _FlickerBandTilt;
                float _FlickerBandHorizontalDistortion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.screenPosition = ComputeScreenPos(output.positionHCS);
                return output;
            }

            half GetFlickerBandMask(float2 uv)
            {
                float phase = frac(
                    uv.y * max(_FlickerBandCount, 1.0)
                    + uv.x * _FlickerBandTilt
                    - _Time.y * _FlickerBandSpeed);

                float centerDistance = abs(phase - 0.5) * 2.0;
                return 1.0h - smoothstep(
                    _FlickerBandWidth,
                    min(1.0, _FlickerBandWidth + _FlickerBandSoftness),
                    centerDistance);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half SmoothNoise(float2 uv)
            {
                float2 p = uv * 180.0;
                float2 i = floor(p);
                float2 f = smoothstep(0.0, 1.0, frac(p));
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return (half)lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                half strength = saturate(_FlickerBandingStrength)
                    * step(0.5h, _FlickerBandingEnabled);
                half band = GetFlickerBandMask(screenUv);
                float2 sampleUv = input.uv;
                sampleUv.x -= band * strength * _FlickerBandHorizontalDistortion;

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, sampleUv);
                half luminance = dot(color.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                color.rgb = lerp(luminance.xxx, color.rgb, saturate(_Saturation));

                half edge = saturate((half)_LightEdgeSoftness);
                half noise = (SmoothNoise(input.uv) - 0.5h) * (half)_LightNoiseStrength;
                half greenMask = smoothstep(0.5h + edge, 0.0h, input.uv.x + noise);
                half redMask = smoothstep(0.5h - edge, 1.0h, input.uv.x - noise);
                half3 greenMultiply = lerp(half3(1, 1, 1), half3(0.0h, 1.0h, 0.45h), greenMask);
                half3 redMultiply = lerp(half3(1, 1, 1), half3(1.0h, 0.08h, 0.05h), redMask);
                half3 lightMultiply = greenMultiply * redMultiply;
                color.rgb *= lerp(half3(1, 1, 1), lightMultiply, saturate(_LightMultiplyStrength));

                half flickerBlend = band * strength * _FlickerBandColor.a;
                color.rgb = lerp(color.rgb, _FlickerBandColor.rgb, flickerBlend);

                color.rgb *= 1.0h - saturate(_Darkness);
                return color;
            }
            ENDHLSL
        }
    }
}
