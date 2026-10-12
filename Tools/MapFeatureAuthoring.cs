namespace ZAMERT
{
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SpinMotor : FakeMono
{
    public new RTDTO data = new RTDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }

    private void OnDrawGizmosSelected()
    {
        if (data == null || data.AngularSpeed == null)
            return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Vector3 axis = data.UseWorldSpace ? (Vector3)data.AngularSpeed : transform.TransformDirection(data.AngularSpeed);
        if (axis.sqrMagnitude < 0.0001f)
            axis = transform.up;
        Gizmos.DrawLine(transform.position, transform.position + axis.normalized * 0.75f);
    }
}

[Serializable]
public class RTDTO : DTO
{
    public override void OnValidate() { }

    [Tooltip("Degrees per second around X, Y and Z.")]
    public SVector3 AngularSpeed = new SVector3 { y = 45f };
    public bool UseWorldSpace;
}

public class MoveMotor : FakeMono
{
    public new MVDTO data = new MVDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }

    private void OnDrawGizmosSelected()
    {
        if (data == null)
            return;
        Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.9f);
        Vector3 end = transform.position + transform.TransformVector(data.LocalOffset);
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawSphere(end, 0.08f);
    }
}

[Serializable]
public class MVDTO : DTO
{
    public override void OnValidate() { }

    public SVector3 LocalOffset = new SVector3 { x = 0f, y = 2f, z = 0f };
    public float Duration = 2f;
    public float StartDelay;
    public bool PingPong = true;
    public bool Loop = true;
    public bool AutoStart = true;
}

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

public class SchematicAnchor : FakeMono
{
    public new SADTO data = new SADTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);
    }
}

[Serializable]
public class SADTO : DTO
{
    public override void OnValidate() { }

    [Tooltip("Name of another compiled schematic to spawn here.")]
    public string SchematicName;
    public bool SpawnOnStart = true;
    public SVector3 Scale = new SVector3 { x = 1f, y = 1f, z = 1f };
}

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

#if UNITY_EDITOR
public class SpinMotorCompiler
{
    [MenuItem("SchematicManager/Compile Spin Motors", priority = -11)]
    public static void OnCompile() { MapFeatureCompile.CompileAllOf<SpinMotor>("Rotators", motor => motor.data); }
}

public class MoveMotorCompiler
{
    [MenuItem("SchematicManager/Compile Move Motors", priority = -11)]
    public static void OnCompile() { MapFeatureCompile.CompileAllOf<MoveMotor>("Movers", motor => motor.data); }
}

public class LightControllerCompiler
{
    [MenuItem("SchematicManager/Compile Light Controllers", priority = -11)]
    public static void OnCompile() { MapFeatureCompile.CompileAllOf<LightController>("Lights", light => light.data); }
}

public class TimerRelayCompiler
{
    [MenuItem("SchematicManager/Compile Timer Relays", priority = -11)]
    public static void OnCompile() { MapFeatureCompile.CompileAllOf<TimerRelay>("Timers", timer => timer.data); }
}

public class SchematicAnchorCompiler
{
    [MenuItem("SchematicManager/Compile Schematic Anchors", priority = -11)]
    public static void OnCompile() { MapFeatureCompile.CompileAllOf<SchematicAnchor>("SchematicAnchors", anchor => anchor.data); }
}

public class CounterCompiler
{
    [MenuItem("SchematicManager/Compile Counters", priority = -11)]
    public static void OnCompile() { MapFeatureCompile.CompileAllOf<Counter>("Counters", counter => counter.data); }
}

public static class MapFeatureCompile
{
    public static void CompileAllOf<T>(string suffix, System.Func<T, DTO> select) where T : FakeMono
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            string dir = AmertCompile.GetSchematicDirectory(schematic);
            if (dir == null)
                continue;

            T[] items = AmertCompile.Collect<T>(schematic);
            foreach (T item in items)
                AmertCompile.RefreshMeta(item);

            AmertCompile.WritePlain(dir, schematic, suffix, items.Select(select).ToList());
            Debug.Log("Compiled " + suffix + " (" + items.Length + ") for " + schematic.gameObject.name);
        }
    }
}

public static class ZamertCompileAll
{
    [MenuItem("SchematicManager/Compile All ZAMERT", priority = -20)]
    public static void CompileEverything()
    {
        CustomColliderCompiler.OnCompile();
        CustomDoorCompiler.OnCompile();
        CustomInteractableToyCompiler.OnCompile();
        CustomTextureCompiler.OnCompile();
        DamageTriggerCompiler.OnCompile();
        FunctionExecutorCompiler.OnCompile();
        GroovyNoiseCompiler.OnCompile();
        HealthObjectCompiler.OnCompile();
        InteractableObjectCompiler.OnCompile();
        InteractablePickupCompiler.OnCompile();
        InteractableTeleporterCompiler.OnCompile();
        ItemSpawnerCompiler.OnCompile();
        LoopSpeakerCompiler.OnCompile();
        MapDoorCompiler.OnCompile();
        MapScp079CameraCompiler.OnCompile();
        ClutterCompiler.OnCompile();
        PlayerCountTriggerCompiler.OnCompile();
        PrefabAnchorCompiler.OnCompile();
        CustomRGBA.Compile();
        SpinMotorCompiler.OnCompile();
        MoveMotorCompiler.OnCompile();
        LightControllerCompiler.OnCompile();
        TimerRelayCompiler.OnCompile();
        SchematicAnchorCompiler.OnCompile();
        CounterCompiler.OnCompile();
        LuaModuleCompiler.OnCompile();
    }
}
#endif
}
