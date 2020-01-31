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
            Mod.Log.Trace($"CHUDREM:ORBP entered");

            Mod.Log.Debug($"  RetreatButton pressed -> withdrawStarted:{ModState.WithdrawStarted} " +
                $"CurrentRound:{___Combat.TurnDirector.CurrentRound} CanWithdrawOn:{ModState.CanWithdrawOnRound} CanApproachOn:{ModState.CanApproachOnRound}");

            if (ModState.WithdrawStarted && ModState.CanWithdrawOnRound == ___Combat.TurnDirector.CurrentRound) {
                Mod.Log.Debug($"Checking for combat damage and active enemies");
                ModState.CombatDamage = Helper.CalculateCombatDamage();
                if (ModState.CombatDamage > 0 && ___Combat.TurnDirector.DoAnyUnitsHaveContactWithEnemy) {
                    int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * Mod.Config.LeopardRepairCostPerDamage;
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
                    Mod.Log.Info($" Immediate withdraw due to no enemies");
                    OnImmediateWithdraw(__instance.IsGoodFaithEffort());
                    return false;
                }
            } else {
                return true;
            }
        }

        public static void OnImmediateWithdraw(bool isGoodFaith) {
            Mod.Log.Trace($"CHUDREM:ORBP:OnImmediateWithdraw - {isGoodFaith}");
            CombatGameState Combat = UnityGameInstance.BattleTechGame.Combat;
            MissionRetreatMessage message = new MissionRetreatMessage(isGoodFaith);
            Combat.MessageCenter.PublishMessage(message);
            ModState.HUD.SelectionHandler.GenericPopup = null;
        }
    }

    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatConfirmed")]
    public static class CombatHUDRetreatESCMenu_OnRetreatConfirmed {
        public static bool Prefix(CombatHUDRetreatEscMenu __instance, CombatGameState ___Combat, CombatHUD ___HUD) {
            Mod.Log.Trace("CHUDREM:ORC entered");

            if (___Combat == null || ___Combat.ActiveContract.ContractTypeValue.IsSkirmish) {
                return true;
            } else {

                int roundsToWait = Helper.RoundsToWaitByAerospace();
                Mod.Log.Info($"Player must wait:{roundsToWait} rounds for pickup."); 

                if (roundsToWait > 0) {
                    ModState.WithdrawStarted = true;
                    ModState.CanWithdrawOnRound = ___Combat.TurnDirector.CurrentRound + roundsToWait;
                    ModState.CanApproachOnRound = ModState.CanWithdrawOnRound + Helper.RoundsToWaitByAerospace(); ;
                    Mod.Log.Info($" Withdraw triggered on round {___Combat.TurnDirector.CurrentRound}. canWithdrawOn:{ModState.CanWithdrawOnRound}  canApproachOn: {ModState.CanApproachOnRound}");

                    ModState.RetreatButton = __instance.RetreatButton;
                    ModState.HUD = ___HUD;

                    Transform textT = __instance.RetreatButton.gameObject.transform.Find("Text");
                    if (textT != null) {
                        GameObject textGO = textT.gameObject;
                        ModState.RetreatButtonText = textGO.GetComponent<TextMeshProUGUI>();
                    }

                    ModState.RetreatButtonText.SetText($"In { roundsToWait } Rounds");

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
            Mod.Log.Debug($"CHUDREM:ORC OnContinue");
            ModState.HUD.SelectionHandler.GenericPopup = null;
        }
    }


    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "Update")]
    public static class CombatHUDRetreatESCMenu_Update {
        public static void Postfix(CombatHUDRetreatEscMenu __instance, CombatGameState ___Combat, CombatHUD ___HUD, bool ___isArena) {
            Mod.Log.Trace("CHUDREM:U entered");

            if (!___isArena) {
                Mod.Log.Debug($" On ESCmenu update -> currentRound:{___Combat.TurnDirector.CurrentRound} canWithdrawOn:{ModState.CanWithdrawOnRound} canApproachOn:{ModState.CanApproachOnRound}");
                if (___Combat.TurnDirector.CurrentRound < ModState.CanWithdrawOnRound) {
                    int withdrawIn = ModState.CanWithdrawOnRound - ___Combat.TurnDirector.CurrentRound;
                    Mod.Log.Debug($" Turns to withdraw: {withdrawIn}  currentRound:{___Combat.TurnDirector.CurrentRound} canWithdrawOn:{ModState.CanWithdrawOnRound} canApproachOn:{ModState.CanApproachOnRound}");

                    __instance.RetreatButton.SetState(ButtonState.Disabled, false);
                    ModState.RetreatButtonText.fontSize = 24;
                    ModState.RetreatButtonText.color = Color.white;
                    ModState.RetreatButtonText.SetText($"In { withdrawIn } Rounds");
                } else if (___Combat.TurnDirector.CurrentRound == ModState.CanWithdrawOnRound) {
                    Mod.Log.Debug($" Can withdraw on this turn. currentRound:{___Combat.TurnDirector.CurrentRound} canWithdrawOn:{ModState.CanWithdrawOnRound} canApproachOn:{ModState.CanApproachOnRound}");

                    if (__instance.RetreatButton.BaseState != ButtonState.Enabled) {
                        __instance.RetreatButton.SetState(ButtonState.Enabled, false);
                    }
                    ModState.RetreatButtonText.fontSize = 24;
                    ModState.RetreatButtonText.color = Color.white;
                    ModState.RetreatButtonText.SetText($"Withdraw");
                } else if (ModState.CanApproachOnRound > ___Combat.TurnDirector.CurrentRound) {
                    int readyIn = ModState.CanApproachOnRound - ___Combat.TurnDirector.CurrentRound;
                    Mod.Log.Debug($" Turns to ready: {readyIn}  currentRound:{___Combat.TurnDirector.CurrentRound} canWithdrawOn:{ModState.CanWithdrawOnRound} canApproachOn:{ModState.CanApproachOnRound}");

                    __instance.RetreatButton.SetState(ButtonState.Disabled, false);
                    ModState.RetreatButtonText.fontSize = 24;
                    ModState.RetreatButtonText.color = Color.white;
                    ModState.RetreatButtonText.SetText($"In { readyIn } Rounds");
                }
            }
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "BeginNewRound")]
    public static class TurnDirector_BeginNewRound {
        static void Postfix(TurnDirector __instance, int round) {
            Mod.Log.Trace("TD:BNR entered");
            Mod.Log.Debug($" OnNewRound -> withdrawStarted:{ModState.WithdrawStarted} canWithdrawOn:{ModState.CanWithdrawOnRound}");

            if (ModState.WithdrawStarted) {
                if (round == ModState.CanWithdrawOnRound) {
                    int readyIn = ModState.CanApproachOnRound - round;
                    GenericPopupBuilder genericPopupBuilder =
                        GenericPopupBuilder.Create(GenericPopupType.Info,
                        $"Sumire is overhead. If you don't withdraw on this round, she will retreat. She will be unavailable for another {readyIn} rounds!")
                        .AddButton("Continue", new Action(OnContinue), true, null);
                    genericPopupBuilder.IsNestedPopupWithBuiltInFade = true;
                    ModState.HUD.SelectionHandler.GenericPopup = genericPopupBuilder.Render();

                    ModState.RetreatButton.SetState(ButtonState.Enabled, true);
                    ModState.RetreatButtonText.SetText($"Withdraw Now");

                } else if (round == ModState.CanApproachOnRound) {
                    ModState.WithdrawStarted = false;
                    ModState.CanApproachOnRound = -1;
                    ModState.CanWithdrawOnRound = -1;

                    ModState.RetreatButton.SetState(ButtonState.Enabled, false);
                    ModState.RetreatButtonText.SetText($"Withdraw");
                }
            }
        }

        public static void OnContinue() {
            Mod.Log.Debug($"TD:BNR OnContinue");
            ModState.HUD.SelectionHandler.GenericPopup = null;
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "OnCombatGameDestroyed")]
    public static class TurnDirector_OnCombatGameDestroyed {
        static void Postfix(TurnDirector __instance) {
            Mod.Log.Debug("TD:OCGD entered");

            ModState.Reset();
            // DO NOT OVERWRITE CombatDamage!
        }
    }


}
