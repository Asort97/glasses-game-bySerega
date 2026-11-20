Shader "Hidden/Skip Frozen Slices"
{
    Properties { _MainTex ("Frame", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _StartX, _Seed;
            // How much blue is removed where the shifted red channel is missing (cyan -> green fringe).
            float _GreenFringe;

            float hash(float x) { return frac(sin(x * 12.9898) * 43758.5453); }

            half4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                // Jitter each shared cut without changing the ordering of adjacent strips.
                float row = saturate(uv.y) * 19.0;
                float baseRow = floor(row);
                float band = baseRow + step(0.38 + 0.44 * hash(baseRow + _Seed), frac(row));
                float r = hash(band * 2.13 + _Seed);
                float dx = (r - 0.5) * 0.076;
                float rgb = (0.003 + 0.006 * hash(band + 0.7 + _Seed)) * (r < 0.5 ? -1 : 1);
                float x = _StartX + uv.x * 0.5 + dx;
                float margin = 0.5 * abs(_MainTex_TexelSize.x);
                float lo = _StartX + margin, hi = _StartX + 0.5 - margin;
                half red = tex2D(_MainTex, float2(clamp(x + rgb, lo, hi), uv.y)).r;
                half3 color = tex2D(_MainTex, float2(clamp(x, lo, hi), uv.y)).rgb;
                half missingRed = saturate(color.g - red);
                color.r = red;
                color.b = saturate(color.b - missingRed * (_GreenFringe > 0 ? _GreenFringe : 0.85));
                // Yellow hues (40-80 degrees): blue is lowest, and red and green are close.
                half peak = max(color.r, color.g);
                half chroma = peak - color.b;
                if (chroma > 0.0h && abs(color.r - color.g) <= chroma / 3.0h)
                    color = peak.xxx;
                return half4(color, 1);
            }
            ENDCG
        }
    }
}
