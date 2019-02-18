using BattleTech;
using BattleTech.UI;
using Harmony;
using System.Reflection;
using TMPro;

namespace DisorderlyWithdrawal.Patches {
    
    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatButtonPressed")]
    public static class CombatHUDRetreatESCMenu_OnRetreatButtonPressed {
        public static bool Prefix(CombatHUDRetreatEscMenu __instance, HBSDOTweenButton ___RetreatButton, CombatGameState ___Combat, CombatHUD ___HUD) {
            DisorderlyWithdrawal.Logger.LogIfDebug("CHUDREM:ORBP entered");

            if (ModState.WithdrawalTriggered && ModState.RoundsUntilOverhead == 0) {

                // Check for combat damage
                float combatDamage = Helper.CalculateCombatDamage();
                if (combatDamage > 0) {
                    GenericPopupBuilder builder = GenericPopupBuilder.Create(GenericPopupType.Warning,
                        $"Enemies are within weapons range, if you retreat now the Leopard will take {combatDamage} damage that will have to be repaired.")
                        .CancelOnEscape()
                        .AddButton("Cancel")
                        .AddButton("Withdraw");
                    builder.IsNestedPopupWithBuiltInFade = true;
                    ___HUD.SelectionHandler.GenericPopup = builder.Render();
                    return false;
                } else {
                    MissionRetreatMessage message = new MissionRetreatMessage(__instance.IsGoodFaithEffort());
                    ___Combat.MessageCenter.PublishMessage(message);
                    ___HUD.SelectionHandler.GenericPopup = null;
                    return false;
                }
            } else {
                return true;
            }
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

                    ModState.RetreatButton.SetState(ButtonState.Disabled);
                    FieldInfo textField = AccessTools.Field(typeof(HBSButtonBase), "Text");
                    TextMeshProUGUI text = (TextMeshProUGUI)textField.GetValue(ModState.RetreatButton);
                    if (text != null) {
                        DisorderlyWithdrawal.Logger.Log($"Setting via text object");
                        text.SetText($"Inbound - { roundsToWait}", new object[] { });
                    } else {
                        DisorderlyWithdrawal.Logger.Log($"Text was null from lookup, using button.SetText ");
                        ModState.RetreatButton.SetText($"Inbound - { roundsToWait}");
                    }

                    GenericPopupBuilder genericPopupBuilder =
                        GenericPopupBuilder.Create(GenericPopupType.Info,
                            $"Sumire is inbound and will be overhead in {roundsToWait} rounds. Survive until then!")
                            .AddButton("Continue");
                    genericPopupBuilder.IsNestedPopupWithBuiltInFade = true;
                    ___HUD.SelectionHandler.GenericPopup = genericPopupBuilder.Render();

                    // TODO: MOVE TO A BETTER PLACE
                    Helper.CalculateCombatDamage();

                    return false;
                } else {
                    return true;
                }
            }
        }
    }


    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "Update")]
    public static class CombatHUDRetreatESCMenu_Update {
        public static void Postfix(CombatHUDRetreatEscMenu __instance, HBSDOTweenButton ___RetreatButton, CombatGameState ___Combat, CombatHUD ___HUD, bool ___isArena) {
            //DisorderlyWithdrawal.Logger.LogIfDebug("CHUDREM:U entered");

            if (!___isArena && ModState.RoundsUntilOverhead > 0) {
                //DisorderlyWithdrawal.Logger.LogIfDebug("CHUDREM:U forcing button to disabled");
                //___RetreatButton.SetState(ButtonState.Disabled, true);
                ___RetreatButton.gameObject.SetActive(false);
                ___RetreatButton.SetText($"Inbound - { ModState.RoundsUntilOverhead }");
                ___RetreatButton.gameObject.SetActive(true);
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
                        $"Sumire is overhead. If you don't withdraw on this round, she will to retreat and you will be unable to withdraw for another {ModState.RoundsUntilReady} rounds!")
                        .AddButton("Continue");
                    genericPopupBuilder.IsNestedPopupWithBuiltInFade = true;
                    ModState.HUD.SelectionHandler.GenericPopup = genericPopupBuilder.Render();

                    ModState.RetreatButton.SetState(ButtonState.Enabled);
                    ModState.RetreatButton.SetText("Withdraw");

                } else if (ModState.RoundsUntilOverhead > 0) {
                    ModState.RetreatButton.SetText($"Withdraw - {ModState.RoundsUntilOverhead} rounds");

                } else if (ModState.RoundsUntilOverhead < 0) {
                    // Player didn't withdraw. Reset the button and prevent them from restarting for N turns
                    ModState.WithdrawalTriggered = false;
                    ModState.RoundsUntilOverhead = -1;
                }

                ModState.RoundsUntilOverhead = ModState.RoundsUntilOverhead - 1;
            }

            if (ModState.RoundsUntilReady > 0) {
                ModState.RoundsUntilReady--;
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
            ModState.HUD = null;
        }
    }

    [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
    public static class SimGameState_GetExpenditures {
        public static void Postfix(SimGameState __instance, ref int __result, bool proRate) {
            DisorderlyWithdrawal.Logger.LogIfDebug($"SGS:GE entered");

            Statistic aerospaceAssets = __instance.CompanyStats.GetStatistic("AerospaceAssets");
            int aerospaceSupport = aerospaceAssets != null ? aerospaceAssets.Value<int>() : 0;
            DisorderlyWithdrawal.Logger.LogIfDebug($"Player has aerospace support:{aerospaceSupport}");

            switch (aerospaceSupport) {
                case 3:
                    __result = __result + DisorderlyWithdrawal.ModConfig.HeavyWingMonthlyCost;
                    DisorderlyWithdrawal.Logger.LogIfDebug($"Charging player for a heavy wing.");
                    break;
                case 2:
                    __result = __result + DisorderlyWithdrawal.ModConfig.MediumWingMonthlyCost;
                    DisorderlyWithdrawal.Logger.LogIfDebug($"Charging player for a medium wing.");
                    break;
                case 1:
                    __result = __result + DisorderlyWithdrawal.ModConfig.LightWingMonthlyCost;
                    DisorderlyWithdrawal.Logger.LogIfDebug($"Charging player for a light wing.");
                    break;
                default:
                    break;
            }
        }
    }
}
