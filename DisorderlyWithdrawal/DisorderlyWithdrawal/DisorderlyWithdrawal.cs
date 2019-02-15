using Harmony;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Reflection;

namespace DisorderlyWithdrawal {
    public class DisorderlyWithdrawal {

        public const string HarmonyPackage = "us.frostraptor.DisorderlyWithdrawal";

        public static Logger Logger;
        public static string ModDir;
        public static ModConfig Config;

        public static string CampaignSeed;

        public static readonly Random Random = new Random();

        public static void Init(string modDirectory, string settingsJSON) {
            ModDir = modDirectory;

            Exception settingsE;
            try {
                DisorderlyWithdrawal.Config = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
            } catch (Exception e) {
                settingsE = e;
                DisorderlyWithdrawal.Config = new ModConfig();
            }

            Logger = new Logger(modDirectory, "disorderly_withdrawal");

            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            Logger.Log($"Assembly version: {fvi.ProductVersion}");

            Logger.LogIfDebug($"ModDir is:{modDirectory}");
            Logger.LogIfDebug($"mod.json settings are:({settingsJSON})");
            Logger.Log($"mergedConfig is:{DisorderlyWithdrawal.Config}");

            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(asm);
        }
    }
}
