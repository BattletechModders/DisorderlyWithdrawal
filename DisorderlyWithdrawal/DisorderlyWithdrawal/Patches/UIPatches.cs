using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace DisorderlyWithdrawal.Patches {

    [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
    [HarmonyPatch(new Type[] { typeof(EconomyScale), typeof(bool) })]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost", "us.frostraptor.IttyBittyLivingSpace" })]
    public static class SimGameState_GetExpenditures {
        public static void Postfix(SimGameState __instance, ref int __result, EconomyScale expenditureLevel, bool proRate) {
            Mod.Log.Trace($"SGS:GE entered with {__result}");

            Statistic aerospaceAssets = __instance.CompanyStats.GetStatistic("AerospaceAssets");
            int aerospaceSupport = aerospaceAssets != null ? aerospaceAssets.Value<int>() : 0;

            switch (aerospaceSupport) {
                case 3:
                    __result = __result + Mod.Config.HeavyWingMonthlyCost;
                    Mod.Log.Trace($"Charging player for a heavy wing, result = {__result}.");
                    break;
                case 2:
                    __result = __result + Mod.Config.MediumWingMonthlyCost;
                    Mod.Log.Trace($"Charging player for a medium wing, result = {__result}.");
                    break;
                case 1:
                    __result = __result + Mod.Config.LightWingMonthlyCost;
                    Mod.Log.Trace($"Charging player for a light wing, result = {__result}.");
                    break;
                default:
                    Mod.Log.Trace($"Charging player for no aerospace, result = {__result}");
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    [HarmonyAfter(new string[] { "dZ.Zappo.MonthlyTechAdjustment", "us.frostraptor.IttyBittyLivingSpace", "us.frostraptor.IttyBittyLivingSpace" })]
    public static class SGCaptainsQuartersStatusScreen_RefreshData {
        public static void Postfix(SGCaptainsQuartersStatusScreen __instance, EconomyScale expenditureLevel, bool showMoraleChange,
            Transform ___SectionOneExpensesList, TextMeshProUGUI ___SectionOneExpensesField, 
            SimGameState ___simState) {

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            if (__instance == null || ___SectionOneExpensesList == null || ___SectionOneExpensesField == null || simGameState == null) {
                Mod.Log.Debug($"SGCQSS:RD - skipping");
                return;
            }

            // TODO: Add this to mech parts maybe?
            //float expenditureCostModifier = simGameState.GetExpenditureCostModifier(expenditureLevel);

            // Determine the level of aerospace support
            Statistic aerospaceAssets = simGameState.CompanyStats.GetStatistic("AerospaceAssets");
            int aerospaceSupport = aerospaceAssets != null ? aerospaceAssets.Value<int>() : 0;

            if (aerospaceSupport == 0) {
                Mod.Log.Debug($"SGCQSS:RD - no aerospace support configured, skipping.");
                return;
            }

            Mod.Log.Info($"SGCQSS:RD - entered. Parsing current keys.");
            List<KeyValuePair<string, int>> currentKeys = GetCurrentKeys(___SectionOneExpensesList, ___simState);
            int aerospaceCost = 0;
            switch (aerospaceSupport) {
                case 3:
                    aerospaceCost = Mod.Config.HeavyWingMonthlyCost;
                    currentKeys.Add(new KeyValuePair<string, int>($"Aerospace: Heavy Wing", Mod.Config.HeavyWingMonthlyCost));
                    break;
                case 2:
                    aerospaceCost = Mod.Config.MediumWingMonthlyCost;
                    currentKeys.Add(new KeyValuePair<string, int>($"Aerospace: Medium Wing", Mod.Config.MediumWingMonthlyCost));
                    break;
                case 1:
                    aerospaceCost = Mod.Config.LightWingMonthlyCost;
                    currentKeys.Add(new KeyValuePair<string, int>($"Aerospace: Light Wing", Mod.Config.LightWingMonthlyCost));
                    break;
            }
            currentKeys.Sort(new ExpensesSorter());

            Mod.Log.Info($"SGCQSS:RD - Clearing items");
            ClearListLineItems(___SectionOneExpensesList, ___simState);

            Mod.Log.Info($"SGCQSS:RD - Adding listLineItems");
            int totalCost = 0;
            try {
                foreach (KeyValuePair<string, int> kvp in currentKeys) {
                    Mod.Log.Info($"SGCQSS:RD - Adding key:{kvp.Key} value:{kvp.Value}");
                    totalCost += kvp.Value;
                    AddListLineItem(___SectionOneExpensesList, ___simState, kvp.Key, SimGameState.GetCBillString(kvp.Value));
                }
            } catch (Exception e) {
                Mod.Log.Info($"SGCQSS:RD - failed to add lineItemParts due to: {e.Message}");
            }

            // Update summary costs
            int newCosts = totalCost;
            string newCostsS = SimGameState.GetCBillString(newCosts);
            Mod.Log.Debug($"SGCQSS:RD - total:{newCosts}");

            try {
                ___SectionOneExpensesField.SetText(SimGameState.GetCBillString(newCosts));
                Mod.Log.Debug($"SGCQSS:RD - updated ");
            } catch (Exception e) {
                Mod.Log.Info($"SGCQSS:RD - failed to update summary costs section due to: {e.Message}");
            }
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

    [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
    [HarmonyAfter(new string[] { "de.morphyum.DropCostPerMech" })]
    public static class AAR_ContractObjectivesWidget_FillInObjectives {

        static void Prefix(AAR_ContractObjectivesWidget __instance, Contract ___theContract) {
            int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * Mod.Config.LeopardRepairCostPerDamage;
            if (repairCost != 0) {
                Mod.Log.Debug($"AAR_COW:FIO adding repair cost objective:{repairCost}");
                string objectiveLabel = $"LEOPARD REPAIR COSTS: {SimGameState.GetCBillString(repairCost)}";
                MissionObjectiveResult missionObjectiveResult = new MissionObjectiveResult(objectiveLabel, "7facf07a-626d-4a3b-a1ec-b29a35ff1ac0", false, true, ObjectiveStatus.Succeeded, false);
                ___theContract.MissionObjectiveResultList.Add(missionObjectiveResult);
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    [HarmonyAfter(new string[] { "de.morphyum.DropCostPerMech", "de.morphyum.PersistentMapClient" })]
    public static class Contract_CompleteContract {

        static void Postfix(Contract __instance) {
            int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * Mod.Config.LeopardRepairCostPerDamage;
            if (repairCost != 0) {
                Mod.Log.Debug($"C:CC adding repair costs:{repairCost}");
                int newMoneyResults = Mathf.FloorToInt(__instance.MoneyResults - repairCost);
                Traverse traverse = Traverse.Create(__instance).Property("MoneyResults");
                traverse.SetValue(newMoneyResults);
            }
        }
    }

}
