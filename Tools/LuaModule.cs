using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LuaModule : FakeMono
{
    public new LMDTO data = new LMDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class LMDTO : DTO
{
    public override void OnValidate()
    {
        if (InteractionMaxRange < 0f) InteractionMaxRange = 0f;
        if (TickInterval < 0f) TickInterval = 0f;
    }

    [TextArea(8, 18)]
    [Tooltip("Full Lua script. if / for / while work. Call api.wait(seconds) inside loops so the server keeps running.")]
    public string InlineScript;

    [Tooltip("Optional .lua file next to the compiled schematic. Used when Inline Script is empty.")]
    public string ScriptPath;

    [Tooltip("Key that calls on_interact(). 0 = no key.")]
    public int InputKeyCode;

    public float InteractionMaxRange = 5f;

    [Tooltip("Calls on_enter(player) when a player touches a trigger collider on this object.")]
    public bool ListenEnter;

    [Tooltip("Calls on_stay(player) at most 4 times a second.")]
    public bool ListenStay;

    [Tooltip("Calls on_exit(player).")]
    public bool ListenExit;

    [Tooltip("Seconds between on_tick() calls. 0 disables the tick.")]
    public float TickInterval;
}

#if UNITY_EDITOR
public class LuaModuleCompiler
{
    [MenuItem("SchematicManager/Compile Lua Modules", priority = -11)]
    public static void OnCompile()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            string dir = AmertCompile.GetSchematicDirectory(schematic);
            if (dir == null)
                continue;

            LuaModule[] modules = AmertCompile.Collect<LuaModule>(schematic);
            foreach (LuaModule module in modules)
                AmertCompile.RefreshMeta(module);

            AmertCompile.WritePlain(dir, schematic, "LuaModules", modules.Select(x => x.data).ToList());
            Debug.Log("Compiled Lua Modules (" + modules.Length + ") for " + schematic.gameObject.name);
        }
    }
}
#endif
