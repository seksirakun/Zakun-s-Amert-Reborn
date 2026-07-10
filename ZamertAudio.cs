using System;
using System.Collections.Generic;
using System.IO;
using LabApi.Features.Wrappers;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    internal static class ZamertAudio
    {
        private static int _counter;

        public static float MasterVolume
        {
            get
            {
                Config cfg = ZAMERTPlugin.Singleton != null ? ZAMERTPlugin.Singleton.Config : null;
                return cfg != null ? Mathf.Clamp(cfg.AudioMasterVolume, 0f, 5f) : 1f;
            }
        }

        public static float Scale(float baseVolume) => Mathf.Max(0f, baseVolume) * MasterVolume;

        public static bool EnsureLoaded(string clip)
        {
            if (string.IsNullOrEmpty(clip))
                return false;

            string folder = ZAMERTPlugin.Singleton != null && ZAMERTPlugin.Singleton.Config != null
                ? ZAMERTPlugin.Singleton.Config.AudioFolderPath
                : null;

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                ServerConsole.AddLog("Cannot find Audio Folder Directory!", ConsoleColor.Red);
                return false;
            }

            if (!AudioClipStorage.AudioClips.ContainsKey(clip))
            {
                try
                {
                    AudioClipStorage.LoadClip(Path.Combine(folder, clip), clip);
                }
                catch (Exception ex)
                {
                    Log.Warn("ZamertAudio: failed to load '" + clip + "': " + ex.Message);
                    return false;
                }
            }

            return AudioClipStorage.AudioClips.ContainsKey(clip);
        }

        public static void PlayOneShot(Vector3 worldPos, string clip, float baseVolume, int playCount, bool spatial, float minDistance, float maxDistance, Transform parent = null)
        {
            if (playCount <= 0 || !EnsureLoaded(clip))
                return;

            if (!TryPlayPerClient(worldPos, clip, baseVolume, playCount, spatial, minDistance, maxDistance, parent))
                PlayBucket(worldPos, clip, Scale(baseVolume), playCount, spatial, minDistance, maxDistance, null, parent);
        }

        private static void PlayBucket(Vector3 worldPos, string clip, float volume, int playCount, bool spatial, float minDistance, float maxDistance, List<ReferenceHub> owners, Transform parent)
        {
            AudioPlayer player = owners == null
                ? AudioPlayer.Create("ZAMERT-OneShot-" + (_counter++), destroyWhenAllClipsPlayed: true)
                : AudioPlayer.Create("ZAMERT-OneShot-" + (_counter++), destroyWhenAllClipsPlayed: true, sendSoundGlobally: false, owners: owners);

            if (player == null)
                return;

            Speaker speaker = player.AddSpeaker("Primary", worldPos, 1f, spatial, minDistance, maxDistance);
            if (parent != null)
            {
                player.transform.parent = speaker.transform.parent = parent;
                speaker.transform.position = worldPos;
            }
            for (int i = 0; i < playCount; i++)
                player.AddClip(clip, volume, false, true);
        }

        private static bool TryPlayPerClient(Vector3 worldPos, string clip, float baseVolume, int playCount, bool spatial, float minDistance, float maxDistance, Transform parent)
        {
            Config cfg = ZAMERTPlugin.Singleton != null ? ZAMERTPlugin.Singleton.Config : null;
            if (cfg == null || !cfg.EnablePerClientAudioVolume)
                return false;

            float master = MasterVolume;
            int totalReady = 0;
            Dictionary<int, List<ReferenceHub>> buckets = new Dictionary<int, List<ReferenceHub>>();

            foreach (Player p in Player.ReadyList)
            {
                if (p == null || !p.IsPlayer)
                    continue;
                ReferenceHub hub = p.ReferenceHub;
                if (hub == null)
                    continue;

                totalReady++;
                float mult = ServerSettings.GetPlayerVolume(hub) * master;
                if (mult <= 0.001f)
                    continue;

                int key = Mathf.Clamp(Mathf.RoundToInt(mult * 10f), 1, 50);
                if (!buckets.TryGetValue(key, out List<ReferenceHub> list))
                    buckets[key] = list = new List<ReferenceHub>();
                list.Add(hub);
            }

            if (buckets.Count == 0)
                return true;

            if (buckets.Count == 1)
            {
                foreach (KeyValuePair<int, List<ReferenceHub>> only in buckets)
                {
                    float vol = Mathf.Max(0f, baseVolume) * (only.Key / 10f);
                    if (only.Value.Count >= totalReady)
                        PlayBucket(worldPos, clip, vol, playCount, spatial, minDistance, maxDistance, null, parent);
                    else
                        PlayBucket(worldPos, clip, vol, playCount, spatial, minDistance, maxDistance, only.Value, parent);
                }
                return true;
            }

            foreach (KeyValuePair<int, List<ReferenceHub>> kv in buckets)
            {
                float vol = Mathf.Max(0f, baseVolume) * (kv.Key / 10f);
                PlayBucket(worldPos, clip, vol, playCount, spatial, minDistance, maxDistance, kv.Value, parent);
            }

            return true;
        }
    }
}
