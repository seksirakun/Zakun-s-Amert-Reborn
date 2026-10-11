using System;
using System.Collections.Generic;
using MEC;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using UnityEngine;
using LightSourceToy = LabApi.Features.Wrappers.LightSourceToy;

namespace ZAMERT
{
    [Serializable]
    public class RTDTO : ZAMERTDTO
    {
        public SVector3 AngularSpeed { get; set; } = new SVector3();
        public bool UseWorldSpace { get; set; }
    }

    public class SpinMotor : ZAMERTInteractable
    {
        public new RTDTO Base { get; set; }
        private Vector3 _speed;

        protected void Start()
        {
            Base = base.Base as RTDTO;
            _speed = Base != null ? (Vector3)Base.AngularSpeed : Vector3.zero;
            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        protected void Update()
        {
            if (!Active || _speed.sqrMagnitude <= 0.0001f)
                return;

            transform.Rotate(_speed * Time.deltaTime, Base != null && Base.UseWorldSpace ? UnityEngine.Space.World : UnityEngine.Space.Self);
        }

        public void SetSpeed(float x, float y, float z)
        {
            _speed = new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class MVDTO : ZAMERTDTO
    {
        public SVector3 LocalOffset { get; set; } = new SVector3();
        public float Duration { get; set; } = 2f;
        public float StartDelay { get; set; }
        public bool PingPong { get; set; } = true;
        public bool Loop { get; set; } = true;
        public bool AutoStart { get; set; } = true;
    }

    public class MoveMotor : ZAMERTInteractable
    {
        public new MVDTO Base { get; set; }
        private Vector3 _origin;
        private float _elapsed;
        private bool _running;
        private bool _delayDone;

        protected void Start()
        {
            Base = base.Base as MVDTO;
            _origin = transform.localPosition;
            _running = Base == null || Base.AutoStart;
            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        protected void Update()
        {
            if (!_running || !Active || Base == null)
                return;

            if (!_delayDone)
            {
                _elapsed += Time.deltaTime;
                if (_elapsed < Mathf.Max(0f, Base.StartDelay))
                    return;
                _elapsed = 0f;
                _delayDone = true;
            }

            float duration = Mathf.Max(0.05f, Base.Duration);
            _elapsed += Time.deltaTime;
            float u;
            if (Base.PingPong)
            {
                float cycle = Mathf.Repeat(_elapsed / duration, 2f);
                u = cycle <= 1f ? cycle : 2f - cycle;
            }
            else
            {
                u = Base.Loop ? Mathf.Repeat(_elapsed / duration, 1f) : Mathf.Clamp01(_elapsed / duration);
                if (!Base.Loop && _elapsed >= duration)
                    _running = false;
            }

            transform.localPosition = _origin + (Vector3)Base.LocalOffset * u;
        }

        public void Restart()
        {
            _elapsed = 0f;
            _delayDone = Base == null || Base.StartDelay <= 0f;
            _running = true;
            if (Base != null)
                transform.localPosition = _origin;
        }
    }

    [Serializable]
    public class LTDTO : ZAMERTDTO
    {
        public bool ApplyOnStart { get; set; } = true;
        public bool Enabled { get; set; } = true;
        public bool OverrideColor { get; set; }
        public SColor Color { get; set; } = new SColor { r = 1f, g = 1f, b = 1f, a = 1f };
        public bool OverrideIntensity { get; set; }
        public float Intensity { get; set; } = 1f;
        public bool OverrideRange { get; set; }
        public float Range { get; set; } = 10f;
        public bool Pulse { get; set; }
        public float PulseSpeed { get; set; } = 1f;
        public float PulseMin { get; set; }
        public float PulseMax { get; set; } = 1f;
    }

    public class LightController : ZAMERTInteractable
    {
        public new LTDTO Base { get; set; }
        private readonly List<LightSourceToy> _lights = new List<LightSourceToy>();
        private readonly List<float> _baseIntensity = new List<float>();
        private bool _enabled = true;
        private float _pulseTime;

        protected void Start()
        {
            Base = base.Base as LTDTO;
            foreach (AdminToys.LightSourceToy toy in GetComponentsInChildren<AdminToys.LightSourceToy>(true))
            {
                LightSourceToy wrapped = LightSourceToy.Get(toy);
                if (wrapped == null)
                    continue;
                _lights.Add(wrapped);
                _baseIntensity.Add(wrapped.Intensity);
            }

            if (Base != null && Base.ApplyOnStart)
                Apply(Base.Enabled, Base.OverrideIntensity ? Base.Intensity : -1f);

            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        protected void Update()
        {
            if (!Active || Base == null || !Base.Pulse || !_enabled || _lights.Count == 0)
                return;

            _pulseTime += Time.deltaTime * Mathf.Max(0.01f, Base.PulseSpeed);
            float wave = (Mathf.Sin(_pulseTime * Mathf.PI * 2f) + 1f) * 0.5f;
            float min = Base.PulseMin;
            float max = Mathf.Max(min, Base.PulseMax);
            float intensity = Mathf.Lerp(min, max, wave);
            for (int i = 0; i < _lights.Count; i++)
            {
                if (_lights[i] != null && !_lights[i].IsDestroyed)
                    _lights[i].Intensity = intensity;
            }
        }

        public void SetEnabled(bool enabled)
        {
            Apply(enabled, -1f);
        }

        public void SetIntensity(float intensity)
        {
            Apply(_enabled, intensity);
        }

        private void Apply(bool enabled, float intensityOverride)
        {
            _enabled = enabled;
            for (int i = 0; i < _lights.Count; i++)
            {
                LightSourceToy light = _lights[i];
                if (light == null || light.IsDestroyed)
                    continue;

                if (Base != null && Base.OverrideColor && Base.Color != null)
                    light.Color = Base.Color;
                if (Base != null && Base.OverrideRange)
                    light.Range = Base.Range;

                float intensity = intensityOverride >= 0f
                    ? intensityOverride
                    : (i < _baseIntensity.Count ? _baseIntensity[i] : light.Intensity);
                if (Base != null && Base.OverrideIntensity && intensityOverride < 0f)
                    intensity = Base.Intensity;
                light.Intensity = enabled ? intensity : 0f;
            }
        }
    }

    [Serializable]
    public class TMDTO : ZAMERTDTO
    {
        public float Delay { get; set; }
        public float Interval { get; set; } = 1f;
        public bool Repeat { get; set; }
        public int RepeatCount { get; set; }
        public bool AutoStart { get; set; } = true;
        public string FunctionName { get; set; }
        public int GroovyNoiseId { get; set; }
        public string GroovyNoiseGroup { get; set; }
    }

    public class TimerRelay : ZAMERTInteractable
    {
        public new TMDTO Base { get; set; }
        private CoroutineHandle _handle;

        protected void Start()
        {
            Base = base.Base as TMDTO;
            if (Base != null && Base.AutoStart && Active)
                StartTimer();
            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        protected override void OnDestroy()
        {
            StopTimer();
            base.OnDestroy();
        }

        public void StartTimer()
        {
            StopTimer();
            _handle = Timing.RunCoroutine(Run());
        }

        public void StopTimer()
        {
            if (_handle.IsRunning)
                Timing.KillCoroutines(_handle);
        }

        private IEnumerator<float> Run()
        {
            if (Base == null)
                yield break;

            if (Base.Delay > 0f)
                yield return Timing.WaitForSeconds(Base.Delay);

            int fired = 0;
            while (Active && this != null)
            {
                SignalBus.Fire(this, Base.FunctionName, Base.GroovyNoiseId, Base.GroovyNoiseGroup);
                fired++;
                if (!Base.Repeat)
                    yield break;
                if (Base.RepeatCount > 0 && fired >= Base.RepeatCount)
                    yield break;
                yield return Timing.WaitForSeconds(Mathf.Max(0.05f, Base.Interval));
            }
        }
    }

    [Serializable]
    public class CNTDTO : ZAMERTDTO
    {
        public int StartValue { get; set; }
        public int TargetValue { get; set; } = 1;
        public int Step { get; set; } = 1;
        public bool ResetOnReach { get; set; } = true;
        public string FunctionName { get; set; }
        public int GroovyNoiseId { get; set; }
        public string GroovyNoiseGroup { get; set; }
    }

    public class Counter : ZAMERTInteractable
    {
        public new CNTDTO Base { get; set; }
        public int Value { get; private set; }

        protected void Start()
        {
            Base = base.Base as CNTDTO;
            Value = Base != null ? Base.StartValue : 0;
            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        public bool Add(int amount)
        {
            if (!Active || Base == null)
                return false;

            Value += amount == 0 ? Base.Step : amount;
            bool reached = Base.TargetValue >= Base.StartValue
                ? Value >= Base.TargetValue
                : Value <= Base.TargetValue;
            if (!reached)
                return false;

            SignalBus.Fire(this, Base.FunctionName, Base.GroovyNoiseId, Base.GroovyNoiseGroup);
            if (Base.ResetOnReach)
                Value = Base.StartValue;
            return true;
        }
    }

    [Serializable]
    public class SADTO : ZAMERTDTO
    {
        public string SchematicName { get; set; }
        public bool SpawnOnStart { get; set; } = true;
        public SVector3 Scale { get; set; } = new SVector3 { x = 1f, y = 1f, z = 1f };
    }

    public class SchematicAnchor : ZAMERTInteractable
    {
        public new SADTO Base { get; set; }
        private static int _depth;

        protected void Start()
        {
            Base = base.Base as SADTO;
            if (Base != null && Base.SpawnOnStart && Active)
                SpawnNow();
            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        public bool SpawnNow()
        {
            if (Base == null || string.IsNullOrWhiteSpace(Base.SchematicName))
                return false;
            if (OSchematic != null && string.Equals(OSchematic.Name, Base.SchematicName, StringComparison.OrdinalIgnoreCase))
            {
                ZAMERTLogger.Warn("SchematicAnchor on '" + gameObject.name + "' skipped because it would spawn its own schematic.");
                return false;
            }

            Vector3 scale = Base.Scale;
            if (scale.sqrMagnitude <= 0.0001f)
                scale = Vector3.one;
            return SpawnNamed(Base.SchematicName, transform, scale);
        }

        public static bool SpawnNamed(string schematicName, Transform origin, Vector3 scale)
        {
            if (string.IsNullOrWhiteSpace(schematicName) || origin == null)
                return false;
            if (_depth >= 4)
            {
                ZAMERTLogger.Warn("Schematic spawn depth limit reached at '" + schematicName + "'.");
                return false;
            }

            SchematicObjectDataList data;
            if (!MapUtils.TryGetSchematicDataByName(schematicName, out data) || data == null)
            {
                ZAMERTLogger.Warn("Schematic '" + schematicName + "' was not found.");
                return false;
            }

            _depth++;
            try
            {
                SchematicObject spawned = ObjectSpawner.SpawnSchematic(schematicName, origin.position, origin.rotation, scale);
                return spawned != null;
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("Failed to spawn schematic '" + schematicName + "': " + ex.Message);
                return false;
            }
            finally
            {
                _depth--;
            }
        }
    }

    internal static class SignalBus
    {
        public static void Fire(ZAMERTInteractable source, string functionName, int groovyNoiseId, string groovyNoiseGroup)
        {
            if (source == null || source.OSchematic == null || ZAMERTPlugin.Singleton == null)
                return;

            if (!string.IsNullOrWhiteSpace(functionName)
                && ZAMERTPlugin.Singleton.FunctionExecutors.TryGetValue(source.OSchematic, out Dictionary<string, FunctionExecutor> functions)
                && functions.TryGetValue(functionName, out FunctionExecutor function)
                && function != null
                && function.Data != null)
            {
                function.Data.Execute(new FunctionArgument(source));
            }

            Dictionary<int, ZAMERTInteractable> codes = null;
            ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(source.OSchematic, out codes);
            if (codes != null && groovyNoiseId != 0 && codes.TryGetValue(groovyNoiseId, out ZAMERTInteractable byCode))
                FireGroovy(byCode, source);

            if (!string.IsNullOrEmpty(groovyNoiseGroup)
                && ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(source.OSchematic, out Dictionary<string, List<ZAMERTInteractable>> groups)
                && groups.TryGetValue(groovyNoiseGroup, out List<ZAMERTInteractable> group))
            {
                for (int i = 0; i < group.Count; i++)
                    FireGroovy(group[i], source);
            }
        }

        private static void FireGroovy(ZAMERTInteractable target, ZAMERTInteractable source)
        {
            if (target == null)
                return;

            FGroovyNoise scripted = target as FGroovyNoise;
            if (scripted != null)
            {
                scripted.FireF(new FunctionArgument(source));
                return;
            }

            GroovyNoise noise = target as GroovyNoise;
            if (noise != null)
            {
                noise.Fire();
                return;
            }

            target.Active = true;
        }
    }
}
