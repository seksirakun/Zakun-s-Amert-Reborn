using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CustomDoor : FakeMono
{
    public new CDDTO data = new CDDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class CDDTO : DTO
{
    public override void OnValidate()
    {
        Animator = PublicFunctions.FindPath(AnimatorObject);
    }

    [JsonIgnore]
    public GameObject AnimatorObject;
    [HideInInspector]
    public string Animator;
    public DoorType DoorType;
    public string OpenAnimation = "DoorOpen";
    public string CloseAnimation = "DoorClose";
    public string LockAnimation = "DoorLock";
    public string UnlockAnimation = "DoorUnlock";
    public string BrokenAnimation = "DoorBreak";
    public Vector3 DoorInstallPos;
    public Vector3 DoorInstallRot;
    public Vector3 DoorInstallScl = Vector3.one;
    public float DoorHealth = 500f;
    public DoorPermissionFlags DoorPermissions = DoorPermissionFlags.None;
    public bool RequireAllPermissions;
    public bool IsLocked;
    public bool IsOpen;
    public DoorDamageType DoorDamageType = DoorDamageType.None;
}

public class CustomDoorCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Custom Doors", priority = -11)]
    public static void OnCompile()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            Compile(schematic);
        }
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
            Debug.LogError("Could Not find root object's compiled directory!");
            return;
        }

        Directory.CreateDirectory(schematicDirectoryPath);

        File.WriteAllText(
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-Doors.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<CustomDoor>().Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        Debug.Log("Successfully compiled Custom Doors.");
    }
}
