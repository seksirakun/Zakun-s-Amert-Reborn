namespace ZAMERT
{
using System;
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
}
