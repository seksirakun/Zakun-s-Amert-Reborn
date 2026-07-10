using System.Collections.Generic;
using System.IO;
using System.Linq;
using LabApi.Features.Wrappers;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public class LoopSpeaker : ZAMERTInteractable
    {
        public new LSDTO Base { get; set; }

        private struct BucketStream
        {
            public AudioPlayer Player;
            public AudioClipPlayback Clip;
        }

        private readonly Dictionary<int, BucketStream> _buckets = new Dictionary<int, BucketStream>();
        private AudioPlayer _globalPlayer;
        private AudioClipPlayback _globalClip;

        private string _currentClip;
        private float _baseVolume;
        private bool _playing;
        private readonly Dictionary<AudioZoneDef, Vector3> _frozenZoneCenters = new Dictionary<AudioZoneDef, Vector3>();

        private static readonly HashSet<LoopSpeaker> ActiveLoops = new HashSet<LoopSpeaker>();
        private static bool _reconcileRunning;

        protected void Start()
        {
            Base = base.Base as LSDTO;
            Log.Debug("Registering LoopSpeaker: " + gameObject.name + " (" + OSchematic.Name + ")");

            if (!ZAMERTPlugin.Singleton.LoopSpeakers.Contains(this))
                ZAMERTPlugin.Singleton.LoopSpeakers.Add(this);

            if (Base.AutoStart && !string.IsNullOrEmpty(Base.AudioName))
                MEC.Timing.CallDelayed(0.5f, () => { if (this != null) Play(); });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopAndDestroy();
            ZAMERTPlugin.Singleton?.LoopSpeakers?.Remove(this);
        }

        private bool EnsureClipLoaded(string audioName)
        {
            if (string.IsNullOrEmpty(audioName)) return false;
            if (!Directory.Exists(ZAMERTPlugin.Singleton.Config.AudioFolderPath))
            {
                Log.Warn("LoopSpeaker: audio folder not found: " + ZAMERTPlugin.Singleton.Config.AudioFolderPath);
                return false;
            }
            if (!AudioClipStorage.AudioClips.ContainsKey(audioName))
            {
                try
                {
                    AudioClipStorage.LoadClip(
                        System.IO.Path.Combine(ZAMERTPlugin.Singleton.Config.AudioFolderPath, audioName),
                        audioName);
                }
                catch (System.Exception ex)
                {
                    Log.Warn("LoopSpeaker: failed to load clip '" + audioName + "': " + ex.Message);
                    return false;
                }
            }

            if (!AudioClipStorage.AudioClips.ContainsKey(audioName))
            {
                Log.Warn("LoopSpeaker: clip was not found after load: " + audioName);
                return false;
            }

            return true;
        }

        private static bool PerClientEnabled()
        {
            Config cfg = ZAMERTPlugin.Singleton != null ? ZAMERTPlugin.Singleton.Config : null;
            return cfg != null && cfg.EnablePerClientAudioVolume;
        }

        private void DestroyPlayer(AudioPlayer player)
        {
            if (player != null)
            {
                try { player.Destroy(); } catch { }
            }
        }

        private void StopAndDestroy()
        {
            foreach (BucketStream stream in _buckets.Values)
                DestroyPlayer(stream.Player);
            _buckets.Clear();

            DestroyPlayer(_globalPlayer);
            _globalPlayer = null;
            _globalClip = null;

            _playing = false;
            ActiveLoops.Remove(this);
        }

        public void Play(string audioName = null)
        {
            string clip = audioName ?? Base.AudioName;
            if (string.IsNullOrEmpty(clip)) return;

            StopAndDestroy();
            if (!EnsureClipLoaded(clip))
                return;

            _currentClip = clip;
            _baseVolume = Mathf.Max(0f, Base.Volume);
            _playing = true;
            ActiveLoops.Add(this);

            FreezeZoneCenters();

            if (PerClientEnabled())
            {
                SyncStreams();
                EnsureReconcileRunning();
            }
            else
            {
                CreateGlobalStream();
            }

            Log.Debug("LoopSpeaker: playing '" + clip + "' on " + gameObject.name);
        }

        public void Stop()
        {
            StopAndDestroy();
            Log.Debug("LoopSpeaker: stopped on " + gameObject.name);
        }

        public void ChangeClip(string audioName)
        {
            if (string.IsNullOrEmpty(audioName)) return;
            Play(audioName);
        }

        public void SetVolume(float volume)
        {
            _baseVolume = Mathf.Max(0f, volume);
            if (!_playing) return;

            if (PerClientEnabled())
            {
                List<int> levels = _buckets.Keys.ToList();
                foreach (int level in levels)
                {
                    BucketStream stream = _buckets[level];
                    if (stream.Clip != null)
                        stream.Clip.Volume = StreamVolume(level);
                }
            }
            else if (_globalClip != null)
            {
                _globalClip.Volume = ZamertAudio.Scale(_baseVolume);
            }
        }

        private float StreamVolume(int level) => _baseVolume * (level / 10f);

        private static int QuantizeLevel(ReferenceHub hub)
        {
            float mult = ServerSettings.GetPlayerVolume(hub) * ZamertAudio.MasterVolume;
            return Mathf.Clamp(Mathf.RoundToInt(mult * 10f), 0, 50);
        }

        private Vector3 WorldPosition() => transform.TransformPoint((Vector3)Base.LocalPosition);

        private AudioPlayer SpawnPlayer(string name)
        {
            AudioPlayer player = AudioPlayer.Create(name, sendSoundGlobally: false);
            if (player == null)
                return null;

            Vector3 localPosition = Base.LocalPosition;
            Vector3 worldPos = transform.TransformPoint(localPosition);
            Speaker speaker = player.AddSpeaker("Primary", worldPos, 1f, Base.IsSpatial, Base.MinDistance, Base.MaxDistance);
            player.transform.parent = speaker.transform.parent = transform;
            player.transform.localPosition = speaker.transform.localPosition = localPosition;
            return player;
        }

        private void CreateGlobalStream()
        {
            try
            {
                _globalPlayer = SpawnPlayer("LoopSpeaker-" + gameObject.GetHashCode());
                if (_globalPlayer == null)
                    return;

                _globalPlayer.Condition = HasAnyZone() ? (System.Func<ReferenceHub, bool>)IsAudibleForHub : null;
                _globalClip = _globalPlayer.AddClip(_currentClip, ZamertAudio.Scale(_baseVolume), true, false);
            }
            catch (System.Exception ex)
            {
                Log.Warn("LoopSpeaker: failed to create global stream: " + ex.Message);
            }
        }

        private void SyncStreams()
        {
            if (!_playing || string.IsNullOrEmpty(_currentClip))
                return;

            HashSet<int> needed = new HashSet<int>();
            foreach (Player p in Player.ReadyList)
            {
                if (p == null || !p.IsPlayer)
                    continue;
                ReferenceHub hub = p.ReferenceHub;
                if (hub == null)
                    continue;
                int level = QuantizeLevel(hub);
                if (level > 0)
                    needed.Add(level);
            }

            List<int> stale = _buckets.Keys.Where(k => !needed.Contains(k)).ToList();
            foreach (int level in stale)
            {
                DestroyPlayer(_buckets[level].Player);
                _buckets.Remove(level);
            }

            foreach (int level in needed)
            {
                if (_buckets.ContainsKey(level))
                    continue;

                BucketStream stream = CreateBucket(level);
                if (stream.Player != null)
                    _buckets[level] = stream;
            }
        }

        private BucketStream CreateBucket(int level)
        {
            try
            {
                AudioPlayer player = SpawnPlayer("LoopSpeaker-" + gameObject.GetHashCode() + "-L" + level);
                if (player == null)
                    return default;

                int capturedLevel = level;
                player.Condition = hub => hub != null && QuantizeLevel(hub) == capturedLevel && IsAudibleForHub(hub);
                AudioClipPlayback clip = player.AddClip(_currentClip, StreamVolume(level), true, false);
                return new BucketStream { Player = player, Clip = clip };
            }
            catch (System.Exception ex)
            {
                Log.Warn("LoopSpeaker: failed to create bucket stream: " + ex.Message);
                return default;
            }
        }

        private bool HasAnyZone() => Base != null && Base.AudioZones != null && Base.AudioZones.Count > 0;

        private void FreezeZoneCenters()
        {
            _frozenZoneCenters.Clear();
            if (!HasAnyZone())
                return;

            foreach (AudioZoneDef zone in Base.AudioZones)
            {
                if (zone != null && !zone.Attached)
                    _frozenZoneCenters[zone] = transform.TransformPoint((Vector3)zone.Center);
            }
        }

        private bool IsAudibleForHub(ReferenceHub hub)
        {
            if (hub == null)
                return true;

            List<AudioZoneDef> zones = Base?.AudioZones;
            if (zones == null || zones.Count == 0)
                return true;

            Vector3 point = hub.PlayerCameraReference != null
                ? hub.PlayerCameraReference.position
                : hub.transform.position;

            bool hasOnlyInside = false;
            bool insideAnyOnlyInside = false;

            foreach (AudioZoneDef zone in zones)
            {
                if (zone == null)
                    continue;

                Vector3 center;
                Quaternion rotation;
                if (zone.Attached)
                {
                    center = transform.TransformPoint((Vector3)zone.Center);
                    rotation = transform.rotation;
                }
                else
                {
                    if (!_frozenZoneCenters.TryGetValue(zone, out center))
                        center = transform.TransformPoint((Vector3)zone.Center);
                    rotation = Quaternion.identity;
                }

                bool inside = zone.Contains(center, rotation, point);

                if (zone.Mode == AudioZoneMode.SilenceInside)
                {
                    if (inside)
                        return false;
                }
                else
                {
                    hasOnlyInside = true;
                    if (inside)
                        insideAnyOnlyInside = true;
                }
            }

            return !hasOnlyInside || insideAnyOnlyInside;
        }

        public static void OnAnyPlayerVolumeChanged()
        {
            if (ActiveLoops.Count == 0)
                return;

            foreach (LoopSpeaker loop in ActiveLoops.ToList())
            {
                if (loop == null)
                    continue;
                if (PerClientEnabled())
                    loop.SyncStreams();
            }
        }

        private static void EnsureReconcileRunning()
        {
            if (_reconcileRunning)
                return;

            _reconcileRunning = true;
            MEC.Timing.RunCoroutine(Reconcile());
        }

        private static IEnumerator<float> Reconcile()
        {
            while (ActiveLoops.Count > 0)
            {
                yield return MEC.Timing.WaitForSeconds(3f);

                if (!PerClientEnabled())
                    continue;

                foreach (LoopSpeaker loop in ActiveLoops.ToList())
                {
                    if (loop != null)
                        loop.SyncStreams();
                }
            }

            _reconcileRunning = false;
        }
    }
}
