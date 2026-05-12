using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class AmertImporter
{
    private static void RestoreAnimationTargets(Transform schematicRoot, List<AnimationDTO> animationModules)
    {
        if (schematicRoot == null || animationModules == null)
            return;

        foreach (AnimationDTO animation in animationModules)
        {
            if (animation == null || string.IsNullOrWhiteSpace(animation.AnimatorAdress))
                continue;

            Transform animatorTransform = FindObjectWithPath(schematicRoot, animation.AnimatorAdress);
            animation.Animator = animatorTransform != null ? animatorTransform.gameObject : null;
        }
    }

    private static void RestoreAnimationTargets(Transform schematicRoot, List<FAnimationDTO> animationModules)
    {
        if (schematicRoot == null || animationModules == null)
            return;

        foreach (FAnimationDTO animation in animationModules)
        {
            if (animation == null || string.IsNullOrWhiteSpace(animation.AnimatorAdress))
                continue;

            Transform animatorTransform = FindObjectWithPath(schematicRoot, animation.AnimatorAdress);
            animation.Animator = animatorTransform != null ? animatorTransform.gameObject : null;
        }
    }

    [MenuItem("SchematicManager/Decompile All", priority = 1)]
    public static void DecompileAll()
    {
        DecompileCustomCollider();
        DecompileCustomDoors();
        DecompileFuncExecutor();
        DecompileGroovyNoise();
        DecompileHealthSchematics();
        DecompileInteractableObjects();
        DecompileInteractablePickups();
        DecompileCustomInteractableToys();
        DecompileLoopSpeakers();
        DecompileItemSpawners();
        DecompileDamageTriggers();
        DecompilePrefabAnchors();
        DecompilePlayerCountTriggers();
    }

    [MenuItem("SchematicManager/Decompile Custom Colliders", priority = 1)]
    public static void DecompileCustomCollider()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<CCDTO> datas = Decompile<CCDTO>(schematic, "Colliders");
            List<FCCDTO> fdatas = Decompile<FCCDTO>(schematic, "FColliders");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                CustomCollider targetComponent = target.AddComponent<CustomCollider>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                RestoreAnimationTargets(schematic.transform, targetComponent.data.AnimationModules);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                CustomCollider targetComponent = target.AddComponent<CustomCollider>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, targetComponent.ScriptValueData.AnimationModules);
                targetComponent.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Custom Doors", priority = 1)]
    public static void DecompileCustomDoors()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<CDDTO> datas = Decompile<CDDTO>(schematic, "Doors");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                CustomDoor targetComponent = target.AddComponent<CustomDoor>();
                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                Transform animatorTransform = FindObjectWithPath(schematic.transform, data.Animator);
                targetComponent.data.AnimatorObject = animatorTransform != null ? animatorTransform.gameObject : null;
                targetComponent.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Function Executor", priority = 1)]
    public static void DecompileFuncExecutor()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<FEDTO> datas = Decompile<FEDTO>(schematic, "Functions");

            foreach (var data in datas)
            {
                schematic.AddComponent<FunctionExecutor>().data = data;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Groovy Noise", priority = 1)]
    public static void DecompileGroovyNoise()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<GNDTO> datas = Decompile<GNDTO>(schematic, "GroovyNoises");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                GroovyNoise targetComponent = target.AddComponent<GroovyNoise>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                targetComponent.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Health Schematics", priority = 1)]
    public static void DecompileHealthSchematics()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<HODTO> datas = Decompile<HODTO>(schematic, "HealthObjects");
            List<FHODTO> fdatas = Decompile<FHODTO>(schematic, "FHealthObjects");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                HealthObject targetComponent = target.AddComponent<HealthObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                RestoreAnimationTargets(schematic.transform, targetComponent.data.AnimationModules);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                HealthObject targetComponent = target.AddComponent<HealthObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, targetComponent.ScriptValueData.AnimationModules);
                targetComponent.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Interactable Objects", priority = 1)]
    public static void DecompileInteractableObjects()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<IODTO> datas = Decompile<IODTO>(schematic, "Objects");
            List<FIODTO> fdatas = Decompile<FIODTO>(schematic, "FObjects");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractableObject targetComponent = target.AddComponent<InteractableObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                RestoreAnimationTargets(schematic.transform, targetComponent.data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, targetComponent.data.DenyAnimationModules);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractableObject targetComponent = target.AddComponent<InteractableObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, targetComponent.ScriptValueData.AnimationModules);
                RestoreAnimationTargets(schematic.transform, targetComponent.ScriptValueData.DenyAnimationModules);
                targetComponent.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Interactable Pickups", priority = 1)]
    public static void DecompileInteractablePickups()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<IPDTO> datas = Decompile<IPDTO>(schematic, "Pickups");
            List<FIPDTO> fdatas = Decompile<FIPDTO>(schematic, "FPickups");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractablePickup interactablePickup = target.AddComponent<InteractablePickup>();

                interactablePickup.ScriptGroup = data.ScriptGroup;
                interactablePickup.data = data;
                RestoreAnimationTargets(schematic.transform, interactablePickup.data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, interactablePickup.data.DenyAnimationModules);
                interactablePickup.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractablePickup interactableObject = target.AddComponent<InteractablePickup>();

                interactableObject.ScriptGroup = data.ScriptGroup;
                interactableObject.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, interactableObject.ScriptValueData.AnimationModules);
                RestoreAnimationTargets(schematic.transform, interactableObject.ScriptValueData.DenyAnimationModules);
                interactableObject.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Custom Interactable Toys", priority = 1)]
    public static void DecompileCustomInteractableToys()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<CITDTO> datas = Decompile<CITDTO>(schematic, "ToyInteractables");
            List<FCITDTO> fdatas = Decompile<FCITDTO>(schematic, "FToyInteractables");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                CustomInteractableToy targetComponent = target.AddComponent<CustomInteractableToy>();
                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                RestoreAnimationTargets(schematic.transform, targetComponent.data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, targetComponent.data.DenyAnimationModules);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                CustomInteractableToy targetComponent = target.AddComponent<CustomInteractableToy>();
                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, targetComponent.ScriptValueData.AnimationModules);
                RestoreAnimationTargets(schematic.transform, targetComponent.ScriptValueData.DenyAnimationModules);
                targetComponent.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Loop Speakers", priority = 1)]
    public static void DecompileLoopSpeakers()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<LSDTO> datas = Decompile<LSDTO>(schematic, "LoopSpeakers");
            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                LoopSpeaker targetComponent = target.AddComponent<LoopSpeaker>();
                targetComponent.data = data;
                targetComponent.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Item Spawners", priority = 1)]
    public static void DecompileItemSpawners()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<ISDTO> datas = Decompile<ISDTO>(schematic, "ItemSpawners");
            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                ItemSpawner targetComponent = target.AddComponent<ItemSpawner>();
                targetComponent.data = data;
                targetComponent.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Damage Triggers", priority = 1)]
    public static void DecompileDamageTriggers()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<DTTDTO> datas = Decompile<DTTDTO>(schematic, "DamageTriggers");
            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                DamageTrigger targetComponent = target.AddComponent<DamageTrigger>();
                targetComponent.data = data;
                targetComponent.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Player Count Triggers", priority = 1)]
    public static void DecompilePlayerCountTriggers()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<PCTDTO> datas = Decompile<PCTDTO>(schematic, "PlayerCountTriggers");
            List<FPCTDTO> fdatas = Decompile<FPCTDTO>(schematic, "FPlayerCountTriggers");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                PlayerCountTrigger targetComponent = target.AddComponent<PlayerCountTrigger>();
                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                PlayerCountTrigger targetComponent = target.AddComponent<PlayerCountTrigger>();
                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                targetComponent.UseScriptValue = true;
            }
        }
    }

    private static readonly Config Config = SchematicManager.Config;
    public static Transform FindObjectWithPath(Transform target, string pathO)
    {
        if (target == null || string.IsNullOrWhiteSpace(pathO))
            return target;

        pathO = pathO.Trim();
        if (pathO != "")
        {
            string[] path = pathO.Split(' ');
            for (int i = path.Length - 1; i > -1; i--)
            {
                if (!int.TryParse(path[i], out int childIndex))
                {
                    Debug.LogWarning("AMERT Importer: Invalid child index '" + path[i] + "' in path '" + pathO + "'.");
                    break;
                }

                if (target.childCount == 0 || target.childCount <= childIndex)
                {
                    Debug.LogWarning("AMERT Importer: Could not find appropriate child for path '" + pathO + "'.");
                    break;
                }
                target = target.GetChild(childIndex);
            }
        }
        return target;
    }

    [MenuItem("SchematicManager/Decompile Prefab Anchors", priority = 1)]
    public static void DecompilePrefabAnchors()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<PFADTO> datas = Decompile<PFADTO>(schematic, "PrefabAnchors");
            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
                PrefabAnchor targetComponent = target.AddComponent<PrefabAnchor>();
                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                targetComponent.UseScriptValue = false;
            }
        }
    }

    public static List<TDO> Decompile<TDO>(Schematic schematic, string name)
    {
        string parentDirectoryPath = Directory.Exists(Config.ExportPath)
            ? Config.ExportPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "MapEditorReborn_CompiledSchematics");
        string schematicDirectoryPath = Path.Combine(parentDirectoryPath, schematic.gameObject.name);

        string amertData = Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-{name}.json");

        if (!Directory.Exists(parentDirectoryPath))
        {
            Debug.LogError("Could Not find root object's compiled directory!");
            return new List<TDO>();
        }
        if (!File.Exists(amertData))
        {
            return new List<TDO>();
        }
        return JsonConvert.DeserializeObject<List<TDO>>(File.ReadAllText(amertData), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto });
    }
}
