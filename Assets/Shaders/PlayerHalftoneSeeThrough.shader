Shader "Custom/WallHoleHalftone"
{
    Properties
    {
        [Header(Wall Textures)]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Main Texture", 2D) = "white" {}

        [Header(Hole Settings)]
        _HoleRadius ("Hole Radius (Screen %)", Range(0, 1)) = 0.1 
        _DitherWidth ("Dither Transition Width", Range(0.001, 0.5)) = 0.05
        _HalftoneScale ("Halftone Scale (Dot Count)", Float) = 75.0
        
        [Header(Occlusion Settings)]
        _MinOpacity ("Minimum Opacity (Hole)", Range(0, 1)) = 0.1 
        _DepthBias ("Player Depth Bias", Float) = 0.0 
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "AlphaTest" 
        }

        // =========================================================
        // HLSLINCLUDE : Ce bloc contient le code partagé par TOUTES les passes
        // Cela évite de réécrire la logique du trou 3 fois !
        // =========================================================
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _MainTex_ST;
            float _HoleRadius;
            float _DitherWidth;
            float _HalftoneScale;
            float _MinOpacity;
            float _DepthBias;
        CBUFFER_END

        float4 _GlobalPlayerPos;
        float _HoleVisibilityMask;

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        // Données envoyées par le modèle 3D
        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            float3 normalOS : NORMAL; // Nécessaire pour l'Outline !
        };

        // Données transmises du Vertex au Fragment
        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float4 screenPos : TEXCOORD0; 
            float3 positionWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            float3 normalWS : TEXCOORD3; // Normales dans l'espace monde
        };

        // Fonction partagée pour initialiser les variables (Vertex)
        Varyings SharedVert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
            OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS); // Calcul des normales
            return OUT;
        }

        // Fonction partagée pour découper les pixels (Fragment)
        void ApplyHalftoneClip(Varyings IN)
        {
            float4 playerHCS = TransformWorldToHClip(_GlobalPlayerPos.xyz);
            float4 playerScreenPos = ComputeScreenPos(playerHCS);
            
            float2 playerUV = playerScreenPos.xy / playerScreenPos.w;
            float2 wallUV = IN.screenPos.xy / IN.screenPos.w;
            
            float2 diff = wallUV - playerUV;
            diff.x *= _ScreenParams.x / _ScreenParams.y;
            float dist2D = length(diff);

            float wallDepth = -TransformWorldToView(IN.positionWS).z;
            float playerDepth = -TransformWorldToView(_GlobalPlayerPos.xyz).z;
            
            bool isOccluding = (wallDepth < (playerDepth - _DepthBias)) && (playerHCS.w > 0.0);

            float holeMask = saturate((dist2D - _HoleRadius) / _DitherWidth);
            holeMask = lerp(1.0, holeMask, _HoleVisibilityMask);

            float opacity = lerp(_MinOpacity, 1.0, holeMask);

            if (!isOccluding)
            {
                opacity = 1.0;
            }

            float2 screenGridUV = IN.positionHCS.xy / _ScreenParams.xy;
            screenGridUV.x *= _ScreenParams.x / _ScreenParams.y;
            screenGridUV *= _HalftoneScale;

            float2 localUV = frac(screenGridUV) - 0.5; 
            float dotThreshold = length(localUV) * 1.414;
            
            // C'est ici que la magie opère : si le pixel échoue, il est détruit pour l'affichage,
            // la profondeur ET les normales !
            clip(opacity - dotThreshold);
        }
        ENDHLSL

        // =========================================================
        // PASSE 1 : UniversalForward (Dessine les couleurs à l'écran)
        // =========================================================
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes IN) { return SharedVert(IN); }

            half4 frag(Varyings IN) : SV_Target
            {
                ApplyHalftoneClip(IN); // Applique le trou
                
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return texColor * _BaseColor; // Affiche la couleur
            }
            ENDHLSL
        }

        // =========================================================
        // PASSE 2 : DepthOnly (Dessine la profondeur pour ton Outline)
        // =========================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask 0 // On n'a pas besoin de couleur, juste écrire dans le ZBuffer

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes IN) { return SharedVert(IN); }

            half4 frag(Varyings IN) : SV_Target
            {
                ApplyHalftoneClip(IN); // Applique le trou dans la profondeur
                return 0; // La caméra s'occupe de sauvegarder la distance
            }
            ENDHLSL
        }

        // =========================================================
        // PASSE 3 : DepthNormals (Dessine les normales pour ton Outline)
        // =========================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes IN) { return SharedVert(IN); }

            half4 frag(Varyings IN) : SV_Target
            {
                ApplyHalftoneClip(IN); // Applique le trou dans les normales
                
                // URP attend qu'on lui renvoie l'orientation 3D (Normal) du pixel
                return half4(NormalizeNormalPerPixel(IN.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}