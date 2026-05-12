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

        private static bool _sendScheduled = false;

        public static SSKeybindSetting CreateIOSettingForKeycode(KeyCode keycode)
            => new SSKeybindSetting(null, IOLabelPreamble + " - " + keycode, keycode, allowSpectatorTrigger: false);

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
            return false;
        }
    }
}
