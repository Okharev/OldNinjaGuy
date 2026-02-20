Shader "Custom/URP/ObstacleCutout"
{
Properties
    {
        // Texture et couleur de base du mur
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        // Rayon de la découpe autour du joueur
        _CutoutRadius("Cutout Radius", Float) = 2.0
        
        // La position du joueur (elle sera mise à jour par notre script C#)
        _GlobalPlayerPosition("Player Position", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        // Opaque car c'est un mur solide par défaut
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Inclusion des bibliothèques de base de URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Les données que le shader reçoit du modèle 3D
            struct Attributes
            {
                float4 positionOS : POSITION; // Position dans l'espace objet
                float2 uv : TEXCOORD0;        // Coordonnées de la texture
            };

            // Les données envoyées du Vertex Shader au Fragment Shader
            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Position à l'écran
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;    // Position réelle dans le monde 3D
            };

            // Déclaration des variables
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _CutoutRadius;
                // Position du joueur récupérée globalement
                float4 _GlobalPlayerPosition; 
            CBUFFER_END

            // --- VERTEX SHADER ---
            // Prépare les données pour chaque sommet (point) de ton mur
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Calcule la position dans le monde 3D
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                // Calcule la position sur ton écran
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            // --- FRAGMENT SHADER ---
            // Calcule la couleur finale de chaque pixel
            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Calculer la distance entre ce pixel du mur et le joueur
                // On utilise seulement X et Z pour ignorer la hauteur (typique en isométrique)
                float2 pixelPosXZ = IN.positionWS.xz;
                float2 playerPosXZ = _GlobalPlayerPosition.xz;
                float dist = distance(pixelPosXZ, playerPosXZ);

                // 2. Créer le trou
                // La fonction clip() annule le dessin du pixel si la valeur entre parenthèses est négative.
                // Si distance < Rayon, alors (distance - Rayon) est négatif -> le pixel disparaît !
                clip(dist - _CutoutRadius);

                // 3. Si le pixel n'a pas été supprimé, on dessine la texture normalement
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                return color;
            }
            ENDHLSL
        }
    }
}