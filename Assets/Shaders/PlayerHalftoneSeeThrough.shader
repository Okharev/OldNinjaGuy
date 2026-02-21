Shader "Custom/WallHoleHalftone"
{
    Properties
    {
        [Header(Wall Textures)]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Main Texture", 2D) = "white" {}

        [Header(Hole Settings)]
        // Values are much smaller now! 0.1 means 10% of the screen.
        _HoleRadius ("Hole Radius (Screen %)", Range(0, 1)) = 0.1 
        _DitherWidth ("Dither Transition Width", Range(0.001, 0.5)) = 0.05
        _HalftoneScale ("Halftone Scale (Dot Count)", Float) = 75.0
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
                float4 screenPos : TEXCOORD0; // Wall's screen position
                float3 positionWS : TEXCOORD1; // Wall's world position
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
            CBUFFER_END

            float4 _GlobalPlayerPos;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS); // Get wall coordinates on screen
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ==========================================
                // STEP 1: SCREEN SPACE POSITIONING
                // Convert player's 3D position to a 2D screen coordinate
                // ==========================================
                float4 playerHCS = TransformWorldToHClip(_GlobalPlayerPos.xyz);
                float4 playerScreenPos = ComputeScreenPos(playerHCS);
                float2 playerUV = playerScreenPos.xy / playerScreenPos.w;

                // Get the current wall pixel's 2D screen coordinate
                float2 wallUV = IN.screenPos.xy / IN.screenPos.w;

                // Calculate the 2D distance between them
                float2 diff = wallUV - playerUV;
                diff.x *= _ScreenParams.x / _ScreenParams.y; // Fix aspect ratio so it stays a perfect circle!
                float dist2D = length(diff);

                // ==========================================
                // STEP 2: DEPTH OCCLUSION CHECK
                // Is the wall actually blocking the player?
                // ==========================================
                float distToWall = distance(GetCameraPositionWS(), IN.positionWS);
                float distToPlayer = distance(GetCameraPositionWS(), _GlobalPlayerPos.xyz);
                
                // We check if wall is closer than player AND that player is not behind the camera
                bool isOccluding = (distToWall < distToPlayer) && (playerHCS.w > 0.0);

                // ==========================================
                // STEP 3: CALCULATE OPACITY
                // ==========================================
                float opacity = saturate((dist2D - _HoleRadius) / _DitherWidth);

                // IF the wall is behind the player, force opacity to 1.0 (draw solid wall)
                if (!isOccluding)
                {
                    opacity = 1.0;
                }

                // ==========================================
                // STEP 4: HALFTONE DITHER
                // ==========================================
                float2 screenGridUV = IN.positionHCS.xy / _ScreenParams.xy;
                screenGridUV.x *= _ScreenParams.x / _ScreenParams.y;
                screenGridUV *= _HalftoneScale;

                float2 localUV = frac(screenGridUV) - 0.5; 
                float dotThreshold = length(localUV) * 1.414;

                // Destroy pixels that fail the threshold test!
                clip(opacity - dotThreshold);

                // Render surviving pixels normally
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }
}