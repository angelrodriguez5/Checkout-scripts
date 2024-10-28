using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnimatorExtensions
{
    public static bool IsPlaying(this Animator animator)
    {
        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1;
    }
}
