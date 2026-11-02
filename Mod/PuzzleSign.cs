using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;

namespace BigPuzzleSDK {

// Runtime (mod) version of the SDK's PuzzleSign: a copy of the game's teaching-area sign board, with none of its text or
// items. The text is the creator's own TextMeshPro on the sign's object. Players can't change anything on it.
public class PuzzleSign : PuzzleVanillaCopy {
    public PuzzleSign(IntPtr ptr) : base(ptr) { }

    // Set in the Inspector (filled in by the loader); the names must match the Unity-side fields
    private Vector2 size = Vector2.one;
    private bool textMade;

    // Called from SetupCopy on the fresh copy, before anything wakes
    internal void SetupSign(GameObject copy) {
        // Stationary: nothing on it can be picked up
        var doomed = new List<GameObject>();
        foreach (Component c in copy.GetComponentsInChildren(Il2CppType.Of<Prop>(), true)) {
            Prop p = c != null ? c.TryCast<Prop>() : null;
            if (p != null && p.gameObject != copy) doomed.Add(p.gameObject);
        }
        // The game's own text (with its translation, text input and icon sub-meshes) goes: the creator's TextMeshPro is the text
        foreach (Component c in copy.GetComponentsInChildren(Il2CppType.Of<TMP_Text>(), true)) {
            TMP_Text t = c != null ? c.TryCast<TMP_Text>() : null;
            if (t != null && t.gameObject != copy) doomed.Add(t.gameObject);
        }
        foreach (GameObject go in doomed) {
            try { if (go != null) UnityEngine.Object.DestroyImmediate(go); } catch (Exception) { }
        }
        var inputs = new List<Component>();
        foreach (Component c in copy.GetComponentsInChildren(Il2CppType.Of<Component>(), true)) {
            if (c == null) continue;
            string type = c.GetIl2CppType().Name;
            if (type == "PeckEffectTextInput" || type == "LocalizedText") inputs.Add(c);
        }
        foreach (Component c in inputs) {
            try { UnityEngine.Object.DestroyImmediate(c); } catch (Exception) { }
        }

        // Size: the board only (the board is turned: its own x is the width, its z the height; the thickness stays)
        Transform board = copy.transform.Find("board");
        if (board != null) board.localScale = new Vector3(board.localScale.x * size.x, board.localScale.y, board.localScale.z * size.y);
    }
}

}
