using LabApi.Loader.Features.Paths;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace ZAMERT
{
    public enum InteractableDetectionMode
    {
        RaycastOnly,
        RaycastWithAssist,
        ProximityOnly,
    }

    public class Config
    {
        [Description("Whether debug logs are written to the console.")]
        public bool Debug { get; set; } = false;

        [Description("The path to ZAMERT audio files. Defaults to '.../SCP Secret Laboratory/LabAPI/audio'.")]
        public string AudioFolderPath { get; set; } = Path.Combine(PathManager.LabApi.FullName, "audio");

        [Description("Server-wide multiplier applied to every ZAMERT sound. 0 = mute, 1 = normal, 2 = double. This is the server's master audio volume.")]
        public float AudioMasterVolume { get; set; } = 1f;

        [Description("Adds a per-client 'ZAMERT - Audio Volume' slider to Server-Specific Settings so each player can scale ZAMERT interaction sounds for themselves (one-shot sounds only).")]
        public bool EnablePerClientAudioVolume { get; set; } = true;

        [Description("When enabled, InteractableObjects will use InteractableToys for the specified keycodes.")]
        public bool EnableIoToys { get; set; } = false;

        [Description("IO schematics that use an InputKeyCode value from this list will be spawned as InteractableToys.")]
        public List<int> IoToysKeycodes { get; set; } = new List<int>() { 0, 101 };

        [Description("Set false to disable IO toys on root IO components.")]
        public bool IoToysNoRoot { get; set; } = false;

        [Description("If enabled, IOs using InteractableToys will spawn a visible primitive debug indicator.")]
        public bool IoToysDebug { get; set; } = false;

        [Description("When enabled, IOs that use InteractableToys will ALSO register an SSKeybind so both input methods work simultaneously. A 200ms dedup window prevents double-triggers.")]
        public bool IoToysBothModes { get; set; } = false;

        [Description("How keybind InteractableObjects are detected. RaycastOnly = direct aim required. RaycastWithAssist = direct aim, falling back to an aim-cone search (useful for non-collidable primitives). ProximityOnly = nearest in-range interactable fires regardless of aim.")]
        public InteractableDetectionMode InteractableObjectDetectionMode { get; set; } = InteractableDetectionMode.RaycastWithAssist;

        [Description("Aim assist sphere radius used by InteractableObject RaycastWithAssist mode.")]
        public float InteractableObjectAssistSphereRadius { get; set; } = 0.35f;

        [Description("Maximum aim angle used by InteractableObject RaycastWithAssist mode.")]
        public float InteractableObjectAssistAngleDegrees { get; set; } = 12f;

        [Description("Enables Lua event execution for ZAMERT objects.")]
        public bool LuaEnabled { get; set; } = true;

        [Description("Caches Lua file scripts and automatically reloads them when the file changes.")]
        public bool LuaCacheFileScripts { get; set; } = true;

        [Description("Allows Lua scripts to run server commands through api.command().")]
        public bool LuaAllowServerCommands { get; set; } = true;

        [Description("Allows Lua scripts to schedule delayed callbacks through api.delay() and api.repeat_every().")]
        public bool LuaAllowDelayedCallbacks { get; set; } = true;

        [Description("Maximum repeat count accepted by Lua api.repeat_every().")]
        public int LuaMaxRepeatCount { get; set; } = 100;

        [Description("When enabled, Lua API helper failures are logged instead of being silently ignored.")]
        public bool LuaLogApiErrors { get; set; } = true;

        public Dictionary<string, List<GateSerializable>> Gates { get; set; } = new Dictionary<string, List<GateSerializable>>
        {
            { "ExampleMapName", new List<GateSerializable> { new GateSerializable(), new GateSerializable() } },
        };

        [Description("If turned on, it will autowork with every MER's door spawning event.")]
        public bool AutoRun { get; set; } = false;

        [Description("When a GroovyNoise is enabled (its Active is switched on by another module), fire it once. Needed by schematics that rely on the old 'enable a GroovyNoise to run it' behaviour, such as multi-stage doors.")]
        public bool GroovyNoiseFireOnEnable { get; set; } = true;

        [Description("Disables ZAMERT SSS (Server-Specific Settings) keybind registration. Enable this if another plugin's SSS categories are being overwritten.")]
        public bool DisableSSS { get; set; } = false;

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
