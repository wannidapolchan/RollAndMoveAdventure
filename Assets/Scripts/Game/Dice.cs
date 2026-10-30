using UnityEngine;

public enum DieType
{
    Plain,
    Lucky
}

/// <summary>
/// The two dice. The Plain Die is free and rolls 1 to 6.
/// The Lucky Die costs a coin and has bigger (and riskier) faces.
/// </summary>
public static class Dice
{
    public const int LuckyDieCost = 1;

    public static readonly int[] PlainFaces = { 1, 2, 3, 4, 5, 6 };
    public static readonly int[] LuckyFaces = { -2, 0, 3, 5, 8, 10 };

    public static int[] Faces(DieType type)
    {
        return type == DieType.Plain ? PlainFaces : LuckyFaces;
    }

    /// <summary>Rolls a die and returns the index of the face (0 to 5).</summary>
    public static int RollFaceIndex()
    {
        return Random.Range(0, 6);
    }

    /// <summary>Name of the image for one face of a die.</summary>
    public static string FaceImage(DieType type, int faceIndex)
    {
        if (type == DieType.Plain)
        {
            return "die_plain_" + PlainFaces[faceIndex];
        }

        int value = LuckyFaces[faceIndex];
        return "die_lucky_" + (value < 0 ? "m" + (-value) : value.ToString());
    }
}
