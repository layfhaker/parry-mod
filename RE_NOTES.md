# Reverse-engineering notes (installed R.E.P.O. Assembly-CSharp.dll)

Game path: `C:\Program Files (x86)\Steam\steamapps\common\REPO`
Assembly: `REPO_Data\Managed\Assembly-CSharp.dll`

## PhysGrabber

- `public static PhysGrabber instance` — local grabber
- `public bool grabbed`
- `internal PhysGrabObject grabbedPhysGrabObject`
- `public PlayerAvatar playerAvatar`
- `public bool isLocal`
- Prefer `SemiFunc.PhysGrabberLocalGetGrabbedPhysGrabObject()` and `SemiFunc.PhysGrabberGetGrabbedPhysGrabObject(grabber)`

## PhysGrabObject

- `public bool grabbedLocal`
- `public List<PhysGrabber> playerGrabbing`
- `public Vector3 centerPoint`
- `public float massOriginal`
- `public Rigidbody rb`
- `public bool dead`
- `internal PhotonView photonView` — use `GetComponent<PhotonView>()`
- `public void FreezeForces(float time, Vector3 force, Vector3 torque)`
- `public void DestroyPhysGrabObject()` → RPC `DestroyPhysGrabObjectRPC` → `Object.Destroy`

## ValuableObject

- Presence of this component is the eligibility check (shop `Item` without it is rejected)
- `public PhysAttribute physAttributePreset` (`public float mass`)
- `internal float dollarValueCurrent` / `dollarValueOriginal`
- `internal PhysGrabObject physGrabObject`
- `internal PhysGrabObjectImpactDetector impactDetector`

## PhysGrabObjectImpactDetector

- `public void DestroyObject(bool effects = true)` — **vanilla destruction path** (host/singleplayer)
- Broadcasts `DestroyObjectRPC` in multiplayer
- Sets `physGrabObject.dead = true`, plays break particles/audio, fires `onDestroy`
- Actual GameObject destroy happens in `PhysGrabObject` when `dead && playerGrabbing.Count == 0`
- `public bool destroyDisable` — forced false before sacrifice so parry always consumes the valuable

## HurtCollider (damage intercept)

- Coroutine `ColliderCheck` every 0.05s, overlap box/sphere
- Player damage: `PlayerHurt(PlayerAvatar)` (private)
- Multiplayer: `PlayerHurt` returns early unless `_player.photonView.IsMine` (local victim authority)
- Fields used for AttackEnergy: `playerDamage`, `physHitForce`, `playerHitForce`, `playerTumbleForce`
- `public Enemy enemyHost`
- `public bool deathPit`, `playerKill`, `playerLogic`
- `CanHit` records the player for `playerDamageCooldown` (typically 0.25s) — first tick is the real hit
- Harmony **Prefix** on `HurtCollider.PlayerHurt`: if parry succeeds, skip vanilla hurt

## Enemy

- `public void Freeze(float time)` — host/singleplayer, RPCs `FreezeRPC` (not Time.timeScale)
- `internal bool HasRigidbody` / `internal EnemyRigidbody Rigidbody`
- `internal EnemyHealth Health` — `Hurt(int, Vector3)`
- `internal EnemyStateStunned StateStunned` — `Set(float)`
- Reaper = `EnemyRunner` (`public HurtCollider hurtCollider`)
- Trudge = `EnemySlowWalker`

## PlayerAvatar

- `public PhysGrabber physGrabber`
- `public PlayerHealth playerHealth` (`internal int health`)
- `internal PlayerTumble tumble`
- `public PlayerVisionTarget PlayerVisionTarget` (chest/head aim point)
- `public PhotonView photonView`
- `internal bool isLocal`

## PlayerTumble

- `public void TumbleRequest(bool isTumbling, bool playerInput)`
- `public void TumbleOverrideTime(float time)`
- `public void TumbleForce(Vector3 force)` — master-authoritative in MP

## Input

- `InputKey.Interact` bound as action `"Use"` default `<Keyboard>/e`
- `SemiFunc.InputDown(InputKey.Interact)` respects rebinds
- `InputManager.instance.KeyDown` ignores Interact while `disableMovementTimer > 0`

## SemiFunc helpers used

- `IsMasterClientOrSingleplayer()`, `IsMasterClient()`, `IsMultiplayer()`
- `PlayerGetLocal()`
- `PhysGrabberLocalIsGrabbing()`, `PhysGrabberLocalGetGrabbedPhysGrabObject()`
- `PhysGrabberGetGrabbedPhysGrabObject(PhysGrabber)`
- `CameraShakeImpactDistance`, `CameraShakeDistance`
- `InputDown(InputKey)`

## Networking implication

Player HP is already local-authoritative (`PlayerHealth.Hurt` requires `photonView.IsMine`). Parry intercept therefore runs on the victim client. Host still owns valuable destruction (`DestroyObject`) and `Enemy.Freeze`. Visual sequence is broadcast with `VP_ParryStart`.
