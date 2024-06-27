using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    // Optional: List<Vector3> normals = new List<Vector3>();

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public Material VoxelMat;

    public static MeshGenerator Instance { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Mesh GenerateMesh(VoxelNode rootNode, float size)
    {
        TraverseSVDAG(rootNode, Vector3.zero, size);
        Mesh mesh = CreateMesh();
        return mesh;
    }

    private Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        Debug.Log("vertices: " + vertices.Count);
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        // Optional: mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals(); // If you haven't calculated normals

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter.mesh = mesh;

        // Assign a material (make sure you have a material in your Assets folder)
        meshRenderer.material = VoxelMat;

        return mesh;
    }

    private void TraverseSVDAG(VoxelNode node, Vector3 position, float size)
    {
        if (node == null) return;

        if (node.IsLeaf)
        {
            // If the node is a leaf and it's filled, create mesh data
            if (node.Value == 1)
            {
                AddVoxelMeshData(position, size);//, node.NodeColor);
            }
        }
        else
        {
            // If the node is not a leaf, recursively traverse its children
            float childSize = size * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 childPosition = CalculateChildPosition(position, childSize, i);
                TraverseSVDAG(node.Children[i], childPosition, childSize);
            }
        }
    }

    private void AddVoxelMeshData(Vector3 position, float size)
    {
        // Define the local vertices of a cube, centered at the origin
        Vector3[] localVertices = {
        new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
        new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
        new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
        new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f)
    };

        // Calculate world position vertices and add to the vertices list
        int vertexStartIndex = vertices.Count;
        for (int i = 0; i < localVertices.Length; i++)
        {
            vertices.Add(position + localVertices[i] * size);
        }

        // Define triangles (order of vertices to create faces)
        int[] newTriangles = {
        0, 2, 1,  0, 3, 2,  // Front face
        2, 3, 6,  3, 7, 6,  // Top face
        1, 2, 6,  1, 6, 5,  // Right face
        0, 7, 3,  0, 4, 7,  // Left face
        0, 1, 5,  0, 5, 4,  // Bottom face
        4, 5, 6,  4, 6, 7   // Back face
    };

        // Add triangles to the triangles list
        for (int i = 0; i < newTriangles.Length; i++)
        {
            triangles.Add(vertexStartIndex + newTriangles[i]);
        }
    }

    Vector3 CalculateChildPosition(Vector3 parentPosition, float childSize, int octantIndex)
    {
        float offsetX = (octantIndex & 1) * childSize;
        float offsetY = (octantIndex & 2) * childSize / 2;
        float offsetZ = (octantIndex & 4) * childSize / 4;
        return new Vector3(parentPosition.x + offsetX, parentPosition.y + offsetY, parentPosition.z + offsetZ);
    }

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }
}
