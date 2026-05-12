using LabApi.Features.Wrappers;
using MoonSharp.Interpreter;
using ProjectMER.Features.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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
        public Dictionary<string, object> Values { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    internal static class LuaScriptService
    {
        internal static LuaExecutionContext ExecuteEvent(ZAMERTInteractable interactable, string eventName, LuaExecutionContext context = null)
        {
            if (interactable == null || interactable.Base == null || string.IsNullOrWhiteSpace(eventName))
                return context;

            if (context == null)
                context = new LuaExecutionContext();

            context.Interactable = context.Interactable ?? interactable;
            context.EventName = eventName.Trim().ToLowerInvariant();
            if (context.Player == null && context.Values.TryGetValue("player", out object cachedPlayer))
                context.Player = cachedPlayer as Player;

            List<LuaEventBinding> bindings = interactable.GetLuaBindings().Where(x => x != null && x.Matches(context.EventName)).ToList();
            if (bindings.Count == 0)
                return context;

            foreach (LuaEventBinding binding in bindings)
            {
                if (!CanExecute(binding))
                    continue;

                string scriptText = ResolveScriptText(interactable, binding);
                if (string.IsNullOrWhiteSpace(scriptText))
                    continue;

                try
                {
                    Script script = new Script(CoreModules.Preset_SoftSandbox);
                    script.Options.DebugPrint = msg => ZAMERTLogger.Info("[Lua] " + msg);
                    script.Globals["ctx"] = BuildContextTable(script, context);
                    script.Globals["api"] = BuildApiTable(script, context);
                    script.Globals["player"] = script.Globals.Get("ctx").Table.Get("player");
                    script.Globals["object"] = script.Globals.Get("ctx").Table.Get("object");
                    script.Globals["schematic"] = script.Globals.Get("ctx").Table.Get("schematic");
                    script.DoString(scriptText);
                    binding.LastExecutedAt = Time.time;
                    binding.WasExecuted = true;
                }
                catch (Exception ex)
                {
                    ZAMERTLogger.Error("Lua event failed on " + interactable.gameObject.name + " [" + context.EventName + "]: " + ex.Message);
                }
            }

            return context;
        }

        private static bool CanExecute(LuaEventBinding binding)
        {
            if (binding == null || !binding.Enabled)
                return false;

            if (binding.RunOnce && binding.WasExecuted)
                return false;

            if (binding.Cooldown > 0f && Time.time - binding.LastExecutedAt < binding.Cooldown)
                return false;

            return true;
        }

        private static string ResolveScriptText(ZAMERTInteractable interactable, LuaEventBinding binding)
        {
            if (binding.SourceMode == LuaScriptSourceMode.Inline)
                return binding.InlineScript ?? string.Empty;

            string scriptPath = binding.ScriptPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(scriptPath))
                return string.Empty;

            string[] candidates = new[]
            {
                scriptPath,
                interactable.OSchematic != null ? Path.Combine(interactable.OSchematic.DirectoryPath, scriptPath) : null,
                Path.Combine(Path.GetDirectoryName(typeof(ZAMERTPlugin).Assembly.Location) ?? string.Empty, scriptPath),
            };

            foreach (string candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (File.Exists(candidate))
                    return File.ReadAllText(candidate);
            }

            ZAMERTLogger.Warn("Lua script file not found: " + scriptPath);
            return string.Empty;
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
            table["player"] = ToDynValue(script, context.Player);
            table["pickup"] = ToDynValue(script, context.Pickup);
            table["schematic"] = ToDynValue(script, interactable != null ? interactable.OSchematic : null);
            table["object"] = ToDynValue(script, interactable);
            table["values"] = ToDynValue(script, context.Values);
            return table;
        }

        private static Table BuildApiTable(Script script, LuaExecutionContext context)
        {
            Table api = new Table(script);

            api["log"] = DynValue.NewCallback((ctx, args) =>
            {
                if (args.Count > 0)
                    ZAMERTLogger.Info("[Lua] " + ToString(args[0]));
                return DynValue.Nil;
            });

            api["command"] = DynValue.NewCallback((ctx, args) =>
            {
                if (args.Count > 0)
                    ZAMERTPlugin.ExecuteCommand(ToString(args[0]));
                return DynValue.Nil;
            });

            api["broadcast"] = DynValue.NewCallback((ctx, args) =>
            {
                string text = args.Count > 0 ? ToString(args[0]) : string.Empty;
                ushort duration = args.Count > 1 ? (ushort)Mathf.RoundToInt(ToFloat(args[1])) : (ushort)5;
                foreach (Player player in Player.List)
                    player.SendBroadcast(text, duration);
                return DynValue.Nil;
            });

            api["hint"] = DynValue.NewCallback((ctx, args) =>
            {
                if (context.Player == null)
                    return DynValue.Nil;

                string text = args.Count > 0 ? ToString(args[0]) : string.Empty;
                float duration = args.Count > 1 ? ToFloat(args[1]) : 5f;
                context.Player.SendHint(text, duration);
                return DynValue.Nil;
            });

            api["set_active"] = DynValue.NewCallback((ctx, args) =>
            {
                if (context.Interactable != null && args.Count > 0)
                    context.Interactable.Active = ToBool(args[0]);
                return DynValue.Nil;
            });

            api["get_active"] = DynValue.NewCallback((ctx, args) =>
            {
                return DynValue.NewBoolean(context.Interactable != null && context.Interactable.Active);
            });

            api["destroy_self"] = DynValue.NewCallback((ctx, args) =>
            {
                if (context.Interactable != null)
                    UnityEngine.Object.Destroy(context.Interactable.gameObject, 0.1f);
                return DynValue.Nil;
            });

            api["cancel"] = DynValue.NewCallback((ctx, args) =>
            {
                context.Cancelled = true;
                return DynValue.Nil;
            });

            api["remove_pickup"] = DynValue.NewCallback((ctx, args) =>
            {
                context.RemovePickup = true;
                return DynValue.Nil;
            });

            api["call_function"] = DynValue.NewCallback((ctx, args) =>
            {
                if (context.Interactable == null || context.Interactable.OSchematic == null || args.Count == 0)
                    return DynValue.Nil;

                string functionName = ToString(args[0]);
                if (string.IsNullOrWhiteSpace(functionName))
                    return DynValue.Nil;

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
                }

                return DynValue.Nil;
            });

            api["get_round_var"] = DynValue.NewCallback((ctx, args) =>
            {
                if (args.Count == 0)
                    return DynValue.Nil;

                string key = ToString(args[0]);
                if (string.IsNullOrWhiteSpace(key) || !ZAMERTPlugin.Singleton.RoundVariable.TryGetValue(key, out object value))
                    return DynValue.Nil;

                return ToDynValue(script, value);
            });

            api["set_round_var"] = DynValue.NewCallback((ctx, args) =>
            {
                if (args.Count >= 2)
                    ZAMERTPlugin.Singleton.RoundVariable[ToString(args[0])] = ToObject(args[1]);
                return DynValue.Nil;
            });

            api["get_schematic_var"] = DynValue.NewCallback((ctx, args) =>
            {
                if (context.Interactable == null || context.Interactable.OSchematic == null || args.Count == 0)
                    return DynValue.Nil;

                if (!ZAMERTPlugin.Singleton.SchematicVariables.TryGetValue(context.Interactable.OSchematic, out Dictionary<string, object> vars))
                    return DynValue.Nil;

                string key = ToString(args[0]);
                return vars.TryGetValue(key, out object value) ? ToDynValue(script, value) : DynValue.Nil;
            });

            api["set_schematic_var"] = DynValue.NewCallback((ctx, args) =>
            {
                if (context.Interactable == null || context.Interactable.OSchematic == null || args.Count < 2)
                    return DynValue.Nil;

                ZAMERTPlugin.Singleton.SchematicVariables[context.Interactable.OSchematic][ToString(args[0])] = ToObject(args[1]);
                return DynValue.Nil;
            });

            api["get_current_health"] = DynValue.NewCallback((ctx, args) =>
            {
                HealthObject healthObject = context.Interactable as HealthObject;
                return healthObject != null ? DynValue.NewNumber(healthObject.Health) : DynValue.Nil;
            });

            api["set_current_health"] = DynValue.NewCallback((ctx, args) =>
            {
                HealthObject healthObject = context.Interactable as HealthObject;
                if (healthObject != null && args.Count > 0)
                    healthObject.Health = ToFloat(args[0]);
                return DynValue.Nil;
            });

            api["damage_healthobject"] = DynValue.NewCallback((ctx, args) =>
            {
                if (args.Count < 2)
                    return DynValue.NewBoolean(false);

                HealthObject target = FindHealthObject(args[0], context.Interactable != null ? context.Interactable.OSchematic : null);
                float damage = ToFloat(args[1]);
                bool result = ScpHealthObjectCombat.TryDamageHealthObject(target, context.Player, damage);
                return DynValue.NewBoolean(result);
            });

            api["damage_nearest_healthobject"] = DynValue.NewCallback((ctx, args) =>
            {
                if (context.Interactable == null || args.Count < 2)
                    return DynValue.NewBoolean(false);

                float maxDistance = ToFloat(args[0]);
                float damage = ToFloat(args[1]);
                HealthObject nearest = ScpHealthObjectCombat.FindNearestHealthObject(context.Interactable.transform.position, maxDistance);
                bool result = ScpHealthObjectCombat.TryDamageHealthObject(nearest, context.Player, damage);
                return DynValue.NewBoolean(result);
            });

            api["spawn_prefab"] = DynValue.NewCallback((ctx, args) =>
            {
                if (context.Interactable == null || args.Count == 0)
                    return DynValue.NewBoolean(false);

                string prefabName = ToString(args[0]);
                Vector3 position = context.Interactable.transform.position;

                if (args.Count >= 4)
                {
                    position = new Vector3(ToFloat(args[1]), ToFloat(args[2]), ToFloat(args[3]));
                }

                GameObject instance = ZamertPrefabRegistry.SpawnPrefab(prefabName, position, context.Interactable.transform.rotation, context.Interactable.transform.localScale);
                return DynValue.NewBoolean(instance != null);
            });

            return api;
        }

        private static HealthObject FindHealthObject(DynValue identifier, SchematicObject schematic)
        {
            string objectId = ToString(identifier);
            int code;
            if (int.TryParse(objectId, out code))
            {
                return ZAMERTPlugin.Singleton.HealthObjects.FirstOrDefault(x =>
                    x != null
                    && x.OSchematic == schematic
                    && x.Base != null
                    && x.Base.Code == code);
            }

            return ZAMERTPlugin.Singleton.HealthObjects.FirstOrDefault(x =>
                x != null
                && x.OSchematic == schematic
                && x.Base != null
                && string.Equals(x.Base.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));
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
                return DynValue.NewNumber(Convert.ToDouble(value));

            if (value is Enum)
                return DynValue.NewString(value.ToString());

            if (value is Vector3 vector)
                return DynValue.NewTable(BuildVectorTable(script, vector));

            if (value is Player player)
                return DynValue.NewTable(BuildPlayerTable(script, player));

            if (value is Pickup pickup)
                return DynValue.NewTable(BuildPickupTable(script, pickup));

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
            table["isScp"] = player.IsSCP;
            table["isAlive"] = player.IsAlive;
            table["health"] = player.Health;
            table["position"] = DynValue.NewTable(BuildVectorTable(script, player.Position));
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

            if (interactable.Base != null)
            {
                table["objectId"] = interactable.Base.ObjectId ?? string.Empty;
                table["code"] = interactable.Base.Code;
                table["scriptGroup"] = interactable.Base.ScriptGroup ?? string.Empty;
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
                    return value.Number.ToString();
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
            return float.TryParse(ToString(value), out parsed) ? parsed : 0f;
        }

        private static bool ToBool(DynValue value)
        {
            if (value == null)
                return false;

            if (value.Type == DataType.Boolean)
                return value.Boolean;

            bool parsed;
            return bool.TryParse(ToString(value), out parsed) && parsed;
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
                case DataType.Nil:
                case DataType.Void:
                    return null;
                default:
                    return value.ToPrintString();
            }
        }
    }
}
