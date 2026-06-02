Shader "Custom/CRT Scanline Lens"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        _EffectStrength("Effect Strength", Range(0, 1)) = 0.75
        _LineCount("Line Count", Range(80, 900)) = 360
        _LineThickness("Black Line Thickness", Range(0, 1)) = 0.58
        _LineDarkness("Black Line Darkness", Range(0, 1)) = 0.88
        _LineSharpness("Line Sharpness", Range(0.001, 0.2)) = 0.025
        _RGBOffset("RGB Offset", Range(0, 0.02)) = 0.003
        _HorizontalBleed("Horizontal Bleed", Range(0, 1)) = 0.2
        _TVNoiseStrength("TV Noise Strength", Range(0, 1)) = 0
        _ChromaKeyColor("Chroma Key Color", Color) = (1, 0, 1, 1)
        _ChromaKeyTolerance("Chroma Key Tolerance", Range(0, 1)) = 0.18
        _ScreenTint("Screen Tint", Color) = (0.9, 1, 0.82, 1)
        _TintStrength("Tint Strength", Range(0, 1)) = 0.25
        _VignetteStrength("Vignette Strength", Range(0, 1)) = 0.38
        _WarpStrength("Screen Warp Strength", Range(0, 0.2)) = 0.025
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
                half _EffectStrength;
                half _LineCount;
                half _LineThickness;
                half _LineDarkness;
                half _LineSharpness;
                half _RGBOffset;
                half _HorizontalBleed;
                half _TVNoiseStrength;
                half4 _ChromaKeyColor;
                half _ChromaKeyTolerance;
                half4 _ScreenTint;
                half _TintStrength;
                half _VignetteStrength;
                half _WarpStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float2 ApplyWarp(float2 uv, half strength)
            {
                float2 centered = uv * 2.0 - 1.0;
                float radiusSq = dot(centered, centered);
                centered += centered * radiusSq * _WarpStrength * strength;
                return centered * 0.5 + 0.5;
            }

            half4 SampleBase(float2 uv)
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half keyDistance = distance(color.rgb, _ChromaKeyColor.rgb);
                half keyMask = 1.0h - smoothstep(_ChromaKeyTolerance, _ChromaKeyTolerance + 0.08h, keyDistance);
                color.rgb = lerp(color.rgb, half3(0.0h, 0.0h, 0.0h), keyMask);
                return color;
            }

            half3 SampleRgbSplit(float2 uv, half strength)
            {
                float2 redUv = uv + float2(_RGBOffset * strength, 0.0);
                float2 blueUv = uv - float2(_RGBOffset * strength, 0.0);

                half r = SampleBase(saturate(redUv)).r;
                half g = SampleBase(uv).g;
                half b = SampleBase(saturate(blueUv)).b;
                return half3(r, g, b);
            }

            half3 ApplyHorizontalBleed(float2 uv, half3 color, half strength)
            {
                float2 texel = float2(1.0 / max(_ScreenParams.x, 1.0), 0.0);
                half3 left = SampleBase(saturate(uv - texel * 1.5)).rgb;
                half3 right = SampleBase(saturate(uv + texel * 1.5)).rgb;
                half3 bleed = (left + color + right) / 3.0h;
                return lerp(color, bleed, _HorizontalBleed * strength);
            }

            float Hash21(float2 value)
            {
                return frac(sin(dot(value, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 PerlinGradient(float2 cell)
            {
                float angle = Hash21(cell) * 6.28318530718;
                return float2(cos(angle), sin(angle));
            }

            float PerlinNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 local = frac(value);
                float2 blend = local * local * local * (local * (local * 6.0 - 15.0) + 10.0);

                float d00 = dot(PerlinGradient(cell), local);
                float d10 = dot(PerlinGradient(cell + float2(1.0, 0.0)), local - float2(1.0, 0.0));
                float d01 = dot(PerlinGradient(cell + float2(0.0, 1.0)), local - float2(0.0, 1.0));
                float d11 = dot(PerlinGradient(cell + float2(1.0, 1.0)), local - float2(1.0, 1.0));

                float x1 = lerp(d00, d10, blend.x);
                float x2 = lerp(d01, d11, blend.x);
                return lerp(x1, x2, blend.y);
            }

            float FractalPerlin(float2 value)
            {
                float noise = 0.0;
                float amplitude = 0.55;
                float amplitudeSum = 0.0;
                float frequency = 1.0;

                [unroll]
                for (int octave = 0; octave < 5; octave++)
                {
                    noise += PerlinNoise(value * frequency) * amplitude;
                    amplitudeSum += amplitude;
                    frequency *= 2.17;
                    amplitude *= 0.5;
                }

                return noise / max(amplitudeSum, 0.0001);
            }

            half3 ApplyTVNoise(float2 uv, half3 color)
            {
                half noiseStrength = saturate(_TVNoiseStrength);
                if (noiseStrength <= 0.0001h)
                    return color;

                float time = floor(_Time.y * 22.0);
                float2 aspectUv = float2(uv.x * (_ScreenParams.x / max(_ScreenParams.y, 1.0)), uv.y);
                float midNoise = FractalPerlin(aspectUv * 72.0 + float2(time * 0.37, -time * 0.19));
                float fineNoise = FractalPerlin(aspectUv * 180.0 + float2(-time * 0.61, time * 0.43));
                float microNoise = PerlinNoise(aspectUv * 420.0 + float2(time * 1.13, time * 0.71));
                float grain = midNoise * 0.34 + fineNoise * 0.46 + microNoise * 0.2;

                half staticValue = saturate((half)(0.5 + grain * 2.15));
                staticValue = saturate((staticValue - 0.5h) * 2.65h + 0.5h);
                half3 staticColor = half3(staticValue, staticValue, staticValue);

                half luminance = dot(color, half3(0.299h, 0.587h, 0.114h));
                color = lerp(color, half3(luminance, luminance, luminance), noiseStrength * 0.28h);
                color = lerp(color, staticColor, noiseStrength * 0.72h);

                return color;
            }

            half GetScanlineMask(float2 uv, half strength)
            {
                float linePhase = frac(uv.y * max(_LineCount, 1.0h));
                float centerDistance = abs(linePhase - 0.5) * 2.0;
                half blackLine = smoothstep(1.0h - _LineThickness - _LineSharpness, 1.0h - _LineThickness + _LineSharpness, centerDistance);
                return 1.0h - blackLine * _LineDarkness * strength;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half strength = saturate(_EffectStrength);
                float2 uv = ApplyWarp(input.uv, strength);

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                half4 baseColor = SampleBase(uv);
                half3 color = strength <= 0.0001h ? baseColor.rgb : SampleRgbSplit(uv, strength);
                color = ApplyHorizontalBleed(uv, color, strength);
                color = lerp(color, color * _ScreenTint.rgb, _TintStrength * strength);
                color *= GetScanlineMask(uv, strength);

                float2 centered = uv * 2.0 - 1.0;
                half vignette = saturate(1.0h - dot(centered, centered) * _VignetteStrength * strength);
                color *= vignette;
                color = ApplyTVNoise(uv, color);

                return half4(saturate(color), baseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
