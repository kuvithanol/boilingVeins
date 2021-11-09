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
    
    class explosionImmunityMod
    {
        float KBfactor = 4.5f;
        public explosionImmunityMod()
        {
            IL.Explosion.Update += Explosion_IL;
            On.Creature.Violence += Creature_Violence;
            On.Explosion.Update += Explosion_Update;
        }



        private void Explosion_Update(On.Explosion.orig_Update orig, Explosion self, bool eu)
        {
            self.force *= KBfactor;
            float radius = self.rad * (0.25f + 0.75f * Mathf.Sin(Mathf.InverseLerp(0f, self.lifeTime, self.frame) * Mathf.PI));
            for (int i = 0; i < self.room.physicalObjects.Length; i++)
            {
                for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
                {
                    if (self.sourceObject != self.room.physicalObjects[i][j] && !self.room.physicalObjects[i][j].slatedForDeletetion)
                    {
                        float stunFactor = 0f;
                        float minDistance = float.MaxValue;
                        int hitChunkIndex = -1;
                        for (int l = 0; l < self.room.physicalObjects[i][j].bodyChunks.Length; l++)
                        {
                            float tempDist = Vector2.Distance(self.pos, self.room.physicalObjects[i][j].bodyChunks[l].pos);
                            minDistance = Mathf.Min(minDistance, tempDist);
                            if (tempDist < radius)
                            {
                                float tempStunFactor = Mathf.InverseLerp(radius, radius * 0.25f, tempDist);
                                if (!self.room.VisualContact(self.pos, self.room.physicalObjects[i][j].bodyChunks[l].pos))
                                {
                                    tempStunFactor -= 0.5f;
                                }
                                if (tempStunFactor > 0f)
                                {
                                    self.room.physicalObjects[i][j].bodyChunks[l].vel += self.PushAngle(self.pos, self.room.physicalObjects[i][j].bodyChunks[l].pos) * (self.force * stunFactor / self.room.physicalObjects[i][j].bodyChunks[l].mass) * tempStunFactor;
                                    self.room.physicalObjects[i][j].bodyChunks[l].pos += self.PushAngle(self.pos, self.room.physicalObjects[i][j].bodyChunks[l].pos) * (self.force * stunFactor / self.room.physicalObjects[i][j].bodyChunks[l].mass) * tempStunFactor * 0.1f;
                                    if (tempStunFactor > stunFactor)
                                    {
                                        stunFactor = tempStunFactor;
                                        hitChunkIndex = l;
                                    }
                                }
                            }
                        }
                        if (hitChunkIndex > -1)
                        {
                            if (self.room.physicalObjects[i][j] is Player player)
                            {
                                if (player.graphicsModule != null && player.graphicsModule.bodyParts != null)
                                {
                                    for (int m = 0; m < player.graphicsModule.bodyParts.Length; m++)
                                    {
                                        player.graphicsModule.bodyParts[m].pos += self.PushAngle(self.pos, player.graphicsModule.bodyParts[m].pos) * stunFactor * self.force * 5f * stunFactor;
                                        player.graphicsModule.bodyParts[m].vel += self.PushAngle(self.pos, player.graphicsModule.bodyParts[m].pos) * stunFactor * self.force * 5f * stunFactor;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            self.force /= KBfactor; //reverts the boost to force after doing the slugcat specific version
            orig(self, eu);
        }
        private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (self is Player & type == Creature.DamageType.Explosion)
            {
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, 0, stunBonus);
            }
            else if (self is Player & type == Creature.DamageType.Stab)
            {
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage * 3, stunBonus);
            }
            else
            {
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            }

        }
        private void Explosion_IL(ILContext il)
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
