using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Writes UnityEngine.Vector3 as {x,y,z}. Serializing Vector3 with the default
/// Json.NET contract explodes on the self-referencing 'normalized' property,
/// so every AMERT serializer must go through this converter.
/// </summary>
public class Vector3JsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector3);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector3 v = (Vector3)value;
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(v.x);
        writer.WritePropertyName("y");
        writer.WriteValue(v.y);
        writer.WritePropertyName("z");
        writer.WriteValue(v.z);
        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        Vector3 v = Vector3.zero;
        if (reader.TokenType == JsonToken.Null)
            return v;

        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            string name = (string)reader.Value;
            float value = Convert.ToSingle(reader.ReadAsDouble() ?? 0d);
            if (name == "x") v.x = value;
            else if (name == "y") v.y = value;
            else if (name == "z") v.z = value;
        }
        return v;
    }
}

/// <summary>
/// Writes UnityEngine.Color as {r,g,b,a} (the shape the ZAMERT plugin's SColor
/// expects). Same self-reference problem as Vector3 ('linear'/'gamma').
/// </summary>
public class ColorJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Color);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Color c = (Color)value;
        writer.WriteStartObject();
        writer.WritePropertyName("r");
        writer.WriteValue(c.r);
        writer.WritePropertyName("g");
        writer.WriteValue(c.g);
        writer.WritePropertyName("b");
        writer.WriteValue(c.b);
        writer.WritePropertyName("a");
        writer.WriteValue(c.a);
        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        Color c = new Color(0f, 0f, 0f, 1f);
        if (reader.TokenType == JsonToken.Null)
            return c;

        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            string name = (string)reader.Value;
            float value = Convert.ToSingle(reader.ReadAsDouble() ?? 0d);
            if (name == "r") c.r = value;
            else if (name == "g") c.g = value;
            else if (name == "b") c.b = value;
            else if (name == "a") c.a = value;
        }
        return c;
    }
}

/// <summary>
/// Resolves $type names by short type name so files written for the plugin
/// ("Type, AdvancedMERTools") can be decompiled back into the Unity project
/// ("Type, Assembly-CSharp") and vice versa.
/// </summary>
public class AmertSerializationBinder : ISerializationBinder
{
    private static Dictionary<string, Type> _types;
    private readonly DefaultSerializationBinder _default = new DefaultSerializationBinder();

    private static Dictionary<string, Type> GetTypes()
    {
        if (_types == null)
        {
            Type[] allTypes;
            try
            {
                allTypes = typeof(AmertSerializationBinder).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                allTypes = ex.Types;
            }

            _types = allTypes
                .Where(t => t != null && t.IsClass && !t.IsAbstract
                    && (typeof(Value).IsAssignableFrom(t) || typeof(Function).IsAssignableFrom(t)))
                .GroupBy(t => t.Name)
                .ToDictionary(g => g.Key, g => g.First());
        }
        return _types;
    }

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        // ScriptValue graphs are consumed by the ZAMERT plugin, whose types live
        // in the "AdvancedMERTools" namespace/assembly. Emitting that name here
        // (instead of the Unity editor assembly) keeps exported $type entries
        // valid on the server no matter what this assembly is called.
        if (typeof(Value).IsAssignableFrom(serializedType) || typeof(Function).IsAssignableFrom(serializedType))
        {
            assemblyName = AmertJson.RuntimeAssemblyName;
            typeName = serializedType.Name;
            return;
        }

        _default.BindToName(serializedType, out assemblyName, out typeName);
    }

    public Type BindToType(string assemblyName, string typeName)
    {
        string shortName = typeName != null && typeName.Contains(".")
            ? typeName.Substring(typeName.LastIndexOf('.') + 1)
            : typeName;

        Type type;
        if (shortName != null && GetTypes().TryGetValue(shortName, out type))
            return type;

        return _default.BindToType(assemblyName, typeName);
    }
}

/// <summary>
/// Central Json.NET configuration for every AMERT compile/decompile call.
/// </summary>
public static class AmertJson
{
    public const string RuntimeAssemblyName = "AdvancedMERTools";
    public const string EditorAssemblyName = "Assembly-CSharp";

    public static JsonSerializerSettings Plain()
    {
        return new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = NewConverters(),
        };
    }

    public static JsonSerializerSettings Typed()
    {
        return new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = new AmertSerializationBinder(),
            Converters = NewConverters(),
        };
    }

    private static List<JsonConverter> NewConverters()
    {
        return new List<JsonConverter>
        {
            new Vector3JsonConverter(),
            new ColorJsonConverter(),
        };
    }

    public static string SerializePlain(object value)
    {
        return JsonConvert.SerializeObject(value, Formatting.Indented, Plain());
    }

    /// <summary>Serializes with $type info rewritten for the ZAMERT plugin assembly.</summary>
    public static string SerializeTyped(object value)
    {
        return JsonConvert.SerializeObject(value, Formatting.Indented, Typed())
            .Replace(EditorAssemblyName, RuntimeAssemblyName);
    }

    public static List<T> DeserializeList<T>(string json)
    {
        List<T> result = JsonConvert.DeserializeObject<List<T>>(json, Typed());
        return result ?? new List<T>();
    }
}

