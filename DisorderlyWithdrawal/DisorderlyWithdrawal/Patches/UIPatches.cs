using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace DisorderlyWithdrawal.Patches {
    
    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost", "dZ.Zappo.MonthlyTechAdjustment" })]
    public static class SGCaptainsQuartersStatusScreen_RefreshData {
        public static void Postfix(SGCaptainsQuartersStatusScreen __instance, bool showMoraleChange,
            Transform ___SectionOneExpensesList, TextMeshProUGUI ___SectionOneExpensesField, 
            SimGameState ___simState) {

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            if (__instance == null || ___SectionOneExpensesList == null || ___SectionOneExpensesField == null || simGameState == null) {
                Mod.Log.Debug($"SGCQSS:RD - skipping");
                return;
            }

            // Determine the level of aerospace support
            Statistic aerospaceAssets = simGameState.CompanyStats.GetStatistic("AerospaceAssets");
            int aerospaceSupport = aerospaceAssets != null ? aerospaceAssets.Value<int>() : 0;                         

            if (aerospaceSupport == 0) {
                Mod.Log.Debug($"SGCQSS:RD - no aerospace support configured, skipping.");
                return; 
            }

            Mod.Log.Info($"SGCQSS:RD - Parsing current keys.");
            List<KeyValuePair<string, int>> currentKeys = GetCurrentKeys(___SectionOneExpensesList, ___simState);
            currentKeys.Sort(new ExpensesSorter());

            Mod.Log.Info($"SGCQSS:RD - Clearing items");
            ClearListLineItems(___SectionOneExpensesList, ___simState);

            int aerospaceCost = 0;
            switch (aerospaceSupport) {
                case 3:
                    currentKeys.Add(new KeyValuePair<string, int>($"Aerospace: Heavy Wing", Mod.Config.HeavyWingMonthlyCost));
                    break;
                case 2:
                    currentKeys.Add(new KeyValuePair<string, int>($"Aerospace: Medium Wing", Mod.Config.MediumWingMonthlyCost));
                    break;
                case 1:
                    currentKeys.Add(new KeyValuePair<string, int>($"Aerospace: Light Wing", Mod.Config.LightWingMonthlyCost));
                    break;
            }

            Mod.Log.Info($"SGCQSS:RD - Adding listLineItems");
            try {
                foreach (KeyValuePair<string, int> kvp in currentKeys) {
                    Mod.Log.Info($"SGCQSS:RD - Adding key:{kvp.Key} value:{kvp.Value}");
                    AddListLineItem(___SectionOneExpensesList, ___simState, kvp.Key, SimGameState.GetCBillString(kvp.Value));
                }
            } catch (Exception e) {
                Mod.Log.Info($"SGCQSS:RD - failed to add lineItemParts due to: {e.Message}");
            }

            // Update summary costs
            string rawSectionOneCosts = ___SectionOneExpensesField.text;
            string sectionOneCostsS = Regex.Replace(rawSectionOneCosts, @"[^\d]", "");
            int sectionOneCosts = int.Parse(sectionOneCostsS);
            Mod.Log.Debug($"SGCQSS:RD raw costs:{rawSectionOneCosts} costsS:{sectionOneCostsS} sectionOneCosts:{sectionOneCosts}");

            int newCosts = sectionOneCosts + aerospaceCost;
            Traverse setFieldT = Traverse.Create(__instance).Method("SetField", new object[] { typeof(TextMeshProUGUI), typeof(string) });
            setFieldT.GetValue(new object[] { ___SectionOneExpensesField, SimGameState.GetCBillString(newCosts) });
            Mod.Log.Debug($"SGCQSS:RD - updated ");

        }

        public static List<KeyValuePair<string, int>> GetCurrentKeys(Transform container, SimGameState sgs) {

            List<KeyValuePair<string, int>> currentKeys = new List<KeyValuePair<string, int>>();
            IEnumerator enumerator = container.GetEnumerator();
            try {
                while (enumerator.MoveNext()) {
                    object obj = enumerator.Current;
                    Transform transform = (Transform)obj;
                    SGKeyValueView component = transform.gameObject.GetComponent<SGKeyValueView>();

                    Mod.Log.Debug($"SGCQSS:RD - Reading key from component:{component.name}.");
                    Traverse keyT = Traverse.Create(component).Field("Key");
                    TextMeshProUGUI keyText = (TextMeshProUGUI)keyT.GetValue();
                    string key = keyText.text;
                    Mod.Log.Debug($"SGCQSS:RD - key found as: {key}");

                    Traverse valueT = Traverse.Create(component).Field("Value");
                    TextMeshProUGUI valueText = (TextMeshProUGUI)valueT.GetValue();
                    string valueS = valueText.text;
                    string digits = Regex.Replace(valueS, @"[^\d]", "");
                    Mod.Log.Debug($"SGCQSS:RD - rawValue:{valueS} digits:{digits}");
                    int value = Int32.Parse(digits);

                    Mod.Log.Debug($"SGCQSS:RD - found existing pair: {key} / {value}");
                    KeyValuePair<string, int> kvp = new KeyValuePair<string, int>(key, value);
                    currentKeys.Add(kvp);

                }
            } catch (Exception e) {
                Mod.Log.Info($"Failed to get key-value pairs: {e.Message}");
            }

            return currentKeys;
        }

        private static void AddListLineItem(Transform list, SimGameState sgs, string key, string value) {
            GameObject gameObject = sgs.DataManager.PooledInstantiate("uixPrfPanl_captainsQuarters_quarterlyReportLineItem-element",
                BattleTechResourceType.UIModulePrefabs, null, null, list);
            SGKeyValueView component = gameObject.GetComponent<SGKeyValueView>();
            gameObject.transform.localScale = Vector3.one;
            component.SetData(key, value);
        }

        private static void ClearListLineItems(Transform container, SimGameState sgs) {
            List<GameObject> list = new List<GameObject>();
            IEnumerator enumerator = container.GetEnumerator();
            try {
                while (enumerator.MoveNext()) {
                    object obj = enumerator.Current;
                    Transform transform = (Transform)obj;
                    list.Add(transform.gameObject);
                }
            } finally {
                IDisposable disposable;
                if ((disposable = (enumerator as IDisposable)) != null) {
                    disposable.Dispose();
                }
            }
            while (list.Count > 0) {
                GameObject gameObject = list[0];
                sgs.DataManager.PoolGameObject("uixPrfPanl_captainsQuarters_quarterlyReportLineItem-element", gameObject);
                list.Remove(gameObject);
            }
        }
    }

}
