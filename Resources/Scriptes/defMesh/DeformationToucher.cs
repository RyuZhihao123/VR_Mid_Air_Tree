using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeformationToucher : MonoBehaviour {

    public MeshFilter targetMeshFilter;
    public Camera mainCamera;

    public float forceOffset = 0.1f;
    public float force = 10.0f;
    public float springForce = 20.0f;
    public float damping = 5.0f; //阻尼
    public bool isActive = false;

    private Mesh targetMesh;

    private Vector3[] oriVertices, disVertices, vexVelocities;
    private int verticesCount;


	// Use this for initialization
	void Start () {
        targetMesh = targetMeshFilter.mesh;
        oriVertices = targetMesh.vertices;
        disVertices = targetMesh.vertices;

        verticesCount = oriVertices.Length;
        vexVelocities = new Vector3[verticesCount];

        //outWorldVertices = new Vector3[verticesCount];
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButton(0) && isActive)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hitInfo))
            {
                Vector3 actingForcePoint = targetMeshFilter.transform.InverseTransformPoint(hitInfo.point + hitInfo.normal * forceOffset); //normal 确实是面的法线，且指向mesh外

                for (int i = 0; i < verticesCount; ++i)
                {
                    Vector3 touchPointToVertex = disVertices[i] - actingForcePoint;
                    float actingForce = force / (1.0f + touchPointToVertex.sqrMagnitude);
                    vexVelocities[i] += touchPointToVertex.normalized * actingForce * Time.deltaTime; //vt = vo + a * t;
                    
                    vexVelocities[i] += (oriVertices[i] - disVertices[i]) * springForce * Time.deltaTime;
                    vexVelocities[i] *= 1.0f - damping * Time.deltaTime;
                    disVertices[i] += vexVelocities[i] * Time.deltaTime;
                }
            }


        }
        //for (int i = 0; i < verticesCount; ++i)
        //{
        //    vexVelocities[i] += (oriVertices[i] - disVertices[i]) * springForce * Time.deltaTime;
        //    vexVelocities[i] *= 1.0f - damping * Time.deltaTime;
        //    disVertices[i] += vexVelocities[i] * Time.deltaTime;
        //}

        targetMesh.vertices = disVertices;
        targetMesh.RecalculateNormals();
        
    }

    public Vector3[] getMeshWorldVertices() //得到组成mesh的vertices的世界坐标
    {
        Vector3[] outWorldVertices = new Vector3[verticesCount];

        for(int i = 0; i < verticesCount; ++i)
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
