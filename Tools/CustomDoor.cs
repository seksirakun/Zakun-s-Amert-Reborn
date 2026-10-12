namespace ZAMERT
{
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

#if UNITY_EDITOR
public class CustomDoorCompiler
{
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
        string dir = AmertCompile.GetSchematicDirectory(schematic);
        if (dir == null)
            return;

        CustomDoor[] doors = AmertCompile.Collect<CustomDoor>(schematic);
        foreach (CustomDoor door in doors)
            AmertCompile.RefreshMeta(door);

        AmertCompile.WritePlain(dir, schematic, "Doors", doors.Select(x => x.data).ToList());
        Debug.Log("Compiled Custom Doors (" + doors.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
}
