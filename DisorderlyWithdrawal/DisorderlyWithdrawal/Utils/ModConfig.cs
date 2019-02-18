
namespace DisorderlyWithdrawal {

    public class ModConfig {

        // If true, extra logging will be used
        public bool Debug = false;

        public int LightWingMonthlyCost = 50000;
        public int MediumWingMonthlyCost = 75000;
        public int HeavyWingMonthlyCost = 100000;
        
        public override string ToString() {
            return $"Debug:{Debug}";
        }
    }
}
