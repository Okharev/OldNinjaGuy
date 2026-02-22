Shader "Custom/URP_StencilMaskOnly"
{
    Properties
    {
        _StencilRef ("Reference Stencil", Int) = 1
    }

    SubShader
    {
        // "Queue"="Geometry-1" s'assure que le masque est calculé juste avant tes objets normaux (Geometry)
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-1" }
        
        // Rend l'objet masque totalement invisible à l'écran
        ColorMask 0 
        // Empêche le masque de bloquer les objets qui sont derrière lui via la profondeur
        ZWrite Off 

        Stencil
        {
            Ref [_StencilRef]
            Comp Always
            Pass Replace
        }

        Pass
        {
            Name "MaskPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // La couleur n'a pas d'importance car ColorMask est à 0
                return half4(0,0,0,0); 
            }
            ENDHLSL
        }
    }
}