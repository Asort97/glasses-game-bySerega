Shader "Custom/SplitChromaticAberration"
{
    Properties
    {
        _LeftColor  ("Left Tint",   Color) = (1, 0, 0, 1)
        _RightColor ("Right Tint",  Color) = (0, 0, 1, 1)
        _Strength   ("Strength",    Range(0.001, 0.05)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "SplitChromaticAberration"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _LeftColor;
            float4 _RightColor;
            float  _Strength;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Ліва половина → лівий колір, права → правий
                float  isRight = step(0.5, uv.x);
                half3  tint    = lerp((half3)_LeftColor.rgb, (half3)_RightColor.rgb, isRight);

                // Зміщення від центру екрану
                float2 dir    = uv - float2(0.5, 0.5);
                float2 offset = dir * _Strength;

                // Оригінальний піксель та зміщена "копія"
                half3 original = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv,                    0).rgb;
                half3 shifted  = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, saturate(uv + offset), 0).rgb;

                // Зміщуємо ТІЛЬКИ ті канали, що є в кольорі тинту.
                // green(0,1,0) → зміщується лише G → тільки зелена бахрома.
                // red(1,0,0)   → зміщується лише R → тільки червона бахрома.
                half3 result;
                result.r = lerp(original.r, shifted.r, tint.r);
                result.g = lerp(original.g, shifted.g, tint.g);
                result.b = lerp(original.b, shifted.b, tint.b);

                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
