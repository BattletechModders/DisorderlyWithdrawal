using BattleTech.UI;
using TMPro;

namespace DisorderlyWithdrawal {

    public static class State {

        public static bool WithdrawStarted = false;

        public static int CanWithdrawOnRound = -1;
        public static int CanApproachOnRound = -1;

        public static HBSDOTweenButton RetreatButton = null;
        public static TextMeshProUGUI RetreatButtonText = null;
        public static CombatHUD HUD = null;

        public static float CombatDamage = 0f;

        public static void Reset() {
            // Reinitialize state
            WithdrawStarted = false;

            CanWithdrawOnRound = -1;
            CanWithdrawOnRound = -1;

            RetreatButton = null;
            RetreatButtonText = null;
            HUD = null;

            CombatDamage = 0f;
        }
    }
}
