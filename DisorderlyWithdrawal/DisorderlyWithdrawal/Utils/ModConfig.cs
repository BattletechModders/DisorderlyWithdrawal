
namespace DisorderlyWithdrawal {

    public class ModConfig {

        // If true, extra logging will be used
        public bool Debug = false;

        public int LightWingMonthlyCost = 50000;
        public float LightWingLeopardDamage = 0.0f;

        public int MediumWingMonthlyCost = 75000;
        public float MediumWingLeopardDamage = 0.3f;

        public int HeavyWingMonthlyCost = 100000;
        public float HeavyWingLeopardDamage = 0.6f;

        public int LeopardRepairCostPerDamage = 100;
        
        public override string ToString() {
            return $"Debug:{Debug}";
        }
    }
}
