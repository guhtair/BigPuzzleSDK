// Inspector fields here are read by the BigPuzzleSDK mod in-game, not by this script
#pragma warning disable 0414

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigPuzzleSDK {

// Which of the game's items a Hook takes. Must stay identical, in the same order and values, to the mod's enum.
[Flags]
// One entry per Vanilla Item type, in the same order as PuzzleVanillaItemType (a hook takes an item if its entry is ticked).
public enum PuzzleHookAccepts { None = 0, Blindfold = 1 << 0, FoldingChair = 1 << 1, Torch = 1 << 2, FmRadio = 1 << 3, WalkieTalkie = 1 << 4, Binoculars = 1 << 5, XrayGoggles = 1 << 6, Clock = 1 << 7, Laser = 1 << 8, Megaphone = 1 << 9, Kettle = 1 << 10, CannonBall = 1 << 11, PermoSign = 1 << 12, SpareSign = 1 << 13, GourdHolder = 1 << 14, Belt = 1 << 15, Backpack = 1 << 16, Gps = 1 << 17, FoldingMap = 1 << 18, Compass = 1 << 19, FlareGun = 1 << 20, CowbellBig = 1 << 21, CowbellMedium = 1 << 22, CowbellSmall = 1 << 23, Paintbrush = 1 << 24, Football = 1 << 25, Buoy = 1 << 26, BuoyRed = 1 << 27 }

// What the hook looks like. Standard: the game's wall hook. Backpack: the game's backpack / belt hanger. Invisible: no model at all. Must stay identical to the mod's enum.
public enum PuzzleHookType { Standard, Backpack, Invisible }

// One of the game's hooks: players can hang items on it. It can start empty, or with an item already hanging.
[PuzzleInfo("A hook players can hang items on. Default Item is what hangs on it at the start (leave empty for an empty hook). Accepted Vanilla and Accepted Custom are what players can hang on it.\nHook Type is how it looks (the game's wall hook or its backpack hanger).")]
[AddComponentMenu("")]   // hidden from Add Component: comes from the prefab
public class PuzzleHook : MonoBehaviour {
    [Tooltip("What the hook looks like: Standard is the game's wall hook (the one the clock and signs hang on), Backpack is the hanger the backpack and belt hang on, Invisible hides the hook completely (items still hang where it would be). This is only the look: what can hang on it is set by Accepted Vanilla and Accepted Custom.")]
    [SerializeField] private PuzzleHookType hookType = PuzzleHookType.Standard;

    [Tooltip("Optional. The item that hangs on the hook at the start: a Vanilla Item or a Custom Item from this puzzle. Leave empty for an empty hook. It can always be hung on this hook.")]
    [SerializeField] private MonoBehaviour defaultItem;

    [Tooltip("Which Vanilla Item types players can hang on it, whatever the hook looks like. The game only made the clock, signs, backpack and belt to hang; other items hang in whatever pose the hook gives them and may look off.")]
    [SerializeField] private PuzzleHookAccepts acceptedVanilla = PuzzleHookAccepts.Clock | PuzzleHookAccepts.SpareSign | PuzzleHookAccepts.PermoSign;

    [Tooltip("Custom Items players can also hang on it.")]
    [SerializeField] private List<PuzzleItem> acceptedCustom = new List<PuzzleItem>();
}

}
