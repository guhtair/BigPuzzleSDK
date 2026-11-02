// BigPuzzleSDK: gives a new Puzzle Sign its TextMeshPro, so the text can be set up right away (text, font, size, colour).
// Made once per sign (the sign remembers it), so a creator who deletes it isn't given it again.
using UnityEditor;
using UnityEngine;
using TMPro;

namespace BigPuzzleSDK.GameAssets {

[InitializeOnLoad]
internal static class PuzzleSignText {
    static PuzzleSignText() {
        EditorApplication.hierarchyChanged += Check;
    }

    private static void Check() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (PuzzleSign sign in Object.FindObjectsByType<PuzzleSign>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
            var so = new SerializedObject(sign);
            SerializedProperty made = so.FindProperty("textMade");
            if (made == null || made.boolValue) continue;
            made.boolValue = true;
            so.ApplyModifiedProperties();
            if (sign.GetComponentInChildren<TMP_Text>(true) != null) continue;   // it already has one (a prefab, a copy)
            Make(sign);
        }
    }

    // The game's own settings for this sign's text: white, top left, 0.05 margins, shrinking to fit between 0.1 and 0.6
    private static void Make(PuzzleSign sign) {
        var go = new GameObject("Text (TMP)", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add sign text");
        go.layer = sign.gameObject.layer;
        go.transform.SetParent(sign.transform, false);
        go.transform.localPosition = new Vector3(0.01f, 0.05f, -0.025f);   // on the board's front
        var text = go.AddComponent<TextMeshPro>();
        text.text = "Sign";
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.1f;
        text.fontSizeMax = 0.6f;
        text.fontSize = 0.6f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = new Vector4(0.05f, 0.05f, 0.05f, 0.05f);
        text.lineSpacing = -20f;
        text.paragraphSpacing = 20f;
        text.rectTransform.sizeDelta = new Vector2(0.70f, 1.00f);   // as wide and tall as the game's board
    }
}

}
