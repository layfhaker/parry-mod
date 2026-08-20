using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using RepoValuableParry.Attacks;
using RepoValuableParry.Detection;
using RepoValuableParry.Destruction;
using RepoValuableParry.Effects;
using RepoValuableParry.Input;
using RepoValuableParry.Networking;
using RepoValuableParry.Physics;
using UnityEngine;

namespace RepoValuableParry.Core
{
    public sealed class ParryManager : MonoBehaviour
    {
        public static ParryManager Instance { get; private set; }

        readonly Dictionary<PlayerAvatar, ParryIntent> _intents = new Dictionary<PlayerAvatar, ParryIntent>();
        readonly Dictionary<int, float> _consumedUntil = new Dictionary<int, float>();
        readonly HashSet<int> _lockedValuables = new HashSet<int>();
        readonly List<int> _scratchIds = new List<int>();
        float _nextSustainScan;

        int _nextSequence = 1;

        public ParryRejectReason LastReject { get; private set; } = ParryRejectReason.None;
        public ParryContext LastContext { get; private set; }
        public ValuableStats LastStats { get; private set; }
        public AttackData LastAttack { get; private set; }
        public string LastEnemyName { get; private set; } = "-";
        public bool LastWouldParry { get; private set; }

        public static void Ensure()
        {
            if (Instance != null)
                return;
            var go = new GameObject("ValuableParryManager");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            Instance = go.AddComponent<ParryManager>();
        }

        void Update()
        {
            if (!ParryConfig.Enabled.Value)
                return;

            ExpireConsumed();
            SustainIntentWhileTouching();
            ExpireIntents();

            if (ParryInput.WasParryPressed())
                TryCreateLocalIntent();
        }

        void SustainIntentWhileTouching()
        {
            if (_intents.Count == 0)
                return;

            PlayerAvatar local = SemiFunc.PlayerGetLocal();
            if (local == null || !_intents.TryGetValue(local, out var intent) || intent == null)
                return;
            if (!intent.StillHoldsSameObject())
                return;
            if (Time.time < _nextSustainScan)
                return;
            _nextSustainScan = Time.time + 0.08f;

            Vector3 body = GenericMeleeAdapter.GetPlayerBodyPoint(local);
            var colliders = Object.FindObjectsOfType<HurtCollider>();
            foreach (var hc in colliders)
            {
                if (hc == null || !hc.isActiveAndEnabled)
                    continue;
                if (AttackDetector.FindAdapter(hc) == null)
                    continue;
                var col = hc.GetComponent<Collider>();
                if (col == null)
                    continue;
                if (col.bounds.SqrDistance(body) > 2.5f * 2.5f)
                    continue;
                intent.ExpiresAt = Mathf.Max(intent.ExpiresAt, Time.time + 0.2f);
                return;
            }
        }

        void TryCreateLocalIntent()
        {
            var player = SemiFunc.PlayerGetLocal();
            if (player == null)
            {
                Reject(ParryRejectReason.WrongPlayer, null);
                return;
            }

            if (!HeldValuableDetector.TryGetHeldValuable(player, out var phys, out var valuable, out var reject))
            {
                Reject(reject, null);
                Plugin.LogVerbose("Parry intent rejected: " + reject);
                return;
            }

            var intent = new ParryIntent
            {
                Player = player,
                PhysObject = phys,
                Valuable = valuable,
                CreatedAt = Time.time,
                ExpiresAt = Time.time + ParryConfig.ParryWindow.Value,
                SequenceHint = _nextSequence
            };
            _intents[player] = intent;
            LastReject = ParryRejectReason.None;
            Plugin.Log("Parry intent created  valuable=" + phys.gameObject.name.Replace("(Clone)", "").Trim());

            TryInterceptNearbyAttacks(player);
        }

        void TryInterceptNearbyAttacks(PlayerAvatar player)
        {
            var colliders = Object.FindObjectsOfType<HurtCollider>();
            Vector3 body = GenericMeleeAdapter.GetPlayerBodyPoint(player);
            foreach (var hc in colliders)
            {
                if (hc == null || !hc.isActiveAndEnabled)
                    continue;
                var adapter = AttackDetector.FindAdapter(hc);
                if (adapter == null)
                    continue;
                Vector3 origin = adapter.GetAttackOrigin(hc);
                if (Vector3.Distance(origin, body) > 18f && Vector3.Distance(hc.transform.position, body) > 8f)
                    continue;
                if (TryInterceptAttack(hc, player))
                    return;
            }
        }

