using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using MonoMod.Cil;
using Noise;
using UnityEngine;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RWCustom;

namespace boilingVeins
{

    class slugcatStatsMod
    {
        public static bool isHunter = false;
        System.Random rand = new System.Random();
        public slugcatStatsMod()
        {
            On.SlugcatStats.ctor += SlugcatStats_ctor;
            On.Player.ThrownSpear += Player_ThrownSpear;
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
        }

        private RWCustom.IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, int slugcatNum)
        {
            orig(slugcatNum);

            switch (slugcatNum)
            {
                case 0:
                    rockLogicMod.powerCoefficient = 1f;
                    return new IntVector2(7, 4);
                case 1:
                    rockLogicMod.powerCoefficient = 0.9f;
                    return new IntVector2(5, 3);
                case 2:
                    rockLogicMod.powerCoefficient = 1.2f;
                    isHunter = true;
                    return new IntVector2(12, 9);
                default:
                    return new IntVector2(0, 0);
            }

        }

        private void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            System.Random rand = new System.Random();
            orig(self, spear);
            if (self.slugcatStats.throwingSkill == -1)
            {
                spear.throwModeFrames = 30;
                spear.spearDamageBonus = float.Parse("" + Math.Pow((rand.NextDouble() / 3 + 0.25f), 0.7f));
                BodyChunk firstchunk = spear.firstChunk;
                firstchunk.vel.x *= 0.77f;
            }
        }

        private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, int slugcatNumber, bool malnourished)
        {
            orig(self, slugcatNumber, malnourished);
            self.throwingSkill = -1;
            self.bodyWeightFac = 3f;
            self.loudnessFac = 2.5f;
            self.runspeedFac = 0.9f;
        }
    }
}
