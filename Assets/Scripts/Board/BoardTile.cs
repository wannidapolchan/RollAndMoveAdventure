using UnityEngine;

/// <summary>
/// Marks an object as a board tile. Pawns stand wherever this object is,
/// so you can move tiles in the scene and the game follows.
/// </summary>
public class BoardTile : MonoBehaviour
{
    [Tooltip("Tile number in code (0 = Start, 29 = Finish). The number on screen is this + 1.")]
    public int index;
}
