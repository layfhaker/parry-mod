# R.E.P.O. Valuable Parry — технический план мода

## 1. Концепт

Мод добавляет **парирование атак мобов ценностями (Valuable)**.

Это не обычный удар предметом и не пассивный блок.

Базовая последовательность:

1. Моб начинает поддерживаемую атаку.
2. Игрок держит `ValuableObject`.
3. Valuable физически находится между источником атаки и телом игрока.
4. Игрок нажимает **E уже во время атаки**.
5. Мод проверяет тайминг, тип атаки, положение Valuable и её способность поглотить силу удара.
6. При успехе исходный hit отменяется.
7. Атака/моб на короткий момент замирает.
8. Энергия удара визуально втягивается в Valuable.
9. Через короткий delay Valuable полностью уничтожается специальным энергетическим взрывом.
10. Моб и игрок отлетают в противоположные стороны.
11. Мощность взрыва зависит от силы исходной атаки.

Главное отличие от обычного удара:

> Без E предмет ведёт себя полностью ванильно.  
> С E в правильный момент игрок **осознанно жертвует Valuable**, а атака превращается в отдельное событие: freeze → absorption → overload → detonation.

---

# 2. Основные дизайн-правила

- Только `ValuableObject`.
- Магазинные `Item` не подходят.
- Никакого пассивного блока простым удержанием предмета.
- Никакого условия «махни предметом навстречу атаке».
- Нажатие **E обязательно**.
- Нажатие E вне активной атаки ничего не даёт.
- Valuable должна реально прикрывать игрока от направления атаки.
- Не любой Valuable способен поглотить любой удар.
- Способность парировать зависит прежде всего от **размера/площади + массы**, не от цены.
- Цена Valuable — только экономическая цена жертвы.
- Successful parry всегда полностью уничтожает Valuable.
- Сила взрыва зависит от силы поглощённой атаки.
- Игрок тоже получает knockback/tumble.
- Сам captured hit не наносит игроку исходный damage.
- Парирование не должно становиться сильным offensive weapon: основной эффект на моба — stagger/knockback, а не большой direct damage.
- Обычные удары Valuable по мобам вообще не менять.

---

# 3. Почему E

Использовать **E**, а не отдельную боевую кнопку.

Причины:

- E уже воспринимается как interaction в R.E.P.O.;
- у Valuable нет собственного осмысленного E-action;
- это ощущается как «использовать ценность ради спасения», а не как отдельная combat stance;
- меньше новых правил для игрока.

Лучше не хардкодить физическую клавишу, а использовать ванильный `Interact` action, если его удастся удобно получить. Тогда переназначения и контроллеры продолжат работать.

Fallback для первого прототипа — прямое чтение E.

---

# 4. State machine

```text
Idle
  ↓
E pressed while holding Valuable
  ↓
ParryIntent (короткий buffer)
  ↓
Incoming supported enemy hit
  ↓
Validation
  ├─ fail → vanilla hit
  └─ success
        ↓
     Captured
        ↓
     Freeze
        ↓
     Absorption
        ↓
     Detonation
        ↓
     Knockback
        ↓
       Done
```

## ParryIntent

E не должен мгновенно искать всё вокруг и сам выбирать удар.

Лучше после нажатия создать очень короткий intent:

```text
примерно 150–200 ms
```

Далее настоящий enemy hit callback проверяет, есть ли ещё активный intent.

Это решает различия между `Update`, `FixedUpdate` и небольшой multiplayer latency.

---

# 5. Определение удерживаемого Valuable

У существующих модов используется локальный `PhysGrabber` и текущий `PhysGrabObject`.

Ожидаемый путь:

```csharp
PhysGrabber.instance
PhysGrabber.instance.grabbed
PhysGrabber.instance.grabbedPhysGrabObject
```

Проверка:

```csharp
PhysGrabObject obj = PhysGrabber.instance?.grabbedPhysGrabObject;

if (obj == null)
    fail;

ValuableObject valuable = obj.GetComponent<ValuableObject>();

if (valuable == null)
    fail;
```

Желательно дополнительно проверить локальный захват (`grabbedLocal`), если поле остаётся актуальным в текущем билде игры.

**Никаких whitelist по названиям prefab.**

Это даст совместимость с обычными кастомными Valuable.

---

