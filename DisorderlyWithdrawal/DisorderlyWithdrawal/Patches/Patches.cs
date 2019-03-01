using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using System;
using TMPro;
using UnityEngine;

namespace DisorderlyWithdrawal.Patches {
    
    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatButtonPressed")]
    public static class CombatHUDRetreatESCMenu_OnRetreatButtonPressed {
        public static bool Prefix(CombatHUDRetreatEscMenu __instance, HBSDOTweenButton ___RetreatButton, CombatGameState ___Combat, CombatHUD ___HUD) {
            DisorderlyWithdrawal.Logger.LogIfDebug($"CHUDREM:ORBP - triggered:{ModState.WithdrawalTriggered} " +
                $"tillOverhead:{ModState.RoundsUntilOverhead} tillReady:{ModState.RoundsUntilReady}");

            if (ModState.WithdrawalTriggered && ModState.RoundsUntilOverhead == 0) {
                DisorderlyWithdrawal.Logger.LogIfDebug($"CHUDREM:ORBP - Checking for combat damage and active enemies");
                ModState.CombatDamage = Helper.CalculateCombatDamage();
                if (ModState.CombatDamage > 0 && ___Combat.TurnDirector.DoAnyUnitsHaveContactWithEnemy) {
                    int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * DisorderlyWithdrawal.ModConfig.LeopardRepairCostPerDamage;
                    void withdrawAction() { OnImmediateWithdraw(__instance.IsGoodFaithEffort()); }
                    GenericPopupBuilder builder = GenericPopupBuilder.Create(GenericPopupType.Warning,
                        $"Enemies are within weapons range! If you withdraw now the Leopard will take {SimGameState.GetCBillString(repairCost)} worth of damage.")
                        .CancelOnEscape()
                        .AddButton("Cancel")
                        .AddButton("Withdraw", withdrawAction, true, null);
                    builder.IsNestedPopupWithBuiltInFade = true;
                    ___HUD.SelectionHandler.GenericPopup = builder.Render();
                    return false;
                } else {
                    OnImmediateWithdraw(__instance.IsGoodFaithEffort());
                    return false;
                }
            } else {
                return true;
            }
        }

        public static void OnImmediateWithdraw(bool isGoodFaith) {
            DisorderlyWithdrawal.Logger.LogIfDebug($"CHUDREM:ORBP:OnImmediateWithdraw - {isGoodFaith}");
            CombatGameState Combat = UnityGameInstance.BattleTechGame.Combat;
            MissionRetreatMessage message = new MissionRetreatMessage(isGoodFaith);
            Combat.MessageCenter.PublishMessage(message);
            ModState.HUD.SelectionHandler.GenericPopup = null;
        }
    }

    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatConfirmed")]
    public static class CombatHUDRetreatESCMenu_OnRetreatConfirmed {
        public static bool Prefix(CombatHUDRetreatEscMenu __instance, CombatGameState ___Combat, CombatHUD ___HUD) {
            DisorderlyWithdrawal.Logger.LogIfDebug("CHUDREM:ORC entered");

            if (___Combat == null || ___Combat.ActiveContract.IsArenaSkirmish) {
                return true;
            } else {

                int roundsToWait = Helper.RoundsToWaitByAerospace();

                DisorderlyWithdrawal.Logger.Log($"Player must wait:{roundsToWait} rounds for pickup."); 

                if (roundsToWait > 0) {
                    ModState.RoundsUntilOverhead = roundsToWait;
                    ModState.WithdrawalTriggered = true;
                    ModState.RetreatButton = __instance.RetreatButton;
                    ModState.HUD = ___HUD;

                    Transform textT = __instance.RetreatButton.gameObject.transform.Find("Text");
                    if (textT != null) {
                        GameObject textGO = textT.gameObject;
                        ModState.RetreatButtonText = textGO.GetComponent<TextMeshProUGUI>();
                    }

                    //ModState.RetreatButton.SetState(ButtonState.Disabled, true);
                    ModState.RetreatButtonText.SetText($"In { roundsToWait } Rounds", new object[] { });

                    GenericPopupBuilder genericPopupBuilder =
                        GenericPopupBuilder.Create(GenericPopupType.Info,
                            $"Sumire is inbound and will be overhead in {roundsToWait} rounds. Survive until then!")
                            .AddButton("Continue", new Action(OnContinue), true, null);
                    genericPopupBuilder.IsNestedPopupWithBuiltInFade = true;
                    ___HUD.SelectionHandler.GenericPopup = genericPopupBuilder.Render();

                    return false;
                } else {
                    return true;
                }
            }
        }

