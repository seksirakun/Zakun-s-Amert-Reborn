using System;
using System.Collections.Generic;

namespace ZAMERT
{
    [Flags]
    [Serializable]
    public enum ColliderActionType
    {
        ModifyHealth = 1, GiveEffect = 2, SendMessage = 4, PlayAnimation = 8,
        SendCommand = 16, Warhead = 32, Explode = 64, PlayAudio = 128,
        CallGroovieNoise = 256, CallFunction = 512, DropItems = 1024, ModifyPrimitive = 2048, ControlSpeaker = 4096,
        ControlItemSpawner = 8192, PlayerLink = 16384,
    }

    [Flags][Serializable]
    public enum CollisionType { OnEnter = 1, OnStay = 2, OnExit = 4 }

    [Flags][Serializable]
    public enum DetectType { Pickup = 1, Player = 2, Projectile = 4 }

    [Serializable]
    public enum DoorType : int { LCZ = 0, HCZ = 1, EZ = 2 }

    [Flags][Serializable]
    public enum EffectFlagE { Disable = 1, Enable = 2, ModifyDuration = 4, ForceDuration = 8, ModifyIntensity = 16, ForceIntensity = 32 }

    [Flags][Serializable]
    public enum Scp914Mode { Rough = 1, Coarse = 2, OneToOne = 4, Fine = 8, VeryFine = 16 }

    public static class Scp914Helper
    {
        // Maps the Scp914Mode flag bits (authored in the Unity tools) to
        // Scp914.Scp914KnobSetting indices (Rough=0 .. VeryFine=4).
        public static List<int> GetKnobSettings(Scp914Mode mode)
        {
            List<int> knobs = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                if (((int)mode & (1 << i)) != 0)
                    knobs.Add(i);
            }
            return knobs;
        }
    }

    [Flags][Serializable]
    public enum TeleportInvokeType { Enter = 1, Exit = 2, Collide = 4 }

    [Flags][Serializable]
    public enum DeadType
    {
        Disappear = 1, GetRigidbody = 2, DynamicDisappearing = 4, Explode = 8,
        ResetHP = 16, PlayAnimation = 32, Warhead = 64, SendMessage = 128,
        DropItems = 256, SendCommand = 512, GiveEffect = 1024, PlayAudio = 2048,
        CallGroovieNoise = 4096, CallFunction = 8192, ModifyPrimitive = 16384, ControlSpeaker = 32768,
        ControlItemSpawner = 65536, PlayerLink = 131072,
    }

    [Flags][Serializable]
    public enum IPActionType
    {
        Disappear = 1, Explode = 2, PlayAnimation = 4, Warhead = 8,
        SendMessage = 16, DropItems = 32, SendCommand = 64, UpgradeItem = 128,
        GiveEffect = 256, PlayAudio = 512, CallGroovieNoise = 1024, CallFunction = 2048,
        ModifyPrimitive = 4096, ControlSpeaker = 8192, ControlItemSpawner = 16384, PlayerLink = 32768,
    }

    [Flags][Serializable]
    public enum LoopSpeakerAction
    {
        Play = 1,
        Stop = 2,
        ChangeClip = 4,
        SetVolume = 8,
    }

    [Flags][Serializable]
    public enum ItemSpawnerAction
    {
        Spawn = 1,
        Stop = 2,
        Reset = 4,
    }

    [Flags][Serializable]
    public enum PrimitiveModifyType
    {
        Color = 1,
        Scale = 2,
        Visibility = 4,
    }

    [Flags][Serializable]
    public enum PlayerCountTriggerMode
    {

        OnReachThreshold = 1,

        OnDropBelowThreshold = 2,
    }

    [Flags]
    [Serializable]
    public enum DamageTriggerMode
    {
        OnEnter = 1,
        OnStay = 2,
        OnExit = 4,
    }

    [Serializable]
    public enum PlayerLinkTargetType
    {
        Triggerer,
        All,
        Zone,
        Around,
    }

    [Serializable]
    public enum ZoneFacility
    {
        None = 0,
        LightContainment = 1,
        HeavyContainment = 2,
        Entrance = 3,
        Surface = 4,
        Other = 5,
    }

    [Flags]
    [Serializable]
    public enum ScpDamageType
    {
        None = 0,
        Scp049 = 1,
        Scp0492 = 2,
        Scp096 = 4,
        Scp173 = 8,
        Scp939 = 16,
        All = Scp049 | Scp0492 | Scp096 | Scp173 | Scp939,
    }

    [Serializable]
    public enum LuaEventType
    {
        Spawned,
        Destroyed,
        Interacted,
        Denied,
        Searching,
        Picked,
        Damaged,
        Died,
        Entered,
        Stayed,
        Exited,
        ThresholdReached,
        ThresholdDropped,
        Tick,
        Custom,
    }

    [Serializable]
    public enum LuaScriptSourceMode
    {
        Inline,
        File,
    }

    [Flags][Serializable]
    public enum InvokeType { Searching = 1, Picked = 2 }

    [Serializable]
    public enum AnimationTypeE { Start, Stop, ModifyParameter }

    [Serializable]
    public enum ParameterTypeE { Integer, Float, Bool, Trigger }

    [Flags][Serializable]
    public enum WarheadActionType { Start = 1, Stop = 2, Lock = 4, UnLock = 8, Disable = 16, Enable = 32 }

    [Serializable]
    public enum MessageTypeE { Cassie, BroadCast, Hint }

    [Flags][Serializable]
    public enum SendType { Interactor = 1, AllExceptAboveOne = 2, Alive = 4, Spectators = 8 }

    [Serializable]
    public enum EffectType
    {
        None = 0, AmnesiaItems, AmnesiaVision, Asphyxiated, Bleeding, Blinded, Burned,
        Concussed, Corroding, Deafened, Decontaminating = 10, Disabled, Ensnared, Exhausted,
        Flashed, Hemorrhage, Invigorated, BodyshotReduction, Poisoned, Scp207, Invisible = 20,
        SinkHole, DamageReduction, MovementBoost, RainbowTaste, SeveredHands, Stained, Vitality,
        Hypothermia, Scp1853, CardiacArrest = 30, InsufficientLighting, SoundtrackMute,
        SpawnProtected, Traumatized, AntiScp207, Scanned, PocketCorroding, SilentWalk,
        [Obsolete("Not functional in-game")] Marshmallow,
        Strangled = 40, Ghostly, FogControl, Slowness, Scp1344, SeveredEyes, PitDeath, Blurred,
        [Obsolete("Only available for Christmas and AprilFools.")] BecomingFlamingo,
        [Obsolete("Only available for Christmas and AprilFools.")] Scp559,
        [Obsolete("Only available for Christmas and AprilFools.")] Scp956Target = 50,
        [Obsolete("Only available for Christmas and AprilFools.")] Snowed, NightVision
    }
}
