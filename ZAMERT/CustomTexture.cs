using MEC;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ZAMERT
{
    public enum TextureMode
    {
        Replace,
        Fill
    }

    [Flags]
    public enum TextureFace
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Front = 16,
        Back = 32,
        All = Top | Bottom | Left | Right | Front | Back
    }

    [Serializable]
    public class CTDTO : ZAMERTDTO
    {
        public string SchematicName;
        public TextureMode Mode = TextureMode.Fill;
        public bool StretchX;
        public bool StretchY;
        public TextureFace Faces = TextureFace.All;
        public float ZFightOffset = 0.005f;
        public int MaxDuplicates = 256;
        public SVector3 ManualSize = new SVector3();
        public bool FlipNormal = false;
        public SVector3 PivotOffset = new SVector3();
    }

    public class CustomTexture : ZAMERTInteractable
    {
        public new CTDTO Base { get; set; }

        protected readonly List<GameObject> SpawnedInstances = new List<GameObject>();
        protected Vector3 SchematicSize = Vector3.zero;
        protected Vector3 SchematicPivotOffset = Vector3.zero;
        private bool _keepInstancesOnDestroy;

        protected virtual void Start()
        {
            Base = base.Base as CTDTO;
            if (Base == null || !Active) return;
            if (string.IsNullOrWhiteSpace(Base.SchematicName))
            {
                ZAMERTLogger.Warn("[CT] SchematicName empty on " + gameObject.name);
                return;
            }

            Timing.RunCoroutine(SpawnRoutine());
        }

        protected virtual IEnumerator<float> SpawnRoutine()
        {
            yield return Timing.WaitForOneFrame;

            SchematicObjectDataList data;
            if (!MapUtils.TryGetSchematicDataByName(Base.SchematicName, out data))
            {
                ZAMERTLogger.Warn("[CT] schematic '" + Base.SchematicName + "' not found in MapUtils. Available: " + string.Join(", ", MapUtils.GetAvailableSchematicNames()));
                yield break;
            }

            SchematicPivotOffset = Base.PivotOffset;

            Vector3 manual = Base.ManualSize;
            if (manual.sqrMagnitude > 0.0001f)
            {
                SchematicSize = manual;
                ZAMERTLogger.Info("[CT] using ManualSize=" + manual + " pivotOffset=" + SchematicPivotOffset);
            }
            else
            {
                Vector3 probePos = transform.position + Vector3.down * 10000f;
                SchematicObject probe = ObjectSpawner.SpawnSchematic(
                    Base.SchematicName,
                    probePos,
                    Quaternion.identity,
                    Vector3.one);

                if (probe == null)
                {
                    ZAMERTLogger.Warn("[CT] probe SpawnSchematic returned null for '" + Base.SchematicName + "'.");
                    yield break;
                }

                yield return Timing.WaitForOneFrame;
                yield return Timing.WaitForOneFrame;
                yield return Timing.WaitForOneFrame;

                Bounds bounds = ComputeBounds(probe.gameObject);
                SchematicSize = bounds.size;
                Vector3 detectedOffset = bounds.center - probePos;
                ZAMERTLogger.Info("[CT] auto-measured size=" + SchematicSize + " detectedPivotOffset=" + detectedOffset + " (use this in Tools PivotOffset if textures are misaligned)");
                probe.Destroy();

                if (SchematicSize.x <= 0f || SchematicSize.y <= 0f)
                {
                    ZAMERTLogger.Warn("[CT] schematic '" + Base.SchematicName + "' has zero X/Y bounds. Set ManualSize on Tools side.");
                    yield break;
                }
                if (SchematicSize.z <= 0f) SchematicSize.z = 0.01f;
            }

            ResolveModeAndSpawn();
            ZAMERTLogger.Info("[CT] '" + Base.SchematicName + "' mode=" + Base.Mode + " spawned=" + SpawnedInstances.Count);
        }

        protected virtual void ResolveModeAndSpawn()
        {
            switch (Base.Mode)
            {
                case TextureMode.Replace:
                    SpawnReplace();
                    _keepInstancesOnDestroy = true;
                    Destroy(gameObject);
                    break;
                case TextureMode.Fill:
                    SpawnFill();
                    break;
            }
        }

        protected void SpawnReplace()
        {
            Transform t = transform;
            Vector3 cubeSize = t.lossyScale;

            int countX, countY;
            float scaleX, scaleY;
            ComputeAxis(cubeSize.x, SchematicSize.x, ResolveStretchX(), out countX, out scaleX);
            ComputeAxis(cubeSize.y, SchematicSize.y, ResolveStretchY(), out countY, out scaleY);

            float scaleZ = cubeSize.z / SchematicSize.z;

            if (CheckBudget(countX * countY)) return;

            Vector3 right = t.right;
            Vector3 up = t.up;
            Vector3 origin = t.position;

            float cellW = cubeSize.x / countX;
            float cellH = cubeSize.y / countY;

            for (int ix = 0; ix < countX; ix++)
            {
                for (int iy = 0; iy < countY; iy++)
                {
                    float ox = (ix + 0.5f) * cellW - cubeSize.x * 0.5f;
                    float oy = (iy + 0.5f) * cellH - cubeSize.y * 0.5f;
                    Vector3 pos = origin + right * ox + up * oy;
                    Vector3 scl = new Vector3(scaleX, scaleY, scaleZ);
                    SpawnInstance(pos, t.rotation, scl);
                }
            }
        }

        protected void SpawnFill()
        {
            if (Base.Faces == TextureFace.None) return;

            Transform t = transform;
            Vector3 half = t.lossyScale * 0.5f;

            TryFace(TextureFace.Top, t.up, t.right, t.forward, half.y, half.x, half.z);
            TryFace(TextureFace.Bottom, -t.up, t.right, -t.forward, half.y, half.x, half.z);
            TryFace(TextureFace.Right, t.right, -t.forward, t.up, half.x, half.z, half.y);
            TryFace(TextureFace.Left, -t.right, t.forward, t.up, half.x, half.z, half.y);
            TryFace(TextureFace.Front, t.forward, t.right, t.up, half.z, half.x, half.y);
            TryFace(TextureFace.Back, -t.forward, -t.right, t.up, half.z, half.x, half.y);
        }

        protected void TryFace(TextureFace face, Vector3 normal, Vector3 right, Vector3 up, float depthHalf, float widthHalf, float heightHalf)
        {
            if ((Base.Faces & face) == 0) return;

            float width = widthHalf * 2f;
            float height = heightHalf * 2f;
            if (width <= 0f || height <= 0f) return;

            int countW, countH;
            float scaleW, scaleH;
            ComputeAxis(width, SchematicSize.x, ResolveStretchX(), out countW, out scaleW);
            ComputeAxis(height, SchematicSize.y, ResolveStretchY(), out countH, out scaleH);

            if (CheckBudget(countW * countH)) return;

            float cellW = width / countW;
            float cellH = height / countH;
            Vector3 outwardNormal = Base.FlipNormal ? -normal : normal;
            Vector3 faceCenter = transform.position + normal * (depthHalf + Base.ZFightOffset);
            Quaternion rot = Quaternion.LookRotation(outwardNormal, up);

            for (int iw = 0; iw < countW; iw++)
            {
                for (int ih = 0; ih < countH; ih++)
                {
                    float ow = (iw + 0.5f) * cellW - widthHalf;
                    float oh = (ih + 0.5f) * cellH - heightHalf;
                    Vector3 pos = faceCenter + right * ow + up * oh;
                    Vector3 scl = new Vector3(scaleW, scaleH, 1f);
                    SpawnInstance(pos, rot, scl);
                }
            }
        }

        protected void SpawnInstance(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            try
            {
                Quaternion finalRotation = rotation * Quaternion.AngleAxis(180f, Vector3.up);
                Vector3 offset = finalRotation * Vector3.Scale(SchematicPivotOffset, scale);
                Vector3 spawnPos = position - offset;
                SchematicObject so = ObjectSpawner.SpawnSchematic(Base.SchematicName, spawnPos, finalRotation, scale);
                if (so != null)
                {
                    Transform parent = OSchematic != null ? OSchematic.transform : transform.parent;
                    if (parent != null)
                        so.transform.SetParent(parent, true);
                    SpawnedInstances.Add(so.gameObject);
                }
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("[CT] instance spawn failed: " + ex);
            }
        }

        protected static void ComputeAxis(float surface, float schem, bool stretch, out int count, out float scale)
        {
            if (stretch)
            {
                count = 1;
                scale = surface / schem;
            }
            else
            {
                count = Mathf.Max(1, Mathf.RoundToInt(surface / schem));
                scale = surface / (count * schem);
            }
            if (scale <= 0f) scale = 1f;
        }

        protected bool CheckBudget(int requested)
        {
            if (requested <= Base.MaxDuplicates) return false;
            ZAMERTLogger.Warn("CustomTexture: requested " + requested + " instances exceeds MaxDuplicates (" + Base.MaxDuplicates + ") for '" + Base.SchematicName + "'. Skipping.");
            return true;
        }

        protected virtual bool ResolveStretchX() => Base.StretchX;
        protected virtual bool ResolveStretchY() => Base.StretchY;

        protected static Bounds ComputeBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                return new Bounds(Vector3.zero, b.size);
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds b = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++) b.Encapsulate(colliders[i].bounds);
                return new Bounds(Vector3.zero, b.size);
            }

            return new Bounds(Vector3.zero, Vector3.one);
        }

        protected override void OnDestroy()
        {
            if (!_keepInstancesOnDestroy)
            {
                foreach (GameObject go in SpawnedInstances)
                {
                    if (go == null) continue;
                    SchematicObject so = go.GetComponent<SchematicObject>();
                    if (so != null) so.Destroy();
                    else UnityObject.Destroy(go);
                }
            }
            SpawnedInstances.Clear();
            base.OnDestroy();
        }
    }

    [Serializable]
    public class FCTDTO : ZAMERTDTO
    {
        public string SchematicName { get; set; }
        public TextureMode Mode { get; set; } = TextureMode.Fill;
        public ScriptValue StretchX { get; set; }
        public ScriptValue StretchY { get; set; }
        public TextureFace Faces { get; set; } = TextureFace.All;
        public ScriptValue ZFightOffset { get; set; }
        public ScriptValue MaxDuplicates { get; set; }
        public ScriptValue ManualSize { get; set; }
        public ScriptValue FlipNormal { get; set; }
        public ScriptValue PivotOffset { get; set; }
    }

    public class FCustomTexture : CustomTexture
    {
        protected override void Start()
        {
            FCTDTO source = ((ZAMERTInteractable)this).Base as FCTDTO;
            if (source == null)
            {
                base.Start();
                return;
            }

            FunctionArgument args = new FunctionArgument(this);
            CTDTO resolved = new CTDTO
            {
                Active = source.Active,
                ObjectId = source.ObjectId,
                ObjectNamePath = source.ObjectNamePath,
                Code = source.Code,
                ScriptGroup = source.ScriptGroup,
                UseScriptValue = true,
                LuaEvents = source.LuaEvents,
                SchematicName = source.SchematicName,
                Mode = source.Mode,
                Faces = source.Faces,
                StretchX = source.StretchX != null && source.StretchX.GetValue(args, false),
                StretchY = source.StretchY != null && source.StretchY.GetValue(args, false),
                ZFightOffset = source.ZFightOffset != null ? source.ZFightOffset.GetValue(args, 0.005f) : 0.005f,
                MaxDuplicates = source.MaxDuplicates != null ? Mathf.RoundToInt(source.MaxDuplicates.GetValue(args, 256f)) : 256,
                FlipNormal = source.FlipNormal != null && source.FlipNormal.GetValue(args, false),
                ManualSize = ReadVector(source.ManualSize, args),
                PivotOffset = ReadVector(source.PivotOffset, args),
            };

            ((ZAMERTInteractable)this).Base = resolved;
            base.Start();
        }

        private static SVector3 ReadVector(ScriptValue value, FunctionArgument args)
        {
            if (value == null)
                return new SVector3();
            return value.GetValue(args, Vector3.zero);
        }
    }

}
