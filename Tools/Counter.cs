namespace ZAMERT
{
using System;

public class Counter : FakeMono
{
    public new CNTDTO data = new CNTDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class CNTDTO : DTO
{
    public override void OnValidate() { }

    public int StartValue;
    public int TargetValue = 1;
    public int Step = 1;
    public bool ResetOnReach = true;
    public string FunctionName;
    public int GroovyNoiseId;
    public string GroovyNoiseGroup;
}
}