        public bool IsAttackConsumed(HurtCollider collider)
        {
            if (collider == null)
                return false;
            int id = collider.GetInstanceID();
            return _consumedUntil.TryGetValue(id, out float until) && Time.time < until;
        }

        public bool TryInterceptAttack(HurtCollider collider, PlayerAvatar player)
        {
            LastWouldParry = false;
            if (!ParryConfig.Enabled.Value)
                return false;
            if (player == null || collider == null)
                return false;
            if (!GameAccess.IsLocalPlayer(player))
                return false;

            if (GameAccess.GetPlayerHealth(player) <= 0)
            {
                Reject(ParryRejectReason.PlayerDead, null);
                return false;
            }

            if (IsAttackConsumed(collider))
            {
                Reject(ParryRejectReason.AttackAlreadyConsumed, null);
                return false;
            }

            if (!_intents.TryGetValue(player, out var intent) || intent == null)
            {
                Reject(ParryRejectReason.NoIntent, null);
                return false;
            }

            if (intent.IsExpired)
            {
                _intents.Remove(player);
                Reject(ParryRejectReason.IntentExpired, null);
                return false;
            }

            if (!intent.StillHoldsSameObject())
            {
                Reject(intent.PhysObject == null ? ParryRejectReason.NoHeldObject : ParryRejectReason.HeldObjectChanged, null);
                _intents.Remove(player);
                return false;
            }

            var adapter = AttackDetector.FindAdapter(collider);
            if (adapter == null || !adapter.IsAttackActive(collider))
            {
                Reject(ParryRejectReason.UnsupportedAttack, SnapshotAttack(collider, adapter, player));
                return false;
            }

            var enemy = adapter.GetEnemy(collider);
            if (enemy == null)
            {
                Reject(ParryRejectReason.NoEnemySource, SnapshotAttack(collider, adapter, player));
                return false;
            }

            Vector3 origin = adapter.GetAttackOrigin(collider);
            Vector3 direction = adapter.GetAttackDirection(collider, player);
            Vector3 body = GenericMeleeAdapter.GetPlayerBodyPoint(player);
            var stats = ParryCapacityCalculator.Evaluate(intent.Valuable, intent.PhysObject, direction);
            LastStats = stats;
            LastEnemyName = EnemyDisplayName(enemy);

            if (_lockedValuables.Contains(intent.PhysObject.GetInstanceID()))
            {
                Reject(ParryRejectReason.AttackAlreadyConsumed, SnapshotAttack(collider, adapter, player));
                return false;
            }

            bool blocking = adapter is GenericMeleeAdapter melee
                ? melee.IsValuableBlocking(collider, stats.Bounds, player, out float coverage, out Vector3 contact)
                : ValuableCoverageDetector.CoversPlayer(stats.Bounds, origin, body, out coverage, out contact);

            if (!blocking)
            {
                stats.Coverage = coverage;
                LastStats = stats;
                Reject(ParryRejectReason.NotCoveringPlayer, SnapshotAttack(collider, adapter, player, origin, direction, contact));
                Plugin.LogVerbose(
                    $"NotCovering  enemy={LastEnemyName}  type={adapter.GetAttackType(collider)}  " +
                    $"origin={origin}  body={body}  valuable={stats.Bounds.center}  " +
                    $"size={stats.Bounds.size}  coverage={coverage:P0}");
                return false;
            }

            stats.Coverage = coverage;
            LastStats = stats;

            if (!stats.MeetsMinimumSize)
            {
                Reject(ParryRejectReason.TooSmall, SnapshotAttack(collider, adapter, player, origin, direction, contact));
                Plugin.LogVerbose($"TooSmall  area={stats.ProjectedArea:0.000}  min={ParryConfig.MinimumArea.Value:0.000}");
                return false;
            }

            float energy = adapter.GetAttackEnergy(collider);
            AttackEnergyCalculator.GetRaw(collider, out float dmg, out float hitForce, out float tumble);
            if (stats.Capacity < energy)
            {
                Reject(ParryRejectReason.InsufficientCapacity, SnapshotAttack(collider, adapter, player, origin, direction, contact, energy));
                Plugin.LogVerbose(
                    $"Capacity {stats.Capacity:0.0} < energy {energy:0.0}  " +
                    $"(dmg={dmg:0} force={hitForce:0.0} tumble={tumble:0.0}  " +
                    $"area={stats.ProjectedArea:0.00} mass={stats.Mass:0.00})");
                return false;
            }

            AttackEnergyCalculator.ScaleExplosion(energy, out float explosionForce, out float explosionRadius);

            var context = new ParryContext
            {
                Player = player,
                PhysObject = intent.PhysObject,
                Valuable = intent.Valuable,
                AttackCollider = collider,
                Enemy = enemy,
                AttackOrigin = origin,
                AttackDirection = direction,
                ContactPoint = contact,
                AttackEnergy = energy,
                ValuableCapacity = stats.Capacity,
                ExplosionForce = explosionForce,
                ExplosionRadius = explosionRadius,
                SequenceId = _nextSequence++,
                EffectSeed = Random.Range(1, int.MaxValue),
                Stats = stats,
                Adapter = adapter,
                Captured = true,
                ValuableLocked = true,
                Attack = SnapshotAttack(collider, adapter, player, origin, direction, contact, energy)
            };

            LastAttack = context.Attack;
            LastContext = context;
            LastReject = ParryRejectReason.None;
            LastWouldParry = true;
            _intents.Remove(player);

            BeginParry(context);
            return true;
        }

