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

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

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
                float4 screenPos : TEXCOORD0; 
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float _HoleRadius;
                float _DitherWidth;
                float _HalftoneScale;
                float _MinOpacity;
                float _DepthBias;
            CBUFFER_END

            // Variables globales envoyées par le script C#
            float4 _GlobalPlayerPos;
            float _HoleVisibilityMask; // 1 = Trou activé, 0 = Mur solide

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
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

                // Calcul du masque du trou
                float holeMask = saturate((dist2D - _HoleRadius) / _DitherWidth);
                
                // NOUVEAU : On utilise la valeur du C# pour forcer le trou à se refermer (1.0) si on ne touche pas le bon Layer
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
                
                clip(opacity - dotThreshold);
                
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }
}