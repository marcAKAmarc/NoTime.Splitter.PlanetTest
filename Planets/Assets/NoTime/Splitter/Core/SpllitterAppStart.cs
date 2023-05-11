using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using static UnityEngine.PlayerLoop.FixedUpdate;

namespace Assets.NoTime.Splitter.Core
{
    public class SplitterPhysicsExport { }
    public class SplitterSimulate { }
    public class SplitterSync { }
    public static class SplitterSystem
    {
        public static event Action SplitterSimulate;
        public static event Action SplitterPhysicsExport;
        public static event Action SplitterSync;

        private static void OnSpllitterFixedUpdate() => SplitterSimulate?.Invoke();
        private static void OnSplitterPhysicsExport() => SplitterPhysicsExport?.Invoke();
        private static void OnSplitterSync() => SplitterSync?.Invoke();

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            var fixedUpdateSystems = PlayerLoop.GetCurrentPlayerLoop().subSystemList.Where(x => x.type.Name == "FixedUpdate").First();
            var currentSystems = PlayerLoop.GetCurrentPlayerLoop();
            var mySplitterPhysicsExport = new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = OnSplitterPhysicsExport,
                type = typeof(SplitterPhysicsExport)
            };

            var mySplitterSimulate = new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = OnSpllitterFixedUpdate,
                type = typeof(SplitterSimulate)
            };

            var mySplitterSync = new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = OnSplitterSync,
                type = typeof(SplitterSync)
            };

            var loop1 = AddSystem<ScriptRunBehaviourFixedUpdate>(in fixedUpdateSystems, mySplitterPhysicsExport);
            var loop2 = AddSystem<ScriptRunBehaviourFixedUpdate>(in loop1, mySplitterSimulate);
            var loop3 = AddSystem<DirectorFixedUpdatePostPhysics>(in loop2, mySplitterSync);
            var final = ReplaceSystem<FixedUpdate>(in currentSystems, loop3);
            PlayerLoop.SetPlayerLoop(final);

            printPlayerLoop();
        }

        private static PlayerLoopSystem ReplaceSystem<T>(in PlayerLoopSystem loopSystem, PlayerLoopSystem systemToAdd) where T : struct
        {
            PlayerLoopSystem newPlayerLoop = new()
            {
                loopConditionFunction = loopSystem.loopConditionFunction,
                type = loopSystem.type,
                updateDelegate = loopSystem.updateDelegate,
                updateFunction = loopSystem.updateFunction
            };

            List<PlayerLoopSystem> newSubSystemList = new();

            foreach (var subSystem in loopSystem.subSystemList)
            {
                if (subSystem.type == typeof(T))
                    newSubSystemList.Add(systemToAdd);
                else
                    newSubSystemList.Add(subSystem);
            }

            newPlayerLoop.subSystemList = newSubSystemList.ToArray();
            return newPlayerLoop;
        }

        private static PlayerLoopSystem AddSystem<T>(in PlayerLoopSystem loopSystem, PlayerLoopSystem systemToAdd) where T : struct
        {
            PlayerLoopSystem newPlayerLoop = new()
            {
                loopConditionFunction = loopSystem.loopConditionFunction,
                type = loopSystem.type,
                updateDelegate = loopSystem.updateDelegate,
                updateFunction = loopSystem.updateFunction
            };

            List<PlayerLoopSystem> newSubSystemList = new();

            foreach (var subSystem in loopSystem.subSystemList)
            {
                newSubSystemList.Add(subSystem);

                if (subSystem.type == typeof(T))
                    newSubSystemList.Add(systemToAdd);
            }

            newPlayerLoop.subSystemList = newSubSystemList.ToArray();
            return newPlayerLoop;
        }



        private static void printPlayerLoop()
        {
            var def = PlayerLoop.GetCurrentPlayerLoop();
            var sb = new StringBuilder();
            RecursivePlayerLoopPrint(def, sb, 0);
            Debug.Log(sb.ToString());
        }

        private static void RecursivePlayerLoopPrint(PlayerLoopSystem def, StringBuilder sb, int depth)
        {
            if (depth == 0)
            {
                sb.AppendLine("ROOT NODE");
            }
            else if (def.type != null)
            {
                for (int i = 0; i < depth; i++)
                {
                    sb.Append("\t");
                }
                sb.AppendLine(def.type.Name);
            }
            if (def.subSystemList != null)
            {
                depth++;
                foreach (var s in def.subSystemList)
                {
                    RecursivePlayerLoopPrint(s, sb, depth);
                }
                depth--;
            }
        }
    }
}


