using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HS2_ChicksDigMapModdersPlugin.Hooks;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HS2_ChicksDigMapModdersPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("HoneySelect2.exe")]
    public class HS2ChicksDigMapModdersPlugin : BaseUnityPlugin
    {

        public const string GUID = "orange.spork.hs2chicksdigmapmoddersplugin";
        public const string PluginName = "HS2ChicksDigMapModdersPlugin";
        public const string Version = "1.0.0";

        public static ConfigEntry<bool> UseModdedMapsForSafeEvents { get; set; }
        public static ConfigEntry<bool> UseModdedMapsForAllEvents { get; set; }
        public static ConfigEntry<bool> UseModdedMapsForFirstTimeGirls { get; set; }
        public static ConfigEntry<bool> EnableDebugLogging { get; set; }


        public static HS2ChicksDigMapModdersPlugin Instance { get; set; }

        internal BepInEx.Logging.ManualLogSource Log => Logger;

        public HS2ChicksDigMapModdersPlugin()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Singleton Only.");
            }

            Instance = this;

            UseModdedMapsForSafeEvents = Config.Bind("Options", "Use Modded Maps For Safe Events", true, new ConfigDescription("Girls Will Show Up In Modded Maps for Standard Events"));
            UseModdedMapsForFirstTimeGirls = Config.Bind("Options", "Use Modded Maps For First Time girls", true, new ConfigDescription("Modded Maps are Selectable From Lobby for a First Meeting"));
            UseModdedMapsForAllEvents = Config.Bind("Options", "Use Modded Maps For All Events", false, new ConfigDescription("Not Recommended...Really Not Recommended, Crashes Possible, Weirdity Guaranteed"));
            EnableDebugLogging = Config.Bind("Options", "Enable Debug Logging", false, new ConfigDescription("Waste HD Space"));

            PatchMe();
        }

        public void PatchMe()
        {
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(GeneralHooks));
        }

        public static int[] ModdedMapIds()
        {
            List<int> moddedMapIds = new List<int>();
            foreach (MapInfo.Param param in BaseMap.infoTable.Values)
            {
                if (param.No > 10000)
                {
                    moddedMapIds.Add(param.No);
                }
            }
            return moddedMapIds.ToArray();
        }

        public static int[] SafeEvents = new int[] { 0, 1, 2, 19, 21, 22, 23, 24, 25, 26, 33 };
        public static bool SafeEventCheck(int eventId)
        {
            return SafeEvents.Contains(eventId);
        }

        public static void DumpEventInfo()
        {
            foreach (EventContentInfoData.Param eventParam in Singleton<Game>.Instance.infoEventContentDic.Values)
            {
                HS2ChicksDigMapModdersPlugin.Instance.Log.LogInfo(string.Format("Event {0} Name {1} Goto [{2}] Meeting [{3}]", eventParam.id, string.Join(":", eventParam.eventNames), string.Join(",", eventParam.goToFemaleMaps), string.Join(",", eventParam.meetingLocationMaps)));
            }
        }

        public static void DumpMapInfo()
        {
            foreach (MapInfo.Param param in BaseMap.infoTable.Values)
            {
                HS2ChicksDigMapModdersPlugin.Instance.Log.LogInfo(string.Format("Map {0} Name {1}", param.No, string.Join(":", param.MapNames)));
            }
        }

    }
}
