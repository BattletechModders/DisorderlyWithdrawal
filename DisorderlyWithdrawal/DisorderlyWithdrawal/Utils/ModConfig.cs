
namespace DisorderlyWithdrawal {

    public class ModConfig {

        // If true, troubleshooting logging will be enabled
        public bool Debug = false;

        // If true, all logging will be enabled
        public bool Trace = false;

        public int LeopardRepairCostPerDamage = 100;

        public int LightWingMonthlyCost = 50000;
        public float LightWingLeopardDamage = 0.0f;

        public int MediumWingMonthlyCost = 75000;
        public float MediumWingLeopardDamage = 0.3f;

        public int HeavyWingMonthlyCost = 100000;
        public float HeavyWingLeopardDamage = 0.6f;

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug}");
            Mod.Log.Info($"  LeopardRepairCostPerDamage:{LeopardRepairCostPerDamage}");
            Mod.Log.Info($"  Light Wing  - MonthlyCost:x{LightWingMonthlyCost} LeopardDamage:{LightWingLeopardDamage}");
            Mod.Log.Info($"  Medium Wing - MonthlyCost:x{MediumWingMonthlyCost} LeopardDamage:{MediumWingLeopardDamage}");
            Mod.Log.Info($"  Heavy Wing  - MonthlyCost:x{HeavyWingMonthlyCost} LeopardDamage:{HeavyWingLeopardDamage}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}
