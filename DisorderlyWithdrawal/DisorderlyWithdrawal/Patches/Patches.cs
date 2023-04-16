using BattleTech.UI;
using System;
using TMPro;
using UnityEngine;

namespace DisorderlyWithdrawal.Patches
{

    [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "Init")]
    [HarmonyPatch(new Type[] { typeof(CombatGameState), typeof(CombatHUD) })]
    public static class CombatHUDRetreatESCMenu_Init
    {
        public static void Postfix(CombatHUDRetreatEscMenu __instance)
        {
            Mod.Log.Trace?.Write($"CHUDREM:INIT - entered");

            ModState.HUD = __instance.HUD;
            ModState.Combat = __instance.Combat; 

            ModState.RetreatButton = __instance.RetreatButton;
            if (__instance.RetreatButton.gameObject.activeSelf)
            {
                Mod.Log.Info?.Write(" We are in not a priority contract or skirmish, enabling withdrawal.");
                ModState.WithdrawIsAvailable = true;

                Transform textT = __instance.RetreatButton.gameObject.transform.Find("Text");
                if (textT != null)
                {
                    GameObject textGO = textT.gameObject;
                    ModState.RetreatButtonText = textGO.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    Mod.Log.Error?.Write("Failed to find text component, cannot proceed! Disabling withdrawal logic.");
                    ModState.WithdrawIsAvailable = false;
                }

            }
            else
            {
                Mod.Log.Info?.Write(" We are in a priority contract or skirmish, preventing withdrawal.");
                ModState.WithdrawIsAvailable = true;

            }

        }

        [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatButtonPressed")]
        public static class CombatHUDRetreatESCMenu_OnRetreatButtonPressed
        {
            public static void Prefix(ref bool __runOriginal, CombatHUDRetreatEscMenu __instance)
            {
                if (!__runOriginal) return;

                HBSDOTweenButton retreatButton = __instance.RetreatButton;
                CombatGameState cgs = __instance.Combat;
                CombatHUD chud = __instance.HUD;

                Mod.Log.Trace?.Write($"CHUDREM:ORBP entered");

                Mod.Log.Debug?.Write($"  RetreatButton pressed -> withdrawStarted:{ModState.WithdrawStarted} " +
                    $"CurrentRound:{cgs.TurnDirector.CurrentRound} CanWithdrawOn:{ModState.CanWithdrawOnRound} CanApproachOn:{ModState.CanApproachOnRound}");

                if (ModState.WithdrawStarted && ModState.CanWithdrawOnRound == cgs.TurnDirector.CurrentRound)
                {
                    Mod.Log.Debug?.Write($"Checking for combat damage and active enemies");
                    ModState.CombatDamage = Helper.CalculateCombatDamage();
                    if (ModState.CombatDamage > 0 && cgs.TurnDirector.DoAnyUnitsHaveContactWithEnemy)
                    {
                        int repairCost = (int)Math.Ceiling(ModState.CombatDamage) * Mod.Config.LeopardRepairCostPerDamage;
                        void withdrawAction() { OnImmediateWithdraw(__instance.IsGoodFaithEffort()); }
                        GenericPopupBuilder builder = GenericPopupBuilder.Create(GenericPopupType.Warning,
                            $"Enemies are within weapons range! If you withdraw now the Leopard will take {SimGameState.GetCBillString(repairCost)} worth of damage.")
                            .CancelOnEscape()
                            .AddButton("Cancel")
                            .AddButton("Withdraw", withdrawAction, true, null);
                        builder.IsNestedPopupWithBuiltInFade = true;
                        chud.SelectionHandler.GenericPopup = builder.Render();
                        __runOriginal = false;
                        return;
                    }
                    else
                    {
                        Mod.Log.Info?.Write($" Immediate withdraw due to no enemies");
                        OnImmediateWithdraw(__instance.IsGoodFaithEffort());
                        __runOriginal = false;
                        return;
                    }
                }
            }

            public static void OnImmediateWithdraw(bool isGoodFaith)
            {
                Mod.Log.Trace?.Write($"CHUDREM:ORBP:OnImmediateWithdraw - {isGoodFaith}");
                CombatGameState Combat = UnityGameInstance.BattleTechGame.Combat;
                MissionRetreatMessage message = new MissionRetreatMessage(isGoodFaith);
                Combat.MessageCenter.PublishMessage(message);
                ModState.HUD.SelectionHandler.GenericPopup = null;
            }
        }

        [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatConfirmed")]
        public static class CombatHUDRetreatESCMenu_OnRetreatConfirmed
        {
            public static void Prefix(ref bool __runOriginal, CombatHUDRetreatEscMenu __instance)
            {
                if (!__runOriginal) return;

                CombatGameState cgs = __instance.Combat;
                CombatHUD chud = __instance.HUD;

                Mod.Log.Trace?.Write("CHUDREM:ORC entered");

                if (cgs == null || cgs.ActiveContract.ContractTypeValue.IsSkirmish) return;

                int roundsToWait = Helper.RoundsToWaitByAerospace();
                Mod.Log.Info?.Write($"Player must wait:{roundsToWait} rounds for pickup.");

                if (roundsToWait > 0)
                {
                    ModState.WithdrawStarted = true;
                    ModState.CanWithdrawOnRound = cgs.TurnDirector.CurrentRound + roundsToWait;
                    ModState.CanApproachOnRound = ModState.CanWithdrawOnRound + Helper.RoundsToWaitByAerospace(); ;
                    Mod.Log.Info?.Write($" Withdraw triggered on round {cgs.TurnDirector.CurrentRound}. canWithdrawOn:{ModState.CanWithdrawOnRound}  canApproachOn: {ModState.CanApproachOnRound}");

                    ModState.RetreatButton = __instance.RetreatButton;
                    ModState.HUD = chud;

                    Transform textT = __instance.RetreatButton.gameObject.transform.Find("Text");
                    if (textT != null)
                    {
                        GameObject textGO = textT.gameObject;
                        ModState.RetreatButtonText = textGO.GetComponent<TextMeshProUGUI>();
                    }

                    ModState.RetreatButtonText.SetText($"In {roundsToWait} Rounds");

                    GenericPopupBuilder genericPopupBuilder =
                        GenericPopupBuilder.Create(GenericPopupType.Info,
                            $"Sumire is inbound and will be overhead in {roundsToWait} rounds. Survive until then!")
                            .AddButton("Continue", new Action(OnContinue), true, null);
                    genericPopupBuilder.IsNestedPopupWithBuiltInFade = true;
                    chud.SelectionHandler.GenericPopup = genericPopupBuilder.Render();

                __runOriginal = false;
                }
            }

            public static void OnContinue()
            {
                Mod.Log.Trace?.Write($"CHUDREM:ORC OnContinue");
                ModState.HUD.SelectionHandler.GenericPopup = null;
            }
        }


        [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "Update")]
        public static class CombatHUDRetreatESCMenu_Update
        {

            public static bool Prepare() { return ModState.WithdrawIsAvailable && ModState.Combat != null && ModState.Combat.TurnDirector != null; }

            public static void Postfix(CombatHUDRetreatEscMenu __instance)
            {
                Mod.Log.Trace?.Write("CHUDREM:U entered");

                CombatGameState cgs = __instance.Combat;

                try
                {
                    Mod.Log.Debug?.Write($" On ESCmenu update -> currentRound:{cgs.TurnDirector.CurrentRound} canWithdrawOn:{ModState.CanWithdrawOnRound} canApproachOn:{ModState.CanApproachOnRound}");
                    if (cgs.TurnDirector.CurrentRound < ModState.CanWithdrawOnRound)
                    {
                        int withdrawIn = ModState.CanWithdrawOnRound - cgs.TurnDirector.CurrentRound;
                        Mod.Log.Debug?.Write($" Turns to withdraw: {withdrawIn}  currentRound:{cgs.TurnDirector.CurrentRound} canWithdrawOn:{ModState.CanWithdrawOnRound} canApproachOn:{ModState.CanApproachOnRound}");

                        __instance.RetreatButton.SetState(ButtonState.Disabled, false);
                        ModState.RetreatButtonText.fontSize = 24;
                        ModState.RetreatButtonText.color = Color.white;
                        ModState.RetreatButtonText.SetText($"In {withdrawIn} Rounds");
                    }
                    else if (cgs.TurnDirector.CurrentRound == ModState.CanWithdrawOnRound)
                    {
                        Mod.Log.Debug?.Write($" Can withdraw on this turn. currentRound:{cgs.TurnDirector.CurrentRound} canWithdrawOn:{ModState.CanWithdrawOnRound} canApproachOn:{ModState.CanApproachOnRound}");

                        if (__instance.RetreatButton.BaseState != ButtonState.Enabled)
                        {
                            __instance.RetreatButton.SetState(ButtonState.Enabled, false);
                        }
                        ModState.RetreatButtonText.fontSize = 24;
                        ModState.RetreatButtonText.color = Color.white;
                        ModState.RetreatButtonText.SetText($"Withdraw");
                    }
                    else if (ModState.CanApproachOnRound > cgs.TurnDirector.CurrentRound)
                    {
                        int readyIn = ModState.CanApproachOnRound - cgs.TurnDirector.CurrentRound;
                        Mod.Log.Debug?.Write($" Turns to ready: {readyIn}  currentRound:{cgs.TurnDirector.CurrentRound} canWithdrawOn:{ModState.CanWithdrawOnRound} canApproachOn:{ModState.CanApproachOnRound}");

                        __instance.RetreatButton.SetState(ButtonState.Disabled, false);
                        ModState.RetreatButtonText.fontSize = 24;
                        ModState.RetreatButtonText.color = Color.white;
                        ModState.RetreatButtonText.SetText($"In {readyIn} Rounds");
                    }
                }
                catch (Exception e)
                {
                    Mod.Log.Error?.Write("Failed to perform update logic!");
                    Mod.Log.Error?.Write(e);
                }
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "BeginNewRound")]
        public static class TurnDirector_BeginNewRound
        {
            static void Postfix(TurnDirector __instance, int round)
            {
                Mod.Log.Trace?.Write("TD:BNR entered");
                Mod.Log.Debug?.Write($" OnNewRound -> withdrawStarted:{ModState.WithdrawStarted} canWithdrawOn:{ModState.CanWithdrawOnRound}");

                if (ModState.WithdrawStarted)
                {
                    if (round == ModState.CanWithdrawOnRound)
                    {
                        int readyIn = ModState.CanApproachOnRound - round;
                        GenericPopupBuilder genericPopupBuilder =
                            GenericPopupBuilder.Create(GenericPopupType.Info,
                            $"Sumire is overhead. If you don't withdraw on this round, she will retreat. She will be unavailable for another {readyIn} rounds!")
                            .AddButton("Continue", new Action(OnContinue), true, null);
                        genericPopupBuilder.IsNestedPopupWithBuiltInFade = true;
                        ModState.HUD.SelectionHandler.GenericPopup = genericPopupBuilder.Render();

                        ModState.RetreatButton.SetState(ButtonState.Enabled, true);
                        ModState.RetreatButtonText.SetText($"Withdraw Now");

                    }
                    else if (round == ModState.CanApproachOnRound)
                    {
                        ModState.WithdrawStarted = false;
                        ModState.CanApproachOnRound = -1;
                        ModState.CanWithdrawOnRound = -1;

                        ModState.RetreatButton.SetState(ButtonState.Enabled, false);
                        ModState.RetreatButtonText.SetText($"Withdraw");
                    }
                    else
                    {
                        int roundsToWait = ModState.CanWithdrawOnRound - round;
                        Mod.Log.Info?.Write($" -- Player must wait:{roundsToWait} rounds for pickup.");
                        ModState.RetreatButtonText.SetText($"In {roundsToWait} Rounds");
                    }
                }
            }

            public static void OnContinue()
            {
                Mod.Log.Debug?.Write($"TD:BNR OnContinue");
                ModState.HUD.SelectionHandler.GenericPopup = null;
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "OnCombatGameDestroyed")]
        public static class TurnDirector_OnCombatGameDestroyed
        {
            static void Postfix(TurnDirector __instance)
            {
                Mod.Log.Debug?.Write("TD:OCGD entered");

                ModState.Reset();
                // DO NOT OVERWRITE CombatDamage!
            }
        }

    }
}
