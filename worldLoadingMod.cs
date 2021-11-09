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

    class worldLoadingMod
    {
        public static bool isArena;
        public worldLoadingMod()
        {
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.WorldLoader.ctor += WorldLoader_ctor;
            On.ArenaGameSession.ctor += ArenaGameSession_ctor;
            On.GameSession.ctor += GameSession_ctor;
        }

        private void GameSession_ctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            orig(self, game);
            isArena = false;
        }

        private void ArenaGameSession_ctor(On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            orig(self, game);
            isArena = true;
        }

        private void WorldLoader_ctor(On.WorldLoader.orig_ctor orig, object self, RainWorldGame game, int playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            int tempchar = playerCharacter;
            if(playerCharacter == 0 || playerCharacter == 2)
            {
                playerCharacter = 2;
            }
            else
            {
                playerCharacter = 0;
            }
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
            playerCharacter = tempchar;
        }

        private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            rockLogicMod.semiCost = 1;
            rockLogicMod.rockHealth.Clear();
            rockLogicMod.rockHealth.Clear();
        }
    }
}
