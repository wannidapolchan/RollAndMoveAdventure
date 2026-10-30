using UnityEngine;

/// <summary>
/// Marks an object as a ladder or a snake and remembers which tiles it joins.
/// For snakes, pawns slide along this object's real position, rotation and size.
/// </summary>
public class BoardLink : MonoBehaviour
{
    [Tooltip("Start tile in code numbers (screen number - 1). Ladder: bottom. Snake: head.")]
    public int from;
    [Tooltip("End tile in code numbers (screen number - 1). Ladder: top. Snake: tail.")]
    public int to;
    public bool isSnake;
}
