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

            Mod.Log.Debug($"  RetreatButton pressed -> withdrawStarted:{State.WithdrawStarted} " +
                $"CurrentRound:{___Combat.TurnDirector.CurrentRound} CanWithdrawOn:{State.CanWithdrawOnRound} CanApproachOn:{State.CanApproachOnRound}");

            if (State.WithdrawStarted && State.CanWithdrawOnRound == ___Combat.TurnDirector.CurrentRound) {
                Mod.Log.Debug($"Checking for combat damage and active enemies");
                State.CombatDamage = Helper.CalculateCombatDamage();
                if (State.CombatDamage > 0 && ___Combat.TurnDirector.DoAnyUnitsHaveContactWithEnemy) {
                    int repairCost = (int)Math.Ceiling(State.CombatDamage) * Mod.Config.LeopardRepairCostPerDamage;
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
            State.HUD.SelectionHandler.GenericPopup = null;
        }
    }

    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatConfirmed")]
    public static class CombatHUDRetreatESCMenu_OnRetreatConfirmed {
        public static bool Prefix(CombatHUDRetreatEscMenu __instance, CombatGameState ___Combat, CombatHUD ___HUD) {
            Mod.Log.Trace("CHUDREM:ORC entered");

            if (___Combat == null || ___Combat.ActiveContract.IsArenaSkirmish) {
                return true;
            } else {

                int roundsToWait = Helper.RoundsToWaitByAerospace();
                Mod.Log.Info($"Player must wait:{roundsToWait} rounds for pickup."); 

                if (roundsToWait > 0) {
                    State.WithdrawStarted = true;
                    State.CanWithdrawOnRound = ___Combat.TurnDirector.CurrentRound + roundsToWait;
                    State.CanApproachOnRound = State.CanWithdrawOnRound + Helper.RoundsToWaitByAerospace(); ;
                    Mod.Log.Info($" Withdraw triggered on round {___Combat.TurnDirector.CurrentRound}. canWithdrawOn:{State.CanWithdrawOnRound}  canApproachOn: {State.CanApproachOnRound}");

                    State.RetreatButton = __instance.RetreatButton;
                    State.HUD = ___HUD;

                    Transform textT = __instance.RetreatButton.gameObject.transform.Find("Text");
                    if (textT != null) {
                        GameObject textGO = textT.gameObject;
                        State.RetreatButtonText = textGO.GetComponent<TextMeshProUGUI>();
                    }

                    State.RetreatButtonText.SetText($"In { roundsToWait } Rounds", new object[] { });

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
            State.HUD.SelectionHandler.GenericPopup = null;
        }
    }


    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "Update")]
    public static class CombatHUDRetreatESCMenu_Update {
        public static void Postfix(CombatHUDRetreatEscMenu __instance, CombatGameState ___Combat, CombatHUD ___HUD, bool ___isArena) {
            Mod.Log.Trace("CHUDREM:U entered");

            if (!___isArena) {
                Mod.Log.Debug($" On ESCmenu update -> currentRound:{___Combat.TurnDirector.CurrentRound} canWithdrawOn:{State.CanWithdrawOnRound} canApproachOn:{State.CanApproachOnRound}");
                if (___Combat.TurnDirector.CurrentRound < State.CanWithdrawOnRound) {
                    int withdrawIn = State.CanWithdrawOnRound - ___Combat.TurnDirector.CurrentRound;
                    Mod.Log.Debug($" Turns to withdraw: {withdrawIn}  currentRound:{___Combat.TurnDirector.CurrentRound} canWithdrawOn:{State.CanWithdrawOnRound} canApproachOn:{State.CanApproachOnRound}");

                    __instance.RetreatButton.SetState(ButtonState.Disabled, false);
                    State.RetreatButtonText.fontSize = 24;
                    State.RetreatButtonText.color = Color.white;
                    State.RetreatButtonText.SetText($"In { withdrawIn } Rounds", new object[] { });
                } else if (___Combat.TurnDirector.CurrentRound == State.CanWithdrawOnRound) {
                    Mod.Log.Debug($" Can withdraw on this turn. currentRound:{___Combat.TurnDirector.CurrentRound} canWithdrawOn:{State.CanWithdrawOnRound} canApproachOn:{State.CanApproachOnRound}");

                    if (__instance.RetreatButton.BaseState != ButtonState.Enabled) {
                        __instance.RetreatButton.SetState(ButtonState.Enabled, false);
                    }
                    State.RetreatButtonText.fontSize = 24;
                    State.RetreatButtonText.color = Color.white;
                    State.RetreatButtonText.SetText($"Withdraw", new object[] { });
                } else if (State.CanApproachOnRound > ___Combat.TurnDirector.CurrentRound) {
                    int readyIn = State.CanApproachOnRound - ___Combat.TurnDirector.CurrentRound;
                    Mod.Log.Debug($" Turns to ready: {readyIn}  currentRound:{___Combat.TurnDirector.CurrentRound} canWithdrawOn:{State.CanWithdrawOnRound} canApproachOn:{State.CanApproachOnRound}");

                    __instance.RetreatButton.SetState(ButtonState.Disabled, false);
                    State.RetreatButtonText.fontSize = 24;
                    State.RetreatButtonText.color = Color.white;
                    State.RetreatButtonText.SetText($"In { readyIn } Rounds", new object[] { });
                }
            }
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "BeginNewRound")]
    public static class TurnDirector_BeginNewRound {
        static void Postfix(TurnDirector __instance, int round) {
            Mod.Log.Trace("TD:BNR entered");
            Mod.Log.Debug($" OnNewRound -> withdrawStarted:{State.WithdrawStarted} canWithdrawOn:{State.CanWithdrawOnRound}");

            if (State.WithdrawStarted) {
                if (round == State.CanWithdrawOnRound) {
                    int readyIn = State.CanApproachOnRound - round;
                    GenericPopupBuilder genericPopupBuilder =
                        GenericPopupBuilder.Create(GenericPopupType.Info,
                        $"Sumire is overhead. If you don't withdraw on this round, she will retreat. She will be unavailable for another {readyIn} rounds!")
                        .AddButton("Continue", new Action(OnContinue), true, null);
                    genericPopupBuilder.IsNestedPopupWithBuiltInFade = true;
                    State.HUD.SelectionHandler.GenericPopup = genericPopupBuilder.Render();

                    State.RetreatButton.SetState(ButtonState.Enabled, true);
                    State.RetreatButtonText.SetText($"Withdraw Now", new object[] { });

                } else if (round == State.CanApproachOnRound) {
                    State.WithdrawStarted = false;
                    State.CanApproachOnRound = -1;
                    State.CanWithdrawOnRound = -1;

                    State.RetreatButton.SetState(ButtonState.Enabled, false);
                    State.RetreatButtonText.SetText($"Withdraw", new object[] { });
                }
            }
        }

        public static void OnContinue() {
            Mod.Log.Debug($"TD:BNR OnContinue");
            State.HUD.SelectionHandler.GenericPopup = null;
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "OnCombatGameDestroyed")]
    public static class TurnDirector_OnCombatGameDestroyed {
        static void Postfix(TurnDirector __instance) {
            Mod.Log.Debug("TD:OCGD entered");

            State.Reset();
            // DO NOT OVERWRITE CombatDamage!
        }
    }


}
