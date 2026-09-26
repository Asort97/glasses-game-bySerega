Shader "Custom/CRT Scanline UI"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)

        _EffectStrength("Effect Strength", Range(0, 1)) = 0.594
        _LineCount("Line Count", Range(80, 900)) = 394
        _LineThickness("Black Line Thickness", Range(0, 1)) = 0.58
        _LineDarkness("Black Line Darkness", Range(0, 1)) = 0.88
        _LineSharpness("Line Sharpness", Range(0.001, 0.2)) = 0.025
        _RGBOffset("RGB Offset", Range(0, 0.02)) = 0.003
        _HorizontalBleed("Horizontal Bleed", Range(0, 1)) = 0.2

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "CRTScanlineUI"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 positionOS : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            half _EffectStrength;
            half _LineCount;
            half _LineThickness;
            half _LineDarkness;
            half _LineSharpness;
            half _RGBOffset;
            half _HorizontalBleed;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionOS = input.positionOS;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.screenPosition = ComputeScreenPos(output.positionHCS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 SampleSprite(float2 uv)
            {
                return tex2D(_MainTex, uv) + _TextureSampleAdd;
            }

            half GetScanlineMask(float screenY, half strength)
            {
                float linePhase = frac(screenY * max(_LineCount, 1.0h));
                float centerDistance = abs(linePhase - 0.5) * 2.0;
                half blackLine = smoothstep(
                    1.0h - _LineThickness - _LineSharpness,
                    1.0h - _LineThickness + _LineSharpness,
                    centerDistance);
                return 1.0h - blackLine * _LineDarkness * strength;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                half strength = saturate(_EffectStrength);
                fixed4 center = SampleSprite(input.uv);

                float2 rgbOffset = float2(_RGBOffset * strength, 0.0);
                half3 splitColor;
                splitColor.r = SampleSprite(input.uv + rgbOffset).r;
                splitColor.g = center.g;
                splitColor.b = SampleSprite(input.uv - rgbOffset).b;

                float2 bleedOffset = float2(_MainTex_TexelSize.x * 1.5, 0.0);
                half3 left = SampleSprite(input.uv - bleedOffset).rgb;
                half3 right = SampleSprite(input.uv + bleedOffset).rgb;
                half3 bleed = (left + splitColor + right) / 3.0h;
                half3 color = lerp(splitColor, bleed, _HorizontalBleed * strength);

                float screenY = input.screenPosition.y / max(input.screenPosition.w, 0.0001);
                color *= GetScanlineMask(screenY, strength);

                fixed4 outputColor = fixed4(color, center.a) * input.color;

                #ifdef UNITY_UI_CLIP_RECT
                outputColor.a *= UnityGet2DClipping(input.positionOS.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(outputColor.a - 0.001);
                #endif

                return outputColor;
            }
            ENDCG
        }
    }
}
