Shader "CartRush/MontanaLowPoly_URP"
{
    Properties
    {
        _RocaClara ("Roca clara", Color) = (0.42, 0.40, 0.47, 1)
        _RocaOscura ("Roca oscura", Color) = (0.20, 0.19, 0.25, 1)
        _Nieve ("Color nieve", Color) = (0.90, 0.90, 1.00, 1)
        _Escala ("Escala ruido grande", Float) = 0.15
        _EscalaDetalle ("Escala detalle", Float) = 0.8
        _UmbralNieve ("Umbral de nieve (0-1.5)", Range(0, 1.5)) = 0.85
        _Suavidad ("Suavidad borde nieve", Range(0.001, 0.5)) = 0.05
        _AlturaNieve ("Altura donde empieza la nieve", Float) = 0
        _PesoAltura ("Peso de la altura", Float) = 0.01
        _Ambiente ("Luz ambiente", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _RocaClara;
                float4 _RocaOscura;
                float4 _Nieve;
                float _Escala;
                float _EscalaDetalle;
                float _UmbralNieve;
                float _Suavidad;
                float _AlturaNieve;
                float _PesoAltura;
                float _Ambiente;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float hash31(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float vnoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash31(i + float3(0, 0, 0));
                float n100 = hash31(i + float3(1, 0, 0));
                float n010 = hash31(i + float3(0, 1, 0));
                float n110 = hash31(i + float3(1, 1, 0));
                float n001 = hash31(i + float3(0, 0, 1));
                float n101 = hash31(i + float3(1, 0, 1));
                float n011 = hash31(i + float3(0, 1, 1));
                float n111 = hash31(i + float3(1, 1, 1));

                return lerp(
                    lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                    lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y),
                    f.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 pos = IN.positionWS;

                // Normal plana por cara (efecto low-poly)
                float3 n = normalize(cross(ddy(pos), ddx(pos)));
                if (dot(n, IN.normalWS) < 0) n = -n;

                // Ruido para variar la roca
                float ruido = vnoise(pos * _Escala) * 0.6 + vnoise(pos * _EscalaDetalle) * 0.4;
                float3 roca = lerp(_RocaOscura.rgb, _RocaClara.rgb, ruido);

                // Nieve: caras que miran arriba + zonas altas
                float valor = n.y + (pos.y - _AlturaNieve) * _PesoAltura + (ruido - 0.5) * 0.3;
                float nieve = smoothstep(_UmbralNieve - _Suavidad, _UmbralNieve + _Suavidad, valor);
                float3 col = lerp(roca, _Nieve.rgb, nieve);

                // Luz
                Light luz = GetMainLight();
                float ndl = saturate(dot(n, luz.direction));
                col *= luz.color * (ndl * (1.0 - _Ambiente) + _Ambiente);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
