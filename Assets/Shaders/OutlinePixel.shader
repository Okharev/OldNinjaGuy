Shader "Hidden/Custom/OutlinePixel"
{
    Properties
    {
        _DepthThreshold ("Depth Threshold", Range(0, 1)) = 0.05
        _ReverseDepthThreshold ("Reverse Depth Threshold", Range(0, 1)) = 0.25
        _NormalThreshold ("Normal Threshold", Range(0, 1)) = 0.6
        _DarkenAmount ("Darken Amount", Range(0, 1)) = 0.3
        _LightenAmount ("Lighten Amount", Range(0, 10)) = 1.5
        _NormalEdgeBias ("Normal Edge Bias", Vector) = (1, 1, 1, 0)
        
        // _LightDirection a été supprimé ici car nous utilisons la lumière dynamique
        
        _PixelSize ("Pixel Size (1 = Résolution native)", Float) = 1.0 
        _SubpixelOffset ("Subpixel Offset", Vector) = (0, 0, 0, 0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "PixelOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // NOUVEAU : Inclusion de la librairie d'éclairage pour GetMainLight()
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _DepthThreshold;
            float _ReverseDepthThreshold;
            float _NormalThreshold;
            float _DarkenAmount;
            float _LightenAmount;
            float3 _NormalEdgeBias;
            // float3 _LightDirection; (Supprimé)
            float _PixelSize;
            float2 _SubpixelOffset;

            float GetDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                
                // --- DÉBUT DES MODIFICATIONS SUBPIXEL ---
                float2 baseTexelSize = _ScreenSize.zw;
                float2 texelSize = baseTexelSize * max(1.0, _PixelSize);
                
                float2 shiftedUV = uv - (_SubpixelOffset * texelSize);
                float2 pixelatedUV = floor(shiftedUV / texelSize) * texelSize + (texelSize * 0.5);
                // --- FIN DES MODIFICATIONS SUBPIXEL ---

                half3 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixelatedUV).rgb;
                float depth = GetDepth(pixelatedUV);
                
                float3 normal = SampleSceneNormals(pixelatedUV); 

                float2 uvs[4];
                uvs[0] = float2(pixelatedUV.x, min(1.0 - 0.001, pixelatedUV.y + texelSize.y));
                uvs[1] = float2(pixelatedUV.x, max(0.0, pixelatedUV.y - texelSize.y));
                uvs[2] = float2(min(1.0 - 0.001, pixelatedUV.x + texelSize.x), pixelatedUV.y);
                uvs[3] = float2(max(0.0, pixelatedUV.x - texelSize.x), pixelatedUV.y);

                float depthDiff = 0.0;
                float depthDiffReversed = 0.0;
                float nearestDepth = depth;
                float2 nearestUV = pixelatedUV; 
                float normalSum = 0.0;

                for (int i = 0; i < 4; i++)
                {
                    float d = GetDepth(uvs[i]);
                    depthDiff += depth - d;
                    depthDiffReversed += d - depth;
                    if (d < nearestDepth)
                    {
                        nearestDepth = d;
                        nearestUV = uvs[i];
                    }

                    float3 n = SampleSceneNormals(uvs[i]);
                    float3 normalDiff = normal - n;

                    float normalBiasDiff = dot(normalDiff, _NormalEdgeBias);
                    float normalIndicator = smoothstep(-0.01, 0.01, normalBiasDiff);
                    normalSum += dot(normalDiff, normalDiff) * normalIndicator;
                }

                float depthEdge = step(_DepthThreshold, depthDiff);
                float reverseDepthEdge = step(_ReverseDepthThreshold, depthDiffReversed);
                
                float indicator = sqrt(normalSum);
                float normalEdge = step(_NormalThreshold, indicator - reverseDepthEdge);
                half3 nearest = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, nearestUV).rgb;

                // --- NOUVELLE GESTION DE LA LUMIÈRE DYNAMIQUE ---
                
                // 1. On récupère la lumière principale (Direction, Couleur, Intensité combinées)
                Light mainLight = GetMainLight();
                
                float3x3 viewToWorld = (float3x3)UNITY_MATRIX_I_V;
                float3 worldNormal = mul(viewToWorld, normal);
                
                // 2. On calcule la direction par rapport à la lumière dynamique
                float ld = dot(worldNormal, mainLight.direction);
                
                half3 depthCol = nearest * _DarkenAmount;
                
                // 3. On applique la couleur ET l'intensité de la lumière à la zone éclairée
                // Si la face tourne le dos à la lumière (ld > 0.0), on assombrit.
                // Sinon, on éclaircit en multipliant par la couleur dynamique de la lumière (mainLight.color) !
                half3 lightColorModifier = (ld > 0.0) ? half3(_DarkenAmount, _DarkenAmount, _DarkenAmount) : (_LightenAmount * mainLight.color);
                half3 normalCol = original * lightColorModifier;
                
                // ------------------------------------------------
                
                half3 edgeMix = lerp(normalCol, depthCol, depthEdge);
                
                float edgeMask = (depthEdge > 0.0) ? depthEdge : normalEdge;
                half3 finalColor = lerp(original, edgeMix, edgeMask);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}