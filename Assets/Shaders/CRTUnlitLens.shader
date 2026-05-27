Shader "Custom/CRT Unlit Lens"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        _CRTStrength("CRT Strength", Range(0, 1)) = 0
        _ScanlineCount("Scanline Count", Range(20, 500)) = 160
        _ScanlineThickness("Scanline Gap Thickness", Range(0, 1)) = 0.28
        _ScanlineDarkness("Scanline Darkness", Range(0, 1)) = 0.45
        _PhosphorStrength("Phosphor Mask Strength", Range(0, 1)) = 0.15
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.035
        _WarpStrength("Screen Warp Strength", Range(0, 0.2)) = 0
        _VignetteStrength("Vignette Strength", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _CRTStrength;
                half _ScanlineCount;
                half _ScanlineThickness;
                half _ScanlineDarkness;
                half _PhosphorStrength;
                half _NoiseStrength;
                half _WarpStrength;
                half _VignetteStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half RandomNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float2 ApplyWarp(float2 uv, half strength)
            {
                float2 centered = uv * 2.0 - 1.0;
                float radiusSq = dot(centered, centered);
                centered += centered * radiusSq * _WarpStrength * strength;
                return centered * 0.5 + 0.5;
            }

            half3 ApplyScanlines(half3 color, float2 uv, half strength)
            {
                float linePosition = frac(uv.y * max(_ScanlineCount, 1.0h));
                float lineEdge = abs(linePosition - 0.5) * 2.0;
                half gapMask = smoothstep(1.0h - saturate(_ScanlineThickness), 1.0h, lineEdge);
                return color * (1.0h - gapMask * _ScanlineDarkness * strength);
            }

            half3 ApplyPhosphorMask(half3 color, float2 uv, half strength)
            {
                half triad = frac(uv.x * _ScanlineCount * 1.5h);
                half3 phosphor = triad < 0.333h
                    ? half3(1.0h, 0.78h, 0.78h)
                    : (triad < 0.666h ? half3(0.78h, 1.0h, 0.78h) : half3(0.78h, 0.78h, 1.0h));

                return color * lerp(half3(1.0h, 1.0h, 1.0h), phosphor, _PhosphorStrength * strength);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half strength = saturate(_CRTStrength);
                float2 uv = ApplyWarp(input.uv, strength);

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                if (strength <= 0.0001h)
                {
                    return color;
                }

                color.rgb = ApplyScanlines(color.rgb, uv, strength);
                color.rgb = ApplyPhosphorMask(color.rgb, uv, strength);

                half noise = RandomNoise(uv * _ScreenParams.xy + _Time.yy) - 0.5h;
                color.rgb += noise * _NoiseStrength * strength;

                float2 centered = uv * 2.0 - 1.0;
                half vignette = saturate(1.0h - dot(centered, centered) * _VignetteStrength * strength);
                color.rgb = saturate(color.rgb * vignette);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
