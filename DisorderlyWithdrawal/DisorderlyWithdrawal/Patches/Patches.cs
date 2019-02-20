using BattleTech;
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
                // TODO: TESTING - REMOVE
                roundsToWait = 1;

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

    [HarmonyPatch(typeof(AAR_ContractResults_Screen), "FillInData")]
    public static class AAR_ContractResults_Screen_FillInData {

        public static void Postfix(AAR_ContractResults_Screen __instance) {
            if (ModState.CombatDamage != 0) {
                int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * DisorderlyWithdrawal.ModConfig.LeopardRepairCostPerDamage;
                GenericPopupBuilder.Create(GenericPopupType.Info,
                    $"The Leopard was damaged in the extraction process, and needs to be repaired." +
                    $"{SimGameState.GetCBillString(repairCost)} will be deducted for repairs."
                    )
                    .AddButton("Continue", new Action(OnContinue), true, null)
                    .Render();
            }
        }

        public static void OnContinue() {
            int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * DisorderlyWithdrawal.ModConfig.LeopardRepairCostPerDamage;
            DisorderlyWithdrawal.Logger.LogIfDebug($"Charging player {SimGameState.GetCBillString(repairCost)} for Leopard repairs.");

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            simGameState.AddFunds(repairCost * -1);

            ModState.CombatDamage = 0;
        }

    }

    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost", "dZ.Zappo.MonthlyTechAdjustment" })]
    public static class SGCaptainsQuartersStatusScreen_RefreshData {
        public static void Postfix(SGCaptainsQuartersStatusScreen __instance, bool showMoraleChange, Transform ___SectionTwoExpensesList) {

        }
    }
}
