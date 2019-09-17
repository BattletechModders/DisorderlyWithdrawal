using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            Vector3 playerUnitsCentroid = FindCentroid(playerPositions);

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

        // --- GEOMETRY FUNCTIONS BELOW

        // Shamelessly stolen from http://csharphelper.com/blog/2014/07/find-the-centroid-of-a-polygon-in-c/
        public static Vector3 FindCentroid(List<Vector3> actorPositions) {

            float centroidX = 0f;
            float centroidY = 0f;
            Vector3[] points = MakeClosedPolygon(actorPositions);
            int xSignSum = 0;
            int ySignSum = 0;
            for (int i = 0; i < actorPositions.Count; i++) {
                float x_0 = points[i].x;
                float x_1 = points[i + 1].x;
                float y_0 = points[i].y;
                float y_1 = points[i + 1].y;
                Mod.Log.Debug($"   point{i} = {x_0}, {y_0}");
                Mod.Log.Debug($"   point{i + 1} = {x_1}, {y_1}");

                xSignSum += Math.Sign(x_0);
                ySignSum += Math.Sign(y_0);

                float secondFactor = (x_0 * y_1) - (x_1 * y_0);
                Mod.Log.Debug($"  secondFactor = {secondFactor} = ({x_0} * {y_1}) - ({x_1} * {y_0})");

                float resultX = (x_0 + x_1) * secondFactor;

                float resultY = (y_0 + y_1) * secondFactor;
                Mod.Log.Debug($" centroidX:{centroidX} + resultX:{resultX}, centroidY:{centroidY} + resultY:{resultY}");

                centroidX += resultX;
                centroidY += resultY;
                Mod.Log.Debug($"  After addition centroidX:{centroidX}, centroidY:{centroidY}");
            }

            float zPosAverage = actorPositions.Select(v => v.z).Sum();
            zPosAverage = zPosAverage / actorPositions.Count;

            // Divide by 6 times the polygon area
            float polygonArea = SignedPolygonArea(actorPositions);
            centroidX = centroidX / (6 * polygonArea);
            centroidY = centroidY / (6 * polygonArea);

            // If the values are negative, the polygon is
            // oriented counterclockwise so reverse the signs.
            if (centroidX < 0) {
                centroidX = -1 * centroidX;
                centroidY = -1 * centroidY;
            }

            // Try to reconcile the signs. Because we use a +/- scale, the signs screw up the calculation.
            if (xSignSum == actorPositions.Count * -1 && centroidX > 0) {
                centroidX = centroidX * -1;
            } else if (xSignSum == actorPositions.Count && centroidX < 0) {
                centroidX = centroidX * -1;
            }

            if (ySignSum == actorPositions.Count * -1 && centroidY > 0) {
                centroidY = centroidY * -1;
            } else if (ySignSum == actorPositions.Count && centroidY < 0) {
                centroidY = centroidY * -1;
            }

            Mod.Log.Debug($"Centroid calculated as position (x:{centroidX}, y:{centroidY}, z:{zPosAverage})");
            return new Vector3(centroidX, centroidY, zPosAverage);
        }

        // Shamelessly stolen from http://csharphelper.com/blog/2014/07/calculate-the-area-of-a-polygon-in-c/
        public static float SignedPolygonArea(List<Vector3> objectPositions) {
            float area = 0f;

            Vector3[] points = MakeClosedPolygon(objectPositions);
            for (int i = 0; i < objectPositions.Count; i++) {
                area +=
                    (points[i + 1].x - points[i].x) *
                    (points[i + 1].y + points[i].y) / 2;
            }

            Mod.Log.Debug($"Points area calculated as 2d polygon is:{area}");
            return area;
        }

        private static Vector3[] MakeClosedPolygon(List<Vector3> objectPositions) {
            Vector3[] areaPoints = new Vector3[objectPositions.Count + 1];
            Vector3[] rawPoints = objectPositions.ToArray();
            for (int i = 0; i < objectPositions.Count; i++) {
                areaPoints[i] = new Vector3(rawPoints[i].x, rawPoints[i].y);
                if (i == 0) {
                    areaPoints[objectPositions.Count] = new Vector3(rawPoints[0].x, rawPoints[0].y);
                }
            }
            return areaPoints;
        }

    }

}
