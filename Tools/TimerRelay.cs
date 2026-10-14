namespace ZAMERT
{
using System;
using UnityEngine;

public class TimerRelay : FakeMono
{
    public new TMDTO data = new TMDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class TMDTO : DTO
{
    public override void OnValidate() { }

    public float Delay;
    [Tooltip("Seconds between repeats.")]
    public float Interval = 1f;
    public bool Repeat;
    [Tooltip("0 repeats forever while Repeat is on.")]
    public int RepeatCount;
    public bool AutoStart = true;
    public string FunctionName;
    public int GroovyNoiseId;
    public string GroovyNoiseGroup;
}
}
