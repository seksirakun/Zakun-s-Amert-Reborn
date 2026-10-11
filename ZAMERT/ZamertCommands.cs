using CommandSystem;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZAMERT
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ZamertCommand : ICommand
    {
        public string Command { get { return "zamert"; } }
        public string[] Aliases { get { return new[] { "zm" }; } }
        public string Description { get { return "ZAMERT status and audio cache."; } }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (ZAMERTPlugin.Singleton == null)
            {
                response = "ZAMERT is not loaded.";
                return false;
            }

            string sub = "status";
            if (arguments.Count > 0 && arguments.Array != null)
                sub = arguments.Array[arguments.Offset].ToLowerInvariant();

            if (sub == "audio")
            {
                int cleared = ZamertAudio.ForgetCachedClips();
                response = "Cleared " + cleared + " cached audio clips. The next playback loads them again.";
                return true;
            }

            if (sub == "prefabs")
            {
                string filter = arguments.Count > 1 && arguments.Array != null ? arguments.Array[arguments.Offset + 1] : string.Empty;
                response = DescribePrefabs(filter);
                return true;
            }

            response = BuildStatus();
            return true;
        }

        private static string BuildStatus()
        {
            ZAMERTPlugin plugin = ZAMERTPlugin.Singleton;
            StringBuilder builder = new StringBuilder();
            builder.Append("ZAMERT ").Append(plugin.Version);
            builder.Append(" | schematics ").Append(plugin.CodeClassPair != null ? plugin.CodeClassPair.Count : 0);
            builder.Append(" | round vars ").Append(plugin.RoundVariable != null ? plugin.RoundVariable.Count : 0);

            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (ZAMERTInteractable item in RegistryMaintenance.All())
            {
                string name = item.GetType().Name;
                int count;
                counts.TryGetValue(name, out count);
                counts[name] = count + 1;
            }

            if (counts.Count == 0)
            {
                builder.Append(" | no live objects");
                return builder.ToString();
            }

            builder.Append(" | ");
            builder.Append(string.Join(", ", counts.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value).ToArray()));
            return builder.ToString();
        }

        private static string DescribePrefabs(string filter)
        {
            ZamertPrefabRegistry.Refresh();
            List<string> names = new List<string>();
            foreach (string prefabName in ZamertPrefabRegistry.PrefabNames)
            {
                if (!string.IsNullOrEmpty(filter) && prefabName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                names.Add(prefabName);
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            string directory = Path.GetDirectoryName(typeof(ZAMERTPlugin).Assembly.Location) ?? string.Empty;
            string file = Path.Combine(directory, "ZAMERT-prefabs.txt");
            File.WriteAllLines(file, names.ToArray());

            int shown = Math.Min(12, names.Count);
            StringBuilder builder = new StringBuilder();
            builder.Append(names.Count).Append(" network prefabs. Full list: ").Append(file);
            if (shown > 0)
            {
                builder.Append(" | ");
                builder.Append(string.Join(", ", names.GetRange(0, shown).ToArray()));
            }

            return builder.ToString();
        }
    }
}
