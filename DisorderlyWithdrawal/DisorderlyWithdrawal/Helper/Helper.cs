using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils.math;

namespace DisorderlyWithdrawal {

    public class ExpensesSorter : IComparer<KeyValuePair<string, int>> {
        public int Compare(KeyValuePair<string, int> x, KeyValuePair<string, int> y) {
            int cmp = y.Value.CompareTo(x.Value);
            if (cmp == 0) {
                cmp = y.Key.CompareTo(x.Key);
            }
            return cmp;
        }
    }

    public static class Helper {

        public static int RoundsToWaitByAerospace() {
            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            Statistic aerospaceAssets = simGameState.CompanyStats.GetStatistic("AerospaceAssets");
            int aerospaceSupport = aerospaceAssets != null ? aerospaceAssets.Value<int>() : 0;
            Mod.Log.Info($"Player has aerospace support:{aerospaceSupport}");

            int flightTimeRoll = 0;
            switch (aerospaceSupport) {
                case 3:
                    flightTimeRoll = Mod.Random.Next(Mod.Config.HeavyWingMinRounds, Mod.Config.HeavyWingMaxRounds);
                    break;
                case 2:
                    flightTimeRoll = Mod.Random.Next(Mod.Config.MediumWingMinRounds, Mod.Config.MediumWingMaxRounds);
                    break;
                case 1:
                    flightTimeRoll = Mod.Random.Next(Mod.Config.LightWingMinRounds, Mod.Config.LightWingMaxRounds);
                    break;
                default:
                    flightTimeRoll = Mod.Random.Next(Mod.Config.NoWingMinRounds, Mod.Config.NoWingMaxRounds);
                    break;
            }
            int roundsToWait = Math.Max(0, flightTimeRoll - aerospaceSupport);
            Mod.Log.Info($"Aerospace support gives {roundsToWait} rounds to wait.");

            return roundsToWait;
        }

        // Damage Functions Below

        public static float CalculateCombatDamage() {
            CombatGameState Combat = UnityGameInstance.BattleTechGame.Combat;

            Mod.Log.Debug($"Finding player units centroid");
            List<AbstractActor> playerUnits  = Combat.LocalPlayerTeam.units;
            List<Vector3> playerPositions = playerUnits.Select(aa => aa.CurrentPosition).ToList();
            Vector3 playerUnitsCentroid = GeometryUtils.FindCentroid(playerPositions);

            Mod.Log.Debug($"Finding enemy units");
            List<AbstractActor> enemyUnits = Combat.AllEnemies;
            float totalDamage = 0.0f;
            foreach (AbstractActor enemy in enemyUnits) {
                float enemyDamage = 0.0f;
                float distance = Vector3.Distance(playerUnitsCentroid, enemy.CurrentPosition);
                Mod.Log.Debug($"Enemy:{enemy.DisplayName}_{enemy?.GetPilot()?.Name} is distance:{distance}m " +
                    $"at position:{enemy.CurrentPosition} from centroid:{playerUnitsCentroid}");

                foreach (Weapon weapon in enemy.Weapons) {
                    if (weapon.MaxRange >= distance) {
                        Mod.Log.Debug($"Enemy:{enemy.DisplayName}_{enemy?.GetPilot()?.Name} " +
                            $"has weapon:{weapon.Name} in range. Adding {weapon.DamagePerShot} damage for {weapon.ShotsWhenFired} shots.");
                        enemyDamage += (weapon.DamagePerShot * weapon.ShotsWhenFired);
                    }
                }

                Mod.Log.Debug($"Total damage from enemy:{enemy.DisplayName}_{enemy?.GetPilot()?.Name} is {enemyDamage}.");
                totalDamage += enemyDamage;
            }

            Mod.Log.Debug($"Total damage from all enemies is:{totalDamage}");
            return totalDamage;
        }
    }
}
