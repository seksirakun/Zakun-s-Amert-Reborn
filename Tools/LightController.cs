namespace ZAMERT
{
using System;
using UnityEngine;

public class LightController : FakeMono
{
    public new LTDTO data = new LTDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class LTDTO : DTO
{
    public override void OnValidate() { }

    public bool ApplyOnStart = true;
    public bool Enabled = true;
    public bool OverrideColor;
    public Color Color = UnityEngine.Color.white;
    public bool OverrideIntensity;
    public float Intensity = 1f;
    public bool OverrideRange;
    public float Range = 10f;
    public bool Pulse;
    public float PulseSpeed = 1f;
    public float PulseMin;
    public float PulseMax = 1f;
}
}
