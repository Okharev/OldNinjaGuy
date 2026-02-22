Shader "Custom/URP_DitherFade"
{
    Properties
    {
        _BaseColor ("Couleur Principale", Color) = (1, 1, 1, 1)
        _BaseMap ("Texture Principale", 2D) = "white" {}
        
        // Le paramètre que notre script C# va modifier
        _DitherFade ("Opacité Dither", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        // L'objet reste Opaque, ce qui est excellent pour les performances
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "UnlitDither"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float4 screenPos : TEXCOORD1; // Pour calculer la position sur l'écran
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _DitherFade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                // On calcule la position de l'objet par rapport à l'écran du joueur
                output.screenPos = ComputeScreenPos(output.positionHCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Calculer les coordonnées exactes du pixel sur l'écran
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 pixelPos = screenUV * _ScreenParams.xy;

                // 2. Créer la matrice de Bayer 4x4
                int x = int(fmod(pixelPos.x, 4));
                int y = int(fmod(pixelPos.y, 4));

                const float dither[16] = {
                     0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                    12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                     3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                    15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
                };

                int index = x + y * 4;
                float limit = dither[index];

                // 3. Annuler le pixel si la valeur DitherFade est inférieure à la limite de la matrice
                // Le -0.001 s'assure qu'à 1.0, aucun pixel n'est accidentellement coupé
                clip(_DitherFade - limit - 0.001