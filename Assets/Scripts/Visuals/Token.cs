using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A player's pawn on the board. Handles every movement animation:
/// hopping tile to tile, climbing ladders, sliding down snakes and celebrating.
/// The pawn has up to three parts: a glowing ring (shows whose turn it is),
/// a shadow, and the pawn body. The ring and shadow can be turned off in Settings.
/// </summary>
public class Token : MonoBehaviour
{

    private SpriteRenderer body;
    private SpriteRenderer shadow;
    private SpriteRenderer ring;
    private Transform bodyTransform;
    private Color playerColor;

    private bool isActive;      // true while it is this player's turn
    private bool isCelebrating; // true on the win screen
    private float squashTimer;  // > 0 right after landing, for a little squash effect

    /// <summary>Creates a new pawn object for a player.</summary>
    /// <param name="size">Longest side of the character image, in world units.</param>
    /// <param name="showShadow">Draw a soft shadow under the character.</param>
    /// <param name="showRing">Draw a glowing ring under the character on its turn.</param>
    public static Token Create(Player player, Vector3 position, float size, bool showShadow, bool showRing)
    {
        var go = new GameObject(player.Name + " Token");
        go.transform.position = position;
        Token token = go.AddComponent<Token>();
        token.Setup(player, size, showShadow, showRing);
        return token;
    }

    private void Setup(Player player, float size, bool showShadow, bool showRing)
    {
        playerColor = player.Color;

        if (showRing)
        {
            ring = MakePart("Ring", "dot", 0.9f, new Vector2(0.5f, 0.5f), SortOrder.Tokens - 2);
            ring.transform.localScale = new Vector3(1f, 0.45f, 1f); // flat oval on the floor
            ring.color = new Color(playerColor.r, playerColor.g, playerColor.b, 0f);
        }
        if (showShadow)
        {
            shadow = MakePart("Shadow", "shadow", 0.6f, new Vector2(0.5f, 0.5f), SortOrder.Tokens - 1);
        }

        body = MakePart("Body", player.PawnImage, size, new Vector2(0.5f, 0.06f), SortOrder.Tokens);
        bodyTransform = body.transform;
    }

    /// <summary>
    /// Adds one part of the pawn. "size" is how big the image's longest side
    /// should be in world units, so any image size works.
    /// </summary>
    private SpriteRenderer MakePart(string partName, string image, float size, Vector2 pivot, int order)
    {
        Texture2D tex = GameAssets.Texture(image);
        float pixelsPerUnit = Mathf.Max(tex.width, tex.height) / size;
        var go = new GameObject(partName);
        go.transform.SetParent(transform, false);
        var sr = GameAssets.AddSpriteRenderer(go);
        sr.sprite = GameAssets.Sprite(image, pixelsPerUnit, pivot);
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>Turns the "your turn" glow and bounce on or off.</summary>
    public void SetActive(bool active)
    {
        isActive = active;
        // The player whose turn it is draws on top of the other pawn.
        int boost = active ? 3 : 0;
        body.sortingOrder = SortOrder.Tokens + boost;
        if (shadow != null) shadow.sortingOrder = SortOrder.Tokens - 1 + boost;
    }

    public void SetCelebrating(bool celebrate)
    {
        isCelebrating = celebrate;
    }

    // ---------------------------------------------------------------- idle animation

    private void Update()
    {
        float time = Time.time;
        float bounce = 0f;
        float ringAlpha = 0f;

        if (isCelebrating)
        {
            bounce = Mathf.Abs(Mathf.Sin(time * 7f)) * 0.45f;
        }
        else if (isActive)
        {
            bounce = Mathf.Abs(Mathf.Sin(time * 4f)) * 0.08f;
            ringAlpha = 0.45f + 0.25f * Mathf.Sin(time * 5f);
        }

        bodyTransform.localPosition = new Vector3(0f, bounce, 0f);
        if (ring != null)
        {
            ring.color = new Color(playerColor.r, playerColor.g, playerColor.b, ringAlpha);
            float ringSize = 1f + 0.1f * Mathf.Sin(time * 5f);
            ring.transform.localScale = new Vector3(ringSize, ringSize * 0.45f, 1f);
        }

        // Shadow gets smaller when the pawn is in the air.
        if (shadow != null)
        {
            float shadowSize = 1f - bounce * 0.8f;
            shadow.transform.localScale = new Vector3(shadowSize, shadowSize, 1f);
        }

        // Squash and stretch after landing.
        if (squashTimer > 0f)
        {
            squashTimer -= Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(squashTimer / 0.15f) * Mathf.PI) * 0.18f;
            bodyTransform.localScale = new Vector3(1f + k, 1f - k, 1f);
        }
        else
        {
            bodyTransform.localScale = Vector3.one;
        }
    }

    // ---------------------------------------------------------------- movement

    /// <summary>Jumps in an arc to a position.</summary>
    public IEnumerator HopTo(Vector3 target, float duration, float height)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = Mathf.Clamp01(t);
            Vector3 pos = Vector3.Lerp(start, target, k);
            pos.y += Mathf.Sin(k * Mathf.PI) * height;
            transform.position = pos;
            yield return null;
        }
        transform.position = target;
        squashTimer = 0.15f;
    }

    /// <summary>Climbs straight up a ladder with a small step-by-step bob.</summary>
    public IEnumerator ClimbTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            Vector3 pos = Vector3.Lerp(start, target, k);
            pos.x += Mathf.Sin(k * Mathf.PI * 10f) * 0.05f; // side to side, like climbing rungs
            transform.position = pos;
            yield return null;
        }
        transform.position = target;
        squashTimer = 0.15f;
    }

    /// <summary>Slides along a list of points at a steady speed (used for snakes).</summary>
    public IEnumerator FollowPath(List<Vector3> points, float duration)
    {
        if (points.Count < 2) yield break;

        // Total length, so the pawn moves at the same speed the whole way.
        float total = 0f;
        for (int i = 1; i < points.Count; i++) total += Vector3.Distance(points[i - 1], points[i]);

        float travelled = 0f;
        int segment = 1;
        float segmentStart = 0f;
        while (segment < points.Count)
        {
            travelled += total * Time.deltaTime / duration;
            float segmentLength = Vector3.Distance(points[segment - 1], points[segment]);
            while (segment < points.Count && travelled > segmentStart + segmentLength)
            {
                segmentStart += segmentLength;
                segment++;
                if (segment < points.Count)
                    segmentLength = Vector3.Distance(points[segment - 1], points[segment]);
            }
            if (segment >= points.Count) break;

            float k = segmentLength > 0f ? (travelled - segmentStart) / segmentLength : 1f;
            transform.position = Vector3.Lerp(points[segment - 1], points[segment], k);
            yield return null;
        }
        transform.position = points[points.Count - 1];
        squashTimer = 0.15f;
    }
}
