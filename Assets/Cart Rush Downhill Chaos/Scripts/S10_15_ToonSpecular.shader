Shader "Tecsup/S10/15_ToonSpecular_URP"
{
    Properties
    {
        _BaseColor ("Color Base", Color) = (0.2, 0.6, 1.0, 1.0)
        _Bands ("Numero de Bandas Difusas", Range(2, 10)) = 4.0
        _ShadowTint ("Tinte de Sombra", Color) = (0.1, 0.1, 0.3, 1.0)
        
        [Header(Brillo Especular Toon)]
        _SpecColor ("Color de Brillo Especular", Color) = (1.0, 1.0, 1.0, 1.0)
        _Shininess ("Exponente de Concentracion", Range(1, 128)) = 32.0
        _SpecCutoff ("Umbral de Brillo (Cutoff)", Range(0.01, 1.0)) = 0.5
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

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldNormal  : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Bands;
                float4 _ShadowTint;
                float4 _SpecColor;
                float _Shininess;
                float _SpecCutoff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = normalInput.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.worldNormal);
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 V = GetWorldSpaceNormalizeViewDir(input.worldPos);

                // 1. Iluminación Difusa Toon
                float NdotL = max(0.0, dot(N, L));
                float toonIntensity = floor(NdotL * _Bands) / _Bands;
                half3 toonLight = lerp(_ShadowTint.rgb, mainLight.color, toonIntensity);
                half3 ambient = SampleSH(N);
                half3 diffuseResult = _BaseColor.rgb * (toonLight + ambient);

                // 2. Brillo Especular Toon (Hard Highlight)
                float3 H = normalize(L + V);
                float NdotH = max(0.0, dot(N, H));
                float specRaw = pow(NdotH, _Shininess) * (NdotL > 0.0);
                float toonSpec = step(_SpecCutoff, specRaw);
                half3 specularResult = mainLight.color * _SpecColor.rgb * toonSpec;

                half3 finalColor = diffuseResult + specularResult;
                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}