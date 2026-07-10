using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public static class ServerSettings
    {
        public static string IOLabelPreamble { get; } = "ZAMERT - Interactable Object";
        public static string HeaderLabel { get; } = "ZAMERT Keybinds";
        public static string AudioVolumeLabel { get; } = "ZAMERT - Audio Volume";

        private static bool _sendScheduled = false;
        private static int _audioVolumeSettingId = 0;
        private static bool _audioVolumeIdKnown = false;

        private static readonly Dictionary<ReferenceHub, float> _playerVolume = new Dictionary<ReferenceHub, float>();

        public static SSKeybindSetting CreateIOSettingForKeycode(KeyCode keycode)
            => new SSKeybindSetting(null, IOLabelPreamble + " - " + keycode, keycode, allowSpectatorTrigger: false);

        public static float GetPlayerVolume(ReferenceHub hub)
        {
            if (hub != null && _playerVolume.TryGetValue(hub, out float v))
                return v;
            return 1f;
        }

        public static void EnsureAudioVolumeSetting()
        {
            Config cfg = ZAMERTPlugin.Singleton?.Config;
            if (cfg == null || cfg.DisableSSS || !cfg.EnablePerClientAudioVolume)
                return;

            if (ServerSpecificSettingsSync.DefinedSettings == null)
                ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[0];

            List<ServerSpecificSettingBase> list = ServerSpecificSettingsSync.DefinedSettings.ToList();
            bool changed = false;

            if (list.FindIndex(x => x is SSGroupHeader && x.Label == HeaderLabel) == -1)
            {
                list.Add(new SSGroupHeader(HeaderLabel));
                changed = true;
            }

            int existingSlider = list.FindIndex(x => x is SSSliderSetting && x.Label == AudioVolumeLabel);
            if (existingSlider == -1)
            {
                SSSliderSetting slider = new SSSliderSetting(null, AudioVolumeLabel, 0f, 100f, 100f, true, "0", "{0}%",
                    "Adjusts the volume of custom (ZAMERT) audio for you only.");
                _audioVolumeSettingId = slider.SettingId;
                _audioVolumeIdKnown = true;
                list.Add(slider);
                changed = true;
            }
            else
            {
                _audioVolumeSettingId = list[existingSlider].SettingId;
                _audioVolumeIdKnown = true;
            }

            if (!changed)
                return;

            ServerSpecificSettingsSync.DefinedSettings = list.ToArray();
            ScheduleSendToAll();
        }

        public static void HandleAudioVolumeSlider(ReferenceHub sender, SSSliderSetting slider)
        {
            if (sender == null || slider == null)
                return;

            bool isOurs = _audioVolumeIdKnown && slider.SettingId == _audioVolumeSettingId;
            if (!isOurs)
            {
                SSSliderSetting orig = slider.OriginalDefinition as SSSliderSetting;
                isOurs = orig != null && orig.Label == AudioVolumeLabel;
            }
            if (!isOurs)
                return;

            _playerVolume[sender] = Mathf.Clamp01(slider.SyncFloatValue / 100f);
            Log.Debug("ZAMERT audio volume updated for " + sender.nicknameSync.MyNick + ": " + _playerVolume[sender].ToString("0.00"));
            LoopSpeaker.OnAnyPlayerVolumeChanged();
        }

        public static void CleanupSettings()
        {
            if (ServerSpecificSettingsSync.DefinedSettings == null)
                return;

            var cleaned = ServerSpecificSettingsSync.DefinedSettings
                .Where(x => !IsZAMERTSetting(x))
                .ToArray();

            if (cleaned.Length != ServerSpecificSettingsSync.DefinedSettings.Length)
            {
                Log.Debug("SSS cleanup: removed " + (ServerSpecificSettingsSync.DefinedSettings.Length - cleaned.Length) + " ZAMERT entries");
                ServerSpecificSettingsSync.DefinedSettings = cleaned;
            }

            _sendScheduled = false;
        }

        public static void RegisterKeybind(KeyCode keycode)
        {
            if (ZAMERTPlugin.Singleton.Config.DisableSSS)
                return;

            if (ServerSpecificSettingsSync.DefinedSettings == null)
                ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[0];

            List<ServerSpecificSettingBase> list = ServerSpecificSettingsSync.DefinedSettings.ToList();

            if (list.FindIndex(x => x is SSGroupHeader && x.Label == HeaderLabel) == -1)
            {
                Log.Debug("Adding ZAMERT keybind header");
                list.Add(new SSGroupHeader(HeaderLabel));
            }

            string label = IOLabelPreamble + " - " + keycode;
            if (list.FindIndex(x => x is SSKeybindSetting && x.Label == label) == -1)
            {
                Log.Debug("Adding ZAMERT keybind slot: " + keycode);
                list.Add(CreateIOSettingForKeycode(keycode));
            }

            ServerSpecificSettingsSync.DefinedSettings = list.ToArray();
            ScheduleSendToAll();

            EnsureAudioVolumeSetting();
        }

        public static void ScheduleSendToAll()
        {
            if (_sendScheduled)
                return;

            _sendScheduled = true;
            MEC.Timing.CallDelayed(1.5f, () =>
            {
                _sendScheduled = false;
                if (ZAMERTPlugin.Singleton == null || ZAMERTPlugin.Singleton.Config.DisableSSS)
                    return;

                Log.Debug("SSS: batched SendToAll");
                ServerSpecificSettingsSync.SendToAll();
            });
        }

        private static bool IsZAMERTSetting(ServerSpecificSettingBase setting)
        {
            if (setting is SSGroupHeader header && header.Label == HeaderLabel)
                return true;
            if (setting is SSKeybindSetting kb && kb.Label != null && kb.Label.StartsWith(IOLabelPreamble))
                return true;
            if (setting is SSSliderSetting slider && slider.Label == AudioVolumeLabel)
                return true;
            return false;
        }
    }
}
