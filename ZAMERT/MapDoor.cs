using Interactables.Interobjects.DoorUtils;
using Mirror;
using System;
using UnityEngine;
using PMERDoorType = ProjectMER.Features.Enums.DoorType;

namespace ZAMERT
{
    [Serializable]
    public class MDDTO : ZAMERTDTO
    {
        public int DoorType = 5;
        public DoorPermissionFlags Permissions = DoorPermissionFlags.None;
        public bool IsOpen = false;
        public bool IsLocked = false;
        public bool RequireAllPermissions = false;
    }

    public class MapDoor : ZAMERTInteractable
    {
        public new MDDTO Base { get; set; }

        private GameObject _doorObject;

        protected virtual void Start()
        {
            Base = base.Base as MDDTO;
            if (Base == null || !Active) return;

            try
            {
                PMERDoorType doorType;
                if (!WorldSpawn.TryFromAuthoredId(Base.DoorType, out doorType))
                    ZAMERTLogger.Warn("[MapDoor] DoorType " + Base.DoorType + " on " + gameObject.name + " is outside 0-8. Spawning an Entrance door.");

                _doorObject = WorldSpawn.SpawnDoor(
                    doorType,
                    transform.position,
                    transform.rotation,
                    transform.lossyScale,
                    Base.Permissions,
                    Base.RequireAllPermissions,
                    Base.IsOpen,
                    Base.IsLocked);

                if (_doorObject == null)
                {
                    ZAMERTLogger.Warn("[MapDoor] door did not spawn on " + gameObject.name);
                    return;
                }

                WorldSpawn.Attach(OSchematic, _doorObject);
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("[MapDoor] spawn failed: " + ex);
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                if (_doorObject != null) NetworkServer.Destroy(_doorObject);
            }
            catch { }
            base.OnDestroy();
        }
    }
}