        public void ReceiveRemoteParryStart(ParryEvent evt)
        {
            if (LastContext != null && LastContext.SequenceId == evt.SequenceId)
                return;

            Vector3 pos = evt.ContactPoint;
            float intensity = Mathf.Clamp01(evt.AttackEnergy / Mathf.Max(1f, ParryConfig.MaxAttackEnergy.Value));
            intensity = Mathf.Lerp(0.6f, 1.4f, intensity) * ParryConfig.EffectsIntensity.Value;
            AttackEnergyCalculator.ScaleExplosion(evt.AttackEnergy, out _, out float radius);
            StartCoroutine(PlayRemoteVisuals(pos, radius, intensity));
        }

        IEnumerator PlayRemoteVisuals(Vector3 pos, float radius, float intensity)
        {
            ParryEffectController.PlayCapture(pos, intensity);
            var fx = ParryEffectController.PlayAbsorption(pos, null, intensity, ParryConfig.AbsorptionDuration.Value);
            yield return new WaitForSeconds(ParryConfig.FreezeDuration.Value);
            ParryEffectController.PlayOverload(pos, intensity);
            yield return new WaitForSeconds(ParryConfig.OverloadDuration.Value);
            ParryEffectController.PlayDetonation(pos, radius, intensity);
            if (fx != null)
                Destroy(fx);
        }

        void BeginParry(ParryContext context)
        {
            MarkConsumed(context.AttackCollider);
            if (context.PhysObject != null)
                _lockedValuables.Add(context.PhysObject.GetInstanceID());

            context.Adapter?.ConsumeAttack(context.AttackCollider);

            Plugin.Log(
                $"PARRY captured  enemy={LastEnemyName}  dmg={context.Attack.PlayerDamage}  " +
                $"force={context.Attack.PhysHitForce:0.00}  tumble={context.Attack.PlayerTumbleForce:0.00}  " +
                $"energy={context.AttackEnergy:0.0}  valuable={context.Stats.Name}  " +
                $"mass={context.Stats.Mass:0.00}  area={context.Stats.ProjectedArea:0.00}  " +
                $"coverage={context.Stats.Coverage:P0}  capacity={context.Stats.Capacity:0.0}");

            if (SemiFunc.IsMasterClientOrSingleplayer())
                ParryNetworkManager.BroadcastParryStart(context);
            else
                ParryNetworkManager.SendCommitToHost(context);

            StartCoroutine(RunSequence(context, applyWorldState: SemiFunc.IsMasterClientOrSingleplayer()));
        }

        public void HostCommitParry(int playerViewId, int enemyViewId, int valuableViewId, float energy, Vector3 contact, int seed)
        {
            var player = FindPlayer(playerViewId);
            var enemy = FindEnemy(enemyViewId);
            var phys = FindPhys(valuableViewId);
            var valuable = phys != null ? phys.GetComponent<ValuableObject>() : null;

            AttackEnergyCalculator.ScaleExplosion(energy, out float explosionForce, out float explosionRadius);
            var context = new ParryContext
            {
                Player = player,
                PhysObject = phys,
                Valuable = valuable,
                Enemy = enemy,
                ContactPoint = contact,
                AttackEnergy = energy,
                ExplosionForce = explosionForce,
                ExplosionRadius = explosionRadius,
                SequenceId = _nextSequence++,
                EffectSeed = seed,
                Adapter = new GenericMeleeAdapter()
            };

            if (phys != null)
                _lockedValuables.Add(phys.GetInstanceID());

            if (enemy != null)
            {
                var hc = enemy.GetComponentInChildren<HurtCollider>();
                if (hc != null)
                {
                    MarkConsumed(hc);
                    hc.enabled = false;
                }
            }

            ParryNetworkManager.BroadcastParryStart(context);
            StartCoroutine(RunSequence(context, applyWorldState: true, playLocalFx: false));
        }

