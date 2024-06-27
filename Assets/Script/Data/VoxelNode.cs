using UnityEngine;

public class VoxelNode
{
    public bool IsLeaf;
    public byte Value; // 0 for empty, 1 for filled
    public VoxelNode[] Children = new VoxelNode[8];
    public Vector3 Position; // The position of the voxel in world space
    public float Size; // The size of the voxel
    public Color NodeColor; // The color of the voxel

    // Constructor to initialize a VoxelNode with position, size, and color
    public VoxelNode(Vector3 position, float size, Color color)
    {
        Position = position;
        Size = size;
        IsLeaf = true;
        Value = 0; // Default to empty
        NodeColor = color;
    }
}
