namespace ZAMERT
{
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

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
