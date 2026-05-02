using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Reflection;

[Flags]
[Serializable]
public enum DoorPermissionFlags
{
    None = 0,
    Checkpoints = 1,
    ExitGates = 2,
    Intercom = 4,
    AlphaWarhead = 8,
    ContainmentLevelOne = 16,
    ContainmentLevelTwo = 32,
    ContainmentLevelThree = 64,
    Armory = 128,
    ScpOverride = 256,
    UncheckedServerGate = 512,
    Isolation = 1024,
    Surface = 2048,
    OtherSurface = 4096,
}

[Flags]
[Serializable]
public enum ColliderActionType
{
	ModifyHealth = 1,
	GiveEffect = 2,
	SendMessage = 4,
	PlayAnimation = 8,
	SendCommand = 16,
	Warhead = 32,
	Explode = 64,
	PlayAudio = 128,
	CallGroovieNoise = 256,
	CallFunction = 512,
	DropItems = 1024,
	ModifyPrimitive = 2048,
	ControlSpeaker = 4096,
	ControlItemSpawner = 8192,
}

[Flags]
[Serializable]
public enum CollisionType
{
	OnEnter = 1,
	OnStay = 2,
	OnExit = 4,
}

[Flags]
[Serializable]
public enum DetectType
{
	Pickup = 1,
	Player = 2,
	Projectile = 4
}

[Flags]
[Serializable]
public enum EffectFlag
{
	Disable = 1,
	Enable = 2,
	ModifyDuration = 4,
	ForceDuration = 8,
	ModifyIntensity = 16,
	ForceIntensity = 32
}

[Serializable]
public enum EffectType
{
	None,

	AmnesiaItems,

	AmnesiaVision,

	Asphyxiated,

	Bleeding,

	Blinded,

	Burned,

	Concussed,

	Corroding,

	Deafened,

	Decontaminating,

	Disabled,

	Ensnared,

	Exhausted,

	Flashed,

	Hemorrhage,

	Invigorated,

	BodyshotReduction,

	Poisoned,

	Scp207,

	Invisible,

	SinkHole,

	DamageReduction,

	MovementBoost,

	RainbowTaste,

	SeveredHands,

	Stained,

	Vitality,

	Hypothermia,

	Scp1853,

	CardiacArrest,

	InsufficientLighting,

	SoundtrackMute,

	SpawnProtected,

	Traumatized,

	AntiScp207,

	Scanned,

	PocketCorroding,

	SilentWalk,

	[Obsolete("Not functional in-game")]
	Marshmallow,

	Strangled,

	Ghostly,

	FogControl,

	Slowness,

	Scp1344,

	SeveredEyes,

	PitDeath,

	Blurred,

	[Obsolete("Only availaible for Christmas and AprilFools.")]
	BecomingFlamingo,

	[Obsolete("Only availaible for Christmas and AprilFools.")]
	Scp559,

	[Obsolete("Only availaible for Christmas and AprilFools.")]
	Scp956Target,

	[Obsolete("Only availaible for Christmas and AprilFools.")]
	Snowed,

	NightVision
}

[Flags]
[Serializable]
public enum TeleportInvokeType
{
	Enter = 1,
	Exit = 2,
	Collide = 4
}

[Flags]
[Serializable]
public enum DeadType
{
	Disappear = 1,
	GetRigidbody = 2,
	DynamicDisappearing = 4,
	Explode = 8,
	ResetHP = 16,
	PlayAnimation = 32,
	Warhead = 64,
	SendMessage = 128,
    DropItems = 256,
	SendCommand = 512,
	GiveEffect = 1024,
	PlayAudio = 2048,
	CallGroovieNoise = 4096,
	CallFunction = 8192,
	ModifyPrimitive = 16384,
	ControlSpeaker = 32768,
	ControlItemSpawner = 65536,
}

[Flags]
[Serializable]
public enum WarheadActionType
{
	Start = 1,
	Stop = 2,
	Lock = 4,
	UnLock = 8,
	Disable = 16,
	Enable = 32
}

[Serializable]
public enum AnimationType
{
	Start,
	Stop,
	ModifyParameter
}

[Serializable]
public enum ParameterType
{
	Integer,
	Float,
	Bool,
	Trigger
}

[Serializable]
public enum MessageType
{
	Cassie,
	BroadCast,
	Hint
}

