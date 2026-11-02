// Inspector fields here are read by the BigPuzzleSDK mod in-game, not by this script
#pragma warning disable 0414

using UnityEngine;

namespace BigPuzzleSDK {

// One of the game's signs: just the board. A TextMeshPro child (made for you when you add this) holds the text, so you have all of
// its settings (text, font, size, colour). Players can't edit the sign.
[PuzzleInfo("A Stationary sign. Edit the text on the TextMeshPro child object.")]
[AddComponentMenu("")]   // hidden from Add Component: comes from the prefab
public class PuzzleSign : MonoBehaviour {
    [Tooltip("The board's size compared to the game's sign: width, height. 1, 1 is the game's own size. The text isn't scaled: it gets a wider box to match (see Fix text to board).")]
    [SerializeField] private Vector2 size = Vector2.one;

    // Set once the TextMeshPro child has been made (by the editor), so deleting it isn't undone
    [SerializeField, HideInInspector] private bool textMade;
}

}
