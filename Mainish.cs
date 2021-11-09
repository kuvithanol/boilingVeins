using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using MonoMod.Cil;
using Noise;
using UnityEngine;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace boilingVeins
{
    [BepInPlugin("Sov_Sam.jihad.rw-steamworks", "RWSteamworks", "0.0.1")]	// (GUID, mod name, mod version)
    public class mod : BaseUnityPlugin
    {
        public mod()
        {
            On.SlugcatStats.ctor += SlugcatStats_ctor;
            On.Player.ThrowObject += Player_ThrowObject;
            On.Creature.Violence += Creature_Violence;
            //On.Explosion.Update += Explosion_Update;
            IL.Explosion.Update += Explosion_Update;
            On.Player.ThrownSpear += Player_ThrownSpear;
        }

        private void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            System.Random rand = new System.Random();
            orig(self, spear);
            if (self.slugcatStats.throwingSkill == -1)
            {
                spear.throwModeFrames = 30;
                spear.spearDamageBonus = float.Parse("" + ((rand.NextDouble() + 1) / 3));
                BodyChunk firstchunk = spear.firstChunk;
                firstchunk.vel.x *= 0.77f;
            }
        }


        //private void Explosion_Update(On.Explosion.orig_Update orig, Explosion self, bool eu)
        //{
        //    foreach(Player ply in )
        //    orig(self, eu);
        //}

        private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (!(self is Player) || !(type == Creature.DamageType.Explosion))
            {
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            }
        }

        int semiCost = 1;
        HUD.FoodMeter i;
        private void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            if (self.grasps[grasp].grabbed is Rock && self.playerState.foodInStomach > 0)
            {
                if (semiCost == 3)
                {
                    foreach (var cam in self.room.game.cameras)
                    {
                        if (cam.hud.owner == self && cam.hud.foodMeter is HUD.FoodMeter fm)
                        {

                            if (fm.showCount > 0)
                                fm.circles[--fm.showCount].EatFade();


                        }
                    }
                    semiCost = 1;
                    self.playerState.foodInStomach -= 1;
                }
                else { semiCost++; }

                PhysicalObject rock = self.grasps[grasp].grabbed;
                Vector2 vector = Vector2.Lerp(rock.firstChunk.pos, rock.firstChunk.lastPos, 0.35f);
                //self.room.AddObject(new SootMark(self.room, vector, 80f, true));
                rock.room.AddObject(new Explosion(self.room, rock, vector, 3, 170f, 2f, 3f, 20f, 0.2f, self, 0.7f, 50f, 1f));
                rock.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, new Color(1, 0.2f, 0.2f)));
                rock.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1, 0.4f, 0.2f)));
                self.room.AddObject(new ExplosionSpikes(self.room, vector, 14, 30f, 5f, 7f, 250f, new Color(1, 0.2f, 0.2f)));
                rock.room.AddObject(new ShockWave(vector, 330f, 0.045f, 5));
                rock.slatedForDeletetion = true;
                rock.room.PlaySound(SoundID.Bomb_Explode, vector);
                rock.room.InGameNoise(new InGameNoise(vector, 5000f, self, 0.7f));

            }
            orig(self, grasp, eu);
        }

        private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, int slugcatNumber, bool malnourished)
        {
            orig(self, slugcatNumber, malnourished);
            self.maxFood = 12;
            self.foodToHibernate = 4;
            self.throwingSkill = -1;
        }

        private void Explosion_Update(ILContext il)
        {
            var cursor = new ILCursor(il);
            ILLabel brContinue = null;

            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchLdfld<Room>(nameof(Room.physicalObjects)),
                x => x.MatchLdloc(2),
                x => x.MatchLdelemRef(),
                x => x.MatchLdloc(3),
                x => x.MatchCallvirt(typeof(List<PhysicalObject>).GetProperty("Item").GetGetMethod()),
                x => x.MatchCallvirt(typeof(UpdatableAndDeletable).GetProperty(nameof(UpdatableAndDeletable.slatedForDeletetion)).GetGetMethod()),
                x => x.MatchBrtrue(out brContinue)
            );

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(UpdatableAndDeletable).GetField(nameof(UpdatableAndDeletable.room)));
            cursor.Emit(OpCodes.Ldfld, typeof(Room).GetField(nameof(Room.physicalObjects)));
            cursor.Emit(OpCodes.Ldloc_2);
            cursor.Emit(OpCodes.Ldelem_Ref);
            cursor.Emit(OpCodes.Ldloc_3);
            cursor.Emit(OpCodes.Callvirt, typeof(List<PhysicalObject>).GetProperty("Item").GetGetMethod());
            cursor.Emit(OpCodes.Isinst, typeof(Player));
            cursor.Emit(OpCodes.Ldnull);
            cursor.Emit(OpCodes.Bne_Un, brContinue);
        }
    }
}