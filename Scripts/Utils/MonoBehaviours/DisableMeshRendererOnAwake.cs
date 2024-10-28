using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class DisableMeshRendererOnAwake : MonoBehaviour
{

    private void Awake()
    {
        var render = GetComponent<MeshRenderer>();
        render.enabled = false;
    }
}
