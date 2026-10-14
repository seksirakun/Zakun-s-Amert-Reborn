namespace ZAMERT
{
using System;
using UnityEngine;

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
}
