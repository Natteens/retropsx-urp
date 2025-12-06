using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Crt : VolumeComponent, IPostProcessComponent
{
    public FloatParameter scanlinesWeight = new FloatParameter(1f);
    public FloatParameter noiseWeight = new FloatParameter(1f);

    public FloatParameter screenBendX = new FloatParameter(1000.0f);
    public FloatParameter screenBendY = new FloatParameter(1000.0f);
    public FloatParameter vignetteAmount = new FloatParameter(0.0f);
    public FloatParameter vignetteSize = new FloatParameter(2.0f);
    public FloatParameter vignetteRounding = new FloatParameter(2.0f);
    public FloatParameter vignetteSmoothing = new FloatParameter(1.0f);

    public FloatParameter scanlinesDensity = new FloatParameter(200.0f);
    public FloatParameter scanlinesSpeed = new FloatParameter(-10.0f);
    public FloatParameter noiseAmount = new FloatParameter(250.0f);
    
    public Vector2Parameter chromaticRed = new Vector2Parameter(new Vector2());
    public Vector2Parameter chromaticGreen = new Vector2Parameter(new Vector2());
    public Vector2Parameter chromaticBlue = new Vector2Parameter(new Vector2());

    // Grille Effect is a modified version of the shader from here:
    // https://godotshaders.com/shader/vhs-and-crt-monitor-effect/
    public FloatParameter grilleOpacity = new FloatParameter(0.4f);
    public FloatParameter grilleCounterOpacity = new FloatParameter(0.2f);
    public FloatParameter grilleResolution = new FloatParameter(360.0f);
    public FloatParameter grilleCounterResolution = new FloatParameter(540.0f);
    public FloatParameter grilleUvRotation = new FloatParameter((float)(90.0f));
    public FloatParameter grilleBrightness = new FloatParameter(15.0f);
    public FloatParameter grilleUvMidPoint = new FloatParameter(0.5f);
    public Vector3Parameter grilleShift = new Vector3Parameter(new Vector3(1.0f, 1.0f, 1.0f));


    //INTERFACE REQUIREMENT 
    public bool IsActive() => true;
    public bool IsTileCompatible() => false;
}