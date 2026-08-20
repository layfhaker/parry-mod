# Valuable Parry — мод для R.E.P.O.

Парирование атак мобов **ценностями (Valuable)**. Это не блок и не удар предметом.

Игрок держит Valuable между собой и атакой, нажимает **E в момент удара**, ценность поглощает удар и взрывается. Атака отменяется, моб и игрок разлетаются. Добыча безвозвратно уничтожается.

Без E предмет ведёт себя полностью ванильно.

## Как это работает

1. Моб начинает melee-атаку.
2. Ты держишь `ValuableObject` так, чтобы он закрывал тебя от удара.
3. Нажимаешь **Interact (E)** уже во время атаки (окно ~200 мс).
4. Если размер/площадь + масса ценности достаточны — удар захватывается.
5. Короткий freeze → энергия втягивается в предмет → детонация.
6. Ценность уничтожается ванильным destruction path (haul/value учитываются).
7. Моб и игрок получают knockback в противоположные стороны.

Магазинные Item, щиты, пассивный блок и «махнуть предметом» **не работают**. Цена ценности не влияет на силу парирования — только на то, жалко ли её ломать.

## Установка

Через **Thunderstore Mod Manager** / r2modman:

1. Community: **R.E.P.O.**
2. Найди мод **Valuable Parry** (`Avariiiprime-ValuableParry`) и поставь в профиль.
3. Зависимость: BepInExPack.

Вручную:

1. Поставь [BepInExPack для R.E.P.O.](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/).
2. Скопируй `RepoValuableParry.dll` в `BepInEx/plugins/`.
3. В логе должно быть: `Valuable Parry v1.0.0 loaded.`

Исходники: [github.com/layfhaker/parry-mod](https://github.com/layfhaker/parry-mod)

Сборка из исходников:

```powershell
dotnet build RepoValuableParry\RepoValuableParry.csproj -c Release
```

Если R.E.P.O. стоит не в стандартном Steam-пути:

```powershell
dotnet build RepoValuableParry\RepoValuableParry.csproj -c Release -p:RepoPath="D:\SteamLibrary\steamapps\common\REPO"
```

Если в игре уже есть `BepInEx\plugins`, DLL копируется туда автоматически после build.

## Управление

| Действие | Клавиша |
| --- | --- |
| Парирование | Interact (E по умолчанию, учитывает ребинды) |

## Конфиг (`BepInEx/config/valuableparry.repovaluableparry.cfg`)

Пользовательские:

- Enable Mod
- Use Vanilla Interact / Fallback Parry Key
- Parry Window
- Player / Enemy Knockback Multiplier
- Effects Intensity
- Camera Shake

Баланс (debug/playtest): веса energy/capacity, minimum coverage/area, freeze duration, сила взрыва.

## Чего мод не делает

- Не добавляет щит и не работает с магазинными предметами.
- Не включает пассивный блок.
- Не считает столкновение предмета с мобом парированием.
- Не использует цену как силу щита.
- Не ставит глобальный `Time.timeScale`.
- Не наносит большой direct damage мобу.
- Не убирает knockback игрока.
- Не меняет обычную физику Valuable.

## Поддерживаемые атаки (MVP)

Melee / swipe / bite / body hit / melee charge через `HurtCollider` + `enemyHost`.

Пока не парируются: пули, лазеры, взрывы, ловушки, падения, электричество, grabs, scripted instakill, PvP.

Generic adapter покрывает большинство melee. Отдельные адаптеры есть для Reaper (`EnemyRunner`) и Trudge (`EnemySlowWalker`).

## Debug

По умолчанию HUD нет. Если нужно отладить, в конфиге `valuableparry.repovaluableparry.cfg` включи `Debug Overlay` / `Debug Gizmos`. В релизе оба выключены.
