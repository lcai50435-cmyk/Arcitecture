using UnityEngine;

/// <summary>
/// General-purpose facing direction tracker
/// Records the last movement direction for characters (player/enemy) and exposes attack/idle facing
/// </summary>
public class DirectionTracker : MonoBehaviour
{
    // Last valid movement direction (X/Y axes)
    private Vector2 lastDirection;
    // Default facing direction (for example, the character initially faces right)
    [Header("默认朝向")]
    public Vector2 defaultDirection = Vector2.up;

    /// <summary>
    /// Gets the last facing direction (read-only externally)
    /// </summary>
    public Vector2 LastDirection
    {
        get
        {
            // If the character has never moved, return the default facing direction
            return lastDirection.magnitude < 0.1f ? defaultDirection : lastDirection;
        }
    }

    /// <summary>
    /// Updates the movement direction (called when the character moves)
    /// </summary>
    /// <param name="currentMoveDir">Current movement direction</param>
    public void UpdateMoveDirection(Vector2 currentMoveDir)
    {
        // Filter invalid input to avoid tiny value jitter
        if (currentMoveDir.magnitude > 0.1f)
        {
            // Normalize to keep the direction vector length at 1
            lastDirection = currentMoveDir.normalized;
        }
    }

    /// <summary>
    /// Resets facing direction (optional, for example when the character respawns)
    /// </summary>
    public void ResetDirection()
    {
        lastDirection = defaultDirection;
    }
}