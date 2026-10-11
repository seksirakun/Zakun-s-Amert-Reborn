using LabApi.Features.Wrappers;
using MEC;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using UnityEngine;
using LuaCoroutine = MoonSharp.Interpreter.Coroutine;

namespace ZAMERT
{
    internal sealed class LuaTask
    {
        public LuaModule Owner;
        public LuaCoroutine Coroutine;
        public float ResumeAt;
        public int BusySlices;
        public bool Alive = true;
    }

    internal static class LuaRuntime
    {
        private static readonly List<LuaTask> Tasks = new List<LuaTask>();
        private static bool _ticker;
        private static int _cursor;

        public static void Start()
        {
            if (_ticker)
                return;

            _ticker = true;
            Timing.RunCoroutine(Tick());
        }

        public static void Stop()
        {
            _ticker = false;
            Tasks.Clear();
        }

        public static void Kill(LuaModule owner)
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                if (Tasks[i].Owner == owner)
                    Tasks[i].Alive = false;
            }
        }

        public static Script Begin(LuaModule owner, LuaExecutionContext context, string source, string sourcePath)
        {
            Script script = LuaScriptService.CreateBoundScript(context, sourcePath);
            owner.Script = script;
            string wrapped = "return function()\n" + source + "\nend";
            DynValue main = script.DoString(wrapped, null, sourcePath ?? "lua-module");
            StartCoroutine(owner, script.CreateCoroutine(main).Coroutine);
            return script;
        }

        public static bool Call(LuaModule owner, string functionName, Player player)
        {
            if (owner == null || owner.Script == null || string.IsNullOrEmpty(functionName))
                return false;

            if (CountFor(owner) >= 8)
            {
                ZAMERTLogger.Warn("Lua module on " + owner.gameObject.name + " already has 8 running scripts. Skipped " + functionName + ".");
                return false;
            }

            if (owner.Context != null)
                owner.Context.Player = player;

            owner.Script.Globals["player"] = LuaScriptService.ToLua(owner.Script, player);
            DynValue function = owner.Script.Globals.Get(functionName);
            if (function.Type != DataType.Function)
                return false;

            StartCoroutine(owner, owner.Script.CreateCoroutine(function).Coroutine);
            return true;
        }

        private static int CountFor(LuaModule owner)
        {
            int count = 0;
            for (int i = 0; i < Tasks.Count; i++)
            {
                if (Tasks[i].Alive && Tasks[i].Owner == owner)
                    count++;
            }

            return count;
        }

        private static void StartCoroutine(LuaModule owner, LuaCoroutine coroutine)
        {
            Config config = ZAMERTPlugin.Singleton != null ? ZAMERTPlugin.Singleton.Config : null;
            int slice = config != null ? config.LuaAutoYieldInstructions : 2500;
            coroutine.AutoYieldCounter = Math.Max(200, slice);

            LuaTask task = new LuaTask
            {
                Owner = owner,
                Coroutine = coroutine,
                ResumeAt = Time.time,
            };
            Tasks.Add(task);
            Step(task);
        }

        private static IEnumerator<float> Tick()
        {
            while (_ticker)
            {
                yield return Timing.WaitForOneFrame;
                if (Tasks.Count == 0)
                    continue;

                Config config = ZAMERTPlugin.Singleton != null ? ZAMERTPlugin.Singleton.Config : null;
                int budget = config != null ? Mathf.Max(1, config.LuaMaxProgramsPerFrame) : 8;
                int guard = Tasks.Count;
                while (budget > 0 && guard-- > 0 && Tasks.Count > 0)
                {
                    if (_cursor >= Tasks.Count)
                        _cursor = 0;

                    LuaTask task = Tasks[_cursor];
                    _cursor++;
                    if (!task.Alive || task.Owner == null)
                    {
                        task.Alive = false;
                        continue;
                    }

                    if (Time.time < task.ResumeAt)
                        continue;

                    budget--;
                    Step(task);
                }

                for (int i = Tasks.Count - 1; i >= 0; i--)
                {
                    if (!Tasks[i].Alive || Tasks[i].Owner == null)
                        Tasks.RemoveAt(i);
                }
            }
        }

        private static void Step(LuaTask task)
        {
            if (!task.Alive || task.Coroutine == null || task.Owner == null)
            {
                task.Alive = false;
                return;
            }

            try
            {
                DynValue result = task.Coroutine.Resume();
                CoroutineState state = task.Coroutine.State;
                if (state == CoroutineState.Dead)
                {
                    task.Alive = false;
                    return;
                }

                bool forced = result.Type == DataType.YieldRequest && result.YieldRequest != null && result.YieldRequest.Forced;
                if (forced || state == CoroutineState.ForceSuspended)
                {
                    task.BusySlices++;
                    int maxBusy = ZAMERTPlugin.Singleton != null ? Mathf.Max(1, ZAMERTPlugin.Singleton.Config.LuaMaxBusySlices) : 8;
                    if (task.BusySlices > maxBusy)
                    {
                        task.Alive = false;
                        ZAMERTLogger.Error("Lua on " + task.Owner.gameObject.name + " was stopped because it ran too long without api.wait(). Put api.wait(seconds) inside loops.");
                        return;
                    }

                    task.ResumeAt = Time.time;
                    return;
                }

                task.BusySlices = 0;
                float seconds = result.Type == DataType.Number ? (float)result.Number : 0f;
                if (seconds < 0f)
                    seconds = 0f;

                float cap = ZAMERTPlugin.Singleton != null ? ZAMERTPlugin.Singleton.Config.MaxFunctionWaitSeconds : 600f;
                if (cap > 0f && seconds > cap)
                    seconds = cap;

                task.ResumeAt = Time.time + seconds;
            }
            catch (Exception ex)
            {
                task.Alive = false;
                string where = task.Owner != null ? task.Owner.gameObject.name : "destroyed module";
                ZAMERTLogger.Error("Lua failed on " + where + ": " + ex.Message);
            }
        }
    }
}
