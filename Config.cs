using LabApi.Loader.Features.Paths;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace ZAMERT
{
    public class Config
    {
        [Description("Whether debug logs are written to the console.")]
        public bool Debug { get; set; } = false;

        [Description("The path to ZAMERT audio files. Defaults to '.../SCP Secret Laboratory/LabAPI/audio'.")]
        public string AudioFolderPath { get; set; } = Path.Combine(PathManager.LabApi.FullName, "audio");

        [Description("When enabled, InteractableObjects will use InteractableToys for the specified keycodes.")]
        public bool EnableIoToys { get; set; } = false;

        [Description("IO schematics that use an InputKeyCode value from this list will be spawned as InteractableToys.")]
        public List<int> IoToysKeycodes { get; set; } = new List<int>() { 0, 101 };

        [Description("Set false to disable IO toys on root IO components.")]
        public bool IoToysNoRoot { get; set; } = false;

        [Description("If enabled, IOs using InteractableToys will spawn a visible primitive debug indicator.")]
        public bool IoToysDebug { get; set; } = false;

        public Dictionary<string, List<GateSerializable>> Gates { get; set; } = new Dictionary<string, List<GateSerializable>>
        {
            { "ExampleMapName", new List<GateSerializable> { new GateSerializable(), new GateSerializable() } },
        };

        [Description("If turned on, it will autowork with every MER's door spawning event.")]
        public bool AutoRun { get; set; } = false;

        public bool CustomSpawnPointEnable { get; set; } = true;

        [Serializable]
        public enum EventList
        {
            Generated,
            Round,
            Decont,
            Warhead,
        }
    }
}
