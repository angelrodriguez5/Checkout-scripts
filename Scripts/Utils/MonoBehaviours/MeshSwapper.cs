using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshSwapper : MonoBehaviour
{
    [Serializable]
    private struct MeshSwapData
    {
        public Mesh mesh;
        [Range(0f, 1f)]
        public float probability;
    }

    [SerializeField] private SkinnedMeshRenderer _skinnedMesh;
    [SerializeField] private List<MeshSwapData> _meshes;

    private void Awake()
    {
        _meshes = _meshes.OrderByDescending(x => x.probability).ToList();

        float total = 0f;
        _meshes.Map(x => total += x.probability);
        // Check that total probability is 1 (with tolerance)
        if (total - 1f >= 0.001f)
            Debug.LogWarning("Sum of mesh probabilities excedes 1, the lower probability meshes will never show", gameObject);

        SwapMesh();
    }

    public void SwapMesh()
    {
        float rng = UnityEngine.Random.value;

        float accumulator = 0f;
        foreach(var meshData in _meshes)
        {
            accumulator += meshData.probability;
            if (rng <= accumulator)
            {
                _skinnedMesh.sharedMesh = meshData.mesh;
                break;
            }
        }
    }
}
