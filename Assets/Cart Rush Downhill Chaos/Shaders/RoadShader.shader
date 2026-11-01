Shader "Custom/Road"
{
    Properties
    {
        [Header(Asphalt)]
        _AsphaltColor ("Asphalt Color", Color) = (0.16, 0.16, 0.17, 1)
        _AsphaltTex ("Asphalt Texture (optional)", 2D) = "white" {}
        _GrainScale ("Grain Scale", Float) = 12
        _GrainStrength ("Grain Strength", Range(0, 1)) = 0.35
        _PatchScale ("Patch Scale (stains)", Float) = 1.5
        _PatchStrength ("Patch Strength", Range(0, 1)) = 0.25
        _Smoothness ("Dry Smoothness", Range(0, 1)) = 0.12
        _TireTrackStrength ("Tire Tracks (polish)", Range(0, 1)) = 0.35

        [Header(Lines)]
        _EdgeColor ("Edge Line Color", Color) = (0.95, 0.95, 0.95, 1)
        _CenterColor ("Center Line Color", Color) = (0.95, 0.75, 0.1, 1)
        _EdgeOffset ("Edge Line Position (0-0.5)", Range(0.01, 0.2)) = 0.06
        _EdgeWidth ("Edge Line Width", Range(0.001, 0.1)) = 0.02
        _CenterGap ("Center Double Line Gap", Range(0, 0.05)) = 0.012
        _CenterWidth ("Center Line Width", Range(0.001, 0.05)) = 0.012
        _DashCount ("Dashes per UV length", Float) = 20
        _DashRatio ("Dash Fill (1 = solid line)", Range(0.05, 1)) = 0.55
        _LineWear ("Line Wear", Range(0, 1)) = 0.4

        [Header(Wetness)]
        _Wet ("Wetness / Puddles", Range(0, 1)) = 0
        _WetSmoothness ("Wet Smoothness", Range(0, 1)) = 0.92
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _AsphaltTex;

        fixed4 _AsphaltColor;
        float _GrainScale, _GrainStrength, _PatchScale, _PatchStrength;
        float _Smoothness, _TireTrackStrength;

        fixed4 _EdgeColor, _CenterColor;
        float _EdgeOffset, _EdgeWidth, _CenterGap, _CenterWidth;
        float _DashCount, _DashRatio, _LineWear;

        float _Wet, _WetSmoothness;

        struct Input
        {
            float2 uv_AsphaltTex; // u = a lo ancho de la calle, v = a lo largo
            float3 worldPos;
        };

        // ---------- Ruido procedural ----------
        float hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float valueNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = hash21(i);
            float b = hash21(i + float2(1, 0));
            float c = hash21(i + float2(0, 1));
            float d = hash21(i + float2(1, 1));
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        float fbm(float2 p)
        {
            float v = 0.0;
            float a = 0.5;
            for (int i = 0; i < 4; i++)
            {
                v += a * valueNoise(p);
                p *= 2.03;
                a *= 0.5;
            }
            return v;
        }

        // Linea vertical (a lo largo de la calle) con antialiasing
        float lineMask(float x, float center, float width, float aa)
        {
            return 1.0 - smoothstep(width * 0.5 - aa, width * 0.5 + aa, abs(x - center));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_AsphaltTex;
            float2 wp = IN.worldPos.xz;

            float aaX = fwidth(uv.x) * 1.0;
            float aaY = fwidth(uv.y) * 1.0;

            // ---------- Asfalto ----------
            float grain = fbm(wp * _GrainScale);
            float patches = fbm(wp * _PatchScale + 17.3);
            float3 tex = tex2D(_AsphaltTex, uv * float2(1, 8)).rgb;

            float3 asphalt = _AsphaltColor.rgb * tex;
            asphalt *= 1.0 + (grain - 0.5) * 2.0 * _GrainStrength;
            asphalt *= 1.0 + (patches - 0.5) * 2.0 * _PatchStrength;

            // Huellas de neumaticos: bandas al 25% y 75% del ancho (centro de cada carril)
            float tracks = lineMask(uv.x, 0.27, 0.07, 0.04) + lineMask(uv.x, 0.73, 0.07, 0.04);
            tracks = saturate(tracks) * _TireTrackStrength;
            asphalt *= 1.0 - tracks * 0.35;

            float smooth = _Smoothness + tracks * 0.25;

            // ---------- Lineas ----------
            // Bordes
            float edges = lineMask(uv.x, _EdgeOffset, _EdgeWidth, aaX)
                        + lineMask(uv.x, 1.0 - _EdgeOffset, _EdgeWidth, aaX);
            edges = saturate(edges);

            // Centro (doble linea)
            float center = lineMask(uv.x, 0.5 - _CenterGap - _CenterWidth * 0.5, _CenterWidth, aaX)
                         + lineMask(uv.x, 0.5 + _CenterGap + _CenterWidth * 0.5, _CenterWidth, aaX);
            center = saturate(center);

            // Segmentado (dash). DashRatio = 1 -> linea continua
            float phase = frac(uv.y * _DashCount);
            float dash = 1.0 - smoothstep(_DashRatio - aaY * _DashCount, _DashRatio + aaY * _DashCount, phase);
            dash = lerp(dash, 1.0, step(0.999, _DashRatio));
            center *= dash;

            // Desgaste de la pintura
            float wearNoise = fbm(wp * 6.0 + 3.7);
            float wearMask = smoothstep(_LineWear - 0.1, _LineWear + 0.1, wearNoise + (1.0 - _LineWear) * 0.35);
            float fine = valueNoise(wp * 40.0);
            wearMask *= lerp(1.0, smoothstep(0.15, 0.6, fine), _LineWear * 0.7);
            edges *= wearMask;
            center *= wearMask;

            float3 col = asphalt;
            col = lerp(col, _EdgeColor.rgb * (0.85 + 0.15 * grain), edges);
            col = lerp(col, _CenterColor.rgb * (0.85 + 0.15 * grain), center);

            float paint = saturate(edges + center);
            smooth = lerp(smooth, 0.25, paint);

            // ---------- Mojado / charcos ----------
            float puddleNoise = fbm(wp * 0.8 + 91.0);
            float puddle = smoothstep(1.0 - _Wet, 1.0 - _Wet + 0.12, puddleNoise + _Wet * 0.25);
            puddle *= step(0.001, _Wet);
            col *= lerp(1.0, 0.55, puddle);
            smooth = lerp(smooth, _WetSmoothness, puddle);

            o.Albedo = col;
            o.Metallic = 0.0;
            o.Smoothness = saturate(smooth);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
