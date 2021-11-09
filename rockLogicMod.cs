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
    class rockLogicMod
    {
        public static float powerCoefficient;

        System.Random rand = new System.Random();
        public static Dictionary<PhysicalObject, int> rockHealth = new Dictionary<PhysicalObject, int>();
        HUD.FoodMeter i;
        public static float KBmultiplier = 1f;
        public rockLogicMod()
        {
            On.Player.ThrowObject += Player_ThrowObject;
            On.Rock.ctor += Rock_ctor;
            On.Rock.DrawSprites += Rock_DrawSprites;
            On.Rock.Update += Rock_Update;
        }

        private void produceRockExplosion(Player self, PhysicalObject rock, Vector2 vector)
        {
            rock.room.AddObject(new Explosion(self.room, rock, vector, 4, 80f * powerCoefficient, 2.3f * powerCoefficient * KBmultiplier, 1.7f * powerCoefficient, 25f, 0.12f, self, 0.7f, 5, 1f));
        }

        static public int semiCost = 1;
        private void Rock_Update(On.Rock.orig_Update orig, Rock self, bool eu)
        {

            if (rand.NextDouble() > 0.98f)
            {
                if ((rand.NextDouble() > 0.85f && rockHealth[self] == 1) || (rand.NextDouble() > 0.50f && rockHealth[self] == 0))
                {
                    Vector2 vector = self.firstChunk.pos;
                    self.room.AddObject(new Smoke.Smolder(self.room, vector, self.firstChunk, null));
                    self.room.AddObject(new Explosion.ExplosionLight(vector, 30f, 1f, 7, new Color(1, 0.8f, 0.7f)));
                    self.room.AddObject(new Explosion.FlashingSmoke(vector, new Vector2(0, 0), 1, new Color(1f, 0.2f, 0), new Color(0.4f, 0.1f, 0), UnityEngine.Random.Range(3, 11)));

                    self.room.PlaySound(SoundID.Gate_Water_Steam_Puff, vector + new Vector2(0, 10), 0.25f, 1.5f);

                    self.room.InGameNoise(new InGameNoise(vector, 5000f, self, 1.7f));
                }
            }
            self.firstChunk.vel.x *= 0.95f;
            orig(self, eu);
        }

        private void Rock_DrawSprites(On.Rock.orig_DrawSprites orig, Rock self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (rockHealth[self] == 1)
            {
                sLeaser.sprites[0].color = new Color(0.5f, 0.1f, 0);
            }
            else if (rockHealth[self] == 0)
            {
                sLeaser.sprites[0].color = new Color(0.9f, 0.6f, 0.3f);
            }
            else if (rockHealth[self] == 2)
            {
                sLeaser.sprites[0].color = new Color(0.3f, 0f, 0f);
            }

        }

        private void Rock_ctor(On.Rock.orig_ctor orig, Rock self, AbstractPhysicalObject abstractPhysicalObject, World world)
        {
            orig(self, abstractPhysicalObject, world);
            rockHealth.Add(self, 3);
        }

        private void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            object grabaroo = self.grasps[grasp].grabbed;
            if (grabaroo is Rock && (self.playerState.foodInStomach > 0 || worldLoadingMod.isArena) && !(grabaroo is WaterNut))
            {
                if (!worldLoadingMod.isArena)
                {
                    if (semiCost == 4 || (semiCost == 2 && slugcatStatsMod.isHunter))
                    {
                        foreach (var cam in self.room.game.cameras)
                        {
                            if (cam.hud.owner == self && cam.hud.foodMeter is HUD.FoodMeter fm)
                            {

                                if (fm.showCount > 0)
                                    fm.circles[--fm.showCount].EatFade();


                            }
                        }

                        self.playerState.foodInStomach -= 1;
                        semiCost = 1;
                    }
                    else { semiCost++; }
                }

                PhysicalObject rock = self.grasps[grasp].grabbed;
                Vector2 vector = Vector2.Lerp(self.bodyChunks[0].pos, self.bodyChunks[1].pos, 0.50f);

                KBmultiplier = 1;

                if (self.gravity != 0f) //this code affects grav controls
                {
                    vector.y -= 12f;
                    if (self.input[0].x == 0)//no x pressing
                    {
                        vector.y -= 7f;
                        vector.x += 10f * self.ThrowDirection;
                        KBmultiplier = 2f;
                    }
                    else // x pressing
                    {
                        vector.x += -8f * self.ThrowDirection;
                    }
                }
                else // this code affects antigrav controls
                {
                    float X = self.input[0].x;
                    float Y = self.input[0].y;
                    vector -= new Vector2(X, Y) * 30f;
                }


                //adds shit to the room independent of rock HP
                rock.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, new Color(1, 0.2f, 0.2f)));
                rock.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1, 0.4f, 0.2f)));
                self.room.AddObject(new ExplosionSpikes(self.room, vector, 14, 30f, 5f, 7f, 250f, new Color(1, 0.2f, 0.2f)));
                rock.room.AddObject(new ShockWave(vector, 330f, 0.045f, 5));
                rock.room.AddObject(new Explosion.ExplosionLight(vector, 30f, 1f, 7, new Color(1, 0.8f, 0.7f)));
                rock.room.AddObject(new Explosion.FlashingSmoke(vector, new Vector2(0, 0), 1, new Color(1f, 0.2f, 0), new Color(0.4f, 0.1f, 0), UnityEngine.Random.Range(3, 11)));
                rock.room.AddObject(new Smoke.Smolder(rock.room, vector, rock.firstChunk, null));
                rock.room.InGameNoise(new InGameNoise(vector, 5000f, self, 1.7f));
                rock.room.PlaySound(SoundID.Gate_Water_Steam_Puff, vector + new Vector2(0, 10), 0.25f, 1.5f);


                self.room.AddObject(new Smoke.Smolder(self.room, vector, self.firstChunk, null));

                


                if (rockHealth[rock] == 0) //adds this to the room dependent on rock HP
                {
                    float tempStorage = powerCoefficient;
                    powerCoefficient *= 1.5f;
                    produceRockExplosion(self, rock, vector);
                    powerCoefficient = tempStorage;

                    self.room.AddObject(new ExplosionSpikes(self.room, vector, 14, 20f, 5f, 15f, 350f, new Color(1, 0.2f, 0.2f)));
                    rock.room.AddObject(new Explosion.ExplosionLight(vector, 300f, 1f, 10, new Color(1, 0.6f, 0.3f)));
                    rock.room.AddObject(new Explosion.ExplosionLight(vector, 30f, 1f, 7, new Color(1, 0.8f, 0.7f)));
                    rock.room.AddObject(new Explosion.FlashingSmoke(vector, new Vector2(0, 0), 1, new Color(1f, 0.2f, 0), new Color(0.4f, 0.1f, 0), UnityEngine.Random.Range(3, 11)));


                    rock.slatedForDeletetion = true;
                    rock.room.PlaySound(SoundID.Spear_Stick_In_Creature, vector, 1.5f, 0.7f);
                    rock.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, vector, 2f, 0.7f);
                }
                else if (rockHealth[rock] == 2)
                {
                    produceRockExplosion(self, rock, vector);
                    
                    rock.room.PlaySound(SoundID.Spear_Stick_In_Creature, vector, 2f, 0.7f);
                    rock.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, vector, 1f, 0.7f);
                }
                else if (rockHealth[rock] == 3)
                {
                    produceRockExplosion(self, rock, vector);

                    rock.room.PlaySound(SoundID.Spear_Stick_In_Creature, vector, 2.2f, 0.7f);
                    rock.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, vector, 0.8f, 0.7f);
                }
                else
                {
                    produceRockExplosion(self, rock, vector);
                    rock.room.AddObject(new Explosion(self.room, rock, vector, 4, 80f, 1.7f, 1.7f, 12f, 0.12f, self, 0.7f, 5, 1f));
                    rock.room.PlaySound(SoundID.Spear_Stick_In_Creature, vector, 1.5f, 0.7f);
                }

                rockHealth[rock] -= 1;



                rock.room.PlaySound(SoundID.Bomb_Explode, vector + new Vector2(0, 10), 0.5f, 1);

                rock.room.InGameNoise(new InGameNoise(vector, 5000f, self, 1.7f));

            }
            else if ((worldLoadingMod.isArena || self.playerState.foodInStomach > 0) && grabaroo is Creature && !(grabaroo is SmallNeedleWorm || grabaroo is Fly || grabaroo is Leech || grabaroo is Hazer || grabaroo is JetFish || grabaroo is LanternMouse || grabaroo is JellyFish))
            {
                //creature throw code
                Creature creature = (Creature)grabaroo;
                if (creature.dead) {
                    Vector2 vector = Vector2.Lerp(creature.firstChunk.pos, creature.firstChunk.lastPos, 0.35f);

                    
                    if ((grabaroo is Lizard && ((Lizard)grabaroo).Template.type == CreatureTemplate.Type.GreenLizard) || (grabaroo is Vulture || grabaroo is DaddyLongLegs || grabaroo is MirosBird || grabaroo is Deer))
                    {
                        creature.room.AddObject(new Explosion(self.room, creature, vector, 8, 320f, 8f, 8f, 40f, 0.5f, self, 1.5f, 20f, 2f));
                        creature.room.AddObject(new Explosion.ExplosionLight(vector, 560f, 2f, 14, new Color(1, 0.2f, 0.2f)));
                        creature.room.AddObject(new Explosion.ExplosionLight(vector, 460f, 2f, 6, new Color(1, 0.4f, 0.2f)));// Massless equation
                        creature.room.AddObject(new ExplosionSpikes(self.room, vector, 28, 60f, 10f, 14f, 500f, new Color(1, 0.2f, 0.2f)));
                        creature.room.AddObject(new ShockWave(vector, 660f, 0.1f, 5));
                    }
                    else if (creature is Snail)
                    {
                        creature.room.AddObject(new Explosion(self.room, creature, vector, 4, 160f, 4f, 4f, 20f, 0.12f, self, 0.7f, 10f, 1f));
                        creature.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, new Color(1, 0.2f, 0.2f)));
                        creature.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1, 0.4f, 0.2f)));// SNAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIL
                        creature.room.AddObject(new ExplosionSpikes(self.room, vector, 14, 30f, 5f, 7f, 250f, new Color(1, 0.2f, 0.2f)));
                        creature.room.AddObject(new ShockWave(vector, 330f, 0.045f, 5));
                    }
                    else
                    {
                        float mass = creature.TotalMass;
                        creature.room.AddObject(new Explosion(self.room, creature, vector, 6, 300f, 2 + .12f * mass, 2 + .12f * mass, 1 + 3f * mass, .01f + 0.02f * mass, self, 0.7f, 20f, 2f));
                        creature.room.AddObject(new Explosion.ExplosionLight(vector, 200f * mass, 1f, 7, new Color(1, 0.2f, 0.2f)));
                        creature.room.AddObject(new Explosion.ExplosionLight(vector, 150f * mass, 1f, 3, new Color(1, 0.4f, 0.2f)));// Mass equation
                        creature.room.AddObject(new ExplosionSpikes(self.room, vector, 20, 50f, 7f, 10f, 100f * mass, new Color(1, 0.2f, 0.2f)));
                        creature.room.AddObject(new ShockWave(vector, 100f * mass, 0.045f, 5)); ;
                    }


                    creature.room.PlaySound(SoundID.Bomb_Explode, vector);
                    creature.slatedForDeletetion = true;
                    if (!worldLoadingMod.isArena)
                    {
                        foreach (var cam in self.room.game.cameras)
                        {
                            if (cam.hud.owner == self && cam.hud.foodMeter is HUD.FoodMeter fm)
                            {

                                if (fm.showCount > 0)
                                    fm.circles[--fm.showCount].EatFade();


                            }
                        }
                    }

                    self.playerState.foodInStomach -= 1;
                }
            }
            orig(self, grasp, eu);
        }
    }
}
