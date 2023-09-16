using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetCoordinates : MonoBehaviour {

    // Use this for initialization
    Mesh targetMesh;
    //private void Start()
    //{
        
    //}

    public Vector3[] getMeshWorldVertices() //得到组成mesh的vertices的世界坐标
    {
        targetMesh = GetComponent<MeshFilter>().mesh;
        int verticesCount = targetMesh.vertices.Length;

        Vector3[] outWorldVertices = new Vector3[verticesCount];

        for (int i = 0; i < verticesCount; ++i)
        {
            outWorldVertices[i] = this.transform.TransformPoint(targetMesh.vertices[i]);
        }

        return outWorldVertices;
    }

    public int[] getMeshTriangles() //返回组成这个Mesh的triangles的index
    {
        return targetMesh.triangles;
    }
}
