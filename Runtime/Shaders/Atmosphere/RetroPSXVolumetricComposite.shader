Shader "Hidden/RetroPSX/VolumetricComposite"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "Depth Aware Composite"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_RetroVolumeTexture);
            float4 _RetroVolumeTexelSize;
            float4 _RetroVolumeParams3;
            float _RetroPreserveAlpha;
            #include "../Includes/RetroPSXCommon.hlsl"
            #include "../Includes/RetroPSXFinalColor.hlsl"

            // Atmospheric scattering is identical for every surface past Max Distance, so depths are
            // compared clamped to it. Planar surfaces are affine in screen space in 1/z (z when ortho).
            float AffineDepth(float2 uv)
            {
                float depth = SampleSceneDepth(uv);
                bool ortho = unity_OrthoParams.w > 0.5;
                float eyeDepth = ortho ? LinearDepthToEyeDepth(depth) : LinearEyeDepth(depth, _ZBufferParams);
                eyeDepth = clamp(eyeDepth, 1e-4, _RetroVolumeParams3.z > 0.0 ? _RetroVolumeParams3.z : 1e20);
                return ortho ? eyeDepth : rcp(eyeDepth);
            }

            float ScatteringLuminance(half3 color)
            {
                return dot(color, half3(0.2126, 0.7152, 0.0722));
            }

            // Must match the raymarch: the depth pixel each volume texel was marched against.
            float2 VolumeDepthUV(float2 volumePixel)
            {
                return (floor((volumePixel + 0.5) * _ScreenSize.xy * _RetroVolumeTexelSize.xy - 0.25) + 0.5) * _ScreenSize.zw;
            }

            float OneSidedSlope(float2 backward, float2 center, float2 forward, float pixel, float size)
            {
                float back = (center.y - backward.y) / max(center.x - backward.x, 1e-4);
                float front = (forward.y - center.y) / max(forward.x - center.x, 1e-4);
                // The smaller one-sided difference keeps silhouettes out of the surface slope.
                return pixel <= 0.0 ? front : pixel >= size - 1.0 ? back : abs(back) < abs(front) ? back : front;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, uv);
                // Positions are in volume texels, at the depth pixels actually read.
                float2 pixelUV = (floor(uv * _ScreenSize.xy) + 0.5) * _ScreenSize.zw;
                float2 pixelPosition = pixelUV * _RetroVolumeTexelSize.zw;
                float pixelDepth = AffineDepth(pixelUV);
                float2 volumePixel = floor(uv * _RetroVolumeTexelSize.zw);
                half4 samples[9];
                float sampleDepths[9];
                float2 samplePositions[9];
                int index;
                [unroll]
                for (index = 0; index < 9; index++)
                {
                    float2 samplePixel = clamp(volumePixel + float2(index % 3 - 1, index / 3 - 1), 0.0, _RetroVolumeTexelSize.zw - 1.0);
                    float2 depthUV = VolumeDepthUV(samplePixel);
                    samplePositions[index] = depthUV * _RetroVolumeTexelSize.zw;
                    sampleDepths[index] = AffineDepth(depthUV);
                    samples[index] = LOAD_TEXTURE2D(_RetroVolumeTexture, samplePixel);
                }
                // A sample is valid when the pixel lies on the plane through it: grazing ground near the
                // horizon keeps its own scattering while depth discontinuities are still rejected.
                float2 slope = float2(
                    OneSidedSlope(float2(samplePositions[3].x, sampleDepths[3]), float2(samplePositions[4].x, sampleDepths[4]), float2(samplePositions[5].x, sampleDepths[5]), volumePixel.x, _RetroVolumeTexelSize.z),
                    OneSidedSlope(float2(samplePositions[1].y, sampleDepths[1]), float2(samplePositions[4].y, sampleDepths[4]), float2(samplePositions[7].y, sampleDepths[7]), volumePixel.y, _RetroVolumeTexelSize.w));
                float tolerance = 0.25 / max(_RetroVolumeParams3.y, 0.25);
                float errors[9];
                float nearestError = 1e20;
                int nearestIndex = 4;
                [unroll]
                for (index = 0; index < 9; index++)
                {
                    float predicted = sampleDepths[index] + dot(slope, pixelPosition - samplePositions[index]);
                    errors[index] = abs(pixelDepth - predicted) / max(abs(pixelDepth), 1e-6);
                    if (errors[index] < nearestError || (index == 4 && errors[index] <= nearestError))
                    {
                        nearestError = errors[index];
                        nearestIndex = index;
                    }
                }
                // Invalid samples belong to another surface and are never blended in. A surface thinner
                // than a volume texel has no valid sample: its own medium is estimated by rescaling the
                // nearest sample's optical depth to the pixel's distance. A sample without medium
                // leaves the original color: no scattering, full transmittance.
                half4 volume = half4(0.0, 0.0, 0.0, 1.0);
                if (nearestError > tolerance)
                {
                    bool ortho = unity_OrthoParams.w > 0.5;
                    float ratio = ortho ? pixelDepth / sampleDepths[nearestIndex] : sampleDepths[nearestIndex] / pixelDepth;
                    half4 nearest = samples[nearestIndex];
                    float transmittance = pow(max((float)nearest.a, 1e-4), ratio);
                    float scale = nearest.a < 0.9999 ? (1.0 - transmittance) / (1.0 - nearest.a) : saturate(ratio);
                    volume = half4(nearest.rgb * scale, transmittance);
                }
                else
                {
                    if (_RetroVolumeParams3.w > 0.5)
                        volume = samples[errors[4] <= tolerance ? 4 : nearestIndex];
                    else
                    {
                        half4 referenceVolume = samples[nearestIndex];
                        float referenceLuminance = ScatteringLuminance(referenceVolume.rgb);
                        float4 sum = 0.0;
                        float weightSum = 0.0;
                        [unroll]
                        for (index = 0; index < 9; index++)
                        {
                            half4 sampleVolume = samples[index];
                            float sampleLuminance = ScatteringLuminance(sampleVolume.rgb);
                            float relativeScatteringDelta = abs(sampleLuminance - referenceLuminance)
                                / max(max(sampleLuminance, referenceLuminance), 0.02);
                            float transmittanceDelta = abs(sampleVolume.a - referenceVolume.a);
                            float weight = errors[index] <= tolerance
                                ? exp(-errors[index] * _RetroVolumeParams3.y * 8.0 - relativeScatteringDelta * 8.0 - transmittanceDelta * 12.0)
                                : 0.0;
                            sum += sampleVolume * weight;
                            weightSum += weight;
                        }
                        volume = (half4)(sum / weightSum);
                    }
                }
                if (_RetroPSXDebugMode == 7)
                    return half4(volume.rgb, _RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0);
                if (_RetroPSXDebugMode == 10)
                    return half4(volume.rgb, _RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0);
                if (_RetroPSXDebugMode == 6)
                    return half4((1.0 - volume.a).xxx, _RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0);
                half3 composite = baseColor.rgb * volume.a + volume.rgb;
                // Resolve debug views retain their legacy composite and skip artistic color.
                int debugMode = _RetroPSXDebugMode;
                bool resolveDebug = debugMode == 1 || debugMode == 2 || debugMode == 3 || debugMode == 4 || debugMode == 5 || debugMode == 8;
                int2 pixel = int2(input.positionCS.xy);
                half3 precise = RetroPSX_ApplyFinalImagePrecision(composite, pixel);
                composite = precise;
                if (!resolveDebug)
                {
                    if (_RetroPreserveAlpha < 0.5 || baseColor.a > 0.0)
                        composite = RetroPSX_ApplyArtisticColor(precise, pixel);
                    if (debugMode == 11)
                        composite = abs(precise - composite) * 4.0;
                    else if (debugMode == 12)
                        composite = RetroPSX_ArtisticDitherThreshold(pixel).xxx;
                }
                return half4(composite, _RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
