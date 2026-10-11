using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using LabApi.Features.Wrappers;
using MEC;
using MoonSharp.Interpreter;
using PlayerRoles;
using ProjectMER.Features.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using PMERDoorType = ProjectMER.Features.Enums.DoorType;

namespace ZAMERT
{
    public class LuaExecutionContext
    {
        public ZAMERTInteractable Interactable { get; set; }
        public string EventName { get; set; }
        public Player Player { get; set; }
        public Pickup Pickup { get; set; }
        public Collider Collider { get; set; }
        public string ToyId { get; set; }
        public float Damage { get; set; }
        public float PreviousHealth { get; set; }
        public float CurrentHealth { get; set; }
        public int PreviousCount { get; set; }
        public int CurrentCount { get; set; }
        public string Detail { get; set; }
        public bool Cancelled { get; set; }
        public bool RemovePickup { get; set; }
        public bool StopPropagation { get; set; }
        public object Result { get; set; }
        public string SourcePath { get; set; }
        public Dictionary<string, object> Values { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public LuaExecutionContext CloneFor(ZAMERTInteractable interactable, string eventName)
        {
            LuaExecutionContext clone = new LuaExecutionContext
            {
                Interactable = interactable,
                EventName = eventName,
                Player = Player,
                Pickup = Pickup,
                Collider = Collider,
                ToyId = ToyId,
                Damage = Damage,
                PreviousHealth = PreviousHealth,
                CurrentHealth = CurrentHealth,
                PreviousCount = PreviousCount,
                CurrentCount = CurrentCount,
                Detail = Detail,
                Cancelled = Cancelled,
                RemovePickup = RemovePickup,
                Result = Result,
            };

            foreach (KeyValuePair<string, object> value in Values)
                clone.Values[value.Key] = value.Value;

            return clone;
        }
    }

    internal static class LuaScriptService
    {
        private sealed class CachedScriptFile
        {
            public DateTime LastWriteUtc { get; set; }
            public string Text { get; set; }
        }

        private static readonly Dictionary<string, CachedScriptFile> ScriptCache = new Dictionary<string, CachedScriptFile>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, Dictionary<string, object>> ObjectVariables = new Dictionary<int, Dictionary<string, object>>();
        private static readonly System.Random Random = new System.Random();

        internal static Script CreateBoundScript(LuaExecutionContext context, string sourcePath)
        {
            Script script = new Script(CoreModules.Preset_SoftSandbox);
            script.Options.DebugPrint = msg => ZAMERTLogger.Info("[Lua] " + msg);
            Table contextTable = BuildContextTable(script, context);
            Table apiTable = BuildApiTable(script, context, sourcePath);
            script.Globals["ctx"] = contextTable;
            script.Globals["api"] = apiTable;
            script.Globals["player"] = contextTable.Get("player");
            script.Globals["object"] = contextTable.Get("object");
            script.Globals["schematic"] = contextTable.Get("schematic");
            script.Globals["values"] = contextTable.Get("values");
            script.Globals["event"] = context.EventName ?? string.Empty;
            return script;
        }

        internal static DynValue ToLua(Script script, object value)
        {
            return ToDynValue(script, value);
        }

        internal static string ReadModuleSource(ZAMERTInteractable owner, string inline, string relativePath, out string fullPath)
        {
            fullPath = null;
            if (!string.IsNullOrWhiteSpace(inline))
                return inline;

            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;

            fullPath = ResolveExternalPath(owner, relativePath, null);
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                ZAMERTLogger.Warn("Lua module file not found: " + relativePath);
                fullPath = null;
                return string.Empty;
            }

            return LoadScriptFile(fullPath);
        }

        internal static LuaExecutionContext ExecuteEvent(ZAMERTInteractable interactable, string eventName, LuaExecutionContext context = null)
        {
            if (interactable == null || interactable.Base == null || string.IsNullOrWhiteSpace(eventName))
                return context;

            Config config = GetConfig();
            if (config != null && !config.LuaEnabled)
                return context;

            if (context == null)
                context = new LuaExecutionContext();

            context.Interactable = context.Interactable ?? interactable;
            context.EventName = eventName.Trim().ToLowerInvariant();
            if (context.Player == null && context.Values.TryGetValue("player", out object cachedPlayer))
                context.Player = cachedPlayer as Player;

            List<LuaEventBinding> bindings = interactable.GetLuaBindings()
                .Where(x => x != null && x.Matches(context.EventName))
                .OrderBy(x => x.Priority)
                .ToList();

            if (bindings.Count == 0)
                return context;

            foreach (LuaEventBinding binding in bindings)
            {
                if (context.StopPropagation)
                    break;

                if (context.Cancelled && !binding.RunWhenCancelled)
                    continue;

                if (!CanExecute(binding))
                    continue;

                string resolvedPath;
                string scriptText = ResolveScriptText(interactable, binding, out resolvedPath);
                if (string.IsNullOrWhiteSpace(scriptText))
                    continue;

                try
                {
                    context.SourcePath = resolvedPath;
                    Script script = new Script(CoreModules.Preset_SoftSandbox);
                    script.Options.DebugPrint = msg => ZAMERTLogger.Info("[Lua] " + msg);

                    Table contextTable = BuildContextTable(script, context);
                    Table apiTable = BuildApiTable(script, context, resolvedPath);
                    script.Globals["ctx"] = contextTable;
                    script.Globals["api"] = apiTable;
                    script.Globals["player"] = contextTable.Get("player");
                    script.Globals["object"] = contextTable.Get("object");
                    script.Globals["schematic"] = contextTable.Get("schematic");
                    script.Globals["values"] = contextTable.Get("values");
                    script.Globals["event"] = context.EventName ?? string.Empty;

                    script.DoString(scriptText);

                    binding.LastExecutedAt = Time.time;
                    binding.WasExecuted = true;

                    if (binding.StopOnSuccess)
                        context.StopPropagation = true;
                }
                catch (Exception ex)
                {
                    ZAMERTLogger.Error("Lua event failed on " + interactable.gameObject.name + " [" + context.EventName + "]: " + ex.Message);
                }
            }

            return context;
        }

        internal static void ClearState(ZAMERTInteractable interactable)
        {
            if (interactable == null)
                return;

            ObjectVariables.Remove(interactable.GetInstanceID());
        }

        private static Config GetConfig()
        {
            return ZAMERTPlugin.Singleton != null ? ZAMERTPlugin.Singleton.Config : null;
        }

        private static bool CanExecute(LuaEventBinding binding)
        {
            if (binding == null || !binding.Enabled)
                return false;

            if (binding.RunOnce && binding.WasExecuted)
                return false;

            if (binding.Cooldown > 0f && Time.time - binding.LastExecutedAt < binding.Cooldown)
                return false;

            float chance = Mathf.Clamp01(binding.Chance);
            if (chance <= 0f)
                return false;

            if (chance < 1f)
            {
                lock (Random)
                {
                    if (Random.NextDouble() > chance)
                        return false;
                }
            }

            return true;
        }

        private static string ResolveScriptText(ZAMERTInteractable interactable, LuaEventBinding binding, out string resolvedPath)
        {
            resolvedPath = null;

            if (binding.SourceMode == LuaScriptSourceMode.Inline)
                return binding.InlineScript ?? string.Empty;

            string scriptPath = binding.ScriptPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(scriptPath))
                return string.Empty;

            string fullPath = ResolveExternalPath(interactable, scriptPath, null);
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                ZAMERTLogger.Warn("Lua script file not found: " + scriptPath);
                return string.Empty;
            }