# 6. Магазинные предметы

Парирование должно определяться именно по наличию:

```csharp
ValuableObject
```

а не по тому, что объект просто является `PhysGrabObject`.

То есть:

```text
ValuableObject → eligible
Item without ValuableObject → not eligible
```

Shop weapons, guns, grenades, heals, drones, upgrades и другие покупаемые предметы не должны работать.

---

# 7. E не должен создавать магический щит

Недостаточно:

```text
держу Valuable + нажал E + рядом моб
```

Предмет должен физически закрывать игрока.

Пример:

```text
VALID

Enemy
  ↓
[VALUABLE]
  ↓
Player
```

```text
INVALID

Enemy
  ↓

        [VALUABLE]

Player
```

---

# 8. Проверка покрытия игрока

Для входящей атаки нужны:

```text
attackOrigin
playerBodyPoint
heldValuable colliders/bounds
```

Строится линия:

```text
attackOrigin ─────────► player chest
```

И проверяется, пересекает ли она Valuable до игрока.

MVP:

1. собрать `Bounds` всех collider'ов Valuable;
2. немного расширить bounds для forgiveness;
3. проверить пересечение attack ray/segment с bounds;
4. убедиться, что Valuable находится между атакой и игроком.

Стартовый forgiveness:

```text
~10–20 cm
```

Позже заменить на более точную проверку реальных collider'ов через `Collider.Raycast`/`Physics.Raycast`.

---

# 9. Размер важнее цены

Нельзя делать:

```text
маленькая драгоценность за $10k
>
огромный телевизор за $1k
```

в способности остановить удар.

Цена предмета не участвует в `ParryCapacity`.

Она уже влияет на решение игрока:

> «Стоит ли мне сейчас уничтожать эту дорогую хрень?»

---

# 10. ParryCapacity Valuable

Для каждой попытки считается:

```text
ParryCapacity
```

Рекомендуемая база:

```text
~70% размер/площадь
~30% масса
```

Не использовать только массу и не использовать только volume.

## Почему projected area

Важно, какой стороной Valuable повернута к атаке.

Картина:

```text
широкой стороной к удару → высокая capacity
ребром → сильно ниже
```

Это отлично соответствует физической философии R.E.P.O.

Для box bounds можно приближённо считать:

```text
Axy = size.x * size.y
Axz = size.x * size.z
Ayz = size.y * size.z

ProjectedArea =
    Axy * abs(dir.z)
  + Axz * abs(dir.y)
  + Ayz * abs(dir.x)
```

Далее:

```text
SizeScore = ProjectedArea * SizeMultiplier
MassScore = sqrt(Mass) * MassMultiplier

ParryCapacity =
    SizeScore * 0.7
  + MassScore * 0.3
```

Коэффициенты должны настраиваться тестами, а не восприниматься как финальные.

---

# 11. Hard minimum size

Нужен отдельный минимальный размер, чтобы исключить абсурд:

```text
яблоко
маленькая кружка
маленькая фигурка
```

Даже если у prefab странная масса.

Варианты hard gate:

```text
ProjectedArea >= MinArea
```

и/или:

```text
CoverageOfPlayerTorso >= MinCoverage
```

Лучше итогово использовать процент покрытия торса.

Пример логики:

```text
< 25% покрытия → никогда не parry
```

Точное число подобрать playtest'ом.

---

# 12. Сила атаки

Для атаки вычисляется не настоящая физическая энергия, а gameplay score:

```text
AttackEnergy
```

У `HurtCollider` в существующих enemy-модах используются параметры вроде:

```csharp
playerDamage
physHitForce
physHitTorque
playerTumbleForce
```

Стартовая модель:

```text
AttackEnergy =
    playerDamage      * DamageWeight
  + physHitForce      * ForceWeight
  + playerTumbleForce * TumbleWeight
```

Почему не только damage:

- одна атака может наносить много HP, но почти не толкать;
- другая может наносить меньше HP, но иметь огромный физический импульс.

Для визуальной «поглощённой энергии» это разные события.

---

# 13. Условие успешного поглощения

```csharp
bool CanAbsorb =
    MeetsMinimumSize
    && ParryCapacity >= AttackEnergy;
```

Для MVP результат **бинарный**:

```text
capacity достаточно → full parry
capacity недостаточно → обычный hit
```

