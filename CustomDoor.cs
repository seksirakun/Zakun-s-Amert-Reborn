using LabApi.Features.Wrappers;
using LabApi.Events.Handlers;
using LabApi.Events.Arguments.PlayerEvents;
using MapGeneration;
using ProjectMER.Features.Serializable;
using System.Linq;
using UnityEngine;

namespace ZAMERT
{

public class CustomDoor : ZAMERTInteractable
{
    public new CDDTO Base { get; set; }

    public Animator Animator { get; private set; }

    public Door Door { get; private set; }

    protected void Start()
    {
        Base = base.Base as CDDTO;
        if (!ZAMERTPlugin.Singleton.CustomDoors.Contains(this))
            ZAMERTPlugin.Singleton.CustomDoors.Add(this);

        Transform animatorTransform = ZAMERTEventHandlers.FindObjectWithPath(OSchematic.transform, Base.Animator);
        if (animatorTransform == null || !animatorTransform.TryGetComponent(out Animator animator))
        {
            ZAMERTLogger.Warn("CustomDoor animator was not found for " + gameObject.name + " in schematic " + OSchematic.Name + ".");
            return;
        }

        Animator = animator;

        SerializableDoor serializableDoor = new SerializableDoor()
        {
            DoorType = (ProjectMER.Features.Enums.DoorType)(int)Base.DoorType,
            RequiredPermissions = Base.DoorPermissions,
            RequireAll = Base.RequireAllPermissions,
            IsLocked = Base.IsLocked,
            IsOpen = Base.IsOpen,
            Room = Room.Get(FacilityZone.Surface).ToList().FirstOrDefault().Name.ToString(),
            Position = transform.TransformPoint(Base.DoorInstallPos),
            Rotation = (transform.rotation * Quaternion.Euler(Base.DoorInstallRot)).eulerAngles,
            Scale = Base.DoorInstallScl,
        };

        Room doorRoom = Room.GetRoomAtPosition(serializableDoor.Position);
        GameObject doorGameObject = serializableDoor.SpawnOrUpdateObject(doorRoom);
        Door = doorGameObject.GetComponent<Door>();
        Door.Transform.parent = transform;

        PlayerEvents.InteractingDoor += OnInteractingDoor;
        PlayerEvents.DamagingShootingTarget += OnDamagingDoor;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ZAMERTPlugin.Singleton?.CustomDoors?.Remove(this);
        PlayerEvents.InteractingDoor -= OnInteractingDoor;
        PlayerEvents.DamagingShootingTarget -= OnDamagingDoor;
    }

    public void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (!ev.IsAllowed || !ev.Door.Equals(Door))
        {
            return;
        }
        Animator.Play(Door.IsOpened ? Base.CloseAnimation : Base.OpenAnimation);
    }

    public void OnDamagingDoor(PlayerDamagingShootingTargetEventArgs ev)
    {
        if (!ev.IsAllowed || !ev.ShootingTarget.GameObject.Equals(Door.GameObject))
        {
            return;
        }
        if (ev.ShootingTarget.IsDestroyed)
        {
            Animator.Play(Base.BrokenAnimation);
        }
    }

    public void OnLockChange(ushort value)
    {
        if (value == 0)
        {
            Animator.Play(Base.UnlockAnimation);
        }
        else
        {
            Animator.Play(Base.LockAnimation);
        }
    }
}
}