            resolvedPath = fullPath;
            return LoadScriptFile(fullPath);
        }

        private static string ResolveExternalPath(ZAMERTInteractable interactable, string requestedPath, string currentDirectory)
        {
            if (string.IsNullOrWhiteSpace(requestedPath))
                return null;

            if (Path.IsPathRooted(requestedPath))
                return requestedPath;

            string pluginDirectory = Path.GetDirectoryName(typeof(ZAMERTPlugin).Assembly.Location) ?? string.Empty;
            string schematicDirectory = interactable != null && interactable.OSchematic != null ? interactable.OSchematic.DirectoryPath : null;
            string[] candidates = new[]
            {
                !string.IsNullOrWhiteSpace(currentDirectory) ? Path.Combine(currentDirectory, requestedPath) : null,
                !string.IsNullOrWhiteSpace(schematicDirectory) ? Path.Combine(schematicDirectory, requestedPath) : null,
                Path.Combine(pluginDirectory, requestedPath),
            };

            return candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x)) ?? candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        private static string LoadScriptFile(string fullPath)
        {
            Config config = GetConfig();
            if (config == null || !config.LuaCacheFileScripts)
                return File.ReadAllText(fullPath);

            DateTime lastWrite = File.GetLastWriteTimeUtc(fullPath);
            if (ScriptCache.TryGetValue(fullPath, out CachedScriptFile cached) && cached.LastWriteUtc == lastWrite)
                return cached.Text;

            string text = File.ReadAllText(fullPath);
            ScriptCache[fullPath] = new CachedScriptFile { LastWriteUtc = lastWrite, Text = text };
            return text;
        }

        private static Table BuildContextTable(Script script, LuaExecutionContext context)
        {
            Table table = new Table(script);
            ZAMERTInteractable interactable = context.Interactable;

            table["event"] = context.EventName ?? string.Empty;
            table["detail"] = context.Detail ?? string.Empty;
            table["damage"] = context.Damage;
            table["previousHealth"] = context.PreviousHealth;
            table["currentHealth"] = context.CurrentHealth;
            table["previousCount"] = context.PreviousCount;
            table["currentCount"] = context.CurrentCount;
            table["toyId"] = context.ToyId ?? string.Empty;
            table["cancelled"] = context.Cancelled;
            table["removePickup"] = context.RemovePickup;
            table["stopPropagation"] = context.StopPropagation;
            table["sourcePath"] = context.SourcePath ?? string.Empty;
            table["time"] = Time.time;
            table["player"] = ToDynValue(script, context.Player);
            table["pickup"] = ToDynValue(script, context.Pickup);
            table["collider"] = ToDynValue(script, context.Collider);
            table["schematic"] = ToDynValue(script, interactable != null ? interactable.OSchematic : null);
            table["object"] = ToDynValue(script, interactable);
            table["values"] = ToDynValue(script, context.Values);
            table["self"] = ToDynValue(script, GetObjectVariables(interactable));
            table["result"] = ToDynValue(script, context.Result);
            return table;
        }

        private static Table BuildApiTable(Script script, LuaExecutionContext context, string sourcePath)
        {
            Table api = new Table(script);
            string currentDirectory = !string.IsNullOrWhiteSpace(sourcePath) ? Path.GetDirectoryName(sourcePath) : null;

            api["log"] = NewApiCallback("log", args =>
            {
                if (args.Count > 0)
                    ZAMERTLogger.Info("[Lua] " + ToString(args[0]));
                return DynValue.Nil;
            });

            api["warn"] = NewApiCallback("warn", args =>
            {
                if (args.Count > 0)
                    ZAMERTLogger.Warn("[Lua] " + ToString(args[0]));
                return DynValue.Nil;
            });

            api["error"] = NewApiCallback("error", args =>
            {
                if (args.Count > 0)
                    ZAMERTLogger.Error("[Lua] " + ToString(args[0]));
                return DynValue.Nil;
            });

            api["command"] = NewApiCallback("command", args =>
            {
                if (args.Count > 0 && (GetConfig() == null || GetConfig().LuaAllowServerCommands))
                    ZAMERTPlugin.ExecuteCommand(ToString(args[0]));
                return DynValue.Nil;
            });

            api["time"] = NewApiCallback("time", args => DynValue.NewNumber(Time.time));
            api["realtime"] = NewApiCallback("realtime", args => DynValue.NewNumber(Time.realtimeSinceStartup));
            api["chance"] = NewApiCallback("chance", args => DynValue.NewBoolean(RollChance(args.Count > 0 ? ToFloat(args[0]) : 0f)));
            api["random_int"] = NewApiCallback("random_int", args =>
            {
                int min = args.Count > 0 ? Mathf.RoundToInt(ToFloat(args[0])) : 0;
                int max = args.Count > 1 ? Mathf.RoundToInt(ToFloat(args[1])) : 100;
                if (max < min)
                {
                    int t = min;
                    min = max;
                    max = t;
                }
                lock (Random)
                    return DynValue.NewNumber(Random.Next(min, max + 1));
            });
            api["random_float"] = NewApiCallback("random_float", args =>
            {
                float min = args.Count > 0 ? ToFloat(args[0]) : 0f;
                float max = args.Count > 1 ? ToFloat(args[1]) : 1f;
                lock (Random)
                    return DynValue.NewNumber(min + (max - min) * Random.NextDouble());
            });

            api["vec"] = NewApiCallback("vec", args =>
            {
                Vector3 vector = args.Count == 1 ? ToVector(args[0]) : ToVector(args, 0);
                return DynValue.NewTable(BuildVectorTable(script, vector));
            });
            api["distance"] = NewApiCallback("distance", args =>
            {
                if (args.Count < 2)
                    return DynValue.NewNumber(0);
                return DynValue.NewNumber(Vector3.Distance(ToVector(args[0]), ToVector(args[1])));
            });
            api["add_vec"] = NewApiCallback("add_vec", args =>
            {
                Vector3 result = (args.Count > 0 ? ToVector(args[0]) : Vector3.zero) + (args.Count > 1 ? ToVector(args[1]) : Vector3.zero);
                return DynValue.NewTable(BuildVectorTable(script, result));
            });

            api["broadcast"] = NewApiCallback("broadcast", args =>
            {
                string text = args.Count > 0 ? ToString(args[0]) : string.Empty;
                ushort duration = args.Count > 1 ? (ushort)Mathf.RoundToInt(ToFloat(args[1])) : (ushort)5;
                foreach (Player player in Player.List)
                    player.SendBroadcast(text, duration);
                return DynValue.Nil;
            });

            api["hint"] = NewApiCallback("hint", args =>
            {
                if (context.Player == null)
                    return DynValue.NewBoolean(false);

                string text = args.Count > 0 ? ToString(args[0]) : string.Empty;
                float duration = args.Count > 1 ? ToFloat(args[1]) : 5f;
                context.Player.SendHint(text, duration);
                return DynValue.NewBoolean(true);
            });

            api["hint_player"] = NewApiCallback("hint_player", args =>
            {
                if (args.Count < 2)
                    return DynValue.NewBoolean(false);

                Player player = FindPlayer(args[0]);
                if (player == null)
                    return DynValue.NewBoolean(false);

                player.SendHint(ToString(args[1]), args.Count > 2 ? ToFloat(args[2]) : 5f);
                return DynValue.NewBoolean(true);
            });

            api["players"] = NewApiCallback("players", args => ToDynValue(script, Player.List.ToArray()));
            api["alive_players"] = NewApiCallback("alive_players", args => ToDynValue(script, Player.List.Where(x => x.IsAlive).ToArray()));
            api["humans"] = NewApiCallback("humans", args => ToDynValue(script, Player.List.Where(x => x.IsHuman).ToArray()));
            api["scps"] = NewApiCallback("scps", args => ToDynValue(script, Player.List.Where(x => x.IsSCP).ToArray()));
            api["player_count"] = NewApiCallback("player_count", args =>
            {
                string filter = args.Count > 0 ? ToString(args[0]).Trim() : string.Empty;
                IEnumerable<Player> players = Player.List;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    players = players.Where(x =>
                        string.Equals(x.Role.ToString(), filter, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(x.Team.ToString(), filter, StringComparison.OrdinalIgnoreCase));
                }
                return DynValue.NewNumber(players.Count());
            });
            api["find_player"] = NewApiCallback("find_player", args => ToDynValue(script, args.Count > 0 ? FindPlayer(args[0]) : context.Player));
            api["current_player"] = NewApiCallback("current_player", args => ToDynValue(script, context.Player));

            api["heal_player"] = NewApiCallback("heal_player", args =>
            {
                Player player = args.Count > 1 ? FindPlayer(args[0]) : context.Player;
                float amount = args.Count > 1 ? ToFloat(args[1]) : args.Count > 0 ? ToFloat(args[0]) : 0f;
                if (player == null || amount <= 0f)
                    return DynValue.NewBoolean(false);
                player.Heal(amount);
                return DynValue.NewBoolean(true);
            });

            api["damage_player"] = NewApiCallback("damage_player", args =>
            {
                Player player = args.Count > 1 ? FindPlayer(args[0]) : context.Player;
                float amount = args.Count > 1 ? ToFloat(args[1]) : args.Count > 0 ? ToFloat(args[0]) : 0f;
                string reason = args.Count > 2 ? ToString(args[2]) : "ZAMERT Lua";
                if (player == null || amount <= 0f)
                    return DynValue.NewBoolean(false);
                player.Damage(amount, reason);
                return DynValue.NewBoolean(true);
            });

            api["kill_player"] = NewApiCallback("kill_player", args =>
            {
                Player player = args.Count > 0 ? FindPlayer(args[0]) : context.Player;
                string reason = args.Count > 1 ? ToString(args[1]) : "ZAMERT Lua";
                if (player == null)
                    return DynValue.NewBoolean(false);
                player.Damage(Mathf.Max(player.Health + 500f, 9999f), reason);
                return DynValue.NewBoolean(true);
            });

            api["teleport_player"] = NewApiCallback("teleport_player", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewBoolean(false);

                Player player;
                Vector3 position;
                if (args.Count == 1)
                {
                    player = context.Player;
                    position = ToVector(args[0]);
                }
                else
                {
                    player = FindPlayer(args[0]);
                    position = args.Count >= 4 ? ToVector(args, 1) : ToVector(args[1]);
                }

                if (player == null)
                    return DynValue.NewBoolean(false);

                player.Position = position;
                return DynValue.NewBoolean(true);
            });

            api["set_active"] = NewApiCallback("set_active", args =>
            {
                ZAMERTInteractable target = args.Count > 1 ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) : context.Interactable;
                bool active = args.Count > 1 ? ToBool(args[1]) : args.Count > 0 ? ToBool(args[0]) : true;
                if (target == null)
                    return DynValue.NewBoolean(false);

                target.Active = active;
                if (target.Base != null)
                    target.Base.Active = active;
                return DynValue.NewBoolean(true);
            });

            api["get_active"] = NewApiCallback("get_active", args =>
            {
                ZAMERTInteractable target = args.Count > 0 ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) : context.Interactable;
                return DynValue.NewBoolean(target != null && target.Active);
            });

            api["set_active_group"] = NewApiCallback("set_active_group", args =>
            {
                if (args.Count < 2)
                    return DynValue.NewNumber(0);

                string group = ToString(args[0]);
                bool active = ToBool(args[1]);
                List<ZAMERTInteractable> targets = FindGroup(context.Interactable != null ? context.Interactable.OSchematic : null, group, args.Count > 2 ? ToString(args[2]) : null);
                foreach (ZAMERTInteractable target in targets)
                {
                    target.Active = active;
                    if (target.Base != null)
                        target.Base.Active = active;
                }
                return DynValue.NewNumber(targets.Count);
            });

            api["destroy_self"] = NewApiCallback("destroy_self", args =>
            {
                if (context.Interactable != null)
                    UnityEngine.Object.Destroy(context.Interactable.gameObject, args.Count > 0 ? ToFloat(args[0]) : 0.1f);
                return DynValue.Nil;
            });

            api["destroy_by_code"] = NewApiCallback("destroy_by_code", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewBoolean(false);
                ZAMERTInteractable target = FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, args.Count > 1 ? ToString(args[1]) : null);
                if (target == null)
                    return DynValue.NewBoolean(false);
                UnityEngine.Object.Destroy(target.gameObject, args.Count > 2 ? ToFloat(args[2]) : 0.1f);
                return DynValue.NewBoolean(true);
            });

            api["cancel"] = NewApiCallback("cancel", args =>
            {
                context.Cancelled = true;
                return DynValue.Nil;
            });
            api["uncancel"] = NewApiCallback("uncancel", args =>
            {
                context.Cancelled = false;
                return DynValue.Nil;
            });
            api["stop"] = NewApiCallback("stop", args =>
            {
                context.StopPropagation = true;
                return DynValue.Nil;
            });
            api["remove_pickup"] = NewApiCallback("remove_pickup", args =>
            {
                context.RemovePickup = true;
                return DynValue.Nil;
            });
            api["keep_pickup"] = NewApiCallback("keep_pickup", args =>
            {
                context.RemovePickup = false;
                return DynValue.Nil;
            });
            api["result"] = NewApiCallback("result", args =>
            {
                context.Result = args.Count > 0 ? ToObject(args[0]) : null;
                return DynValue.Nil;
            });

            api["get_value"] = NewApiCallback("get_value", args =>
            {
                if (args.Count == 0)
                    return DynValue.Nil;
                string key = ToString(args[0]);
                return context.Values.TryGetValue(key, out object value) ? ToDynValue(script, value) : DynValue.Nil;
            });
            api["set_value"] = NewApiCallback("set_value", args =>
            {
                if (args.Count >= 2)
                    context.Values[ToString(args[0])] = ToObject(args[1]);
                return DynValue.Nil;
            });

            api["get_self_var"] = NewApiCallback("get_self_var", args => GetScopedVar(script, GetObjectVariables(context.Interactable), args));
            api["set_self_var"] = NewApiCallback("set_self_var", args => SetScopedVar(GetObjectVariables(context.Interactable), args));
            api["inc_self_var"] = NewApiCallback("inc_self_var", args => IncrementScopedVar(script, GetObjectVariables(context.Interactable), args));
            api["get_round_var"] = NewApiCallback("get_round_var", args => GetScopedVar(script, ZAMERTPlugin.Singleton.RoundVariable, args));
            api["set_round_var"] = NewApiCallback("set_round_var", args => SetScopedVar(ZAMERTPlugin.Singleton.RoundVariable, args));
            api["inc_round_var"] = NewApiCallback("inc_round_var", args => IncrementScopedVar(script, ZAMERTPlugin.Singleton.RoundVariable, args));
            api["get_schematic_var"] = NewApiCallback("get_schematic_var", args =>
            {
                Dictionary<string, object> vars = GetSchematicVariables(context.Interactable != null ? context.Interactable.OSchematic : null);
                return vars == null ? DynValue.Nil : GetScopedVar(script, vars, args);
            });
            api["set_schematic_var"] = NewApiCallback("set_schematic_var", args =>
            {
                Dictionary<string, object> vars = GetSchematicVariables(context.Interactable != null ? context.Interactable.OSchematic : null);
                return vars == null ? DynValue.Nil : SetScopedVar(vars, args);
            });
            api["inc_schematic_var"] = NewApiCallback("inc_schematic_var", args =>
            {
                Dictionary<string, object> vars = GetSchematicVariables(context.Interactable != null ? context.Interactable.OSchematic : null);
                return vars == null ? DynValue.Nil : IncrementScopedVar(script, vars, args);
            });

            api["find_by_code"] = NewApiCallback("find_by_code", args =>
            {
                if (args.Count == 0)
                    return DynValue.Nil;
                return ToDynValue(script, FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, args.Count > 1 ? ToString(args[1]) : null));
            });
            api["find_group"] = NewApiCallback("find_group", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewTable(new Table(script));
                return ToDynValue(script, FindGroup(context.Interactable != null ? context.Interactable.OSchematic : null, ToString(args[0]), args.Count > 1 ? ToString(args[1]) : null));
            });
            api["count_group"] = NewApiCallback("count_group", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewNumber(0);
                return DynValue.NewNumber(FindGroup(context.Interactable != null ? context.Interactable.OSchematic : null, ToString(args[0]), args.Count > 1 ? ToString(args[1]) : null).Count);
            });

            api["trigger"] = NewApiCallback("trigger", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewBoolean(false);
                ZAMERTInteractable target = FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, args.Count > 2 ? ToString(args[2]) : null);
                string targetEvent = args.Count > 1 ? ToString(args[1]).Trim().ToLowerInvariant() : LuaEventType.Custom.ToString().ToLowerInvariant();
                if (target == null || string.IsNullOrWhiteSpace(targetEvent))
                    return DynValue.NewBoolean(false);
                ExecuteEvent(target, targetEvent, context.CloneFor(target, targetEvent));
                return DynValue.NewBoolean(true);
            });

            api["trigger_group"] = NewApiCallback("trigger_group", args =>
            {
                if (args.Count < 2)
                    return DynValue.NewNumber(0);
                List<ZAMERTInteractable> targets = FindGroup(context.Interactable != null ? context.Interactable.OSchematic : null, ToString(args[0]), args.Count > 2 ? ToString(args[2]) : null);
                string targetEvent = ToString(args[1]).Trim().ToLowerInvariant();
                foreach (ZAMERTInteractable target in targets)
                    ExecuteEvent(target, targetEvent, context.CloneFor(target, targetEvent));
                return DynValue.NewNumber(targets.Count);
            });

            api["run_interactable"] = NewApiCallback("run_interactable", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewBoolean(false);
                ZAMERTInteractable target = FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, args.Count > 1 ? ToString(args[1]) : null);
                return DynValue.NewBoolean(RunInteractableAction(target, context));
            });

            api["call_function"] = NewApiCallback("call_function", args =>
            {
                if (context.Interactable == null || context.Interactable.OSchematic == null || args.Count == 0)
                    return DynValue.NewBoolean(false);

                string functionName = ToString(args[0]);
                if (string.IsNullOrWhiteSpace(functionName))
                    return DynValue.NewBoolean(false);

                if (ZAMERTPlugin.Singleton.FunctionExecutors.TryGetValue(context.Interactable.OSchematic, out Dictionary<string, FunctionExecutor> functions)
                    && functions.TryGetValue(functionName, out FunctionExecutor executor))
                {
                    List<object> values = new List<object>();
                    for (int i = 1; i < args.Count; i++)
                        values.Add(ToObject(args[i]));

                    executor.Data.Execute(new FunctionArgument
                    {
                        Arguments = values,
                        Player = context.Player,
                        Schematic = context.Interactable.OSchematic,
                        Transform = context.Interactable.transform,
                    });
                    return DynValue.NewBoolean(true);
                }

                return DynValue.NewBoolean(false);
            });

            api["get_current_health"] = NewApiCallback("get_current_health", args =>
            {
                HealthObject healthObject = args.Count > 0 ? FindHealthObject(args[0], context.Interactable != null ? context.Interactable.OSchematic : null) : context.Interactable as HealthObject;
                return healthObject != null ? DynValue.NewNumber(healthObject.Health) : DynValue.Nil;
            });

            api["set_current_health"] = NewApiCallback("set_current_health", args =>
            {
                HealthObject healthObject = args.Count > 1 ? FindHealthObject(args[0], context.Interactable != null ? context.Interactable.OSchematic : null) : context.Interactable as HealthObject;
                float value = args.Count > 1 ? ToFloat(args[1]) : args.Count > 0 ? ToFloat(args[0]) : 0f;
                if (healthObject == null)
                    return DynValue.NewBoolean(false);
                healthObject.Health = value;
                healthObject.IsAlive = value > 0f;
                return DynValue.NewBoolean(true);
            });

            api["damage_healthobject"] = NewApiCallback("damage_healthobject", args =>
            {
                if (args.Count < 2)
                    return DynValue.NewBoolean(false);

                HealthObject target = FindHealthObject(args[0], context.Interactable != null ? context.Interactable.OSchematic : null);
                float damage = ToFloat(args[1]);
                bool result = ScpHealthObjectCombat.TryDamageHealthObject(target, context.Player, damage);
                return DynValue.NewBoolean(result);
            });

            api["heal_healthobject"] = NewApiCallback("heal_healthobject", args =>
            {
                HealthObject target = args.Count > 1 ? FindHealthObject(args[0], context.Interactable != null ? context.Interactable.OSchematic : null) : context.Interactable as HealthObject;
                float amount = args.Count > 1 ? ToFloat(args[1]) : args.Count > 0 ? ToFloat(args[0]) : 0f;
                if (target == null || amount <= 0f)
                    return DynValue.NewBoolean(false);
                target.Health += amount;
                target.IsAlive = true;
                return DynValue.NewBoolean(true);
            });

            api["nearest_healthobject"] = NewApiCallback("nearest_healthobject", args =>
            {
                Vector3 origin = context.Interactable != null ? context.Interactable.transform.position : Vector3.zero;
                float maxDistance = args.Count > 0 ? ToFloat(args[0]) : 5f;
                if (args.Count > 1)
                    origin = ToVector(args[1]);
                return ToDynValue(script, FindNearestHealthObject(origin, maxDistance, context.Interactable != null ? context.Interactable.OSchematic : null));
            });

            api["damage_nearest_healthobject"] = NewApiCallback("damage_nearest_healthobject", args =>
            {
                if (context.Interactable == null || args.Count < 2)
                    return DynValue.NewBoolean(false);

                float maxDistance = ToFloat(args[0]);
                float damage = ToFloat(args[1]);
                HealthObject nearest = FindNearestHealthObject(context.Interactable.transform.position, maxDistance, context.Interactable.OSchematic);
                bool result = ScpHealthObjectCombat.TryDamageHealthObject(nearest, context.Player, damage);
                return DynValue.NewBoolean(result);
            });

            api["speaker_play"] = NewApiCallback("speaker_play", args =>
            {
                LoopSpeaker speaker = args.Count > 0 ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, "speaker") as LoopSpeaker : context.Interactable as LoopSpeaker;
                if (speaker == null)
                    return DynValue.NewBoolean(false);
                speaker.Play(args.Count > 1 ? ToString(args[1]) : null);
                return DynValue.NewBoolean(true);
            });
            api["speaker_stop"] = NewApiCallback("speaker_stop", args =>
            {
                LoopSpeaker speaker = args.Count > 0 ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, "speaker") as LoopSpeaker : context.Interactable as LoopSpeaker;
                if (speaker == null)
                    return DynValue.NewBoolean(false);
                speaker.Stop();
                return DynValue.NewBoolean(true);
            });
            api["speaker_volume"] = NewApiCallback("speaker_volume", args =>
            {
                LoopSpeaker speaker = args.Count > 1 ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, "speaker") as LoopSpeaker : context.Interactable as LoopSpeaker;
                float volume = args.Count > 1 ? ToFloat(args[1]) : args.Count > 0 ? ToFloat(args[0]) : 1f;
                if (speaker == null)
                    return DynValue.NewBoolean(false);
                speaker.SetVolume(volume);
                return DynValue.NewBoolean(true);
            });

            api["spawner_spawn"] = NewApiCallback("spawner_spawn", args => ControlSpawner(args, context, "spawn"));
            api["spawner_stop"] = NewApiCallback("spawner_stop", args => ControlSpawner(args, context, "stop"));
            api["spawner_reset"] = NewApiCallback("spawner_reset", args => ControlSpawner(args, context, "reset"));
            api["damage_trigger_start"] = NewApiCallback("damage_trigger_start", args => ControlDamageTrigger(args, context, true));
            api["damage_trigger_stop"] = NewApiCallback("damage_trigger_stop", args => ControlDamageTrigger(args, context, false));
            api["counter_add"] = NewApiCallback("counter_add", args =>
            {
                if (args.Count < 2)
                    return DynValue.NewBoolean(false);
                Counter counter = FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) as Counter;
                if (counter == null)
                    return DynValue.NewBoolean(false);
                return DynValue.NewBoolean(counter.Add(Mathf.RoundToInt(ToFloat(args[1]))));
            });
            api["timer_start"] = NewApiCallback("timer_start", args =>
            {
                TimerRelay timer = args.Count > 0
                    ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) as TimerRelay
                    : context.Interactable as TimerRelay;
                if (timer == null)
                    return DynValue.NewBoolean(false);
                timer.StartTimer();
                return DynValue.NewBoolean(true);
            });
            api["timer_stop"] = NewApiCallback("timer_stop", args =>
            {
                TimerRelay timer = args.Count > 0
                    ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) as TimerRelay
                    : context.Interactable as TimerRelay;
                if (timer == null)
                    return DynValue.NewBoolean(false);
                timer.StopTimer();
                return DynValue.NewBoolean(true);
            });
            api["light_set"] = NewApiCallback("light_set", args =>
            {
                LightController light = args.Count > 0
                    ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) as LightController
                    : context.Interactable as LightController;
                if (light == null)
                    return DynValue.NewBoolean(false);
                if (args.Count > 1)
                    light.SetEnabled(ToFloat(args[1]) != 0f);
                if (args.Count > 2)
                    light.SetIntensity(ToFloat(args[2]));
                return DynValue.NewBoolean(true);
            });
            api["rotator_speed"] = NewApiCallback("rotator_speed", args =>
            {
                SpinMotor motor = args.Count > 0
                    ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) as SpinMotor
                    : context.Interactable as SpinMotor;
                if (motor == null || args.Count < 4)
                    return DynValue.NewBoolean(false);
                motor.SetSpeed(ToFloat(args[1]), ToFloat(args[2]), ToFloat(args[3]));
                return DynValue.NewBoolean(true);
            });
            api["mover_restart"] = NewApiCallback("mover_restart", args =>
            {
                MoveMotor mover = args.Count > 0
                    ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) as MoveMotor
                    : context.Interactable as MoveMotor;
                if (mover == null)
                    return DynValue.NewBoolean(false);
                mover.Restart();
                return DynValue.NewBoolean(true);
            });
            api["spawn_schematic"] = NewApiCallback("spawn_schematic", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewBoolean(false);
                SchematicAnchor anchor = FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, null) as SchematicAnchor;
                if (anchor != null)
                    return DynValue.NewBoolean(anchor.SpawnNow());
                if (context.Interactable == null)
                    return DynValue.NewBoolean(false);
                return DynValue.NewBoolean(SchematicAnchor.SpawnNamed(ToString(args[0]), context.Interactable.transform, Vector3.one));
            });
            api["wait"] = DynValue.NewCallback((ctx, args) =>
            {
                float seconds = args.Count > 0 ? Mathf.Max(0f, ToFloat(args[0])) : 0f;
                return DynValue.NewYieldReq(new DynValue[] { DynValue.NewNumber(seconds) });
            });
            api["spawn_door"] = NewApiCallback("spawn_door", args =>
            {
                if (context.Interactable == null)
                    return DynValue.NewBoolean(false);

                PMERDoorType doorType;
                if (!WorldSpawn.TryFromName(args.Count > 0 ? ToString(args[0]) : "ez", out doorType))
                    return DynValue.NewBoolean(false);

                Vector3 position = context.Interactable.transform.position;
                Quaternion rotation = context.Interactable.transform.rotation;
                if (args.Count >= 4)
                    position = new Vector3(ToFloat(args[1]), ToFloat(args[2]), ToFloat(args[3]));

                GameObject door = WorldSpawn.SpawnDoor(doorType, position, rotation, context.Interactable.transform.lossyScale, DoorPermissionFlags.None, false, false, false);
                WorldSpawn.Attach(context.Interactable.OSchematic, door);
                return DynValue.NewBoolean(door != null);
            });
            api["door_open"] = NewApiCallback("door_open", args =>
            {
                Door door = NearestContextDoor(context, args.Count > 1 ? ToFloat(args[1]) : 6f);
                if (door == null)
                    return DynValue.NewBoolean(false);
                door.IsOpened = args.Count == 0 || ToFloat(args[0]) != 0f;
                return DynValue.NewBoolean(true);
            });
            api["door_lock"] = NewApiCallback("door_lock", args =>
            {
                Door door = NearestContextDoor(context, args.Count > 1 ? ToFloat(args[1]) : 6f);
                if (door == null)
                    return DynValue.NewBoolean(false);
                door.IsLocked = args.Count == 0 || ToFloat(args[0]) != 0f;
                return DynValue.NewBoolean(true);
            });
            api["give_item"] = NewApiCallback("give_item", args =>
            {
                Player player = args.Count > 1 ? FindPlayer(args[0]) : context.Player;
                string itemName = args.Count > 1 ? ToString(args[1]) : args.Count > 0 ? ToString(args[0]) : string.Empty;
                ItemType itemType;
                if (player == null || !Enum.TryParse(itemName, true, out itemType))
                    return DynValue.NewBoolean(false);
                return DynValue.NewBoolean(player.AddItem(itemType, ItemAddReason.AdminCommand) != null);
            });
            api["set_role"] = NewApiCallback("set_role", args =>
            {
                Player player = args.Count > 1 ? FindPlayer(args[0]) : context.Player;
                string roleName = args.Count > 1 ? ToString(args[1]) : args.Count > 0 ? ToString(args[0]) : string.Empty;
                bool useSpawn = args.Count > 2 && ToFloat(args[2]) != 0f;
                RoleTypeId role;
                if (player == null || !Enum.TryParse(roleName, true, out role))
                    return DynValue.NewBoolean(false);
                player.SetRole(role, RoleChangeReason.RemoteAdmin, useSpawn ? RoleSpawnFlags.All : RoleSpawnFlags.None);
                return DynValue.NewBoolean(true);
            });
            api["prefab_count"] = NewApiCallback("prefab_count", args => DynValue.NewNumber(ZamertPrefabRegistry.PrefabNames.Count));
            api["list_prefabs"] = NewApiCallback("list_prefabs", args =>
            {
                string filter = args.Count > 0 ? ToString(args[0]) : string.Empty;
                List<string> names = new List<string>();
                foreach (string prefabName in ZamertPrefabRegistry.PrefabNames)
                {
                    if (!string.IsNullOrEmpty(filter) && prefabName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    names.Add(prefabName);
                    if (names.Count >= 40)
                        break;
                }
                return ToDynValue(script, names);
            });

            api["spawn_prefab"] = NewApiCallback("spawn_prefab", args =>
            {
                if (context.Interactable == null || args.Count == 0)
                    return DynValue.NewBoolean(false);

                string prefabName = ToString(args[0]);
                Vector3 position = context.Interactable.transform.position;
                Quaternion rotation = context.Interactable.transform.rotation;
                Vector3 scale = context.Interactable.transform.localScale;

                if (args.Count >= 2)
                    position = args.Count >= 4 ? ToVector(args, 1) : ToVector(args[1]);
                if (args.Count >= 5)
                    rotation = Quaternion.Euler(ToVector(args[4]));
                if (args.Count >= 6)
                    scale = ToVector(args[5]);

                GameObject instance = ZamertPrefabRegistry.SpawnPrefab(prefabName, position, rotation, scale);
                return ToDynValue(script, instance != null ? instance.name : null);
            });

            api["include"] = NewApiCallback("include", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewBoolean(false);
                string path = ResolveExternalPath(context.Interactable, ToString(args[0]), currentDirectory);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return DynValue.NewBoolean(false);
                script.DoString(LoadScriptFile(path));
                return DynValue.NewBoolean(true);
            });

            api["run_file"] = NewApiCallback("run_file", args =>
            {
                if (args.Count == 0)
                    return DynValue.NewBoolean(false);
                string path = ResolveExternalPath(context.Interactable, ToString(args[0]), currentDirectory);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return DynValue.NewBoolean(false);
                Script child = new Script(CoreModules.Preset_SoftSandbox);
                child.Options.DebugPrint = msg => ZAMERTLogger.Info("[Lua] " + msg);
                child.Globals["ctx"] = BuildContextTable(child, context);
                child.Globals["api"] = BuildApiTable(child, context, path);
                child.Globals["player"] = child.Globals.Get("ctx").Table.Get("player");
                child.Globals["object"] = child.Globals.Get("ctx").Table.Get("object");
                child.Globals["schematic"] = child.Globals.Get("ctx").Table.Get("schematic");
                child.DoString(LoadScriptFile(path));
                return DynValue.NewBoolean(true);
            });

            api["delay"] = NewApiCallback("delay", args =>
            {
                Config config = GetConfig();
                if (config != null && !config.LuaAllowDelayedCallbacks)
                    return DynValue.NewBoolean(false);
                if (args.Count < 2 || !IsCallable(args[1]))
                    return DynValue.NewBoolean(false);
                float seconds = Mathf.Max(0f, ToFloat(args[0]));
                DynValue callback = args[1];
                Timing.CallDelayed(seconds, () => SafeCall(script, callback, context));
                return DynValue.NewBoolean(true);
            });

            api["repeat_every"] = NewApiCallback("repeat_every", args =>
            {
                Config config = GetConfig();
                if (config != null && !config.LuaAllowDelayedCallbacks)
                    return DynValue.NewBoolean(false);
                if (args.Count < 3 || !IsCallable(args[2]))
                    return DynValue.NewBoolean(false);
                float interval = Mathf.Max(0.05f, ToFloat(args[0]));
                int count = Mathf.Max(1, Mathf.RoundToInt(ToFloat(args[1])));
                int max = config != null ? Mathf.Max(1, config.LuaMaxRepeatCount) : 100;
                count = Mathf.Min(count, max);
                Timing.RunCoroutine(RepeatLuaCallback(script, args[2], context, interval, count));
                return DynValue.NewBoolean(true);
            });

            return api;
        }

        private static DynValue NewApiCallback(string name, Func<CallbackArguments, DynValue> action)
        {
            return DynValue.NewCallback((ctx, args) =>
            {
                try
                {
                    return action(args) ?? DynValue.Nil;
                }
                catch (Exception ex)
                {
                    Config config = GetConfig();
                    if (config == null || config.LuaLogApiErrors)
                        ZAMERTLogger.Warn("Lua api." + name + " failed: " + ex.Message);
                    return DynValue.Nil;
                }
            });
        }

        private static IEnumerator<float> RepeatLuaCallback(Script script, DynValue callback, LuaExecutionContext context, float interval, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return Timing.WaitForSeconds(interval);
                SafeCall(script, callback, context, i + 1);
            }
        }

        private static void SafeCall(Script script, DynValue callback, LuaExecutionContext context, int tick = 0)
        {
            try
            {
                Table ctxTable = BuildContextTable(script, context);
                DynValue tickValue = tick > 0 ? DynValue.NewNumber(tick) : DynValue.Nil;
                script.Call(callback, DynValue.NewTable(ctxTable), tickValue);
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Warn("Lua delayed callback failed: " + ex.Message);
            }
        }

        private static bool IsCallable(DynValue value)
        {
            return value != null && (value.Type == DataType.Function || value.Type == DataType.ClrFunction);
        }

        private static bool RollChance(float chance)
        {
            chance = Mathf.Clamp01(chance);
            lock (Random)
                return Random.NextDouble() <= chance;
        }

        private static Dictionary<string, object> GetObjectVariables(ZAMERTInteractable interactable)
        {
            if (interactable == null)
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            int id = interactable.GetInstanceID();
            if (!ObjectVariables.TryGetValue(id, out Dictionary<string, object> vars))
            {
                vars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                ObjectVariables[id] = vars;
            }

            return vars;
        }

        private static Dictionary<string, object> GetSchematicVariables(SchematicObject schematic)
        {
            if (schematic == null || ZAMERTPlugin.Singleton == null)
                return null;

            if (!ZAMERTPlugin.Singleton.SchematicVariables.TryGetValue(schematic, out Dictionary<string, object> vars))
            {
                vars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                ZAMERTPlugin.Singleton.SchematicVariables[schematic] = vars;
            }

            return vars;
        }

        private static DynValue GetScopedVar(Script script, Dictionary<string, object> vars, CallbackArguments args)
        {
            if (vars == null || args.Count == 0)
                return DynValue.Nil;

            string key = ToString(args[0]);
            return vars.TryGetValue(key, out object value) ? ToDynValue(script, value) : DynValue.Nil;
        }

        private static DynValue SetScopedVar(Dictionary<string, object> vars, CallbackArguments args)
        {
            if (vars != null && args.Count >= 2)
                vars[ToString(args[0])] = ToObject(args[1]);
            return DynValue.Nil;
        }

        private static DynValue IncrementScopedVar(Script script, Dictionary<string, object> vars, CallbackArguments args)
        {
            if (vars == null || args.Count == 0)
                return DynValue.Nil;

            string key = ToString(args[0]);
            float add = args.Count > 1 ? ToFloat(args[1]) : 1f;
            float current = vars.TryGetValue(key, out object value) ? ConvertToFloat(value) : 0f;
            float result = current + add;
            vars[key] = result;
            return DynValue.NewNumber(result);
        }

        private static DynValue ControlSpawner(CallbackArguments args, LuaExecutionContext context, string action)
        {
            ItemSpawner spawner = args.Count > 0 ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, "spawner") as ItemSpawner : context.Interactable as ItemSpawner;
            if (spawner == null)
                return DynValue.NewBoolean(false);

            if (action == "spawn") spawner.SpawnItems();
            else if (action == "stop") spawner.Stop();
            else if (action == "reset") spawner.Reset();

            return DynValue.NewBoolean(true);
        }

        private static DynValue ControlDamageTrigger(CallbackArguments args, LuaExecutionContext context, bool start)
        {
            DamageTrigger trigger = args.Count > 0 ? FindInteractable(args[0], context.Interactable != null ? context.Interactable.OSchematic : null, "damagetrigger") as DamageTrigger : context.Interactable as DamageTrigger;
            if (trigger == null)
                return DynValue.NewBoolean(false);

            if (start) trigger.StartTicking();
            else trigger.StopTicking();
            return DynValue.NewBoolean(true);
        }

        private static bool RunInteractableAction(ZAMERTInteractable target, LuaExecutionContext context)
        {
            if (target == null || !target.Active)
                return false;

            if (target is InteractableObject interactableObject)
            {
                interactableObject.RunProcess(context.Player, "Lua");
                return true;
            }

            if (target is CustomInteractableToy customToy)
            {
                customToy.RunProcess(context.Player, "Lua");
                return true;
            }

            if (target is InteractablePickup interactablePickup && context.Pickup != null)
            {
                bool remove;
                interactablePickup.RunProcess(context.Player, context.Pickup, out remove, "lua");
                context.RemovePickup |= remove;
                return true;
            }

            if (target is CustomCollider customCollider && context.Collider != null)
            {
                customCollider.RunProcess(context.Collider, "lua");
                return true;
            }

            return false;
        }

        private static Door NearestContextDoor(LuaExecutionContext context, float range)
        {
            if (context == null || context.Interactable == null)
                return null;
            if (range <= 0f)
                range = 6f;
            return WorldSpawn.NearestDoor(context.Interactable.transform.position, range);
        }

        private static Player FindPlayer(DynValue identifier)
        {
            if (identifier == null || identifier.Type == DataType.Nil || identifier.Type == DataType.Void)
                return null;

            if (identifier.Type == DataType.Number)
                return Player.Get(Mathf.RoundToInt((float)identifier.Number));

            if (identifier.Type == DataType.Table)
            {
                DynValue tableId = identifier.Table.Get("id");
                if (tableId.Type == DataType.Number)
                {
                    Player byTable = Player.Get(Mathf.RoundToInt((float)tableId.Number));
                    if (byTable != null)
                        return byTable;
                }

                DynValue name = identifier.Table.Get("name");
                if (name.Type == DataType.String)
                    identifier = name;
            }

            string text = ToString(identifier);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            int id;
            if (int.TryParse(text, out id))
            {
                Player byId = Player.Get(id);
                if (byId != null)
                    return byId;
            }

            return Player.List.FirstOrDefault(x =>
                string.Equals(x.UserId, text, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Nickname, text, StringComparison.OrdinalIgnoreCase)
                || (x.Nickname != null && x.Nickname.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static ZAMERTInteractable FindInteractable(DynValue identifier, SchematicObject schematic, string typeFilter)
        {
            if (identifier == null || ZAMERTPlugin.Singleton == null)
                return null;

            int code;
            string objectId = ToString(identifier);
            bool isCode = identifier.Type == DataType.Number;
            if (isCode)
                code = Mathf.RoundToInt((float)identifier.Number);
            else
                isCode = int.TryParse(objectId, out code);

            if (isCode && schematic != null)
            {
                if (ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(schematic, out Dictionary<int, ZAMERTInteractable> codeDict)
                    && codeDict.TryGetValue(code, out ZAMERTInteractable interactable)
                    && IsTypeMatch(interactable, typeFilter))
                {
                    return interactable;
                }
            }

            return GetAllInteractables(schematic)
                .FirstOrDefault(x => x != null
                    && IsTypeMatch(x, typeFilter)
                    && (string.Equals(x.Base?.ObjectId, objectId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(x.gameObject != null ? x.gameObject.name : string.Empty, objectId, StringComparison.OrdinalIgnoreCase)));
        }

        private static List<ZAMERTInteractable> FindGroup(SchematicObject schematic, string group, string typeFilter)
        {
            if (ZAMERTPlugin.Singleton == null || schematic == null || string.IsNullOrWhiteSpace(group))
                return new List<ZAMERTInteractable>();

            if (!ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(schematic, out Dictionary<string, List<ZAMERTInteractable>> groupDict))
                return new List<ZAMERTInteractable>();

            if (!groupDict.TryGetValue(group, out List<ZAMERTInteractable> values))
                return new List<ZAMERTInteractable>();

            return values.Where(x => x != null && IsTypeMatch(x, typeFilter)).ToList();
        }

        private static IEnumerable<ZAMERTInteractable> GetAllInteractables(SchematicObject schematic)
        {
            if (ZAMERTPlugin.Singleton == null)
                return Enumerable.Empty<ZAMERTInteractable>();

            IEnumerable<ZAMERTInteractable> values = RegistryMaintenance.All();

            if (schematic != null)
                values = values.Where(x => x != null && x.OSchematic == schematic);

            return values;
        }

        private static bool IsTypeMatch(ZAMERTInteractable interactable, string typeFilter)
        {
            if (interactable == null)
                return false;

            if (string.IsNullOrWhiteSpace(typeFilter) || typeFilter == "*")
                return true;

            string filter = typeFilter.Replace("_", string.Empty).Replace("-", string.Empty).Trim().ToLowerInvariant();
            if (filter == "health" || filter == "healthobject")
                return interactable is HealthObject;
            if (filter == "object" || filter == "interactable" || filter == "interactableobject")
                return interactable is InteractableObject;
            if (filter == "pickup" || filter == "interactablepickup")
                return interactable is InteractablePickup;
            if (filter == "toy" || filter == "customtoy" || filter == "custominteractabletoy")
                return interactable is CustomInteractableToy;
            if (filter == "collider" || filter == "customcollider")
                return interactable is CustomCollider;
            if (filter == "speaker" || filter == "loopspeaker")
                return interactable is LoopSpeaker;
            if (filter == "spawner" || filter == "itemspawner")
                return interactable is ItemSpawner;
            if (filter == "damagetrigger" || filter == "killpart")
                return interactable is DamageTrigger;
            if (filter == "playercount" || filter == "playercounttrigger")
                return interactable is PlayerCountTrigger;
            if (filter == "door" || filter == "customdoor")
                return interactable is CustomDoor;

            return interactable.GetType().Name.Replace("_", string.Empty).Replace("-", string.Empty).Equals(filter, StringComparison.OrdinalIgnoreCase);
        }

        private static HealthObject FindHealthObject(DynValue identifier, SchematicObject schematic)
        {
            return FindInteractable(identifier, schematic, "health") as HealthObject;
        }

        private static HealthObject FindNearestHealthObject(Vector3 origin, float maxDistance, SchematicObject schematic)
        {
            HealthObject nearest = null;
            float nearestSqrDistance = maxDistance * maxDistance;

            foreach (HealthObject healthObject in ZAMERTPlugin.Singleton?.HealthObjects ?? new List<HealthObject>())
            {
                if (healthObject == null || !healthObject.Active || !healthObject.IsAlive)
                    continue;
                if (schematic != null && healthObject.OSchematic != schematic)
                    continue;

                float sqrDistance = (healthObject.transform.position - origin).sqrMagnitude;
                if (sqrDistance > nearestSqrDistance)
                    continue;

                nearest = healthObject;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        private static DynValue ToDynValue(Script script, object value)
        {
            if (value == null)
                return DynValue.Nil;

            if (value is DynValue dynValue)
                return dynValue;

            if (value is string text)
                return DynValue.NewString(text);

            if (value is bool flag)
                return DynValue.NewBoolean(flag);

            if (value is int || value is float || value is double || value is long || value is short || value is byte)
                return DynValue.NewNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture));

            if (value is Enum)
                return DynValue.NewString(value.ToString());

            if (value is Vector3 vector)
                return DynValue.NewTable(BuildVectorTable(script, vector));

            if (value is Player player)
                return DynValue.NewTable(BuildPlayerTable(script, player));

            if (value is Pickup pickup)
                return DynValue.NewTable(BuildPickupTable(script, pickup));

            if (value is Collider collider)
                return DynValue.NewTable(BuildColliderTable(script, collider));

            if (value is SchematicObject schematic)
                return DynValue.NewTable(BuildSchematicTable(script, schematic));

            if (value is ZAMERTInteractable interactable)
                return DynValue.NewTable(BuildInteractableTable(script, interactable));

            if (value is IDictionary<string, object> dict)
            {
                Table table = new Table(script);
                foreach (KeyValuePair<string, object> kv in dict)
                    table[kv.Key] = ToDynValue(script, kv.Value);
                return DynValue.NewTable(table);
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                Table table = new Table(script);
                int index = 1;
                foreach (object item in enumerable)
                    table[index++] = ToDynValue(script, item);
                return DynValue.NewTable(table);
            }

            return DynValue.NewString(value.ToString());
        }

        private static Table BuildVectorTable(Script script, Vector3 vector)
        {
            Table table = new Table(script);
            table["x"] = vector.x;
            table["y"] = vector.y;
            table["z"] = vector.z;
            return table;
        }

        private static Table BuildPlayerTable(Script script, Player player)
        {
            Table table = new Table(script);
            if (player == null)
                return table;

            table["id"] = player.PlayerId;
            table["name"] = player.Nickname ?? string.Empty;
            table["userId"] = player.UserId ?? string.Empty;
            table["role"] = player.Role.ToString();
            table["team"] = player.Team.ToString();
            table["isScp"] = player.IsSCP;
            table["isHuman"] = player.IsHuman;
            table["isAlive"] = player.IsAlive;
            table["health"] = player.Health;
            table["position"] = DynValue.NewTable(BuildVectorTable(script, player.Position));
            table["room"] = player.Room != null ? player.Room.Name.ToString() : string.Empty;
            table["zone"] = player.Zone.ToString();
            table["currentItem"] = player.CurrentItem != null ? player.CurrentItem.Type.ToString() : string.Empty;
            return table;
        }

        private static Table BuildPickupTable(Script script, Pickup pickup)
        {
            Table table = new Table(script);
            if (pickup == null)
                return table;

            table["type"] = pickup.Type.ToString();
            table["serial"] = pickup.Serial;
            table["position"] = pickup.Transform != null ? DynValue.NewTable(BuildVectorTable(script, pickup.Transform.position)) : DynValue.Nil;
            table["room"] = pickup.Room != null ? pickup.Room.Name.ToString() : string.Empty;
            table["zone"] = pickup.Room != null ? pickup.Room.Zone.ToString() : string.Empty;
            return table;
        }

        private static Table BuildColliderTable(Script script, Collider collider)
        {
            Table table = new Table(script);
            if (collider == null)
                return table;

            table["name"] = collider.gameObject != null ? collider.gameObject.name : string.Empty;
            table["tag"] = collider.gameObject != null ? collider.gameObject.tag : string.Empty;
            table["position"] = DynValue.NewTable(BuildVectorTable(script, collider.transform.position));
            return table;
        }

        private static Table BuildSchematicTable(Script script, SchematicObject schematic)
        {
            Table table = new Table(script);
            if (schematic == null)
                return table;

            table["name"] = schematic.Name ?? string.Empty;
            table["position"] = DynValue.NewTable(BuildVectorTable(script, schematic.transform.position));
            table["directoryPath"] = schematic.DirectoryPath ?? string.Empty;
            return table;
        }

        private static Table BuildInteractableTable(Script script, ZAMERTInteractable interactable)
        {
            Table table = new Table(script);
            if (interactable == null)
                return table;

            table["name"] = interactable.gameObject != null ? interactable.gameObject.name : string.Empty;
            table["type"] = interactable.GetType().Name;
            table["active"] = interactable.Active;
            table["position"] = DynValue.NewTable(BuildVectorTable(script, interactable.transform.position));
            table["schematic"] = ToDynValue(script, interactable.OSchematic);

            if (interactable.Base != null)
            {
                table["objectId"] = interactable.Base.ObjectId ?? string.Empty;
                table["code"] = interactable.Base.Code;
                table["scriptGroup"] = interactable.Base.ScriptGroup ?? string.Empty;
            }

            if (interactable is HealthObject healthObject)
            {
                table["health"] = healthObject.Health;
                table["isAlive"] = healthObject.IsAlive;
            }

            return table;
        }

        private static string ToString(DynValue value)
        {
            if (value == null || value.Type == DataType.Nil || value.Type == DataType.Void)
                return string.Empty;

            switch (value.Type)
            {
                case DataType.String:
                    return value.String ?? string.Empty;
                case DataType.Boolean:
                    return value.Boolean.ToString();
                case DataType.Number:
                    return value.Number.ToString(CultureInfo.InvariantCulture);
                default:
                    return value.ToPrintString();
            }
        }

        private static float ToFloat(DynValue value)
        {
            if (value == null)
                return 0f;

            if (value.Type == DataType.Number)
                return (float)value.Number;

            float parsed;
            return float.TryParse(ToString(value), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : 0f;
        }

        private static float ConvertToFloat(object value)
        {
            if (value == null)
                return 0f;

            try
            {
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                float parsed;
                return float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : 0f;
            }
        }

        private static bool ToBool(DynValue value)
        {
            if (value == null)
                return false;

            if (value.Type == DataType.Boolean)
                return value.Boolean;

            if (value.Type == DataType.Number)
                return Math.Abs(value.Number) > 0.0001;

            bool parsed;
            return bool.TryParse(ToString(value), out parsed) && parsed;
        }

        private static Vector3 ToVector(CallbackArguments args, int startIndex)
        {
            return new Vector3(
                args.Count > startIndex ? ToFloat(args[startIndex]) : 0f,
                args.Count > startIndex + 1 ? ToFloat(args[startIndex + 1]) : 0f,
                args.Count > startIndex + 2 ? ToFloat(args[startIndex + 2]) : 0f);
        }

        private static Vector3 ToVector(DynValue value)
        {
            if (value == null || value.Type == DataType.Nil || value.Type == DataType.Void)
                return Vector3.zero;

            if (value.Type == DataType.Table)
            {
                Table table = value.Table;
                DynValue x = table.Get("x");
                DynValue y = table.Get("y");
                DynValue z = table.Get("z");
                if (x.IsNil()) x = table.Get(1);
                if (y.IsNil()) y = table.Get(2);
                if (z.IsNil()) z = table.Get(3);
                return new Vector3(ToFloat(x), ToFloat(y), ToFloat(z));
            }

            string[] split = ToString(value).Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length >= 3)
            {
                float x;
                float y;
                float z;
                float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z);
                return new Vector3(x, y, z);
            }

            return Vector3.zero;
        }

        private static object ToObject(DynValue value)
        {
            if (value == null)
                return null;

            switch (value.Type)
            {
                case DataType.Boolean:
                    return value.Boolean;
                case DataType.Number:
                    return value.Number;
                case DataType.String:
                    return value.String;
                case DataType.Table:
                    return ToDictionary(value.Table);
                case DataType.Nil:
                case DataType.Void:
                    return null;
                default:
                    return value.ToPrintString();
            }
        }

        private static object ToDictionary(Table table)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (TablePair pair in table.Pairs)
            {
                string key = pair.Key.Type == DataType.Number
                    ? Mathf.RoundToInt((float)pair.Key.Number).ToString(CultureInfo.InvariantCulture)
                    : ToString(pair.Key);
                dict[key] = ToObject(pair.Value);
            }
            return dict;
        }
    }
}