Не вводить сразу partial block.

---

# 14. Что происходит при недостаточной capacity

Пример:

```text
Trudge наносит тяжёлый удар
игрок держит маленький Valuable
игрок нажимает E
```

Если capacity недостаточно:

- нет freeze;
- нет absorption;
- нет специального взрыва;
- нет гарантированного уничтожения Valuable;
- атака продолжается ванильно.

Valuable может потом разбиться от обычной физики моба — но это уже не parry.

---

# 15. Какие атаки поддерживать

Не считать parryable всё, что наносит damage.

Ввести категории:

```csharp
enum ParryAttackType
{
    Melee,
    Charge,
    Bite,
    BodySlam,
    Grab,
    Projectile,
    Explosion,
    Environmental,
    Special
}
```

## MVP

Поддерживать:

- melee hand/swipe;
- bite;
- прямой body hit;
- melee charge, если есть понятный источник удара.

Не поддерживать пока:

- bullets;
- lasers;
- explosions;
- traps;
- pits;
- falls;
- electricity;
- status damage;
- PvP;
- self damage;
- grabs;
- special scripted insta-kills.

---

# 16. Attack Adapter architecture

Разные враги устроены по-разному, поэтому не стоит писать один огромный Harmony patch с десятками `if`.

Интерфейс:

```csharp
interface IParryAttackAdapter
{
    bool CanHandle(HurtCollider collider);
    bool IsAttackActive(HurtCollider collider);

    Enemy GetEnemy(HurtCollider collider);

    Vector3 GetAttackOrigin(HurtCollider collider);
    Vector3 GetAttackDirection(
        HurtCollider collider,
        PlayerAvatar target);

    float GetAttackEnergy(HurtCollider collider);

    void FreezeAttack(float duration);
    void ConsumeAttack();
    void ApplyPostParryReaction();
}
```

Структура:

```text
Attacks/
  GenericMeleeAdapter.cs
  ReaperAdapter.cs
  TrudgeAdapter.cs
  ...
```

Сначала попробовать generic путь через `HurtCollider`.

Enemy-specific adapter нужен только если конкретная атака ведёт себя нестандартно.

---

# 17. Перехват damage

Первая задача reverse engineering:

1. открыть актуальный `Assembly-CSharp.dll`;
2. найти `HurtCollider`;
3. найти method, через который enemy collider собирается применить `playerDamage`;
4. найти наиболее общий callback **до изменения HP**;
5. поставить Harmony Prefix;
6. перед vanilla damage вызвать:

```csharp
ParryManager.TryInterceptAttack(...)
```

Если success:

```text
vanilla hit не проходит
```

Важно ловить именно enemy hit, а не потом возвращать HP игроку.

---

# 18. Почему решение принимается на hit callback

E создаёт только intent.

Настоящая схема:

```text
T=0
player presses E

T=+40 ms
enemy HurtCollider реально пытается ударить player

ParryManager:
- intent still active?
- same Valuable still held?
- attack supported?
- Valuable covers player?
- capacity enough?

YES
→ capture attack
```

Это намного надёжнее, чем на E заранее угадывать, какой enemy сейчас атакует.

---

# 19. Freeze

Это главный визуальный маркер parry.

Стартовая длительность:

```text
~180 ms
```

Диапазон для теста:

```text
120–300 ms
```

**Не использовать глобальный `Time.timeScale`.**

В multiplayer это остановит весь мир и создаст проблемы.

Замораживать только участников события:

- атакующего enemy;
- конкретную attack animation/state;
- Valuable;
- при необходимости очень коротко игрока.

---

# 20. После freeze старая атака должна исчезнуть

Нельзя:

```text
freeze
→ взрыв
→ enemy продолжает тот же animation hit
→ снова наносит damage
```

После успешного parry атака получает:

```text
Consumed = true
```

Adapter должен:

- закрыть hit window;
- сбросить attack state;
- перевести enemy в recovery/stagger/idle;
- не позволить тому же `HurtCollider` повторно обработать этот же swing.

---

# 21. Один attack нельзя парировать дважды

У active attack нужен идентификатор или tracking.

Например:

```csharp
HashSet<int> consumedAttackInstances;
```

Если collider остаётся активным несколько physics ticks, он всё равно должен считаться одним ударом.

---

# 22. Visual timeline

