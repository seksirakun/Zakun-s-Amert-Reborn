using HarmonyLib;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public static class ZMapper
    {
        private static readonly HashSet<string> _hitMethods    = new HashSet<string>();
        private static readonly HashSet<string> _targetMethods = new HashSet<string>();
        private static bool _allHit      = false;
        private static bool _initialized = false;

        public static void Init(Harmony harmony)
        {
            if (_initialized || harmony == null)
                return;
            _initialized = true;

            PatchMethod(harmony, typeof(ZAMERTPlugin), "Enable");
            PatchMethod(harmony, typeof(ZAMERTPlugin), "Disable");

            PatchMethods(harmony, typeof(HealthObject),  new[] { "Start", "Register", "Damage", "CheckDead", "Update" });
            PatchMethods(harmony, typeof(FHealthObject), new[] { "Start", "Damage", "CheckDead" });

            PatchMethods(harmony, typeof(InteractableObject),  new[] { "Start", "Register", "RunProcess" });
            PatchMethods(harmony, typeof(FInteractableObject), new[] { "Start", "RunProcess" });

            PatchMethods(harmony, typeof(InteractablePickup),  new[] { "Start", "RunProcess" });
            PatchMethods(harmony, typeof(FInteractablePickup), new[] { "Start", "RunProcess" });

            PatchMethods(harmony, typeof(CustomCollider),  new[] { "Start", "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit" });
            PatchMethods(harmony, typeof(FCustomCollider), new[] { "Start", "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit" });

            PatchMethods(harmony, typeof(GroovyNoise),  new[] { "Start", "RunProcess" });
            PatchMethods(harmony, typeof(FGroovyNoise), new[] { "Start", "RunProcess" });

            TryPatchByReflection(harmony, typeof(SchematicObject), new[] { "Init", "Spawn", "Awake", "Start" });

            TryPatchFullName(harmony, "ProjectMER.Events.Handlers.Schematic", "add_SchematicSpawned");
        }

        public static void Prefix(MethodBase __originalMethod)
        {
            if (__originalMethod == null || _allHit)
                return;

            string key = Key(__originalMethod);
            if (_hitMethods.Contains(key))
                return;

            _hitMethods.Add(key);

            string typeName  = __originalMethod.DeclaringType?.FullName ?? "?";
            string methodName = __originalMethod.Name;
            int    token      = 0;
            string ptr        = "N/A";
            string mvid       = "N/A";

            try { token = __originalMethod.MetadataToken; }                catch { }
            try { mvid  = __originalMethod.Module.ModuleVersionId.ToString("B"); } catch { }
            try
            {
                RuntimeHelpers.PrepareMethod(__originalMethod.MethodHandle);
                ptr = "0x" + __originalMethod.MethodHandle.GetFunctionPointer().ToString("X");
            }
            catch { }

            Log.Info("[ZMapper] " + typeName + "::" + methodName);
            Log.Info("[ZMapper]   Token=" + string.Format("0x{0:X8}", token) + "  Ptr=" + ptr + "  MVID=" + mvid);

            if (_targetMethods.Count > 0 && _hitMethods.Count >= _targetMethods.Count)
            {
                _allHit = true;
                Log.Info("[ZMapper] All target methods hit - done.");
            }
        }

        private static void PatchMethod(Harmony harmony, Type type, string name)
        {
            MethodInfo mi = AccessTools.Method(type, name);
            if (mi == null) return;
            string key = Key(mi);
            if (_targetMethods.Contains(key)) return;
            _targetMethods.Add(key);
            harmony.Patch(mi, prefix: new HarmonyMethod(typeof(ZMapper), nameof(Prefix)));
        }

        private static void PatchMethods(Harmony harmony, Type type, IEnumerable<string> names)
        {
            foreach (string name in names)
                PatchMethod(harmony, type, name);
        }

        private static void TryPatchByReflection(Harmony harmony, Type type, IEnumerable<string> names)
        {
            if (type == null) return;
            MethodInfo[] all = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            HashSet<string> wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            foreach (MethodInfo mi in all)
            {
                if (mi == null || !wanted.Contains(mi.Name)) continue;
                string key = Key(mi);
                if (_targetMethods.Contains(key)) continue;
                _targetMethods.Add(key);
                harmony.Patch(mi, prefix: new HarmonyMethod(typeof(ZMapper), nameof(Prefix)));
            }
        }

        private static void TryPatchFullName(Harmony harmony, string fullTypeName, string methodName)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                .FirstOrDefault(t => t.FullName == fullTypeName);
            if (type == null) return;
            PatchMethod(harmony, type, methodName);
        }

        private static string Key(MethodBase m)
        {
            return (m.DeclaringType?.FullName ?? "?") + "::" + m.Name;
        }
    }
}
