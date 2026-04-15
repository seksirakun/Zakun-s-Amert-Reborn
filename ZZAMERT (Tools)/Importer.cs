using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class AmertImporter
{
    [MenuItem("SchematicManager/Decompile All", priority = 1)]
    public static void DecompileAll()
    {
        DecompileCustomCollider();
        DecompileFuncExecutor();
        DecompileGroovyNoise();
        DecompileHealthSchematics();
        DecompileInteractableObjects();
        DecompileInteractablePickups();
        DecompileLoopSpeakers();
        DecompileItemSpawners();
        DecompileDamageTriggers();
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
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                CustomCollider targetComponent = target.AddComponent<CustomCollider>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = true;
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
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                HealthObject targetComponent = target.AddComponent<HealthObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
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
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractableObject targetComponent = target.AddComponent<InteractableObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
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
                interactablePickup.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                interactablePickup.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractablePickup interactableObject = target.AddComponent<InteractablePickup>();

                interactableObject.ScriptGroup = data.ScriptGroup;
                interactableObject.ScriptValueData = data;
                interactableObject.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                interactableObject.UseScriptValue = true;
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
        pathO = pathO.Trim();
        if (pathO != "")
        {
            string[] path = pathO.Split(' ');
            for (int i = path.Length - 1; i > -1; i--)
            {
                if (target.childCount == 0 || target.childCount <= int.Parse(path[i].ToString()))
                {
                    Debug.Log("AMERT Importer: Could not find appropriate child!");
                    break;
                }
                target = target.GetChild(int.Parse(path[i]));
            }
        }
        return target;
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
