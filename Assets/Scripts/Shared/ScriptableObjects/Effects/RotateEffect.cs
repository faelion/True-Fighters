using UnityEngine;
using UnityEngine.AI;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Rotate Effect", fileName = "RotateEffect")]
    public class RotateEffect : Effect
    {
        public override void Apply(ServerWorld world, GameEntity source, GameEntity target, Vector3? targetPos = null)
        {
            if (!source.TryGetComponent(out ServerGame.Entities.TransformComponent t)) return;

            float dx = targetPos.Value.x - t.posX;
            float dy = targetPos.Value.z - t.posY;
            float distSq = dx * dx + dy * dy;

            float dist = Mathf.Sqrt(distSq);
            float nx = dx / dist;
            float ny = dy / dist;

            float angle = Mathf.Atan2(nx, ny) * Mathf.Rad2Deg;
            t.rotZ = angle;

            Debug.Log($"[RotateEffect] Rotated {target.Id} to {angle}");
        }
    }
}
