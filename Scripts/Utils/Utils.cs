using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static bool IsFacingRight(Transform transform)
    {
        return Vector3.Dot(transform.right, Vector3.right) > 0;
    }

    /// <summary>
    /// Sets transform.right to be pointing towards target maintaining the sprite right side up
    /// </summary>
    public static void LookAt2D(Transform transform, Vector3 target)
    {
        transform.right = target - transform.position;

        // Sprites by default look to the right
        // If our final rotation is not looking to the right we need to apply a 180º rotation
        // so our sprites don't end up upside down
        if (!IsFacingRight(transform))
        {
            // Adjust rotation so the sprite is not upside down when facing left
            Quaternion rotation = transform.localRotation;
            rotation *= Quaternion.Euler(180f, 0f, 0f);
            transform.localRotation = rotation;
        }
    }

    /// <summary>
    /// Sets transform.right to look in the direction of the target position
    /// snapping to either Vector3.right or Vector3.left
    /// </summary>
    public static void Face2D(Transform transform, Vector3 target)
    {
        Vector3 lookDirection = target - transform.position;
        transform.right = Vector3.Dot(lookDirection, Vector3.right) > 0 ? Vector3.right : Vector3.left;
    }

    /// <summary>
    /// Whether or not target is in the general direction of transform.right
    /// </summary>
    public static bool IsFacing2D(Transform transform, Vector3 target)
    {
        Vector3 lookDirection = target - transform.position;
        return Vector3.Dot(lookDirection, transform.right) > 0;
    }
}
