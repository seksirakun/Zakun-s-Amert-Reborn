using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZAMERT
{

public class DummyDoor : MonoBehaviour
{
    public Animator Animator { get; private set; }

    public SerializableDoor SerializableDoor { get; private set; }

    public Door RealDoor { get; private set; } = null;

    public static Config Config => ZAMERTPlugin.Singleton?.Config;

    public void Start()
    {
        MEC.Timing.CallDelayed(1f, () =>
        {
            Animator = this.transform.GetChild(0).GetComponent<Animator>();
            if (RealDoor == null)
            {

                foreach (MapEditorObject mapEditorObject in UnityEngine.Object.FindObjectsOfType<MapEditorObject>())
                {
                    if (SerializableDoor == mapEditorObject.Base)
                    {
                        RealDoor = mapEditorObject.GetComponent<Door>();
                        break;
                    }
                }
                if (RealDoor == null)
                {
                    float distance = float.MaxValue;
                    foreach (Door door in Door.List)
                    {
                        if (distance > Vector3.Distance(door.Position, this.transform.position))
                        {
                            distance = Vector3.Distance(door.Position, transform.position);
                            RealDoor = door;
                        }
                    }
                    if (RealDoor == null)
                    {
                        ServerConsole.AddLog("Failed to find proper door!", ConsoleColor.Red);
                        Destroy(this.gameObject);
                    }
                }
            }
            this.transform.parent = RealDoor.Base.transform;
            this.transform.localEulerAngles = Vector3.zero;
            this.transform.localPosition = Vector3.zero;
            if (RealDoor.Base.Rooms.Length != 0)
            {
                ZAMERTPlugin.Singleton.DummyDoors.Remove(this);
                Destroy(this.gameObject);
                return;
            }
            Animator.Play("DoorClose");
        });
    }

    public void OnInteractDoor(bool trigger)
    {
        if (this.RealDoor == null || Animator == null)
        {
            return;
        }
        Animator.Play(trigger ? "DoorOpen" : "DoorClose");
    }

    public void Update()
    {
        if (RealDoor == null)
        {
            return;
        }
        if ((RealDoor as BreakableDoor).IsBroken)
        {
            ZAMERTPlugin.Singleton.DummyDoors.Remove(this);
            Destroy(this.gameObject, 0.5f);
        }
    }
}

public class DummyGate : MonoBehaviour, IDoorPermissionRequester
{
    public Animator Animator { get; private set; }

    public GateSerializable GateSerializable { get; private set; }

    public Pickup[] Pickups { get; set; }

    public float Cooldown { get; set; } = 0f;

    private bool isOpened = false;

    public DoorPermissionsPolicy PermissionsPolicy { get; set; }
    public string RequesterLogSignature { get; } = "DummyGate";

    public void Start()
    {
        PermissionsPolicy = new DoorPermissionsPolicy()
        {
            RequiredPermissions = GateSerializable != null ? GateSerializable.DoorPermissions : DoorPermissionFlags.None,
            RequireAll = GateSerializable != null && GateSerializable.RequireAllPermission,
            Bypass2176 = false,
        };

        Pickups = gameObject.GetComponentsInChildren<InventorySystem.Items.Pickups.ItemPickupBase>().Select(x => Pickup.Get(x)).ToArray();
        if (MapUtils.LoadedMaps.Count > 0)
        {
            List<GateSerializable> gates = new List<GateSerializable>();
            foreach (MapSchematic map in MapUtils.LoadedMaps.Values)
            {
                if (ZAMERTPlugin.Singleton.Config.Gates.TryGetValue(map.Name, out List<GateSerializable> mapGates))
                {
                    gates.AddRange(mapGates);
                }
            }
            if (gates.Count > ZAMERTPlugin.Singleton.DummyGates.Count)
            {
                GateSerializable = gates[ZAMERTPlugin.Singleton.DummyGates.Count];
                if (GateSerializable.IsOpened)
                {
                    MEC.Timing.CallDelayed(3f, () => { IsOpened = true; });
                }
            }
        }

        Animator = this.transform.GetChild(1).GetComponent<Animator>();
        ZAMERTPlugin.Singleton.DummyGates.Add(this);
        MEC.Timing.RunCoroutine(Enumerator());
    }

    private IEnumerator<float> Enumerator()
    {
        yield return MEC.Timing.WaitUntilTrue(() => Round.IsRoundStarted);
        MEC.Timing.CallDelayed(0.3f, Apply);
        yield break;
    }

    public void Apply()
    {

    }

    public void Update()
    {
        if (Cooldown >= 0)
        {
            Cooldown -= Time.deltaTime;
        }
    }

    public void OnSearchingPickup(PlayerSearchingPickupEventArgs ev)
    {
        if (Cooldown > 0)
        {
            return;
        }

        bool toggleOpen = false;
        if (Pickups.Contains(ev.Pickup))
        {
            ev.IsAllowed = false;
            if (GateSerializable != null)
            {
                if (!GateSerializable.IsLocked && CheckPermission(ev.Player, GateSerializable.DoorPermissions))
                {
                    toggleOpen = true;
                }
            }
            else
            {
                toggleOpen = true;
            }
        }
        if (toggleOpen)
        {
            IsOpened = !IsOpened;
        }
    }

    public bool CheckPermission(Player player, DoorPermissionFlags doorPermission)
    {
        if (doorPermission == DoorPermissionFlags.None)
        {
            return false;
        }
        if (player != null)
        {
            if (player.IsBypassEnabled)
            {
                return true;
            }
            if (player.IsSCP)
            {
                return doorPermission.HasFlag(DoorPermissionFlags.ScpOverride);
            }
            if (player.CurrentItem == null)
            {
                return false;
            }
            if (player.CurrentItem is KeycardItem keycard)
            {
                DoorPermissionFlags keycardPermissions = keycard.Base.GetPermissions(this);
                if (GateSerializable != null && GateSerializable.RequireAllPermission)
                {
                    return (keycardPermissions & doorPermission) == doorPermission;
                }
                return (keycardPermissions & doorPermission) > DoorPermissionFlags.None;
            }
        }
        return false;
    }

    public bool IsOpened
    {
        get
        {
            return isOpened;
        }
        set
        {
            if (Animator != null)
            {
                Animator.Play(value ? "GateOpen" : "GateClose");
            }
            Cooldown = 3f;
            isOpened = value;
        }
    }
}
}
