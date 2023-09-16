using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Grid : MonoBehaviour {
    public int xSize, ySize;
    public Vector3[] vertices;
    private Mesh mesh;

    //public void doDemo()
    //{
    //    print("xxx");
    //}
    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        
        
        mesh.name = "ProGrid";

        vertices = new Vector3[(xSize) * (ySize)];
        for(int i = 0, y = 0; y  < ySize; ++y)
        {
            for(int x = 0; x < xSize; ++x, ++i)
            {
                vertices[i] = new Vector3(x, y);
            }
        }

        mesh.vertices = vertices;
        int[] triangles = new int[(xSize - 1) * (ySize - 1) * 6];
        for(int y = 0, ti=0, vi = 0; y < ySize - 1; ++y, ++vi)
        {
            for (int x = 0; x < xSize - 1; ++x, ti+=6, ++vi)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = triangles[ti + 4] = vi + xSize;
                triangles[ti + 2] = triangles[ti + 3] = vi + 1;
                triangles[ti + 5] = vi + xSize + 1;
                
            }
        }
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds(); //重新计算包围体，不知道干嘛用

        Material material = new Material(Shader.Find("Diffuse"));
        material.SetColor("_Color", Color.yellow);

        GetComponent<MeshRenderer>().sharedMaterial = material;

    }

    //void Update()
    //{
    //    vertices[10] += new Vector3(0.0f, 0.0f, -0.5f);
    //    mesh.vertices = vertices;
    //    mesh.RecalculateNormals();
    //    mesh.RecalculateBounds(); //重新计算包围体，不知道干嘛用
    //}
    public void initMesh()
    {
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds(); //重新计算包围体，不知道干嘛用

    }

    //private void OnDrawGizmos()
    //{
    //    if (vertices == null)
    //        return;

    //    Gizmos.color = Color.black;
    //    for(int i = 0; i < vertices.Length; ++i)
    //    {
    //        Gizmos.DrawSphere(vertices[i], 0.1f);
    //    }
    //}

	//// Use this for initialization
	//void Start () {
		
	//}
	
	//// Update is called once per frame
	//void Update () {
		
	//}
}