## 0 ms — Capture

- vanilla hit отменён;
- enemy резко стопается;
- обычный impact sound подавляется;
- короткий тяжёлый `thump/vacuum`.

## 20–160 ms — Absorption

Энергия идёт:

```text
contact point → Valuable
```

Эффекты:

- втягивающиеся частицы;
- линии/дуги;
- emission/glow;
- внутренние трещины;
- лёгкая дрожь;
- небольшое distortion;
- rising hum.

## ~160–190 ms — Overload

- glow резко усиливается;
- crack pattern ускоряется;
- pitch звука растёт.

## ~190 ms — Detonation

- Valuable разрушается изнутри;
- radial shockwave;
- fragments;
- enemy отлетает;
- player отлетает;
- мощный bass hit;
- camera shake.

---

# 23. Взрыв не должен выглядеть как граната

Никакого обязательного fireball.

Лучше визуальный язык:

```text
kinetic absorption
→ overload
→ pressure/energy shockwave
```

Так игрок считывает именно «предмет впитал удар», а не «в предмете была бомба».

---

# 24. Полное уничтожение Valuable

Successful parry:

```text
100% sacrifice
```

Не:

```text
-20% value
```

Не:

```text
random chance to break
```

Не:

```text
сломается только если fragile
```

Именно гарантированная потеря создаёт экономическую цену механики.

---

# 25. Не делать `Destroy(gameObject)` вслепую

Нужно найти ванильный destruction path `ValuableObject`/`PhysGrabObjectImpactDetector`.

Причины:

- value tracking;
- fragments;
- audio;
- Photon/network cleanup;
- extraction totals;
- object ownership;
- другие моды.

Задача reverse engineering:

- найти изменение `dollarValueCurrent`;
- найти полный destruction callback;
- найти impact/break logic;
- вызвать максимально ванильный путь;
- поверх него проиграть наши energy VFX.

---

# 26. Данные Valuable нужно сохранить до destruction

Перед разрушением сохранить:

```csharp
struct SacrificedValuableData
{
    string Name;
    float DollarValue;
    float Mass;

    Bounds Bounds;

    Vector3 Position;
    Quaternion Rotation;
}
```

После ванильного destroy оригинальный объект уже может быть недоступен, а VFX ещё продолжаются.

---

# 27. Масштаб взрыва

Мощность берётся из `AttackEnergy`.

Не делать полностью линейный бесконечный scale.

Например:

```csharp
float normalized = Mathf.Clamp01(
    (energy - minEnergy) /
    (maxEnergy - minEnergy)
);

float explosionForce =
    Mathf.Lerp(
        minForce,
        maxForce,
        Mathf.Sqrt(normalized)
    );
```

Масштабировать:

- radius;
- enemy knockback;
- player knockback;
- fragment velocity;
- shockwave size;
- camera shake;
- particle intensity;
- audio intensity.

Delay лучше держать примерно одинаковым, чтобы parry всегда читался одинаково.

---

# 28. Knockback игрока обязателен

Это core mechanic, а не наказание сбоку.

Игрок спасается от исходной атаки, но может:

- улететь в яму;
- свалиться с лестницы;
- врезаться в стену;
- попасть под другого моба;
- сбить товарища;
- снести другие Valuable.

Именно это делает парирование R.E.P.O.-шным, а не безопасной invulnerability-кнопкой.

---

# 29. Направление knockback

Центр:

```text
detonationPoint
```

Enemy:

```csharp
(enemy.position - detonationPoint).normalized
```

Player:

```csharp
(player.position - detonationPoint).normalized
```

С небольшим `Vector3.up * upBias`.

Получается:

```text
Enemy ← [Explosion] → Player
```

---

# 30. Direct damage мобу

Для первой версии:

```text
0 или очень маленький direct damage
```

Главный эффект:

- attack cancelled;
- stagger;
- knockback.

Иначе sacrifice станет слишком сильным offensive инструментом.

---

# 31. Окружающая физика

После MVP можно позволить shockwave толкать:

- других игроков;
- другие Valuable;
- мусор;
- физические объекты.

Особенно хорошо, если parry спас игрока, но ударной волной разбил ещё две дорогие вещи.

Однако первый прототип должен гарантированно толкать только:

```text
attacker + parrying player
```

---