#if UNITY_EDITOR
/// <summary>
/// Shared helpers used by every "SchematicManager/Compile ..." menu command.
/// </summary>
public static class AmertCompile
{
    /// <summary>
    /// Resolves (and creates) the compiled-schematic output directory,
    /// or returns null with an error log when the root export dir is missing.
    /// </summary>
    public static string GetSchematicDirectory(Schematic schematic)
    {
        string parentDirectoryPath = ResolveExportRoot();

        if (!Directory.Exists(parentDirectoryPath))
        {
            Debug.LogError("AMERT: could not find the compiled-schematics directory: " + parentDirectoryPath);
            return null;
        }

        try
        {
            string schematicDirectoryPath = Path.Combine(parentDirectoryPath, schematic.gameObject.name);
            Directory.CreateDirectory(schematicDirectoryPath);
            return schematicDirectoryPath;
        }
        catch (Exception ex)
        {
            Debug.LogError("AMERT: could not create output directory for schematic '" + schematic.gameObject.name + "': " + ex.Message);
            return null;
        }
    }

    /// <summary>Root export directory (used by teleporter compile, which writes per-map files).</summary>
    public static string GetExportRootDirectory()
    {
        string parentDirectoryPath = ResolveExportRoot();

        if (!Directory.Exists(parentDirectoryPath))
        {
            Debug.LogError("AMERT: could not find the compiled-schematics directory: " + parentDirectoryPath);
            return null;
        }
        return parentDirectoryPath;
    }

    private static string ResolveExportRoot()
    {
        string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MapEditorReborn_CompiledSchematics");

        Config config = SchematicManager.Config;
        // SchematicManager.Config's own == / != operator overload throws when
        // either side is null instead of handling it, so a plain "config != null"
        // here would crash. ReferenceEquals bypasses that overload entirely.
        if (ReferenceEquals(config, null))
            return defaultPath;

        string exportPath;
        try
        {
            exportPath = config.ExportPath;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("AMERT: could not read SchematicManager.Config.ExportPath, falling back to Desktop: " + ex.Message);
            return defaultPath;
        }

        return Directory.Exists(exportPath) ? exportPath : defaultPath;
    }

    /// <summary>
    /// Stamps ObjectId / ObjectNamePath / Code / ScriptGroup / UseScriptValue on
    /// both DTOs of a component, so exports never rely on stale inspector-time
    /// values. Only the DTO variant actually being exported (matching
    /// UseScriptValue) is validated: the other variant is normally never
    /// touched in the inspector, so its ScriptValue/List fields are commonly
    /// null, and running OnValidate() on it has nothing to do with what gets
    /// written to disk.
    /// </summary>
    public static void RefreshMeta(FakeMono mono)
    {
        if (mono == null)
            return;

        string objectId = PublicFunctions.FindPath(mono.transform);
        string objectNamePath = PublicFunctions.FindNamePath(mono.transform);

        StampMeta(mono._data, mono, objectId, objectNamePath);
        StampMeta(mono._ScriptValueData, mono, objectId, objectNamePath);

        DTO active = mono.UseScriptValue ? mono._ScriptValueData : mono._data;
        ValidateDto(active, mono);
    }

    private static void StampMeta(DTO dto, FakeMono mono, string objectId, string objectNamePath)
    {
        if (dto == null)
            return;

        dto.ObjectId = objectId;
        dto.ObjectNamePath = objectNamePath;
        dto.Code = mono.ScriptId != 0 ? mono.ScriptId : mono.GetInstanceID();
        dto.ScriptGroup = mono.ScriptGroup;
        dto.UseScriptValue = mono.UseScriptValue;
    }

    private static void ValidateDto(DTO dto, FakeMono mono)
    {
        if (dto == null)
            return;

        try
        {
            dto.OnValidate();
            PublicFunctions.ValidateCommonData(dto);
        }
        catch (Exception ex)
        {
            string path = PublicFunctions.FindNamePath(mono.transform);
            Debug.LogWarning("AMERT: validation failed on '" + (string.IsNullOrEmpty(path) ? mono.name : path)
                + "' (" + mono.GetType().Name + ", UseScriptValue=" + mono.UseScriptValue + "): " + ex.Message
                + "\nThis usually means a ScriptValue or module list field on that component was never set up "
                + "(open it in the inspector once and fill in the fields that mode needs), or "
                + "UseScriptValue is toggled on but the Script Value variant was never configured.");
        }
    }

    /// <summary>Writes "&lt;Schematic&gt;-&lt;suffix&gt;.json" without $type info.</summary>
    public static void WritePlain(string schematicDirectory, Schematic schematic, string suffix, object payload)
    {
        string path = Path.Combine(schematicDirectory, schematic.gameObject.name + "-" + suffix + ".json");
        try
        {
            File.WriteAllText(path, AmertJson.SerializePlain(payload));
        }
        catch (Exception ex)
        {
            Debug.LogError("AMERT: failed to write '" + path + "': " + ex.Message);
        }
    }

    /// <summary>Writes "&lt;Schematic&gt;-&lt;suffix&gt;.json" with $type info for ScriptValue graphs.</summary>
    public static void WriteTyped(string schematicDirectory, Schematic schematic, string suffix, object payload)
    {
        string path = Path.Combine(schematicDirectory, schematic.gameObject.name + "-" + suffix + ".json");
        try
        {
            File.WriteAllText(path, AmertJson.SerializeTyped(payload));
        }
        catch (Exception ex)
        {
            Debug.LogError("AMERT: failed to write '" + path + "': " + ex.Message);
        }
    }

    /// <summary>All (non-missing) components of a type under a schematic, including inactive objects.</summary>
    public static T[] Collect<T>(Schematic schematic) where T : Component
    {
        return schematic.transform.GetComponentsInChildren<T>(true)
            .Where(x => x != null)
            .ToArray();
    }
}
#endif
