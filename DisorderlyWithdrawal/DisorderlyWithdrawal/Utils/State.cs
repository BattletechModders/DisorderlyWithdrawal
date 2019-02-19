using BattleTech.UI;
using TMPro;

namespace DisorderlyWithdrawal {

    public static class ModState {

        public static bool WithdrawalTriggered = false;
        public static int RoundsUntilOverhead = -1;
        public static int RoundsUntilReady = -1;

        public static HBSDOTweenButton RetreatButton = null;
        public static TextMeshProUGUI RetreatButtonText = null;
        public static CombatHUD HUD = null;

        public static float CombatDamage = 0f;
    }
}
