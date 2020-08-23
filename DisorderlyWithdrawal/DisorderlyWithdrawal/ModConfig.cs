
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

        public int LightWingMaxRounds = 4;
        public int LightWingMinRounds = 3;

        public int MediumWingMaxRounds = 3;
        public int MediumWingMinRounds = 2;

        public int HeavyWingMaxRounds = 2;
        public int HeavyWingMinRounds = 1;

        public int NoWingMaxRounds = 6;
        public int NoWingMinRounds = 4;

        public void LogConfig() {
            Mod.Log.Info?.Write("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info?.Write($"  DEBUG: {this.Debug}");
            Mod.Log.Info?.Write($"  LeopardRepairCostPerDamage:{LeopardRepairCostPerDamage}");
            Mod.Log.Info?.Write($"  Light Wing  - MonthlyCost:x{LightWingMonthlyCost} LeopardDamage:{LightWingLeopardDamage} MaxRounds:{LightWingMaxRounds} MinRounds:{LightWingMinRounds}");
            Mod.Log.Info?.Write($"  Medium Wing - MonthlyCost:x{MediumWingMonthlyCost} LeopardDamage:{MediumWingLeopardDamage} MaxRounds:{MediumWingMaxRounds} MinRounds:{MediumWingMinRounds}");
            Mod.Log.Info?.Write($"  Heavy Wing  - MonthlyCost:x{HeavyWingMonthlyCost} LeopardDamage:{HeavyWingLeopardDamage} MaxRounds:{HeavyWingMaxRounds} MinRounds:{HeavyWingMinRounds}");
            Mod.Log.Info?.Write("=== MOD CONFIG END ===");
        }
    }
}
