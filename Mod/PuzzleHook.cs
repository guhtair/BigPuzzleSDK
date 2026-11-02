using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace BigPuzzleSDK {

// Which of the game's items a hook takes. Must stay identical, in the same order and values, to the Unity SDK's enum.
[Flags]
// One entry per Vanilla Item type, in the same order as PuzzleVanillaItemType (a hook takes an item if its entry is ticked).
public enum PuzzleHookAccepts { None = 0, Blindfold = 1 << 0, FoldingChair = 1 << 1, Torch = 1 << 2, FmRadio = 1 << 3, WalkieTalkie = 1 << 4, Binoculars = 1 << 5, XrayGoggles = 1 << 6, Clock = 1 << 7, Laser = 1 << 8, Megaphone = 1 << 9, Kettle = 1 << 10, CannonBall = 1 << 11, PermoSign = 1 << 12, SpareSign = 1 << 13, GourdHolder = 1 << 14, Belt = 1 << 15, Backpack = 1 << 16, Gps = 1 << 17, FoldingMap = 1 << 18, Compass = 1 << 19, FlareGun = 1 << 20, CowbellBig = 1 << 21, CowbellMedium = 1 << 22, CowbellSmall = 1 << 23, Paintbrush = 1 << 24, Football = 1 << 25, Buoy = 1 << 26, BuoyRed = 1 << 27 }

// What the hook looks like. Standard: the game's wall hook. Backpack: the game's backpack / belt hanger. Invisible: no model at all (items still hang in place). Must stay identical to the Unity SDK's enum.
public enum PuzzleHookType { Standard, Backpack, Invisible }

// Runtime (mod) version of the SDK's PuzzleHook: a copy of one of the game's hooks (the wall hook the clock and
// signs hang on, or the pack hanger the backpack and belt hang on), with no item on it. Its Default Item, if any, is hung on
// it once both exist; what else players can hang on it is Accepts plus the Custom Items listed.
// Which items may go on a hook is decided in ModGourd.Allowed (see Decide).
public class PuzzleHook : PuzzleVanillaCopy {
    public PuzzleHook(IntPtr ptr) : base(ptr) { }

    // Set in the Inspector (filled in by the loader); the names must match the Unity-side fields
    private PuzzleHookType hookType = PuzzleHookType.Standard;
    private PuzzleVanillaCopy defaultItem;
    private PuzzleHookAccepts acceptedVanilla = PuzzleHookAccepts.Clock | PuzzleHookAccepts.SpareSign | PuzzleHookAccepts.PermoSign;
    private List<PuzzleItem> acceptedCustom = new List<PuzzleItem>();

    private static readonly List<PuzzleHook> hooks = new List<PuzzleHook>();
    // What kind of game item each of our Vanilla Items is (by prop), so Accepted Vanilla can tell a clock from a sign
    private static readonly Dictionary<IntPtr, PuzzleVanillaItemType> itemKinds = new Dictionary<IntPtr, PuzzleVanillaItemType>();

    // Groups we added to a prop (prop pointer + group): those props may only use the hooks we added them for
    private static readonly HashSet<string> primed = new HashSet<string>();

    private PuzzleHookAccepts effective;   // Accepts plus the kind of Item
    private bool attached;                 // the Item hangs on it
    private bool grouped;                  // the Custom Items have been told this hook's group
    private float nextWatch;


    // Which model: the creator's choice. What it takes is Accepted Vanilla / Accepted Custom, whatever it looks like (an item that doesn't
    // fit the hook's own group is given it when pointed at the hook, see Prime).
    internal bool UsesPackHanger { get { return hookType == PuzzleHookType.Backpack; } }

    internal static void NoteKind(Prop prop, PuzzleVanillaItemType type) {
        if (prop != null) itemKinds[prop.Pointer] = type;
    }

    // The entry for an item type (the enum is one bit per PuzzleVanillaItemType, in order)
    private static PuzzleHookAccepts FlagOf(PuzzleVanillaItemType type) {
        return (PuzzleHookAccepts)(1 << (int)type);
    }

    // Called from SetupCopy, before the template is chosen: what the hook takes decides which hook it is
    internal void Prepare() {
        effective = acceptedVanilla;
        PuzzleVanillaItem vanilla = defaultItem as PuzzleVanillaItem;
        if (vanilla != null) effective |= FlagOf(vanilla.itemType);
        hooks.RemoveAll(h => h == null);
        hooks.Add(this);
    }

    // Called from SetupCopy on the fresh copy, before anything wakes: the hook comes without its item
    internal void SetupHook(GameObject copy) {
        var props = new List<GameObject>();
        foreach (Component c in copy.GetComponentsInChildren(Il2CppType.Of<Prop>(), true)) {
            Prop p = c != null ? c.TryCast<Prop>() : null;
            if (p != null && p.gameObject != copy) props.Add(p.gameObject);
        }
        foreach (GameObject go in props) UnityEngine.Object.DestroyImmediate(go);
        if (hookType != PuzzleHookType.Invisible) return;
        // Invisible: hide the hook's model but keep its holder, so items still hang where the hook would be
        foreach (Component c in copy.GetComponentsInChildren(Il2CppType.Of<Renderer>(), true)) {
            Renderer r = c != null ? c.TryCast<Renderer>() : null;
            if (r != null) r.enabled = false;
        }
    }

    // The hook's own holder (null until it exists)
    internal PropHome HookHome {
        get {
            foreach (PropHome home in Homes) {
                if (home == null) continue;
                string g = home.pinGroup.ToString();
                if (g == "WallHook" || g == "PackHanger") return home;
            }
            return null;
        }
    }

    private static Prop PropOf(PuzzleVanillaCopy c) {
        if (c == null) return null;
        try { return c.FirstProp(out ulong id); } catch (Exception) { return null; }
    }

    // Called every frame by PuzzleVanillaCopy.Update; works every third of a second
    internal void Watch() {
        if (Time.time < nextWatch) return;
        nextWatch = Time.time + 0.3f;
        PropHome home = HookHome;
        if (home == null) return;
        Prop itemProp = PropOf(defaultItem);

        // Every PC: the items that may hang here carry this hook's group (each PC checks for itself)
        if (!grouped) {
            bool done = true;
            if (defaultItem != null) done &= Group(itemProp, home);
            foreach (PuzzleItem custom in acceptedCustom) if (custom != null) done &= Group(custom.GameProp, home);
            grouped = done;
        }

        // The host hangs the Item on it (everyone else gets it through the game's own sync)
        if (attached || defaultItem == null || !PuzzleSDK.IsHost || itemProp == null) return;
        if (!itemProp.gameObject.activeInHierarchy || !PuzzleVanillaCopy.all.Contains(this) || !PuzzleVanillaCopy.all.Contains(defaultItem)) return;
        try {
            try { itemProp.ServerSetUnpinned(); } catch (Exception) { }
            itemProp.ServerSetPinned(home);
            attached = true;
        } catch (Exception ex) {
            attached = true;
            Plugin.log.LogWarning($"[PuzzleHook] '{name}': could not hang '{defaultItem.name}' on the hook: {(ex.InnerException ?? ex).GetType().Name}: {(ex.InnerException ?? ex).Message}");
        }
    }

    // Adds the hook's group to an item's groups (the game lets an item go where its groups say). False while the item doesn't exist yet.
    private static bool Group(Prop prop, PropHome home) {
        if (prop == null) return false;
        try {
            var groups = prop.propGroups;
            PropGroup wanted = home.pinGroup;
            if (!groups.Contains(wanted)) {
                groups.Add(wanted);
                primed.Add(prop.Pointer + "|" + wanted);
            }
        } catch (Exception ex) {
            Plugin.log.LogWarning($"[PuzzleHook] could not let '{prop.name}' hang on a hook: {ex.GetType().Name}: {ex.Message}");
        }
        return true;
    }

    // Is the prop one of the kinds this hook takes (or its Item, or a listed Custom Item)?
    private bool Accepts(Prop prop) {
        IntPtr ptr = prop.Pointer;
        Prop own = PropOf(defaultItem);
        if (own != null && own.Pointer == ptr) return true;
        foreach (PuzzleItem custom in acceptedCustom) {
            Prop p = custom != null ? custom.GameProp : null;
            if (p != null && p.Pointer == ptr) return true;
        }
        return (effective & KindOf(prop)) != 0;
    }

    private static PuzzleHookAccepts KindOf(Prop prop) {
        if (itemKinds.TryGetValue(prop.Pointer, out PuzzleVanillaItemType known)) return FlagOf(known);
        try {
            var groups = prop.propGroups;
            if (groups.Contains(PropGroup.Backpack)) return PuzzleHookAccepts.Backpack;
            if (groups.Contains(PropGroup.WearHolster)) return PuzzleHookAccepts.Belt;
            string n = prop.gameObject.name;
            if (n.Contains("PermoSign")) return PuzzleHookAccepts.PermoSign;
            if (n.Contains("Clock")) return PuzzleHookAccepts.Clock;
            if (n.Contains("Sign")) return PuzzleHookAccepts.SpareSign;
        } catch (Exception) { }
        return PuzzleHookAccepts.None;
    }

    // Before the game checks whether a prop fits a home: if it is a hook of ours that takes the prop, the prop gets the
    // hook's group first (the game only lets a prop go where its groups say). Cheap: only hook homes do anything.
    internal static void Prime(Prop prop, PropHome home) {
        if (hooks.Count == 0 || home == null || prop == null) return;
        PuzzleHook hook = HookOf(home);
        if (hook != null && hook.Accepts(prop)) Group(prop, home);
    }

    private static PuzzleHook HookOf(PropHome home) {
        foreach (PuzzleHook hook in hooks) {
            if (hook == null) continue;
            PropHome mine = hook.HookHome;
            if (mine != null && mine.Pointer == home.Pointer) return hook;
        }
        return null;
    }

    // ModGourd.Allowed asks this for every home: true if the answer is the hook's to give, with the answer in ok
    internal static bool Decide(Prop prop, PropHome home, out bool ok) {
        ok = true;
        if (hooks.Count == 0 || home == null || prop == null) return false;
        PuzzleHook hook = HookOf(home);
        if (hook != null) {
            ok = hook.Accepts(prop);
            if (ok) Group(prop, home);
            return true;
        }
        // A group we gave a prop only counts on the hooks we gave it for, not on the game's own
        if (primed.Count > 0 && primed.Contains(prop.Pointer + "|" + home.pinGroup)) { ok = false; return true; }
        return false;
    }
}

}
