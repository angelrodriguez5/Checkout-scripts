using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGrabbable
{
    public GameObject GameObject { get;}

    /// <summary>
    /// Set the parent of this object and adjust local position and rotation
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="localPosition"></param>
    /// <param name="localRotation"></param>
    public void AttachTo(Transform parent, Vector3 localPosition, Quaternion localRotation, bool blockInteraction = true);
    public void AttachToPlayer(Transform parent, bool blockInteraction = true);

    /// <summary>
    /// Unparent object
    /// </summary>
    public void Detach();

    /// <summary>
    /// This item was thrown with a certain velocity
    /// </summary>
    public void Throw(Vector3 velocity);
}