# 32. Multiplayer authority

Клиент не должен сам решать:

```text
"я успешно парировал"
```

## Client

При E отправляет:

```text
ParryRequest
```

Минимум:

```text
player id
held Valuable network id
input sequence/timestamp
```

## Host

Проверяет:

1. игрок действительно держит этот Valuable;
2. объект существует;
3. это `ValuableObject`;
4. есть подходящая активная attack;
5. геометрия покрытия валидна;
6. timing валиден;
7. capacity >= attack energy;
8. attack ещё не consumed.

Только host принимает `SUCCESS`.

## Broadcast

Host отправляет:

```text
ParryStartEvent
```

с:

```text
sequence id
player id
enemy id
valuable id
attack energy
contact point
effect seed
```

---

# 33. Multiplayer timeline

Все клиенты проигрывают одну последовательность:

```text
0ms freeze
~180ms detonation
then knockback
```

Host должен быть авторитетом для:

- damage cancellation;
- Valuable destruction;
- enemy state;
- physics impulse, где это важно.

Клиенты могут локально проигрывать:

- particles;
- audio;
- camera shake;
- cosmetic fragments.

---

# 34. План multiplayer разработки

Не пытаться делать сеть в первый день.

Порядок:

1. singleplayer;
2. host player in multiplayer;
3. remote client parry;
4. latency handling;
5. double-request protection;
6. late join cleanup/state check.

---

# 35. Архитектура файлов

```text
RepoValuableParry/
│
├── Plugin.cs
│
├── Core/
│   ├── ParryManager.cs
│   ├── ParryIntent.cs
│   ├── ParryContext.cs
│   ├── AttackData.cs
│   ├── ValuableStats.cs
│   └── ParryRejectReason.cs
│
├── Input/
│   └── ParryInput.cs
│
├── Detection/
│   ├── HeldValuableDetector.cs
│   ├── ValuableCoverageDetector.cs
│   └── AttackDetector.cs
│
├── Physics/
│   ├── ParryCapacityCalculator.cs
│   ├── AttackEnergyCalculator.cs
│   └── KnockbackCalculator.cs
│
├── Attacks/
│   ├── IParryAttackAdapter.cs
│   ├── GenericMeleeAdapter.cs
│   └── EnemySpecific/
│       ├── ReaperAdapter.cs
│       ├── TrudgeAdapter.cs
│       └── ...
│
├── Destruction/
│   └── ValuableSacrifice.cs
│
├── Effects/
│   ├── ParryEffectController.cs
│   ├── AbsorptionEffect.cs
│   ├── DetonationEffect.cs
│   ├── ParryAudio.cs
│   └── CameraEffects.cs
│
├── Networking/
│   ├── ParryNetworkManager.cs
│   ├── ParryRequest.cs
│   └── ParryEvent.cs
│
├── Patches/
│   ├── HurtColliderPatch.cs
│   └── ValuablePatch.cs
│
└── Debug/
    ├── ParryDebugOverlay.cs
    ├── DebugGizmos.cs
    └── DebugCommands.cs
```

---

# 36. ParryContext

```csharp
public sealed class ParryContext
{
    public PlayerAvatar Player;

    public PhysGrabObject PhysObject;
    public ValuableObject Valuable;

    public HurtCollider AttackCollider;
    public Enemy Enemy;

    public Vector3 AttackOrigin;
    public Vector3 AttackDirection;
    public Vector3 ContactPoint;

    public float AttackEnergy;
    public float ValuableCapacity;

    public float ExplosionForce;
    public float ExplosionRadius;

    public int SequenceId;
}
```

---

# 37. Validation pipeline

Не писать один огромный `if`.

```text
TryInterceptAttack
  ↓
HasActiveIntent
  ↓
StillHoldingSameObject
  ↓
IsValuable
  ↓
SupportedAttack
  ↓
AttackTargetsPlayer
  ↓
ValuableCoversAttackLine
  ↓
MeetsMinimumSize
  ↓
Capacity >= Energy
  ↓
AttackNotConsumed
  ↓
SUCCESS
```

---

# 38. Причины отказа

```csharp
enum ParryRejectReason
{
    NoIntent,
    IntentExpired,
    NoHeldObject,
    HeldObjectChanged,
    NotValuable,
    UnsupportedAttack,
    NoEnemySource,
    NotCoveringPlayer,
    TooSmall,
    InsufficientCapacity,
    AttackAlreadyConsumed,
    NetworkAuthority
}
```

