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
    class slugcatColorMod
    {
		static int foodCount = 0;
        static int maxFood = 0;
        static float fullness;
        bool firstframe = true;

        public slugcatColorMod()
        {
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.Player.Update += Player_Update;
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            
            foodCount = self.FoodInStomach;
            maxFood = self.MaxFoodInStomach;
            fullness = foodCount / (float)maxFood;

            orig(self, eu);

            if(mod.rand.NextDouble() > 0.98f)
            {
               if(mod.rand.NextDouble() > fullness || (worldLoadingMod.isArena && mod.rand.NextDouble() > 0.80))
                {
                    Vector2 vector = self.firstChunk.pos;

                    self.room.AddObject(new Explosion.ExplosionLight(vector, 30f, 1f, 7, new Color(1, 0.8f, 0.7f)));
                    self.room.AddObject(new Explosion.FlashingSmoke(vector, new Vector2(0, 0), 1, new Color(1f, 0.2f, 0), new Color(0.4f, 0.1f, 0), UnityEngine.Random.Range(3, 11)));

                    self.room.PlaySound(SoundID.Gate_Water_Steam_Puff, vector + new Vector2(0, 10), 0.25f, 1.5f);
                }
            }
        }

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            //if (firstframe) //confirmed that this stuff runs only once
            //{
            //    firstframe = false;
            //}


            Color slugColor = PlayerGraphics.SlugcatColor(self.player.playerState.slugcatCharacter);
            Color postColor;

            
            if (worldLoadingMod.isArena == false)
            {
                float lerpAmount = fullness;
                
                postColor = Color.Lerp(slugColor * new Color(0.5f, 0.2f, 0.01f), slugColor, (float)Math.Pow(lerpAmount,2.5f));
            }
            else
            {
                postColor = slugColor;
            }

            sLeaser.sprites[0].color = postColor;
            sLeaser.sprites[1].color = postColor;
            sLeaser.sprites[2].color = postColor;
            sLeaser.sprites[3].color = postColor;
            sLeaser.sprites[4].color = postColor;
            sLeaser.sprites[5].color = postColor;
            sLeaser.sprites[6].color = postColor;
            sLeaser.sprites[7].color = postColor;
            sLeaser.sprites[8].color = postColor;
            // no 9
            // no 10
            // no 11 (mark of comms)
        }
    }
}