        IEnumerator RunSequence(ParryContext context, bool applyWorldState, bool playLocalFx = true)
        {
            float intensity = ExplosionIntensity(context.AttackEnergy);
            Transform valuableTransform = context.PhysObject != null ? context.PhysObject.transform : null;

            if (applyWorldState)
                FreezeParticipants(context);

            GameObject absorption = null;
            if (playLocalFx)
            {
                ParryEffectController.PlayCapture(context.ContactPoint, intensity);
                yield return new WaitForSeconds(0.02f);
                absorption = ParryEffectController.PlayAbsorption(
                    context.ContactPoint,
                    valuableTransform,
                    intensity,
                    ParryConfig.AbsorptionDuration.Value);
            }

            yield return new WaitForSeconds(Mathf.Max(0.02f, ParryConfig.FreezeDuration.Value - 0.05f));

            if (playLocalFx)
            {
                ParryEffectController.PlayOverload(context.ContactPoint, intensity);
                yield return new WaitForSeconds(ParryConfig.OverloadDuration.Value);
            }
            else
            {
                yield return new WaitForSeconds(ParryConfig.OverloadDuration.Value);
            }

            Vector3 detonation = context.PhysObject != null
                ? context.PhysObject.centerPoint
                : context.ContactPoint;

            if (applyWorldState)
            {
                context.Sacrificed = ValuableSacrifice.Capture(context.Valuable, context.PhysObject);
                ValuableSacrifice.DestroyVanilla(context.Valuable, context.PhysObject);
            }

            if (playLocalFx)
                ParryEffectController.PlayDetonation(detonation, context.ExplosionRadius, intensity);

            ApplyKnockback(context, detonation, applyWorldState);
            if (applyWorldState)
                context.Adapter?.ApplyPostParryReaction(context.Enemy);

            if (absorption != null)
                Destroy(absorption, 0.2f);

            if (context.PhysObject != null)
                _lockedValuables.Remove(context.PhysObject.GetInstanceID());
        }

        void FreezeParticipants(ParryContext context)
        {
            float duration = ParryConfig.FreezeDuration.Value + ParryConfig.OverloadDuration.Value;
            context.Adapter?.FreezeAttack(context.AttackCollider, context.Enemy, duration);

            if (context.PhysObject != null)
                context.PhysObject.FreezeForces(duration, Vector3.zero, Vector3.zero);
        }

        void ApplyKnockback(ParryContext context, Vector3 detonation, bool applyEnemy)
        {
            if (context.Player != null && GameAccess.IsLocalPlayer(context.Player))
            {
                var tumble = GameAccess.GetTumble(context.Player);
                if (tumble != null)
                {
                    tumble.TumbleRequest(true, false);
                    tumble.TumbleOverrideTime(ParryConfig.PlayerTumbleTime.Value);
                    Vector3 playerPos = context.Player.transform.position;
                    tumble.TumbleForce(KnockbackCalculator.PlayerForce(detonation, playerPos, context.ExplosionForce));
                }
            }

            if (!applyEnemy || context.Enemy == null || !GameAccess.EnemyHasRigidbody(context.Enemy))
                return;

            var erb = GameAccess.GetEnemyRigidbody(context.Enemy);
            Vector3 enemyPos = erb != null ? erb.transform.position : context.Enemy.transform.position;
            Vector3 force = KnockbackCalculator.EnemyForce(detonation, enemyPos, context.ExplosionForce) * 2.2f;
            Vector3 torque = Vector3.Cross(force.normalized, Vector3.up) * context.ExplosionForce * 0.6f;

            // FreezeTimer zeroes velocity every FixedUpdate and skips FreezeForces.
            // Drop the freeze so the queued impulse actually launches the mob.
            var freezeField = HarmonyLib.AccessTools.Field(typeof(Enemy), "FreezeTimer");
            freezeField?.SetValue(context.Enemy, 0f);

            if (erb != null)
                erb.FreezeForces(force, torque);

            var rb = GameAccess.GetRigidbody(erb);
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.AddForce(force, ForceMode.Impulse);
                rb.AddTorque(torque, ForceMode.Impulse);
            }

