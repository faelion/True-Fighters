using System;
using System.Collections.Generic;
using ServerGame.Content;

namespace ServerGame.Systems
{
    // Minimal server-side ability processor: checks cooldown/range and triggers projectile spawns
    public class AbilitySystem : ISystem
    {
        // cooldowns[playerId][key] = seconds remaining
        private readonly Dictionary<int, Dictionary<string, float>> cooldowns = new Dictionary<int, Dictionary<string, float>>();

        public void Tick(ServerWorld world, float dt)
        {
            // 1) Cooldowns
            foreach (var kv in cooldowns)
            {
                var byKey = kv.Value;
                if (byKey == null) continue;
                var keys = new List<string>(byKey.Keys);
                foreach (var key in keys)
                {
                    float v = byKey[key];
                    if (v <= 0f) continue;
                    v -= dt;
                    byKey[key] = v < 0f ? 0f : v;
                }
            }

            // 2) Projectile simulation + collisions (unified here)
            List<int> toRemove = null;
            foreach (var kv in world.AbilityEffects)
            {
                var eff = kv.Value;
                eff.lifeMs -= (int)(dt * 1000f);

                if (eff.lifeMs <= 0)
                {
                    (toRemove ??= new List<int>()).Add(eff.id);
                    continue;
                }

                if (eff.type == AbilityEffectType.Projectile)
                {
                    float step = eff.speed * dt;
                    eff.posX += eff.dirX * step;
                    eff.posY += eff.dirY * step;

                    foreach (var p in world.Players.Values)
                    {
                        if (p.playerId == eff.ownerPlayerId)
                            continue;
                        float dx = p.posX - eff.posX;
                        float dy = p.posY - eff.posY;
                        float dist2 = dx * dx + dy * dy;
                        if (dist2 < 0.25f)
                        {
                            p.hit = true;
                            eff.lifeMs = 0;
                            (toRemove ??= new List<int>()).Add(eff.id);
                            break;
                        }
                    }
                }
            }

            if (toRemove != null)
            {
                foreach (var id in toRemove)
                {
                    world.AbilityEffects.Remove(id);
                    world.MarkEffectDespawned(id);
                }
            }
        }

        public bool TryCast(ServerWorld world, int playerId, string key, float targetX, float targetY)
        {
            if (!world.Players.TryGetValue(playerId, out var caster))
                caster = world.EnsurePlayer(playerId);

            if (!world.AbilityBooks.TryGetValue(playerId, out var book) || book == null || !book.TryGetValue(key, out var def))
                return false; // no such ability

            if (!cooldowns.TryGetValue(playerId, out var cdByKey))
            {
                cdByKey = new Dictionary<string, float>();
                cooldowns[playerId] = cdByKey;
            }

            if (cdByKey.TryGetValue(key, out float cd) && cd > 0f)
                return false; // still on cooldown

            // Range check (point targeting)
            float dx = targetX - caster.posX;
            float dy = targetY - caster.posY;
            float dist2 = dx * dx + dy * dy;
            if (dist2 > def.range * def.range)
                return false;

            // Cast time omitted for now (instant)
            // Payload by kind
            switch (def.kind)
            {
                case Content.AbilityKind.Projectile:
                {
                    int projId = world.SpawnAbilityProjectile(playerId, targetX, targetY, def.projectileSpeed, def.projectileLifeMs, def.id);
                    world.MarkEffectSpawned(projId);
                    break;
                }
                case Content.AbilityKind.Area:
                {
                    int areaId = world.SpawnAbilityArea(playerId, targetX, targetY, def.areaRadius, def.areaLifeMs, def.id);
                    world.MarkEffectSpawned(areaId);
                    break;
                }
                case Content.AbilityKind.Dash:
                {
                    // Emit a dash event for VFX/UI (server-authoritative move not implemented here)
                    float dxd = targetX - caster.posX;
                    float dyd = targetY - caster.posY;
                    float len = (float)Math.Sqrt(dxd * dxd + dyd * dyd);
                    if (len < 0.0001f) { dxd = 1; dyd = 0; len = 1; }
                    dxd /= len; dyd /= len;
                    world.EnqueueAbilityEvent(new AbilityEventMessage
                    {
                        abilityIdOrKey = def.id,
                        casterId = playerId,
                        eventType = AbilityEventType.Dash,
                        posX = caster.posX,
                        posY = caster.posY,
                        dirX = dxd,
                        dirY = dyd,
                        speed = def.dashSpeed,
                        serverTick = Environment.TickCount
                    });
                    break;
                }
                case Content.AbilityKind.Heal:
                {
                    // Emit a heal event with amount for VFX/UI
                    world.EnqueueAbilityEvent(new AbilityEventMessage
                    {
                        abilityIdOrKey = def.id,
                        casterId = playerId,
                        eventType = AbilityEventType.Heal,
                        posX = targetX,
                        posY = targetY,
                        value = def.healAmount,
                        serverTick = Environment.TickCount
                    });
                    break;
                }
            }
            
            // Set cooldown
            cdByKey[key] = def.cooldown;
            return true;
        }
    }
}
