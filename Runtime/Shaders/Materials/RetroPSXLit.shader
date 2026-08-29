Shader "RetroPSX/Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(Off,0,Multiply,1,Albedo,2)] _VertexColorMode("Vertex Color", Float) = 1
        [Enum(Gouraud,1,Flat,0)] _ShadingMode("Shading", Float) = 1
        [Toggle] _TextureModulation("Texture Modulation", Float) = 1
        _ModulationTint("Modulation Tint", Color) = (0.5,0.5,0.5,1)
        _ModulationStrength("Modulation Strength", Range(0,1)) = 1
        [Toggle] _Overbright("PSX Overbright", Float) = 1
        [Enum(Unlit,0,VertexLit,1,ModernLit,2,ProfileDefault,3)] _LightingMode("Lighting", Float) = 3
        _VertexLightingStrength("Vertex Lighting Strength", Range(0,1)) = 1
        _GeometrySnapStrength("Geometry Snap", Range(0,1)) = 1
        _AffineStrength("Affine Strength", Range(0,1)) = 1
        [Toggle] _MaterialColorPrecision("Material Color Precision", Float) = 1
        [Toggle] _MaterialDither("Material Dither", Float) = 1
        _MaterialDitherStrength("Material Dither Strength", Range(0,1)) = 1
        _FogParticipation("Fog Participation", Range(0,1)) = 1
        [Toggle] _BlackTransparent("Black As Transparent", Float) = 0
        [HideInInspector] _TransparencyMode("Transparency Mode", Float) = 0
        [HideInInspector] _AlphaClip("Alpha Clip", Float) = 0
        [HideInInspector] _SrcBlend("Source Blend", Float) = 1
        [HideInInspector] _DstBlend("Destination Blend", Float) = 0
        [HideInInspector] _BlendOp("Blend Operation", Float) = 0
        [HideInInspector] _ZWrite("Z Write", Float) = 1
        [HideInInspector] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "RetroPSXForward"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            BlendOp [_BlendOp]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex RetroVertex
            #pragma fragment RetroFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "../Includes/RetroPSXMaterialPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex RetroDepthVertex
            #pragma fragment RetroDepthFragment
            #pragma multi_compile_instancing
            #include "../Includes/RetroPSXDepthPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex RetroShadowVertex
            #pragma fragment RetroDepthFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "../Includes/RetroPSXDepthPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "RetroPSX.Editor.RetroPSXMaterialInspector"
    Fallback Off
}