[Flags]
[Serializable]
public enum SendType
{
	Interactor = 1,
	AllExceptAboveOne = 2,
	Alive = 4,
	Spectators = 8
}

[Flags]
[Serializable]
public enum Scp914Mode
{
	Rough = 1,
	Coarse = 2,
	OneToOne = 4,
	Fine = 8,
	VeryFine = 16
}

[Flags]
[Serializable]
public enum IPActionType
{
	Disappear = 1,
	Explode = 2,
	PlayAnimation = 4,
	Warhead = 8,
	SendMessage = 16,
	DropItems = 32,
	SendCommand = 64,
	UpgradeItem = 128,
	GiveEffect = 256,
	PlayAudio = 512,
	CallGroovieNoise = 1024,
	CallFunction = 2048,
	ModifyPrimitive = 4096,
	ControlSpeaker = 8192,
	ControlItemSpawner = 16384,
}

[Flags]
[Serializable]
public enum LoopSpeakerAction
{
	Play = 1,
	Stop = 2,
	ChangeClip = 4,
	SetVolume = 8,
}

[Flags]
[Serializable]
public enum ItemSpawnerAction
{
	Spawn = 1,
	Stop = 2,
	Reset = 4,
}

[Flags]
[Serializable]
public enum PrimitiveModifyType
{
	Color = 1,
	Scale = 2,
	Visibility = 4,
}

[Flags]
[Serializable]
public enum PlayerCountTriggerMode
{
	OnReachThreshold = 1,
	OnDropBelowThreshold = 2,
}

[Flags]
[Serializable]
public enum InvokeType
{
	Searching = 1,
	Picked = 2
}

[Serializable]
public enum RoleTypeId : sbyte
{

	None = -1,

	Scp173,

	ClassD,

	Spectator,

	Scp106,

	NtfSpecialist,

	Scp049,

	Scientist,

	Scp079,

	ChaosConscript,

	Scp096,

	Scp0492,

	NtfSergeant,

	NtfCaptain,

	NtfPrivate,

	Tutorial,

	FacilityGuard,

	Scp939,

	CustomRole,

	ChaosRifleman,

	ChaosMarauder,

	ChaosRepressor,

	Overwatch,

	Filmmaker,

	Scp3114,

	Destroyed,

	Flamingo,

	AlphaFlamingo,

	ZombieFlamingo
}

public class PublicFunctions
{
	public static string FindPath(GameObject mono)
    {
		if (mono == null)
			return "";
		return FindPath(mono.transform);
    }

	public static string FindPath(Transform transform)
	{
		string path = "";
		if (transform.TryGetComponent<Schematic>(out _))
		{
			return path;
		}
		while (transform.parent != null)
		{
			for (int i = 0; i < transform.parent.childCount; i++)
			{
				if (transform.parent.GetChild(i) == transform)
				{
					path += i.ToString();
				}
			}
			transform = transform.parent;
			if (transform.TryGetComponent<Schematic>(out _)) break;
			path += " ";
		}
		return path;
	}

	public static void OnIDChange(int original, int New)
    {

		AMERTs.RemoveWhere(x => x == null);

		foreach (FakeMono noise in AMERTs)
        {
			if (noise is GroovyNoise && (noise as GroovyNoise).data.Settings != null)
            {
				foreach (GMDTO id in (noise as GroovyNoise).data.Settings)
				{
					for (int i = 0; i < id.Targets.Count; i++)
					{
						if (id.Targets[i] == original)
						{
							id.Targets[i] = New;
						}
					}
				}
			}
			else
            {

                object arr;
				object obj;
				FieldInfo info = noise.GetType().GetField("data", BindingFlags.Instance | BindingFlags.Public);
                if (info != null && (obj = info.GetValue(noise)) != null)
                {
					FieldInfo info1 = obj.GetType().GetField("GroovieNoiseToCall", BindingFlags.Instance | BindingFlags.Public);
					if (info1 != null && (arr = info1.GetValue(obj)) != null)
					{
						foreach (CGNModule module in (List<CGNModule>)arr)
						{
							if (module.GroovieNoiseId == original)
								module.GroovieNoiseId = New;
						}
					}
                }
            }
        }
    }

	public static HashSet<FakeMono> AMERTs = new HashSet<FakeMono> { };
}