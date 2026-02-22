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

        // NOUVEAU : Paramètre pour contrôler le Cel-Shading
        [Header(Retro Lighting)]
        _LightSteps ("Light Steps (Cel Shading)", Range(1, 10)) = 4.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "AlphaTest" 
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _MainTex_ST;
            float _HoleRadius;
            float _DitherWidth;
            float _HalftoneScale;
            float _MinOpacity;
            float _DepthBias;
            float _LightSteps; // NOUVEAU : On déclare la variable ici
        CBUFFER_END

        float4 _GlobalPlayerPos;
        float _HoleVisibilityMask;

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            float3 normalOS : NORMAL; 
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float4 screenPos : TEXCOORD0; 
            float3 positionWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            float3 normalWS : TEXCOORD3; 
        };

        Varyings SharedVert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
            OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS); 
            return OUT;
        }

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
            
            clip(opacity - dotThreshold);
        }
        ENDHLSL

        // =========================================================
        // PASSE 1 : UniversalForward 
        // =========================================================

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            Varyings vert(Attributes IN) { return SharedVert(IN); }

            half4 frag(Varyings IN) : SV_Target
            {
                ApplyHalftoneClip(IN);
                
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 albedo = texColor.rgb * _BaseColor.rgb;
                float3 normalWS = normalize(IN.normalWS);

                // --- 1. LUMIÈRE PRINCIPALE & OMBRES ---
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord); 
                
                // Angle de la lumière en escalier
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                NdotL = floor(NdotL * _LightSteps) / _LightSteps;
                
                // NOUVEAU : Ombre en escalier
                float mainShadow = mainLight.shadowAttenuation;
                mainShadow = floor(mainShadow * _LightSteps) / _LightSteps;
                
                // Application de l'ombre stylisée
                float3 diffuseLight = mainLight.color * NdotL * mainLight.distanceAttenuation * mainShadow;
                float3 ambientLight = SampleSH(normalWS);
                
                float3 finalColor = albedo * (diffuseLight + ambientLight);

                // --- 2. LUMIÈRES ADDITIONNELLES ---
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalizedScreenSpaceUV = IN.positionHCS.xy / _ScreenParams.xy;

                uint lightCount = GetAdditionalLightsCount();
                
                LIGHT_LOOP_BEGIN(lightCount)
                    Light addLight = GetAdditionalLight(lightIndex, IN.positionWS);
                    
                    // Angle de la lumière additionnelle en escalier
                    float addNdotL = saturate(dot(normalWS, addLight.direction));
                    addNdotL = floor(addNdotL * _LightSteps) / _LightSteps;
                    
                    // NOUVEAU : Ombre additionnelle en escalier
                    float addShadow = addLight.shadowAttenuation;
                    addShadow = floor(addShadow * _LightSteps) / _LightSteps;
                    
                    float3 addDiffuse = addLight.color * addNdotL * addLight.distanceAttenuation * addShadow;
                    finalColor += albedo * addDiffuse;
                LIGHT_LOOP_END

                return half4(finalColor, texColor.a * _BaseColor.a);
            }
            ENDHLSL
        }

        // =========================================================
        // PASSE 2 : ShadowCaster 
        // =========================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            Varyings vert(Attributes IN) { return SharedVert(IN); }
            half4 frag(Varyings IN) : SV_Target { ApplyHalftoneClip(IN); return 0; }
            ENDHLSL
        }

        // =========================================================
        // PASSE 3 : DepthOnly 
        // =========================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask 0 

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            Varyings vert(Attributes IN) { return SharedVert(IN); }
            half4 frag(Varyings IN) : SV_Target { ApplyHalftoneClip(IN); return 0; }
            ENDHLSL
        }

        // =========================================================
        // PASSE 4 : DepthNormals 
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
            half4 frag(Varyings IN) : SV_Target { ApplyHalftoneClip(IN); return half4(NormalizeNormalPerPixel(IN.normalWS), 0.0); }
            ENDHLSL
        }
    }
}