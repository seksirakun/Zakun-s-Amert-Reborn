namespace ZAMERT
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.ComponentModel;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;

public class GroovyNoise : FakeMono
{
    public new GNDTO data = new GNDTO();
    public new FGNDTO ScriptValueData = new FGNDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class GNDTO : DTO
{
    public override void OnValidate()
    {
    }

    public List<GMDTO> Settings;
}

[Serializable]
public class FGNDTO : DTO
{
    public override void OnValidate()
    {
        Settings.ForEach(x => x.OnValidate());
    }

    public List<FGMDTO> Settings;
}

#if UNITY_EDITOR
public class GroovyNoiseCompiler
{
    [MenuItem("SchematicManager/Compile Groovy Noise", priority = -11)]
    public static void OnCompile()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            Compile(schematic);
        }
    }

    public static void Compile(Schematic schematic)
    {
        string dir = AmertCompile.GetSchematicDirectory(schematic);
        if (dir == null)
            return;

        GroovyNoise[] noises = AmertCompile.Collect<GroovyNoise>(schematic);
        foreach (GroovyNoise noise in noises)
            AmertCompile.RefreshMeta(noise);

        AmertCompile.WritePlain(dir, schematic, "GroovyNoises", noises.Where(x => !x.UseScriptValue).Select(x => x.data).ToList());
        AmertCompile.WriteTyped(dir, schematic, "FGroovyNoises", noises.Where(x => x.UseScriptValue).Select(x => x.ScriptValueData).ToList());
        Debug.Log("Compiled Groovy Noises (" + noises.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
}
