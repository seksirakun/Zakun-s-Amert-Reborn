using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public static class ServerSettings
    {
        public static SSGroupHeader Header { get; } = new SSGroupHeader("ZAMERT Keybinds");
        public static string IOLabelPreamble { get; } = "ZAMERT - Interactable Object";

        public static readonly KeyCode[] DefaultKeybindSlots =
        {
            KeyCode.E,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H
        };

        public static SSKeybindSetting CreateIOSettingForKeycode(KeyCode keycode)
            => new SSKeybindSetting(null, IOLabelPreamble + " - " + keycode, keycode, allowSpectatorTrigger: false);

        public static void RegisterSettings()
        {
            if (ServerSpecificSettingsSync.DefinedSettings == null)
                ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[0];

            List<ServerSpecificSettingBase> list = ServerSpecificSettingsSync.DefinedSettings.ToList();

            if (list.FindIndex(x => x is SSGroupHeader && x.Label == Header.Label) == -1)
            {
                Log.Debug("Adding ZAMERT keybind header");
                list.Add(Header);
            }

            foreach (KeyCode key in DefaultKeybindSlots)
            {
                string label = IOLabelPreamble + " - " + key;

                if (list.FindIndex(x => x is SSKeybindSetting kb && kb.Label == label) == -1)
                {
                    Log.Debug("Adding ZAMERT keybind slot: " + key);
                    list.Add(CreateIOSettingForKeycode(key));
                }
            }

            ServerSpecificSettingsSync.DefinedSettings = list.ToArray();
            ServerSpecificSettingsSync.SendToAll();
        }
    }
}
