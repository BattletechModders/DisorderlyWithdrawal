using BattleTech.UI;

namespace DisorderlyWithdrawal {

    public static class ModState {

        public static bool WithdrawalTriggered = false;
        public static int RoundsUntilOverhead = 0;
        public static int RoundsUntilReady = 0;

        public static HBSDOTweenButton RetreatButton = null;
        public static CombatHUD HUD = null;
    }
}