            int damage = ParryConfig.EnemyDirectDamage.Value;
            if (damage > 0)
            {
                var health = GameAccess.GetHealth(context.Enemy);
                health?.Hurt(damage, force.normalized);
            }
        }

        static string EnemyDisplayName(Enemy enemy)
        {
            if (enemy == null)
                return "-";
            var parent = enemy.GetComponentInParent<EnemyParent>();
            if (parent != null && !string.IsNullOrEmpty(parent.enemyName))
                return parent.enemyName;
            return enemy.gameObject.name.Replace("(Clone)", "").Trim();
        }

        static PlayerAvatar FindPlayer(int viewId)
        {
            if (viewId == 0)
                return null;
            var view = PhotonView.Find(viewId);
            return view != null ? view.GetComponent<PlayerAvatar>() : null;
        }

        static Enemy FindEnemy(int viewId)
        {
            if (viewId == 0)
                return null;
            var view = PhotonView.Find(viewId);
            return view != null ? view.GetComponent<Enemy>() : null;
        }

        static PhysGrabObject FindPhys(int viewId)
        {
            if (viewId == 0)
                return null;
            var view = PhotonView.Find(viewId);
            return view != null ? view.GetComponent<PhysGrabObject>() : null;
        }

        AttackData SnapshotAttack(
            HurtCollider collider,
            IParryAttackAdapter adapter,
            PlayerAvatar player,
            Vector3 origin = default,
            Vector3 direction = default,
            Vector3 contact = default,
            float energy = -1f)
        {
            if (collider == null)
                return default;

            var data = new AttackData
            {
                ColliderId = collider.GetInstanceID(),
                Collider = collider,
                Enemy = adapter != null ? adapter.GetEnemy(collider) : collider.enemyHost,
                Origin = origin == default && adapter != null ? adapter.GetAttackOrigin(collider) : origin,
                Direction = direction == default && adapter != null ? adapter.GetAttackDirection(collider, player) : direction,
                ContactPoint = contact,
                Energy = energy >= 0f ? energy : adapter != null ? adapter.GetAttackEnergy(collider) : AttackEnergyCalculator.Compute(collider),
                PlayerDamage = collider.playerDamage,
                PhysHitForce = Mathf.Max(collider.physHitForce, collider.playerHitForce),
                PlayerTumbleForce = collider.playerTumbleForce,
                AttackType = adapter != null ? adapter.GetAttackType(collider) : ParryAttackType.Melee
            };
            LastAttack = data;
            return data;
        }

        void MarkConsumed(HurtCollider collider)
        {
            if (collider == null)
                return;
            _consumedUntil[collider.GetInstanceID()] = Time.time + 1.6f;
        }

        void ExpireConsumed()
        {
            if (_consumedUntil.Count == 0)
                return;
            _scratchIds.Clear();
            foreach (var kv in _consumedUntil)
            {
                if (Time.time >= kv.Value)
                    _scratchIds.Add(kv.Key);
            }
            foreach (int id in _scratchIds)
                _consumedUntil.Remove(id);
        }

        void ExpireIntents()
        {
            if (_intents.Count == 0)
                return;
            _scratchIds.Clear();
            PlayerAvatar expired = null;
            foreach (var kv in _intents)
            {
                if (kv.Value == null || kv.Value.IsExpired)
                {
                    expired = kv.Key;
                    break;
                }
            }
            if (expired != null)
                _intents.Remove(expired);
        }

        void Reject(ParryRejectReason reason, AttackData? attack)
        {
            LastReject = reason;
            LastWouldParry = false;
            if (attack.HasValue)
                LastAttack = attack.Value;
            if (ParryConfig.VerboseLogs.Value && reason != ParryRejectReason.NoIntent)
                Plugin.LogVerbose("Parry rejected: " + reason);
        }

        static float ExplosionIntensity(float energy)
        {
            float minE = ParryConfig.MinAttackEnergy.Value;
            float maxE = Mathf.Max(minE + 0.01f, ParryConfig.MaxAttackEnergy.Value);
            float n = Mathf.Clamp01((energy - minE) / (maxE - minE));
            return Mathf.Lerp(0.65f, 1.45f, Mathf.Sqrt(n)) * ParryConfig.EffectsIntensity.Value;
        }
    }
}
