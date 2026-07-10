#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Rebuilds AMERT components in the Unity scene from previously compiled
/// "&lt;Schematic&gt;-*.json" files (the reverse of the Compile menu commands).
/// </summary>
public static class AmertImporter
{
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
        DecompileMapDoors();
        DecompileClutter();
        DecompileScp079Cameras();
        DecompileCustomTextures();
    }

    [MenuItem("SchematicManager/Decompile Custom Colliders", priority = 1)]
    public static void DecompileCustomCollider()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (CCDTO data in Load<CCDTO>(schematic, "Colliders"))
            {
                CustomCollider component = AddComponentFor<CustomCollider>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                component.UseScriptValue = false;
            }

            foreach (FCCDTO data in Load<FCCDTO>(schematic, "FColliders"))
            {
                CustomCollider component = AddComponentFor<CustomCollider>(schematic, data);
                if (component == null)
                    continue;

                component.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                component.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Custom Doors", priority = 1)]
    public static void DecompileCustomDoors()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (CDDTO data in Load<CDDTO>(schematic, "Doors"))
            {
                CustomDoor component = AddComponentFor<CustomDoor>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                Transform animatorTransform = FindObjectWithPath(schematic.transform, data.Animator);
                data.AnimatorObject = animatorTransform != null ? animatorTransform.gameObject : null;
                component.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Function Executor", priority = 1)]
    public static void DecompileFuncExecutor()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (FEDTO data in Load<FEDTO>(schematic, "Functions"))
            {
                schematic.gameObject.AddComponent<FunctionExecutor>().data = data;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Groovy Noise", priority = 1)]
    public static void DecompileGroovyNoise()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (GNDTO data in Load<GNDTO>(schematic, "GroovyNoises"))
            {
                GroovyNoise component = AddComponentFor<GroovyNoise>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }

            foreach (FGNDTO data in Load<FGNDTO>(schematic, "FGroovyNoises"))
            {
                GroovyNoise component = AddComponentFor<GroovyNoise>(schematic, data);
                if (component == null)
                    continue;

                component.ScriptValueData = data;
                component.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Health Schematics", priority = 1)]
    public static void DecompileHealthSchematics()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (HODTO data in Load<HODTO>(schematic, "HealthObjects"))
            {
                HealthObject component = AddComponentFor<HealthObject>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                component.UseScriptValue = false;
            }

            foreach (FHODTO data in Load<FHODTO>(schematic, "FHealthObjects"))
            {
                HealthObject component = AddComponentFor<HealthObject>(schematic, data);
                if (component == null)
                    continue;

                component.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                component.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Interactable Objects", priority = 1)]
    public static void DecompileInteractableObjects()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (IODTO data in Load<IODTO>(schematic, "Objects"))
            {
                InteractableObject component = AddComponentFor<InteractableObject>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, data.DenyAnimationModules);
                component.UseScriptValue = false;
            }

            foreach (FIODTO data in Load<FIODTO>(schematic, "FObjects"))
            {
                InteractableObject component = AddComponentFor<InteractableObject>(schematic, data);
                if (component == null)
                    continue;

                component.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, data.DenyAnimationModules);
                component.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Interactable Pickups", priority = 1)]
    public static void DecompileInteractablePickups()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (IPDTO data in Load<IPDTO>(schematic, "Pickups"))
            {
                InteractablePickup component = AddComponentFor<InteractablePickup>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, data.DenyAnimationModules);
                component.UseScriptValue = false;
            }

            foreach (FIPDTO data in Load<FIPDTO>(schematic, "FPickups"))
            {
                InteractablePickup component = AddComponentFor<InteractablePickup>(schematic, data);
                if (component == null)
                    continue;

                component.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, data.DenyAnimationModules);
                component.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Custom Interactable Toys", priority = 1)]
    public static void DecompileCustomInteractableToys()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (CITDTO data in Load<CITDTO>(schematic, "ToyInteractables"))
            {
                CustomInteractableToy component = AddComponentFor<CustomInteractableToy>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, data.DenyAnimationModules);
                component.UseScriptValue = false;
            }

            foreach (FCITDTO data in Load<FCITDTO>(schematic, "FToyInteractables"))
            {
                CustomInteractableToy component = AddComponentFor<CustomInteractableToy>(schematic, data);
                if (component == null)
                    continue;

                component.ScriptValueData = data;
                RestoreAnimationTargets(schematic.transform, data.AnimationModules);
                RestoreAnimationTargets(schematic.transform, data.DenyAnimationModules);
                component.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Loop Speakers", priority = 1)]
    public static void DecompileLoopSpeakers()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (LSDTO data in Load<LSDTO>(schematic, "LoopSpeakers"))
            {
                LoopSpeaker component = AddComponentFor<LoopSpeaker>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Item Spawners", priority = 1)]
    public static void DecompileItemSpawners()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (ISDTO data in Load<ISDTO>(schematic, "ItemSpawners"))
            {
                ItemSpawner component = AddComponentFor<ItemSpawner>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Damage Triggers", priority = 1)]
    public static void DecompileDamageTriggers()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (DTTDTO data in Load<DTTDTO>(schematic, "DamageTriggers"))
            {
                DamageTrigger component = AddComponentFor<DamageTrigger>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Prefab Anchors", priority = 1)]
    public static void DecompilePrefabAnchors()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (PFADTO data in Load<PFADTO>(schematic, "PrefabAnchors"))
            {
                PrefabAnchor component = AddComponentFor<PrefabAnchor>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Player Count Triggers", priority = 1)]
    public static void DecompilePlayerCountTriggers()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (PCTDTO data in Load<PCTDTO>(schematic, "PlayerCountTriggers"))
            {
                PlayerCountTrigger component = AddComponentFor<PlayerCountTrigger>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }

            foreach (FPCTDTO data in Load<FPCTDTO>(schematic, "FPlayerCountTriggers"))
            {
                PlayerCountTrigger component = AddComponentFor<PlayerCountTrigger>(schematic, data);
                if (component == null)
                    continue;

                component.ScriptValueData = data;
                component.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Map Doors", priority = 1)]
    public static void DecompileMapDoors()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (MDDTO data in Load<MDDTO>(schematic, "MapDoors"))
            {
                MapDoor component = AddComponentFor<MapDoor>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Clutter", priority = 1)]
    public static void DecompileClutter()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (CLDTO data in Load<CLDTO>(schematic, "Clutter"))
            {
                Clutter component = AddComponentFor<Clutter>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Scp079 Cameras", priority = 1)]
    public static void DecompileScp079Cameras()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (SCDTO data in Load<SCDTO>(schematic, "Scp079Cameras"))
            {
                MapScp079Camera component = AddComponentFor<MapScp079Camera>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Custom Textures", priority = 1)]
    public static void DecompileCustomTextures()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            foreach (CTDTO data in Load<CTDTO>(schematic, "CustomTextures"))
            {
                CustomTexture component = AddComponentFor<CustomTexture>(schematic, data);
                if (component == null)
                    continue;

                component.data = data;
                component.UseScriptValue = false;
            }

            foreach (FCTDTO data in Load<FCTDTO>(schematic, "FCustomTextures"))
            {
                CustomTexture component = AddComponentFor<CustomTexture>(schematic, data);
                if (component == null)
                    continue;

                component.ScriptValueData = data;
                component.UseScriptValue = true;
            }
        }
    }

    private static T AddComponentFor<T>(Schematic schematic, DTO data) where T : FakeMono
    {
        Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);
        if (target == null)
        {
            Debug.LogWarning("AMERT Importer: could not resolve target for ObjectId '" + (data.ObjectId ?? "null") + "' in schematic '" + schematic.gameObject.name + "'.");
            return null;
        }

        T component = target.gameObject.AddComponent<T>();
        component.ScriptGroup = data.ScriptGroup;
        return component;
    }

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

    public static Transform FindObjectWithPath(Transform target, string pathO)
    {
        if (target == null || string.IsNullOrWhiteSpace(pathO))
            return target;

        string[] path = pathO.Trim().Split(' ');
        for (int i = path.Length - 1; i >= 0; i--)
        {
            if (!int.TryParse(path[i], out int childIndex))
            {
                Debug.LogWarning("AMERT Importer: invalid child index '" + path[i] + "' in path '" + pathO + "'.");
                break;
            }

            if (childIndex < 0 || childIndex >= target.childCount)
            {
                Debug.LogWarning("AMERT Importer: child index out of range for path '" + pathO + "'.");
                break;
            }
            target = target.GetChild(childIndex);
        }
        return target;
    }

    public static List<TDO> Load<TDO>(Schematic schematic, string name)
    {
        string dir = AmertCompile.GetSchematicDirectory(schematic);
        if (dir == null)
            return new List<TDO>();

        string path = Path.Combine(dir, schematic.gameObject.name + "-" + name + ".json");
        if (!File.Exists(path))
            return new List<TDO>();

        try
        {
            return AmertJson.DeserializeList<TDO>(File.ReadAllText(path));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("AMERT Importer: failed to read '" + path + "': " + ex.Message);
            return new List<TDO>();
        }
    }
}
#endif
