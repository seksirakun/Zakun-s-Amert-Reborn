using Mirror;
using ProjectMER.Features.Serializable;
using System;
using UnityEngine;
using PMERCameraType = ProjectMER.Features.Enums.CameraType;

namespace ZAMERT
{
    [Serializable]
    public class SCDTO : ZAMERTDTO
    {
        public int CameraType = (int)PMERCameraType.Lcz;
        public string Label = "Custom Camera";
    }

    public class MapScp079Camera : ZAMERTInteractable
    {
        public new SCDTO Base { get; set; }

        private GameObject _cameraObject;

        protected virtual void Start()
        {
            Base = base.Base as SCDTO;
            if (Base == null || !Active) return;

            try
            {
                SerializableScp079Camera data = new SerializableScp079Camera
                {
                    CameraType = (PMERCameraType)Base.CameraType,
                    Label = Base.Label ?? "Custom Camera",
                };

                _cameraObject = data.SpawnOrUpdateObject(null, null);
                if (_cameraObject == null)
                {
                    ZAMERTLogger.Warn("[Scp079Camera] SpawnOrUpdateObject returned null on " + gameObject.name);
                    return;
                }

                NetworkServer.UnSpawn(_cameraObject);
                _cameraObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
                _cameraObject.transform.localScale = transform.lossyScale;
                NetworkServer.Spawn(_cameraObject);

                if (OSchematic != null)
                {
                    System.Reflection.FieldInfo blocks = typeof(ProjectMER.Features.Objects.SchematicObject)
                        .GetField("_attachedBlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (blocks != null && blocks.GetValue(OSchematic) is System.Collections.Generic.List<GameObject> list)
                        list.Add(_cameraObject);
                }
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("[Scp079Camera] spawn failed: " + ex);
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                if (_cameraObject != null) NetworkServer.Destroy(_cameraObject);
            }
            catch { }
            base.OnDestroy();
        }
    }
}
