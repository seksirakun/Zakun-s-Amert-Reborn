using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum TextureMode
{
    Replace,
    Fill
}

[Flags]
public enum TextureFace
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8,
    Front = 16,
    Back = 32,
    All = Top | Bottom | Left | Right | Front | Back
}

public class CustomTexture : FakeMono
{
    public new CTDTO data = new CTDTO();
    public new FCTDTO ScriptValueData = new FCTDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class CTDTO : DTO
{
    public override void OnValidate()
    {
        if (MaxDuplicates <= 0) MaxDuplicates = 256;
        if (ZFightOffset < 0f) ZFightOffset = 0.005f;
    }

    [Tooltip("Schematic adı (texture olarak kullanılacak).")]
    public string SchematicName;

    public TextureMode Mode = TextureMode.Fill;

    [Tooltip("X ekseninde stretch: açıksa boyutu uzatır, kapalıysa duplicate ile döşer.")]
    public bool StretchX;

    [Tooltip("Y ekseninde stretch: açıksa boyutu uzatır, kapalıysa duplicate ile döşer.")]
    public bool StretchY;

    [Header("Fill Mode")]
    [Tooltip("Hangi yüzler doldurulacak (Fill mode'da geçerli).")]
    public TextureFace Faces = TextureFace.All;

    [Header("Advanced")]
    [Tooltip("Yüzeyden ne kadar uzakta dursun (Z-fight engelleyici).")]
    public float ZFightOffset = 0.005f;

    [Tooltip("Aşılırsa duplicate üretilmez, log basılır.")]
    public int MaxDuplicates = 256;

    [Tooltip("Auto-measure başarısız olursa veya manuel kontrol istiyorsan: schematic'in dünya boyutu (X,Y,Z). 0 bırakılırsa runtime ölçer.")]
    public SVector3 ManualSize = new SVector3();

    [Tooltip("Schematic'in görünür yüzü içe doğru bakıyorsa açar (180° flip).")]
    public bool FlipNormal = false;

    [Tooltip("Schematic'in pivot'u merkezde değilse: spawn position - bounds.center offset'i. 0 bırakılırsa runtime probe'tan hesaplar.")]
    public SVector3 PivotOffset = new SVector3();
}

[Serializable]
public class FCTDTO : DTO
{
    public override void OnValidate()
    {
        StretchX.OnValidate();
        StretchY.OnValidate();
        ZFightOffset.OnValidate();
        MaxDuplicates.OnValidate();
    }

    public string SchematicName;
    public TextureMode Mode = TextureMode.Fill;
    public ScriptValue StretchX;
    public ScriptValue StretchY;
    public TextureFace Faces = TextureFace.All;
    public ScriptValue ZFightOffset;
    public ScriptValue MaxDuplicates;
}

public class CustomTextureCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Custom Textures", priority = -11)]
    public static void OnCompile()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
            Compile(schematic);
    }

    public static void Compile(Schematic schematic)
    {
        string parentDirectoryPath = Directory.Exists(Config.ExportPath)
            ? Config.ExportPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "MapEditorReborn_CompiledSchematics");
        string schematicDirectoryPath = Path.Combine(parentDirectoryPath, schematic.gameObject.name);

        if (!Directory.Exists(parentDirectoryPath))
        {
            Debug.LogError("Could not find root compiled directory.");
            return;
        }
        Directory.CreateDirectory(schematicDirectoryPath);

        File.WriteAllText(
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-CustomTextures.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<CustomTexture>().Where(x => !x.UseScriptValue).Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        File.WriteAllText(
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-FCustomTextures.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<CustomTexture>().Where(x => x.UseScriptValue).Select(x => x.ScriptValueData).ToList(),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto }).Replace("Assembly-CSharp", "AdvancedMERTools"));

        Debug.Log("Compiled Custom Textures for " + schematic.gameObject.name);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CustomTexture))]
public class CustomTextureEditor : Editor
{
    private void OnSceneGUI()
    {
        CustomTexture tex = (CustomTexture)target;
        if (tex == null) return;

        TextureFace faces = tex.UseScriptValue ? tex.ScriptValueData.Faces : tex.data.Faces;
        TextureMode mode = tex.UseScriptValue ? tex.ScriptValueData.Mode : tex.data.Mode;
        if (mode != TextureMode.Fill || faces == TextureFace.None) return;

        Transform t = tex.transform;
        Vector3 center = t.position;
        Vector3 ex = t.right * t.lossyScale.x * 0.5f;
        Vector3 ey = t.up * t.lossyScale.y * 0.5f;
        Vector3 ez = t.forward * t.lossyScale.z * 0.5f;

        DrawFace(faces, TextureFace.Top, center + ey, ex, ez, new Color(1f, 0.55f, 0f, 0.25f));
        DrawFace(faces, TextureFace.Bottom, center - ey, ex, ez, new Color(1f, 0.55f, 0f, 0.25f));
        DrawFace(faces, TextureFace.Right, center + ex, ez, ey, new Color(1f, 0.55f, 0f, 0.25f));
        DrawFace(faces, TextureFace.Left, center - ex, ez, ey, new Color(1f, 0.55f, 0f, 0.25f));
        DrawFace(faces, TextureFace.Front, center + ez, ex, ey, new Color(1f, 0.55f, 0f, 0.25f));
        DrawFace(faces, TextureFace.Back, center - ez, ex, ey, new Color(1f, 0.55f, 0f, 0.25f));
    }

    private static void DrawFace(TextureFace mask, TextureFace face, Vector3 center, Vector3 u, Vector3 v, Color fill)
    {
        if ((mask & face) == 0) return;
        Vector3[] verts =
        {
            center - u - v,
            center + u - v,
            center + u + v,
            center - u + v,
        };
        Handles.DrawSolidRectangleWithOutline(verts, fill, new Color(1f, 0.45f, 0f, 1f));
    }
}
#endif
