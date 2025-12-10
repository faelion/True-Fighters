using System.Collections.Generic;
using UnityEngine;
using Shared.ScriptableObjects;

namespace ServerGame.Systems
{
    public class ServerGameLoopSystem : ISystem
    {
        public enum GameState { Waiting, Playing, Finished }
        public GameState CurrentState { get; private set; } = GameState.Playing;

        private float gameTime = 0f;

        public void Tick(ServerWorld world, float dt)
        {
            if (world.GameMode == null) return;

            if (CurrentState == GameState.Playing)
            {
                gameTime += dt;

                if (world.GameMode.victoryCondition != null)
                {
                    if (world.GameMode.victoryCondition.CheckVictory(world, gameTime, out int winner))
                    {
                        Debug.Log($"[ServerGameLoop] Game Over! Winner Team: {winner}");
                        CurrentState = GameState.Finished;
                        // In future: Send GAME_OVER packet
                    }
                }
            }
        }
    }
}