В debug mode логировать конкретную причину.

---

# 39. Debug overlay

В developer mode показывать:

```text
Valuable: Television
Mass: 2.4
Projected Area: 0.82
Coverage: 61%
Capacity: 46

Enemy: Reaper
Damage: 20
Hit Force: 3
Tumble Force: 2
Attack Energy: 42

Result:
PARRY POSSIBLE
```

В релизе overlay выключен.

---

# 40. Debug gizmos

Рисовать:

```text
RED    incoming attack line
GREEN  Valuable bounds/colliders
YELLOW contact point
BLUE   knockback vectors
```

Это особенно важно для отладки ситуаций «почему предмет был передо мной, но не сработало».

---

# 41. Конфиг

Пользовательские настройки держать минимальными:

```text
Enable Mod
Use Vanilla Interact / Parry Key
Parry Window
Player Knockback Multiplier
Enemy Knockback Multiplier
Effects Intensity
Camera Shake
```

Developer/balance:

```text
DamageWeight
ForceWeight
TumbleWeight

SizeWeight
MassWeight

MinimumCoverage
MinimumArea

FreezeDuration

MinExplosionForce
MaxExplosionForce
MinExplosionRadius
MaxExplosionRadius
```

---

# 42. Что не показывать игроку

Не нужен обычный UI:

- Shield Strength: 46
- Attack: 42
- Parry Meter
- Cooldown Bar

Это убивает физическую интуицию.

Игрок должен думать:

> «Эта огромная хрень, наверное, меня прикроет».

Точные числа — только debug mode.

---

# 43. Edge cases

## Valuable отпущена после E, но до hit

Если capture ещё не произошёл:

```text
fail
```

## Valuable отпущена после capture

Sequence уже зафиксирована:

```text
она всё равно уничтожается
```

## Два игрока держат один Valuable

Parry owner = игрок, отправивший request.

После успешного capture Valuable получает lock.

## Два игрока одновременно нажали E

Первый authoritative success consumes Valuable.

Второй request → fail.

## Два enemy hits приходят почти одновременно

Первый успешный capture блокирует Valuable до конца sequence.

## Игрок умер от другого источника во время absorption

Captured hit уже отменён, но sacrifice sequence всё равно заканчивается.

## Enemy умер/исчез во время freeze

Valuable всё равно уничтожается; enemy impulse просто пропускается.

---

# 44. Unparryable атаки

Некоторые атаки должны явно иметь:

```text
CanParry = false
```

Даже при огромном Valuable:

- scripted instant kills;
- AoE без понятного направления;
- environmental damage;
- специальные состояния;
- атаки, для которых freeze ломает AI.

Это задаётся adapter'ом.

---

# 45. Сверхсильные, но parryable атаки

Не надо делать любую сильную атаку автоматически unparryable.

Правильнее:

```text
очень сильный удар
→ требует реально огромного Valuable
→ создаёт огромный explosion
```

Это как раз рождает лучшие моменты.

---

# 46. Обычный удар Valuable

Абсолютно важное правило:

```text
если игрок не нажал E
→ мод не вмешивается
```

Даже если игрок физически въебал Valuable прямо в атакующую руку моба:

```text
это обычная ванильная физика
```

Так сохраняется принципиальное различие.

---

# 47. Проверка визуальной читаемости

Снять два коротких клипа без HUD.

### Clip A

Игрок бьёт моба Valuable.

Зритель должен видеть:

> обычный физический удар.

### Clip B

Моб атакует, игрок нажимает E:

```text
freeze
→ energy absorption
→ delay
→ valuable explodes
→ both fly away
```

Зритель должен видеть:

> предмет намеренно пожертвовали для поглощения атаки.

Если визуально оба события похожи, mechanic ещё не готова.

---

# 48. Этапы разработки

## Phase 1 — Setup

- BepInEx plugin;
- Harmony;
- ссылки на актуальные game assemblies;
- базовый logger;
- config.

REPOLib не делать обязательной зависимостью без причины.

## Phase 2 — Reverse engineering

Через ILSpy/dnSpy проверить актуальные:

```text
HurtCollider
PlayerAvatar
PhysGrabber
PhysGrabObject
ValuableObject
PhysGrabObjectImpactDetector
Enemy
EnemyHealth
```

Записать реальные method names/fields в `RE_NOTES.md`.

## Phase 3 — Input

```text
E → ParryIntent
```

Лог:

```text
Parry intent created
```

## Phase 4 — Held Valuable

Определить текущий `PhysGrabObject`.

Проверить `ValuableObject`.

## Phase 5 — One enemy hit interception

Выбрать одного melee enemy.

Пока:

```text
valid E intent → cancel hit
```

Без размеров, VFX и destruction.

## Phase 6 — Spatial validation

Добавить проверку:

```text
Valuable между enemy hit и player
```

## Phase 7 — AttackEnergy

Снять реальные значения:

```text
playerDamage
physHitForce
playerTumbleForce
```

для нескольких атак.

Нормализовать коэффициенты.

## Phase 8 — ValuableCapacity

Снять:

```text
mass
bounds
projected area
coverage
```

для нескольких Valuable.

## Phase 9 — Binary capacity test

```text
capacity >= energy
```

## Phase 10 — Freeze

Остановить только конкретного attacker.

Проверить, что AI после freeze не ломается.

## Phase 11 — Consume attack

Убедиться, что тот же swing не наносит hit сразу после разморозки.

## Phase 12 — Sacrifice

Найти ванильный полный destruction path.

Successful parry всегда уничтожает Valuable.

## Phase 13 — Knockback

Добавить impulses:

```text
enemy ← explosion → player
```

## Phase 14 — Energy scaling

Привязать силу knockback/VFX к AttackEnergy.

## Phase 15 — VFX/audio

Сделать:

```text
Capture
→ Absorption
→ Overload
→ Detonation
```

## Phase 16 — Host multiplayer

Host принимает решение и синхронизирует результат.

## Phase 17 — Remote client multiplayer

Client отправляет request, host валидирует.

## Phase 18 — Generic attack adapter

Попробовать распространить систему на большинство melee HurtCollider.

## Phase 19 — Enemy-specific adapters

Добавлять только там, где generic path недостаточен.

## Phase 20 — Balance/playtest

Размеры, timing, knockback, explosion curve.

---

# 49. Первый прототип, который должен сделать coding agent

Не начинать с полного мода.

Первый deliverable:

```text
1. Plugin загружается.
2. E регистрируется.
3. Held PhysGrabObject определяется.
4. ValuableObject проверяется.
5. Shop Item отклоняется.
6. Один melee enemy hit перехватывается.
7. E в коротком окне отменяет этот hit.
8. Без E hit полностью ванильный.
9. Debug log печатает:
   enemy
   damage
   hit force
   tumble force
   valuable name
   mass
   bounds
```

После этого зафиксировать реальные имена методов/полей.

Только затем делать capacity/freeze/destruction.

---

# 50. MVP Definition of Done

- [ ] BepInEx plugin стабильно загружается.
- [ ] E создаёт короткий `ParryIntent`.
- [ ] Без Valuable парирование невозможно.
- [ ] Shop Items не работают.
- [ ] Обычный swing Valuable остаётся ванильным.
- [ ] Хотя бы один melee enemy поддержан.
- [ ] E вне attack window не спасает.
- [ ] Valuable должна стоять между attack и player.
- [ ] Есть minimum size.
- [ ] Capacity считается из размера/площади + массы.
- [ ] Цена Valuable не влияет на capacity.
- [ ] AttackEnergy считается из игровых attack parameters.
- [ ] Слишком слабая Valuable не parry сильный hit.
- [ ] Successful parry отменяет captured damage.
- [ ] Атакующий enemy заметно freeze.
- [ ] Есть короткая absorption delay.
- [ ] Valuable гарантированно уничтожается.
- [ ] Это уничтожение корректно отражается в value/extraction system.
- [ ] Взрыв масштабируется от AttackEnergy.
- [ ] Enemy получает knockback/stagger.
- [ ] Player получает knockback/tumble.
- [ ] Старый enemy swing не продолжается после parry.
- [ ] Один hit нельзя обработать дважды.
- [ ] Host multiplayer синхронен.
- [ ] Remote client multiplayer синхронен.
- [ ] У всех клиентов Valuable исчезает одинаково.
- [ ] Обычная физика игры вне parry не изменена.

