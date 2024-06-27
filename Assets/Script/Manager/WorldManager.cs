using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public Material worldMaterial;
    private Container container;
    // Start is called before the first frame update
    void Start()
    {
        int counter = 0;
        GameObject cont = new GameObject("Container");
        cont.transform.parent = transform;
        container = cont.AddComponent<Container>();
        container.Initialise(worldMaterial, Vector3.zero);
        
        for (int i = 0; i < 100; i++)
        {
            counter += 10;
            for (int x = 0; x < 10; x++)
            {
                for (int z = 0; z < 10; z++)
                {
                    // int randomYHeight = Random.Range(0, 16);
                    for (int y = 0; y < 10; y++)
                    {
                        container[new Vector3(x+counter, y+counter, z+counter)] = new Voxel() { ID = 1 };
                    }
                }
            }
        }


        container.GenerateMesh();
        container.UploadMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
