namespace ZAMERT
{
using System;
using UnityEngine;

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
}
