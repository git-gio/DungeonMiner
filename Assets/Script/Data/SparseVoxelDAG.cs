using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

public class SparseVoxelDAG : MonoBehaviour
{
    public byte[,,] voxelGrid;
    public int gridDepth = 4; // Define the depth of your octree
    public VoxelNode rootNode; // Where the root of the octree will be stored

    private Dictionary<string, VoxelNode> subtreeCache = new Dictionary<string, VoxelNode>();
    public static SparseVoxelDAG Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    // Start is called before the first frame update
    /*void Start()
    {
        int gridSize = 1 << gridDepth; // Calculate the size of the grid
        voxelGrid = new byte[gridSize, gridSize, gridSize];

        // Fill the voxel grid with test data
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Simple pattern: fill every other voxel
                    voxelGrid[x, y, z] = (byte)((x + y + z) % 2);
                }
            }
        }

        // Build the DAG from the voxel grid
        rootNode = BuildDAG(voxelGrid, gridDepth);
    }*/

    public VoxelNode InitializeVoxels()
    {
        int gridSize = 1 << gridDepth; // Calculate the size of the grid
        voxelGrid = new byte[gridSize, gridSize, gridSize];

        // Fill the voxel grid with test data
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Simple pattern: fill every other voxel
                    voxelGrid[x, y, z] = (byte)((x + y + z) % 2);
                }
            }
        }

        // Assume the world's origin is also the root node's position and the grid size is its size
        Vector3 rootPosition = new Vector3(0, 0, 0);
        int rootSize = gridSize; // Ensure this is a power of two

        // Build the DAG from the voxel grid with position and size
        rootNode = BuildDAG(voxelGrid, gridDepth, rootPosition, rootSize);
        return rootNode;
    }

        // Update is called once per frame
        void Update()
    {
        
    }

    VoxelNode BuildDAG(byte[,,] grid, int depth, Vector3 position, int size)
    {
        // Generate a key for the current grid to check for duplicates
        string key = GenerateSubtreeKey(grid);

        // Check if a node with this configuration already exists to avoid duplications
        if (subtreeCache.TryGetValue(key, out VoxelNode existingNode))
        {
            return existingNode;
        }

        // Create a new node
        VoxelNode node;
        if (depth == 0 || IsHomogeneous(grid))
        {
            // If we're at the maximum depth or the current grid section is homogeneous, create a leaf node
            node = new VoxelNode(position, size, IsHomogeneous(grid) ? Color.green : Color.red)
            {
                IsLeaf = true,
                Value = grid[0, 0, 0]
            };
        }
        else
        {
            // Otherwise, create an internal node and recursively construct its children
            node = new VoxelNode(position, size, Color.blue) { IsLeaf = false };
            int newSize = size / 2;
            for (int i = 0; i < 8; i++)
            {
                byte[,,] octant = GetOctant(grid, i, newSize);
                Vector3 childPosition = CalculateChildPosition(position, newSize, i);
                node.Children[i] = BuildDAG(octant, depth - 1, childPosition, newSize);
            }
        }

        // Inside the BuildDAG method, before returning a new node
        // Color nodeColor = CalculateColorBasedOnPosition(position, size, gridDepth);
        node.NodeColor = Color.white;


        // Add the node to the cache and return it
        subtreeCache[key] = node;
        return node;
    }

    bool IsHomogeneous(byte[,,] grid)
    {
        byte firstValue = grid[0, 0, 0];
        foreach (byte value in grid)
        {
            if (value != firstValue)
            {
                return false;
            }
        }
        return true;
    }

    byte[,,] GetOctant(byte[,,] grid, int octantIndex, int size)
    {
        int xStart = (octantIndex & 1) * size;
        int yStart = (octantIndex & 2) * size / 2;
        int zStart = (octantIndex & 4) * size / 4;

        byte[,,] octant = new byte[size, size, size];

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    octant[x, y, z] = grid[xStart + x, yStart + y, zStart + z];
                }
            }
        }
        return octant;
    }

    /*VoxelNode BuildDAG(byte[,,] grid, int depth)
    {
        string key = GenerateSubtreeKey(grid);

        // Check if an identical subtree is already in the cache
        if (subtreeCache.TryGetValue(key, out VoxelNode existingNode))
        {
            return existingNode; // Reuse the existing node
        }

        if (depth == 0 || IsHomogeneous(grid))
        {
            var node = new VoxelNode { IsLeaf = true, Value = grid[0, 0, 0] };
            subtreeCache[key] = node;
            return node;
        }
        else
        {
            VoxelNode node = new VoxelNode { IsLeaf = false };
            int newDepth = depth - 1;
            int size = 1 << newDepth;
            for (int i = 0; i < 8; i++)
            {
                byte[,,] octant = GetOctant(grid, i, size);
                node.Children[i] = BuildDAG(octant, newDepth);
            }
            subtreeCache[key] = node; // Add the new subtree to the cache
            return node;
        }
    }*/

    string GenerateSubtreeKey(byte[,,] grid)
    {
        // If the grid is homogeneous, return a simple key (all values are the same)
        if (IsHomogeneous(grid))
        {
            return $"Homogeneous_{grid[0, 0, 0]}";
        }

        // If the grid is not homogeneous, create a more complex key
        // This example uses a StringBuilder to efficiently build a string, but you may want
        // to consider a more sophisticated hashing function for larger or more complex grids
        StringBuilder keyBuilder = new StringBuilder();
        int gridSize = grid.GetLength(0); // Assuming the grid is cubic

        for (int z = 0; z < gridSize; z++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    keyBuilder.Append(grid[x, y, z].ToString());
                }
            }
        }

        // Use a hashing function to create a hash of the string
        string key = keyBuilder.ToString();
        using (var hashAlgorithm = SHA256.Create())
        {
            // Convert the input string to a byte array and compute the hash
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(key));

            // Create a new StringBuilder to collect the bytes and create a string
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data and format each one as a hexadecimal string
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string
            return sBuilder.ToString();
        }
    }

    void VisualizeDAG(VoxelNode node, Vector3 position, float size)
    {
        if (node == null) return;

        // Visualization for the node
        /*Gizmos.color = node.IsLeaf ? Color.green : Color.blue;
        Gizmos.DrawWireCube(position, Vector3.one * size);*/

        if (!node.IsLeaf)
        {
            // Recursively visualize children
            float childSize = size / 2f;
            for (int i = 0; i < node.Children.Length; i++)
            {
                Vector3 childPosition = CalculateChildPosition(position, childSize, i);
                VisualizeDAG(node.Children[i], childPosition, childSize);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (rootNode != null)
        {
            VisualizeDAG(rootNode, Vector3.zero, gridDepth);
        }
    }

    Vector3 CalculateChildPosition(Vector3 parentPosition, float childSize, int octantIndex)
    {
        float offsetX = (octantIndex & 1) * childSize;
        float offsetY = (octantIndex & 2) * childSize / 2;
        float offsetZ = (octantIndex & 4) * childSize / 4;
        return new Vector3(parentPosition.x + offsetX, parentPosition.y + offsetY, parentPosition.z + offsetZ);
    }
}
