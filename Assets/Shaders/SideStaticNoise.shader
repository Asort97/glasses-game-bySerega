Shader "Custom/SideStaticNoise"
{
    Properties
    {
        _LeftAmount("Left Amount", Range(0, 1)) = 0
        _RightAmount("Right Amount", Range(0, 1)) = 0
        _EdgeSoftness("Edge Softness", Range(0.001, 0.5)) = 0.12
        _EdgeRoughness("Edge Roughness", Range(0, 1)) = 0.7
        _Intensity("Intensity", Range(0, 1)) = 1
        _NoiseScale("Noise Scale", Range(1, 12)) = 2
        _NoiseSpeed("Noise Speed", Range(0, 60)) = 20
        _BlackDensity("Black Dot Density", Range(0, 1)) = 0.38
        _WhiteBoost("White Boost", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "SideStaticNoise"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _LeftAmount;
            float _RightAmount;
            float _EdgeSoftness;
            float _EdgeRoughness;
            float _Intensity;
            float _NoiseScale;
            float _NoiseSpeed;
            float _BlackDensity;
            float _WhiteBoost;
            float4 _NoiseResolution;

            float Hash21(float2 value)
            {
                return frac(sin(dot(value, float2(127.1, 311.7))) * 43758.5453123);
            }

            float SmoothNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 local = frac(value);
                float2 blend = local * local * (3.0 - 2.0 * local);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));

                return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
            }

            float RevealMask(float distanceFromSide, float amount, float2 uv)
            {
                if (amount <= 0.0001)
                    return 0.0;

                float softness = max(_EdgeSoftness, 0.001);
                float2 drift = float2(_Time.y * 0.12, -_Time.y * 0.08);
                float coarseNoise = SmoothNoise(uv * float2(4.0, 2.4) + drift) * 2.0 - 1.0;
                float mediumNoise = SmoothNoise(uv * float2(13.0, 7.0) - drift * 1.7) * 2.0 - 1.0;
                float roughEdge = (coarseNoise * 0.75 + mediumNoise * 0.25) * _EdgeRoughness * softness;
                float reveal = (amount - distanceFromSide + roughEdge) / softness;
                return smoothstep(0.0, 1.0, saturate(reveal));
            }

            float SideMask(float2 uv)
            {
                float leftAmount = saturate(_LeftAmount);
                float leftMask = RevealMask(uv.x, leftAmount, uv);

                float rightAmount = saturate(_RightAmount);
                float rightMask = RevealMask(1.0 - uv.x, rightAmount, uv);

                return saturate(max(leftMask, rightMask) * _Intensity);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);

                float revealMask = SideMask(uv);
                if (revealMask <= 0.0001)
                    return source;

                float timeStep = floor(_Time.y * _NoiseSpeed);
                float2 noiseUv = floor(uv * (900.0 / max(_NoiseScale, 1.0))) + float2(timeStep * 13.0, timeStep * 37.0);
                float checker = fmod(noiseUv.x + noiseUv.y, 2.0);
                float brokenChecker = fmod(floor(noiseUv.x * 1.7) + floor(noiseUv.y * 0.9), 2.0);
                float jitter = Hash21(floor(noiseUv * 0.35));
                float grain = frac(checker * 0.37 + brokenChecker * 0.29 + jitter * 0.83);
                float staticValue = step(saturate(_BlackDensity), grain);
                staticValue = saturate(staticValue + saturate(_WhiteBoost) * 0.04);
                half3 staticColor = half3(staticValue, staticValue, staticValue);

                half3 result = lerp(source.rgb, staticColor, revealMask);
                return half4(result, source.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
