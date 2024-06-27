using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// struct for allocation
public struct Voxel
{
    // byte for space saving
    public int ID;

    public bool isSolid
    {
        get
        {
            return ID != 0;
        }
    }
}
