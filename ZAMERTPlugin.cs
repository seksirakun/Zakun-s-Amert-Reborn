using CommandSystem;
using HarmonyLib;
using LabApi.Events.CustomHandlers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using ProjectMER.Features.Objects;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UserSettings.ServerSpecific;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public class ZAMERTPlugin : Plugin<Config>
    {
        public override string Name => "ZAMERT";
        public override string Description => "ZAMERT";
        public override string Author => "seksirakun48";
        public override Version Version => new Version(1, 0, 8);
        public override Version RequiredApiVersion => new Version(1, 1, 6);

        public static ZAMERTPlugin Singleton { get; private set; }
        public static Config Configs => Singleton.Config;

        private ZAMERTEventHandlers _eventsHandler;
        private Harmony _harmony;
        public override LoadPriority Priority => LoadPriority.Lowest;
        public static string AudioDir => Singleton.Config.AudioFolderPath;

        public List<HealthObject> HealthObjects { get; private set; }
        public List<InteractablePickup> InteractablePickups { get; private set; }
        public List<InteractableTeleporter> InteractableTeleporters { get; private set; }
        public List<CustomCollider> CustomColliders { get; private set; }
        public List<DummyDoor> DummyDoors { get; private set; }
        public List<DummyGate> DummyGates { get; private set; }
        public List<GroovyNoise> GroovyNoises { get; private set; }
        public List<CustomDoor> CustomDoors { get; private set; }
        public List<InteractableObject> InteractableObjects { get; private set; }

        public Dictionary<SchematicObject, Dictionary<string, List<ZAMERTInteractable>>> ZAMERTGroup { get; private set; }
        public Dictionary<SchematicObject, Dictionary<int, ZAMERTInteractable>> CodeClassPair { get; private set; }
        public Dictionary<Type, RandomExecutionModule> TypeSingletonPair { get; private set; }
        public Dictionary<int, List<InteractableObject>> IOkeys { get; private set; }
        public Dictionary<SchematicObject, Dictionary<string, FunctionExecutor>> FunctionExecutors { get; private set; }
        public Dictionary<SchematicObject, Dictionary<string, object>> SchematicVariables { get; private set; }
        public Dictionary<string, object> RoundVariable { get; private set; }

        private void InitCollections()
        {
            HealthObjects = new List<HealthObject>();
            InteractablePickups = new List<InteractablePickup>();
            InteractableTeleporters = new List<InteractableTeleporter>();
            CustomColliders = new List<CustomCollider>();
            DummyDoors = new List<DummyDoor>();
            DummyGates = new List<DummyGate>();
            GroovyNoises = new List<GroovyNoise>();
            CustomDoors = new List<CustomDoor>();
            InteractableObjects = new List<InteractableObject>();
            ZAMERTGroup = new Dictionary<SchematicObject, Dictionary<string, List<ZAMERTInteractable>>>();
            CodeClassPair = new Dictionary<SchematicObject, Dictionary<int, ZAMERTInteractable>>();
            TypeSingletonPair = new Dictionary<Type, RandomExecutionModule>();
            IOkeys = new Dictionary<int, List<InteractableObject>>();
            FunctionExecutors = new Dictionary<SchematicObject, Dictionary<string, FunctionExecutor>>();
            SchematicVariables = new Dictionary<SchematicObject, Dictionary<string, object>>();
            RoundVariable = new Dictionary<string, object>();
        }

        public override void Enable()
        {
            Singleton = this;
            InitCollections();

            _eventsHandler = new ZAMERTEventHandlers();
            _harmony = new Harmony("ZAMERT");

            CustomHandlersManager.RegisterEventsHandler(_eventsHandler);
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += _eventsHandler.OnSSInput;
            ProjectMER.Events.Handlers.Schematic.SchematicSpawned += _eventsHandler.OnSchematicSpawned;

            if (string.IsNullOrEmpty(Config.AudioFolderPath))
                Config.AudioFolderPath = Path.Combine(PathManager.LabApi.FullName, "audio");

            if (!Directory.Exists(Config.AudioFolderPath))
            {
                Log.Warn("ZAMERT audio directory does not exist: " + Config.AudioFolderPath);
                Log.Info("Creating ZAMERT audio directory");
                Directory.CreateDirectory(Config.AudioFolderPath);
            }

            _harmony.PatchAll();

            if (Config.Debug)
                ZMapper.Init(_harmony);

            VersionInfo.PrintInfo();
        }

        public override void Disable()
        {
            CustomHandlersManager.UnregisterEventsHandler(_eventsHandler);
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= _eventsHandler.OnSSInput;
            ProjectMER.Events.Handlers.Schematic.SchematicSpawned -= _eventsHandler.OnSchematicSpawned;

            _harmony?.UnpatchAll("ZAMERT");
            _harmony = null;
            _eventsHandler = null;

            InitCollections(); // reset
            Singleton = null;
        }

        public static void ExecuteCommand(string context)
        {
            if (string.IsNullOrWhiteSpace(context)) return;
            string[] array = context.Trim().Split(new char[] { ' ' }, 512, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length == 0) return;
            ICommand command1;
            if (CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(array[0], out command1))
            {
                Log.Debug("Executing command: $ " + command1.Command + " " + string.Join(" ", array.Segment(1).ToArray()));
                command1.Execute(array.Segment(1), ServerConsole.Scs, out _);
            }
        }
    }
}