---

# 51. Что coding agent НЕ должен делать

- Не добавлять shield item.
- Не разрешать магазинные предметы.
- Не делать passive block.
- Не требовать swing velocity Valuable.
- Не считать столкновение предмета с рукой моба само по себе parry.
- Не использовать цену как силу щита.
- Не использовать глобальный `Time.timeScale`.
- Не вызывать голый `Destroy(gameObject)` до исследования ванильного destruction path.
- Не делать client-authoritative result.
- Не пытаться поддержать все damage types сразу.
- Не добавлять HUD-метры без необходимости.
- Не давать большой direct damage мобу по умолчанию.
- Не убирать player knockback.
- Не менять обычную физику Valuable.

---

# 52. Баланс-философия

У каждого parry должны быть три независимые цены:

### Timing

Нужно нажать E в момент атаки.

### Physics

Нужно физически закрыться подходящим Valuable.

### Economy

Successful parry уничтожает добычу.

И четвёртый риск:

### Chaos

Даже после спасения игрока отбрасывает взрывом.

Итог:

```text
TIMING
× PHYSICAL COVERAGE
× SIZE/MASS
× ECONOMIC SACRIFICE
× CHAOTIC KNOCKBACK
```

а не:

```text
PRESS E = INVINCIBILITY
```

---

# 53. Финальный пример

Игрок несёт Orb за $6000.

Enemy начинает сильный melee hit.

Игрок физически держит Orb между собой и мобом и нажимает E уже во время attack.

Host проверяет:

```text
Orb is Valuable           YES
held by player            YES
attack active             YES
attack supported          YES
Orb covers attack line    YES
minimum size              YES
capacity >= attack energy YES
timing valid              YES
attack not consumed       YES
```

Timeline:

```text
0 ms
enemy hit captured
player damage cancelled

10 ms
enemy attack freezes

20–160 ms
energy flows from contact point into Orb
Orb glows / cracks / vibrates

~180 ms
Orb overloads

~190 ms
Orb explodes completely

Enemy flies backward
Player flies backward

Player survives the captured attack
but can tumble into a pit

$6000 Orb is permanently gone
```

Именно это событие — центр всего мода.

---

# 54. Подтверждённые технические опорные точки

По существующим R.E.P.O.-модам:

- используется BepInEx + Harmony;
- `PhysGrabObject` имеет grab lifecycle;
- локально удерживаемый объект можно получать через `PhysGrabber`;
- `PhysGrabObject` можно отличить как Valuable через `ValuableObject`;
- у `ValuableObject` есть физические/value/durability presets;
- масса доступна через physical preset;
- enemy-классы используют `HurtCollider`;
- у `HurtCollider` используются `playerDamage`, `physHitForce`, `physHitTorque`, `playerTumbleForce`;
- enemy states успешно Harmony-патчатся существующими модами;
- REPOLib умеет работать с valuables/network prefabs/network events, но core mechanic можно сначала сделать без жёсткой зависимости от неё.

Важно: перед реализацией всё равно сверить реальные методы и поля с **актуальным `Assembly-CSharp.dll` установленной версии R.E.P.O.**

---

# 55. Ссылки для реализации

REPOLib:

https://github.com/ZehsTeam/REPOLib

https://thunderstore.io/c/repo/p/Zehs/REPOLib/

Enemy/HurtCollider examples:

https://github.com/Ardot66/REPO.EnemyOverhaul

Особенно:

```text
Source/Patches/Reaper.cs
Source/Patches/Trudge.cs
Source/Patches/Huntsman.cs
```

PhysGrabObject / Valuable / impact examples:

https://github.com/cmooref17/REPO-RemoveCartProtection

Valuable durability:

https://github.com/cmooref17/REPO-FragileValuables

Получение локально удерживаемого Valuable:

https://github.com/mchorse/repo-displayprices

---

# 56. Критерий успеха концепта

Мод готов не тогда, когда:

> «E может отменить damage, если в руке Valuable».

Он готов, когда без UI и объяснения зритель видит:

> **«Удар моба остановился, вошёл в ценность, она переполнилась энергией и взорвалась, спасая игрока ценой добычи и разбрасывая обоих».**

Если это визуально не считывается — parry нужно переделывать, даже если код технически работает.
