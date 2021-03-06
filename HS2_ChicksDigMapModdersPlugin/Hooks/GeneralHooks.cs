﻿using ADV;
using ADV.Commands.EventCG;
using HarmonyLib;
using HS2;
using Illusion.Extensions;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static HS2.GlobalHS2Calc;

namespace HS2_ChicksDigMapModdersPlugin.Hooks
{
    public static class GeneralHooks
    {
        private static BepInEx.Logging.ManualLogSource Log = HS2ChicksDigMapModdersPlugin.Instance.Log;

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Setting), "Do")]
        public static Exception SettingDoFinalizer(ADV.EventCG.Data ___data, string[] ___args, Exception __exception, Setting __instance)
        {
            if (HS2ChicksDigMapModdersPlugin.EnableDebugLogging.Value)
            {
                Log.LogInfo($"Setting.Do {string.Join(",", ___args)}");
                Log.LogInfo($"Data {___data}");
                if (__exception != null)
                    Log.LogInfo($"Exception Thrown: {__exception.Message} {__exception.StackTrace} ");
            }

            if (___data == null)
            {
                GameObject dataGo = new GameObject("MapData", new Type[] { typeof(ADV.EventCG.Data)});
                GameObject camPosGo = new GameObject("camPos", new Type[] { typeof(ADV.EventCG.CameraData) });
                camPosGo.transform.parent = dataGo.transform;
                ___data = dataGo.GetComponent<ADV.EventCG.Data>();
                bool hasNo = int.TryParse(___args[2], out int no);
                if (hasNo)
                {
                    Transform transform = ___data.transform;
                    Transform transform2 = __instance.scenario.commandController.GetChara(no).backup.transform;
                    transform.position += transform2.position;
                    transform.rotation *= transform2.rotation;
                }

                ___data.camRoot = __instance.scenario.advScene.advCamera.transform;
                ___data.SetChaRoot(__instance.scenario.commandController.Character, __instance.scenario.commandController.Characters);
                ___data.Next(0, __instance.scenario.commandController.Characters);

                if (HS2ChicksDigMapModdersPlugin.EnableDebugLogging.Value)
                {
                    Log.LogInfo($"Created Data {___data}");                    
                }
            }
            return null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextScenario), "_RequestNextLine")]
        public static void TextScenarioRequestNextLine(TextScenario __instance, int ___currentLine)
        {
            if (HS2ChicksDigMapModdersPlugin.EnableDebugLogging.Value)
            {
                Command command = __instance.CommandPacks[___currentLine].Command;
                Log.LogInfo($"Executing Command Line: (CommandList Add - {command} {string.Join(",", __instance.CommandPacks[___currentLine].Args)}");
            }
        }

            [HarmonyPostfix, HarmonyPatch(typeof(LobbyMapSelectUI), "InitList")]
        public static void InitList(int _eventNo, LobbyMapSelectInfoScrollController ___scrollCtrl)
        {
            if (HS2ChicksDigMapModdersPlugin.EnableDebugLogging.Value)
            {
                Log.LogInfo(string.Format("Checking Map Visibility for Event {0}", _eventNo));
            }
            if (_eventNo == 24 && HS2ChicksDigMapModdersPlugin.UseModdedMapsForFirstTimeGirls.Value)
            {
                int[] array = Singleton<Game>.Instance.infoEventContentDic[_eventNo].meetingLocationMaps.AddRangeToArray(HS2ChicksDigMapModdersPlugin.ModdedMapIds());
                List<MapInfo.Param> maps = BaseMap.infoTable.Values.Where((MapInfo.Param map) => map.Draw != -1).ToList();
                array = GlobalHS2Calc.ExcludeAchievementMap(array);
                array = GlobalHS2Calc.ExcludeFursRoomAchievementMap(array);
                maps = GlobalHS2Calc.ExcludeAppendMap(maps);
                ___scrollCtrl.SelectInfoClear();
                ___scrollCtrl.Init(maps, array);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GlobalHS2Calc), "SelectMapID")]
        public static void SelectMapIDPostfix(CharaInfo _charaInfo, List<int> _useMap, HashSet<int> _used)
        {
            if (!HS2ChicksDigMapModdersPlugin.UseModdedMapsForSafeEvents.Value)
            {
                return;
            }
            else if (!HS2ChicksDigMapModdersPlugin.SafeEventCheck(_charaInfo.eventID) && !HS2ChicksDigMapModdersPlugin.UseModdedMapsForAllEvents.Value)
            {
                return;
            }

            if (HS2ChicksDigMapModdersPlugin.EnableDebugLogging.Value)
            {
                Log.LogInfo(string.Format("Before Selecting {0} Event {6} map {1}-{5} useMap {2} used {3} available {4}", _charaInfo.chaFile.parameter.fullname, _charaInfo.mapID, string.Join(" ", _useMap), string.Join(" ", _used), string.Join(" ", Singleton<Game>.Instance.infoEventContentDic[_charaInfo.eventID].goToFemaleMaps), NameForMapId(_charaInfo.mapID), _charaInfo.eventID));
            }

            int[] goToFemaleMaps = Singleton<Game>.Instance.infoEventContentDic[_charaInfo.eventID].goToFemaleMaps;
            goToFemaleMaps = ExcludeAchievementMap(goToFemaleMaps);
            List<int> list = new List<int>();
            if (!((IReadOnlyCollection<int>)(object)goToFemaleMaps).IsNullOrEmpty())
            {
                list = goToFemaleMaps.AddRangeToArray(HS2ChicksDigMapModdersPlugin.ModdedMapIds()).Except(_useMap).Shuffle().ToList();
            }
            bool flag = false;
            for (int i = 0; i < list.Count; i++)
            {
                int num = list[i];
                if (!_used.Contains(num))
                {
                    _used.Add(num);
                    _charaInfo.mapID = num;
                    flag = true;
                    break;
                }
            }
            if (!flag && list.Any())
            {
                _charaInfo.mapID = list.Shuffle().FirstOrDefault();
            }

            if (HS2ChicksDigMapModdersPlugin.EnableDebugLogging.Value)
            {
                Log.LogInfo(string.Format("After Selecting {0} Event {6} map {1}-{5} useMap {2} used {3} available {4}", _charaInfo.chaFile.parameter.fullname, _charaInfo.mapID, string.Join(" ", _useMap), string.Join(" ", _used), string.Join(" ", list), NameForMapId(_charaInfo.mapID), _charaInfo.eventID));
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GlobalHS2Calc), "SelectMapIDMultiple")]
        public static void SelectMapIDMultiplePostfix(List<CharaInfo> _charaInfos, List<int> _useMap, HashSet<int> _used)
        {
            for (int i = 0; i < _charaInfos.Count; i += 2)
            {
                if (!HS2ChicksDigMapModdersPlugin.UseModdedMapsForSafeEvents.Value)
                {
                    continue;
                }
                else if (!HS2ChicksDigMapModdersPlugin.SafeEventCheck(_charaInfos[i].eventID) && !HS2ChicksDigMapModdersPlugin.UseModdedMapsForAllEvents.Value)
                {
                    continue;
                }

                if (HS2ChicksDigMapModdersPlugin.EnableDebugLogging.Value)
                {
                    Log.LogInfo(string.Format("Before Selecting {0} Event {6} map {1}-{5} useMap {2} used {3} available {4}", _charaInfos[i].chaFile.parameter.fullname, _charaInfos[i].mapID, string.Join(" ", _useMap), string.Join(" ", _used), string.Join(" ", Singleton<Game>.Instance.infoEventContentDic[_charaInfos[i].eventID].goToFemaleMaps), NameForMapId(_charaInfos[i].mapID), _charaInfos[i].eventID));
                }

                int[] goToFemaleMaps = Singleton<Game>.Instance.infoEventContentDic[_charaInfos[i].eventID].goToFemaleMaps;
                goToFemaleMaps = ExcludeAchievementMap(goToFemaleMaps);
                List<int> list = new List<int>();
                if (!((IReadOnlyCollection<int>)(object)goToFemaleMaps).IsNullOrEmpty())
                {
                    list = goToFemaleMaps.AddRangeToArray(HS2ChicksDigMapModdersPlugin.ModdedMapIds()).Except(_useMap).Shuffle().ToList();
                }
                bool flag = false;
                for (int j = 0; j < list.Count; j++)
                {
                    int num = list[j];
                    if (!_used.Contains(num))
                    {
                        _used.Add(num);
                        _charaInfos[i].mapID = num;
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    _charaInfos[i].mapID = list.Shuffle().FirstOrDefault();
                }
                _charaInfos[i + 1].mapID = _charaInfos[i].mapID;

                if (HS2ChicksDigMapModdersPlugin.EnableDebugLogging.Value)
                {
                    Log.LogInfo(string.Format("After Selecting {0} Event {6} map {1}-{5} useMap {2} used {3} available {4}", _charaInfos[i].chaFile.parameter.fullname, _charaInfos[i].mapID, string.Join(" ", _useMap), string.Join(" ", _used), string.Join(" ", list), NameForMapId(_charaInfos[i].mapID), _charaInfos[i].eventID));
                }
            }

        }

        public static string NameForMapId(int id)
        {
            foreach (MapInfo.Param param in BaseMap.infoTable.Values)
            {
                if (param.No == id)
                {
                    return string.Join(":", param.MapNames);
                }
            }
            return null;
        }
    }
}
