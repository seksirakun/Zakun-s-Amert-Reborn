using ProjectMER.Features;
using ProjectMER.Features.Objects;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Utils;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public static class VersionInfo
    {
        public static void PrintInfo()
        {
            Log.Info("========== ZAMERT v" + ZAMERTPlugin.Singleton.Version + " ==========");

            TryLog("SL Version",       () => GameCore.Version.VersionString);
            TryLog("Unity Version",    () => Application.unityVersion);
            TryLog("LabApi Version",   () =>
            {
                var name = typeof(LabApi.Loader.Features.Plugins.Plugin<>).Assembly.GetName();
                return name.Version != null ? name.Version.ToString() : "unknown";
            });
            TryLog("ProjectMER Version", () =>
            {
                var pmer = ProjectMER.ProjectMER.Singleton;
                return pmer != null ? pmer.Version.ToString() : "NOT LOADED";
            });

            Log.Info("offsets");

            PrintMethod("PMER MapUtils.LoadMap",               typeof(MapUtils),         "LoadMap");
            PrintMethod("PMER SchematicObject.Awake",          typeof(SchematicObject),   "Awake");
            PrintMethod("PMER SchematicObject.Start",          typeof(SchematicObject),   "Start");
            PrintMethod("PMER SchematicObject.Init",           typeof(SchematicObject),   "Init");
            PrintMethod("PMER SchematicObject.Destroy",        typeof(SchematicObject),   "Destroy");

            PrintMethod("ExplosionUtils.ServerExplode",        typeof(ExplosionUtils),    "ServerExplode");
            PrintMethod("ExplosionUtils.ServerSpawnEffect",    typeof(ExplosionUtils),    "ServerSpawnEffect");

            PrintMethod("Hitmarker.SendHitmarkerDirectly",     typeof(Hitmarker),         "SendHitmarkerDirectly");

            PrintMethod("AudioPlayer.Create",                  typeof(AudioPlayer), "Create");

            PrintField("InventoryItemLoader.AvailableItems",
                typeof(InventorySystem.InventoryItemLoader), "AvailableItems");

            Log.Info("==================================================");
        }

        private static void TryLog(string label, Func<string> getter)
        {
            try
            {
                Log.Info(label + ": " + getter());
            }
            catch (Exception ex)
            {
                Log.Warn(label + ": ERROR - " + ex.Message);
            }
        }

        private static void PrintMethod(string label, Type type, string methodName)
        {
            try
            {
                MethodInfo mi = type.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                if (mi == null)
                {
                    Log.Warn("  [" + label + "]: NOT FOUND");
                    return;
                }

                RuntimeHelpers.PrepareMethod(mi.MethodHandle);
                IntPtr ptr = mi.MethodHandle.GetFunctionPointer();
                Log.Info(string.Format("  [{0}]  Token=0x{1:X8}  Ptr=0x{2:X}", label, mi.MetadataToken, ptr.ToInt64()));
            }
            catch (Exception ex)
            {
                Log.Warn("  [" + label + "]: " + ex.Message);
            }
        }

        private static void PrintField(string label, Type type, string fieldName)
        {
            try
            {
                FieldInfo fi = type.GetField(
                    fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                if (fi == null)
                {
                    Log.Warn("  [" + label + "]: NOT FOUND");
                    return;
                }

                IntPtr ptr = fi.FieldHandle.Value;
                Log.Info(string.Format("  [{0}]  Token=0x{1:X8}  Handle=0x{2:X}", label, fi.MetadataToken, ptr.ToInt64()));
            }
            catch (Exception ex)
            {
                Log.Warn("  [" + label + "]: " + ex.Message);
            }
        }
    }
}
