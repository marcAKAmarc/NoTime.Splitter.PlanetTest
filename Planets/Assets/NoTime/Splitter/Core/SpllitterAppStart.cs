using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using static UnityEngine.PlayerLoop.FixedUpdate;
using static UnityEngine.PlayerLoop.PreUpdate;
using static UnityEngine.PlayerLoop.Update;

namespace NoTime.Splitter.Core
{
    public class SplitterPhysicsExport { }
    public class SplitterSimulate { }
    public class SplitterPhysicsSync { }
    public class SplitterHardSync { }

    public class SplitterInvestigate { }
    public static class SplitterSystem
    {
        public static event Action SplitterSimulate;
        public static event Action SplitterPhysicsExport;
        public static event Action SplitterPhysicsSync;
        public static event Action SplitterHardSync;
        public static event Action<string> InvestigatoryEvents;

        private static void OnSpllitterSimulate() => SplitterSimulate?.Invoke();
        private static void OnSplitterPhysicsExport() => SplitterPhysicsExport?.Invoke();
        private static void OnSplitterPhysicsSync() => SplitterPhysicsSync?.Invoke();
        private static void OnSplitterHardSync() => SplitterHardSync?.Invoke();
        private static void OnInvestigatoryEvents(string message) => InvestigatoryEvents?.Invoke(message);


        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            var fixedUpdateSystems = PlayerLoop.GetCurrentPlayerLoop().subSystemList.Where(x => x.type.Name == "FixedUpdate").First();
            var preUpdateSystem = PlayerLoop.GetCurrentPlayerLoop().subSystemList.Where(x => x.type.Name == "PreUpdate").First();
            var updateSystem = PlayerLoop.GetCurrentPlayerLoop().subSystemList.Where(x => x.type.Name == "Update").First();
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
                updateDelegate = OnSpllitterSimulate,
                type = typeof(SplitterSimulate)
            };

            var mySplitterPhysicsSync = new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = OnSplitterPhysicsSync,
                type = typeof(SplitterPhysicsSync)
            };

            var mySplitterHardSync = new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = OnSplitterHardSync,
                type = typeof(SplitterHardSync)
            };

            fixedUpdateSystems = AddSystemAfter<ScriptRunBehaviourFixedUpdate>(in fixedUpdateSystems, mySplitterPhysicsExport);
            fixedUpdateSystems = AddSystemAfter<ScriptRunBehaviourFixedUpdate>(in fixedUpdateSystems, mySplitterSimulate);
            
            fixedUpdateSystems = AddSystemAfter<PhysicsFixedUpdate>(in fixedUpdateSystems, mySplitterPhysicsSync);
            currentSystems = ReplaceSystem<FixedUpdate>(in currentSystems, fixedUpdateSystems);
            //var hardsync = AddSystemBefore<ScriptRunBehaviourUpdate>(in updateSystem, mySplitterHardSync);
            //currentSystems = ReplaceSystem<Update>(in currentSystems, hardsync);
            PlayerLoop.SetPlayerLoop(currentSystems);

            InitInvestigation();

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
        private static PlayerLoopSystem ReplaceSystem(in PlayerLoopSystem loopSystem, PlayerLoopSystem systemToAdd, int index)
        {
            PlayerLoopSystem newPlayerLoop = new()
            {
                loopConditionFunction = loopSystem.loopConditionFunction,
                type = loopSystem.type,
                updateDelegate = loopSystem.updateDelegate,
                updateFunction = loopSystem.updateFunction
            };

            List<PlayerLoopSystem> newSubSystemList = new();

            for(int i = 0; i < loopSystem.subSystemList.Length; i++) 
            {
                if(i != index)
                    newSubSystemList.Add(loopSystem.subSystemList[i]);
                else
                    newSubSystemList.Add(systemToAdd);
            }

            newPlayerLoop.subSystemList = newSubSystemList.ToArray();
            return newPlayerLoop;
        }
        private static PlayerLoopSystem AddSystemAfter<T>(in PlayerLoopSystem loopSystem, PlayerLoopSystem systemToAdd) where T : struct
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

        private static int _cnt = 0;
        private static PlayerLoopSystem AddSystemAfter(in PlayerLoopSystem loopSystem, PlayerLoopSystem systemToAdd, int insertAfter) 
        {
            _cnt = 0;
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

                if (_cnt == insertAfter)
                    newSubSystemList.Add(systemToAdd);

                _cnt += 1;
            }

            newPlayerLoop.subSystemList = newSubSystemList.ToArray();
            return newPlayerLoop;
        }
        private static PlayerLoopSystem AddSystemBefore<T>(in PlayerLoopSystem loopSystem, PlayerLoopSystem systemToAdd) where T : struct
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
                newSubSystemList.Add(subSystem);
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
                if (def.updateDelegate != null)
                    sb.AppendLine(def.updateDelegate.ToString());
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

        private static int _skipAmt;
        private static void InitInvestigation()
        {
            var currentLoop = PlayerLoop.GetCurrentPlayerLoop();
            var preupdate = currentLoop.subSystemList.Where(x => x.type.Name == "PreUpdate").First();
            for (int p = 0; p < currentLoop.subSystemList.Length; p++) {
                var parentSubSystem = currentLoop.subSystemList[p];
                _skipAmt = 0;
                for (int i = 0; i + _skipAmt < parentSubSystem.subSystemList.Length; i++)
                {

                    //if (parentSubSystem.subSystemList[i + _skipAmt].updateDelegate == null)
                    if(true)
                    {
                        int currentParentIndex = p;
                        int currentIndex = i + _skipAmt;
                        var myInvestigatoryEvent = new PlayerLoopSystem
                        {
                            subSystemList = null,
                            updateDelegate = () => OnInvestigatoryEvents(currentLoop.subSystemList[currentParentIndex].type.Name + " - "  + parentSubSystem.subSystemList[currentIndex].type.Name),
                            type = typeof(SplitterInvestigate)
                        };

                        parentSubSystem = AddSystemAfter(in parentSubSystem, myInvestigatoryEvent, i + _skipAmt);

                        _skipAmt += 1;
                    }

                }
                currentLoop = ReplaceSystem(in currentLoop, parentSubSystem, p);
            }
            
            PlayerLoop.SetPlayerLoop(currentLoop);
        }
    }
}