        public static void OnContinue() {
            DisorderlyWithdrawal.Logger.LogIfDebug($"CHUDREM:ORC OnContinue");
            ModState.HUD.SelectionHandler.GenericPopup = null;
        }
    }


    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "Update")]
    public static class CombatHUDRetreatESCMenu_Update {
        public static void Postfix(CombatHUDRetreatEscMenu __instance, CombatGameState ___Combat, CombatHUD ___HUD, bool ___isArena) {
            //DisorderlyWithdrawal.Logger.LogIfDebug($"CHUDREM:U entered - isArena:{___isArena} untilOverhead:{ModState.RoundsUntilOverhead} untilReady:{ModState.RoundsUntilReady}");

            if (!___isArena) {
                if (ModState.RoundsUntilOverhead > 0) {
                    //DisorderlyWithdrawal.Logger.LogIfDebug($"CHUDREM:U - wait til overhead - untilOverhead:{ModState.RoundsUntilOverhead} untilReady:{ModState.RoundsUntilReady}");
                    __instance.RetreatButton.SetState(ButtonState.Disabled, false);
                    ModState.RetreatButtonText.fontSize = 24;
                    ModState.RetreatButtonText.color = Color.white;
                    ModState.RetreatButtonText.SetText($"In { ModState.RoundsUntilOverhead } Rounds", new object[] { });
                } else if (ModState.RoundsUntilOverhead == 0) {
                    //DisorderlyWithdrawal.Logger.LogIfDebug($"CHUDREM:U - withdraw turn - untilOverhead:{ModState.RoundsUntilOverhead} untilReady:{ModState.RoundsUntilReady}");
                    if (__instance.RetreatButton.BaseState != ButtonState.Enabled) {
                        __instance.RetreatButton.SetState(ButtonState.Enabled, false);
                    }
                    ModState.RetreatButtonText.fontSize = 24;
                    ModState.RetreatButtonText.color = Color.white;
                    ModState.RetreatButtonText.SetText($"Withdraw", new object[] { });
                } else if (ModState.RoundsUntilReady > 0) {
                    //DisorderlyWithdrawal.Logger.LogIfDebug($"CHUDREM:U - wait til ready - untilOverhead:{ModState.RoundsUntilOverhead} untilReady:{ModState.RoundsUntilReady}");
                    __instance.RetreatButton.SetState(ButtonState.Disabled, false);
                    ModState.RetreatButtonText.fontSize = 24;
                    ModState.RetreatButtonText.color = Color.white;
                    ModState.RetreatButtonText.SetText($"In { ModState.RoundsUntilReady } Rounds", new object[] { });
                }
            }
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "BeginNewRound")]
    public static class TurnDirector_BeginNewRound {
        static void Postfix(TurnDirector __instance) {
            DisorderlyWithdrawal.Logger.LogIfDebug($"TD:BNR entered - withdrawalTrigger:{ModState.WithdrawalTriggered} roundsUntilWithdrawal:{ModState.RoundsUntilOverhead}");

            if (ModState.WithdrawalTriggered) {
                if (ModState.RoundsUntilOverhead == 0) {
                    ModState.RoundsUntilReady = Helper.RoundsToWaitByAerospace();

                    GenericPopupBuilder genericPopupBuilder =
                        GenericPopupBuilder.Create(GenericPopupType.Info,
                        $"Sumire is overhead. If you don't withdraw on this round, she will retreat. She will be unavailable for another {ModState.RoundsUntilReady} rounds!")
                        .AddButton("Continue", new Action(OnContinue), true, null);
                    genericPopupBuilder.IsNestedPopupWithBuiltInFade = true;
                    ModState.HUD.SelectionHandler.GenericPopup = genericPopupBuilder.Render();

                    ModState.RetreatButton.SetState(ButtonState.Enabled, true);
                    ModState.RetreatButtonText.SetText($"Withdraw Now", new object[] { });

                } else if (ModState.RoundsUntilReady == 0) {
                    ModState.WithdrawalTriggered = false;
                    ModState.RoundsUntilOverhead = -1;
                    ModState.RoundsUntilReady = -1;

                    ModState.RetreatButton.SetState(ButtonState.Enabled, false);
                    ModState.RetreatButtonText.SetText($"Withdraw", new object[] { });
                }
            }
        }

        public static void OnContinue() {
            DisorderlyWithdrawal.Logger.LogIfDebug($"TD:BNR OnContinue");
            ModState.HUD.SelectionHandler.GenericPopup = null;
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "EndCurrentRound")]
    public static class TurnDirector_EndCurrentRound {
        static void Postfix(TurnDirector __instance) {
            DisorderlyWithdrawal.Logger.LogIfDebug($"TD:ECR entered - withdrawalTrigger:{ModState.WithdrawalTriggered} roundsUntilWithdrawal:{ModState.RoundsUntilOverhead}");

            if (ModState.WithdrawalTriggered) {
                if (ModState.RoundsUntilOverhead == 0) {
                    // Player didn't withdraw. Reset the button and prevent them from restarting for N turns
                    ModState.WithdrawalTriggered = false;
                    ModState.RoundsUntilOverhead = -1;
                } else if (ModState.RoundsUntilOverhead > 0) {
                    ModState.RoundsUntilOverhead = ModState.RoundsUntilOverhead - 1;
                } else if (ModState.RoundsUntilReady > 0) {
                    ModState.RoundsUntilReady = ModState.RoundsUntilReady - 1;
                } 
            }
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "OnCombatGameDestroyed")]
    public static class TurnDirector_OnCombatGameDestroyed {
        static void Postfix(TurnDirector __instance) {
            DisorderlyWithdrawal.Logger.LogIfDebug("TD:OCGD entered");

            ModState.WithdrawalTriggered = false;
            ModState.RoundsUntilOverhead = 0;
            ModState.RoundsUntilReady = 0;
            ModState.RetreatButton = null;
            ModState.RetreatButtonText = null;
            ModState.HUD = null;
            // DO NOT OVERWRITE CombatDamage!
        }
    }

    [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost" })]
    public static class SimGameState_GetExpenditures {
        public static void Postfix(SimGameState __instance, ref int __result, bool proRate) {
            DisorderlyWithdrawal.Logger.LogIfDebug($"SGS:GE entered with {__result}");

            Statistic aerospaceAssets = __instance.CompanyStats.GetStatistic("AerospaceAssets");
            int aerospaceSupport = aerospaceAssets != null ? aerospaceAssets.Value<int>() : 0;
            
            switch (aerospaceSupport) {
                case 3:
                    __result = __result + DisorderlyWithdrawal.ModConfig.HeavyWingMonthlyCost;
                    DisorderlyWithdrawal.Logger.LogIfDebug($"Charging player for a heavy wing, result = {__result}.");
                    break;
                case 2:
                    __result = __result + DisorderlyWithdrawal.ModConfig.MediumWingMonthlyCost;
                    DisorderlyWithdrawal.Logger.LogIfDebug($"Charging player for a medium wing, result = {__result}.");
                    break;
                case 1:
                    __result = __result + DisorderlyWithdrawal.ModConfig.LightWingMonthlyCost;
                    DisorderlyWithdrawal.Logger.LogIfDebug($"Charging player for a light wing, result = {__result}.");
                    break;
                default:
                    DisorderlyWithdrawal.Logger.LogIfDebug($"Charging player for no aerospace, result = {__result}");
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
    [HarmonyAfter(new string[] { "de.morphyum.DropCostPerMech" })]
    public static class AAR_ContractObjectivesWidget_FillInObjectives {

        static void Prefix(AAR_ContractObjectivesWidget __instance, Contract ___theContract) {
            int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * DisorderlyWithdrawal.ModConfig.LeopardRepairCostPerDamage;
            if (repairCost != 0) {
                DisorderlyWithdrawal.Logger.LogIfDebug($"AAR_COW:FIO adding repair cost objective:{repairCost}");
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
            int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * DisorderlyWithdrawal.ModConfig.LeopardRepairCostPerDamage;
            if (repairCost != 0) {
                DisorderlyWithdrawal.Logger.LogIfDebug($"C:CC adding repair costs:{repairCost}");
                int newMoneyResults = Mathf.FloorToInt(__instance.MoneyResults - repairCost);
                Traverse traverse = Traverse.Create(__instance).Property("MoneyResults");
                traverse.SetValue(newMoneyResults);
            }
        }
    }

    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost", "dZ.Zappo.MonthlyTechAdjustment" })]
    public static class SGCaptainsQuartersStatusScreen_RefreshData {
        public static void Postfix(SGCaptainsQuartersStatusScreen __instance, bool showMoraleChange,
            Transform ___SectionOneExpensesList, TextMeshProUGUI ___SectionOneExpensesField) {

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            if (__instance == null || ___SectionOneExpensesList == null || ___SectionOneExpensesField == null || simGameState == null) {
                DisorderlyWithdrawal.Logger.LogIfDebug($"SGCQSS:RD - skipping");
                return;
            } else {
                DisorderlyWithdrawal.Logger.LogIfDebug($"SGCQSS:RD - entered");
            }

            // Set the aerospace cost
            Statistic aerospaceAssets = simGameState.CompanyStats.GetStatistic("AerospaceAssets");
            int aerospaceSupport = aerospaceAssets != null ? aerospaceAssets.Value<int>() : 0;
            int aerospaceCost = 0;
            switch (aerospaceSupport) {
                case 3:
                    aerospaceCost = DisorderlyWithdrawal.ModConfig.HeavyWingMonthlyCost;
                    break;
                case 2:
                    aerospaceCost = DisorderlyWithdrawal.ModConfig.MediumWingMonthlyCost;
                    break;
                case 1:
                    aerospaceCost = DisorderlyWithdrawal.ModConfig.LightWingMonthlyCost;
                    break;
                default:
                    aerospaceCost = 0;
                    break;
            }
            string aerospaceCostS = SimGameState.GetCBillString(aerospaceCost);
            DisorderlyWithdrawal.Logger.LogIfDebug($"SGCQSS:RD - aerospace cost is: {aerospaceCostS}");

            try {
                Traverse addListLineItemT = Traverse.Create(__instance).Method("AddListLineItem");
                addListLineItemT.GetValue(new object[] { ___SectionOneExpensesList, "Aerospace", aerospaceCostS });
                DisorderlyWithdrawal.Logger.LogIfDebug($"SGCQSS:RD - added lineItemParts");
            } catch (Exception e) {
                DisorderlyWithdrawal.Logger.LogIfDebug($"SGCQSS:RD - failed to add lineItemParts due to: {e.Message}");
            }

            string rawSectionOneCosts = ___SectionOneExpensesField.text;
            string sectionOneCostsS = rawSectionOneCosts.Substring(1);
            int sectionOneCosts = int.Parse(sectionOneCostsS);
            DisorderlyWithdrawal.Logger.LogIfDebug($"SGCQSS:RD raw costs:{rawSectionOneCosts} costsS:{sectionOneCostsS} sectionOneCosts:{sectionOneCosts}");

            int newCosts = sectionOneCosts + aerospaceCost;
            Traverse setFieldT = Traverse.Create(__instance).Method("SetField", new object[] { typeof(TextMeshProUGUI), typeof(string) });
            setFieldT.GetValue(new object[] { ___SectionOneExpensesField, SimGameState.GetCBillString(newCosts) });
            DisorderlyWithdrawal.Logger.LogIfDebug($"SGCQSS:RD - updated ");

        }
    }
}
