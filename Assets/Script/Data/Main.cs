using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    VoxelNode voxels;
    Mesh voxelMesh;
    // Start is called before the first frame update
    void Start()
    {
        voxels = SparseVoxelDAG.Instance.InitializeVoxels();
        voxelMesh = MeshGenerator.Instance.GenerateMesh(voxels, SparseVoxelDAG.Instance.gridDepth);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
