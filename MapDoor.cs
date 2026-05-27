using Interactables.Interobjects.DoorUtils;
using Mirror;
using ProjectMER.Features.Serializable;
using System;
using UnityEngine;
using PMERDoorType = ProjectMER.Features.Enums.DoorType;

namespace ZAMERT
{
    [Serializable]
    public class MDDTO : ZAMERTDTO
    {
        public int DoorType = (int)PMERDoorType.EntranceDoor;
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
                SerializableDoor data = new SerializableDoor
                {
                    DoorType = (PMERDoorType)Base.DoorType,
                    RequiredPermissions = Base.Permissions,
                    IsOpen = Base.IsOpen,
                    IsLocked = Base.IsLocked,
                    RequireAll = Base.RequireAllPermissions,
                    Position = Vector3.zero,
                    Rotation = Vector3.zero,
                    Index = 1,
                };

                _doorObject = data.SpawnOrUpdateObject(null, null);
                if (_doorObject == null)
                {
                    ZAMERTLogger.Warn("[MapDoor] SpawnOrUpdateObject returned null on " + gameObject.name);
                    return;
                }

                NetworkServer.UnSpawn(_doorObject);
                _doorObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
                _doorObject.transform.localScale = transform.lossyScale;
                NetworkServer.Spawn(_doorObject);

                if (OSchematic != null)
                {
                    System.Reflection.FieldInfo blocks = typeof(ProjectMER.Features.Objects.SchematicObject)
                        .GetField("_attachedBlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (blocks != null && blocks.GetValue(OSchematic) is System.Collections.Generic.List<GameObject> list)
                        list.Add(_doorObject);
                }
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
