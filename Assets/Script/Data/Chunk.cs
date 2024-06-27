using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public GameObject[,,] voxels;
    public Vector3Int chunkPosition;

    public Chunk(Vector3Int position, int chunkWidth, int chunkHeight, int chunkDepth)
    {
        voxels = new GameObject[chunkWidth, chunkHeight, chunkDepth];
        chunkPosition = position;
    }
}
