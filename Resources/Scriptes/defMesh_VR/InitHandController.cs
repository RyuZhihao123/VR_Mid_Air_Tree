using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Leap;
using Leap.Unity;


public class InitHandController : MonoBehaviour {

    private LeapProvider provider;
    private Frame frame;
    //private LineRenderer line; //画线
    public MeshFilter targetMeshFilter;

    public float forceOffset = 0.1f;
    public float force = 10.0f;
    //public float springForce = 20.0f;
    public float springForce = 0.0f;

    public float damping = 5.0f; //阻尼
    public float touchInfluenceRadius = 0.0005f; //触摸点的影响半径，只有在actionForcePoint指定半径内的点才能被影响。
    public bool isActive = false;


    private Mesh targetMesh;
    private Vector3[] oriVertices, disVertices, vexVelocities;
    private int verticesCount;
    private List<Vector3[]> sphereTriangles; //保存有变形sphere，变形后的三角面数据，用于射线检测,根据targetMesh 来处理

    public float tempActiveTouchLength = 0.03f;//临时的激活touch响应的长度，当食指到球的距离小于这个距离时，开始检测球mesh的所有点和食指的距离

    void initSphereTriangles()
    {
        //sphereTriangles = new List<Vector3[]>();
        for(int i = 0; i < targetMesh.triangles.Length; i+=3)
        {
            Vector3[] tri = new Vector3[3];
            tri[0] = targetMesh.vertices[targetMesh.triangles[i]];
            tri[1] = targetMesh.vertices[targetMesh.triangles[i+1]];
            tri[2] = targetMesh.vertices[targetMesh.triangles[i+2]];

            //sphereTriangles.Add(tri);
            //Debug.Log(i / 3);
            sphereTriangles[i / 3] = tri;
        }
    }

    Mesh getSphere(int percision, float radius)//percision 精度，64，表示这个球是64*64的网格，
    {
        Mesh mesh;
        mesh = new Mesh();

        Vector3[] vertices;
        Vector2[] uvs;
        Vector3[] normals;
        int[] triangles;

        vertices = new Vector3[(percision + 1) * (percision + 1)];
        uvs = new Vector2[(percision + 1) * (percision + 1)];
        normals = new Vector3[(percision + 1) * (percision + 1)];
        triangles = new int[percision * percision * 6];

        float PI = 3.1415926f;

        //球的顶点数据
        for(int y = 0; y <= percision; ++y){
            for(int x = 0; x <= percision; ++x)
            {
                float xSeg = x * 1.0f / percision;
                float ySeg = y * 1.0f / percision;
                float yPos = Mathf.Cos(PI * ySeg);
                float xPos = Mathf.Sin(PI * ySeg) * Mathf.Cos(xSeg * 2.0f * PI);
                float zPos = Mathf.Sin(PI * ySeg) * Mathf.Sin(xSeg * 2.0f * PI);
                int index = y * (percision + 1) + x;
                vertices[index] = radius * new Vector3(xPos, yPos, zPos);
                normals[index] = new Vector3(xPos, yPos, zPos);
                uvs[index] = new Vector2(xSeg, ySeg);
            }
        }

        //球的triangles索引
        for (int y = 0; y < percision; ++y)
        {
            for (int x = 0; x < percision; ++x)
            {
                int index = 6 * (y * percision + x);
                triangles[index] = y * (percision+1) + x;
                triangles[index+1] = y * (percision + 1) + x + 1;
                triangles[index+2] = (y+1) * (percision + 1) + x;
                triangles[index+3] = (y+1) * (percision + 1) + x+1;
                triangles[index+4] = (y+1) * (percision + 1) + x;
                triangles[index+5] = y * (percision + 1) + x + 1;

            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;

        return mesh;
    }

    // Use this for initialization
    void Start () {
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;
        //line = this.GetComponent<LineRenderer>();

        targetMeshFilter.mesh = getSphere(64, 0.1f);

        targetMesh = targetMeshFilter.mesh;
        oriVertices = targetMesh.vertices;
        disVertices = targetMesh.vertices;

        verticesCount = oriVertices.Length;
        vexVelocities = new Vector3[verticesCount];

        //待删除
        sphereTriangles = new List<Vector3[]>();
        for (int i = 0; i < targetMesh.triangles.Length; i += 3)
        {
            Vector3[] tri = new Vector3[3];
            tri[0] = targetMesh.vertices[targetMesh.triangles[i]];
            tri[1] = targetMesh.vertices[targetMesh.triangles[i + 1]];
            tri[2] = targetMesh.vertices[targetMesh.triangles[i + 2]];

            //sphereTriangles.Add(tri);
            //Debug.Log(i / 3);
            sphereTriangles.Add(tri);
        }
        //待删除

        //Debug.Log("len: " + targetMesh.triangles.Length);
        //Debug.Log("len / 3: " + targetMesh.triangles.Length / 3);
        //this.initSphereTriangles();
    }

    // Update is called once per frame
    private Hand lastHand;
    int i = 0;
	void Update () {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("A!");
            GameObject.Find("Init").GetComponent<GenerateTree>().GeneratePointCloud();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("S!");
            GameObject.Find("Init").GetComponent<GenerateTree>().IterationGenerateTree();
        }


        Frame frame = provider.CurrentFrame;
        foreach(Hand hand in frame.Hands)
        {
            if (hand.IsRight)
            {
                if (i == 0)
                {
                    lastHand = hand;
                    i++;
                    return;
                }
                for (int index = 1; index >= 0; --index)
                {
                    Finger indexFinger = hand.Fingers[index]; //indexFinger 食指
                                                              //Debug.Log("indexFinger: direction " + indexFinger.Direction);

                    Vector3 bias = (indexFinger.TipPosition - lastHand.Fingers[index].TipPosition).ToVector3().normalized;
                    //line.SetPositions(new Vector3[] { indexFinger.TipPosition.ToVector3(), (indexFinger.TipPosition + 100.0f * indexFinger.Direction).ToVector3() });

                    //Ray ray = new Ray(indexFinger.TipPosition.ToVector3(), indexFinger.Direction.ToVector3());
                    //RaycastHit hitInfo;
                    //float collisionPoint_t = getNearestCollisionPoint(ray, sphereTriangles);
                    //Debug.DrawLine(ray.origin, ray.origin + ray.direction * collisionPoint_t, new Color(1.0f, 0.6f, 0.3f));
                    //RaycastHit hitInfo;

                    //if (Physics.Raycast(ray, out hitInfo) && (hitInfo.point - indexFinger.TipPosition.ToVector3()).sqrMagnitude < tempActiveTouchLength) 
                    //{
                        //Debug.Log("ok");

                        /*
                         * 这里有个问题，需要重写，碰撞 检测的是ray和碰撞体的碰撞，
                         * 也就是那个球，而不是和组成mesh的一堆三角形的碰撞。
                         */
                        //Vector3 actingForcePoint = targetMeshFilter.transform.InverseTransformPoint(ray.origin + ray.direction * (collisionPoint_t - forceOffset)); //normal 确实是面的法线，且指向mesh外
                        //                                                                                                                                            //Debug.Log(hitInfo.point.magnitude + "  in:  " + targetMeshFilter.transform.InverseTransformPoint(hitInfo.point).magnitude);
                        Vector3 actingForcePoint = indexFinger.TipPosition.ToVector3();
                        ////if（）finger和碰撞点的distance小于一个阈值，则按压球的点，然后只要
                        for (int i = 0; i < verticesCount; ++i)
                        {

                            Vector3 touchPointToVertex = disVertices[i] - actingForcePoint;
                            if (touchPointToVertex.sqrMagnitude > touchInfluenceRadius)
                            {
                                continue;
                            }
                            float actingForce = force / Mathf.Pow((1.0f + touchPointToVertex.sqrMagnitude), 2);
                            vexVelocities[i] += (touchPointToVertex.normalized + 3.0f * bias).normalized * actingForce * Time.deltaTime; //vt = vo + a * t;

                            vexVelocities[i] += (oriVertices[i] - disVertices[i]) * springForce * Time.deltaTime;
                            vexVelocities[i] *= 1.0f - damping * Time.deltaTime;
                            disVertices[i] += vexVelocities[i] * Time.deltaTime;
                        }

                        targetMesh.vertices = disVertices;

                        targetMesh.RecalculateNormals();
                        //this.initSphereTriangles();
                    //}
                }
                lastHand = hand;
            }
        }
        
	}

    //float getNearestCollisionPoint()


   float getNearestCollisionPoint(Ray ray, List<Vector3[]> triangles)
    {
        float result = -1.0f;
        List<float> vec_t = new List<float>();
        for (int i = 0; i < triangles.Count; ++i)
        {
            float res = checkSingleTriCollision(ray, triangles[i]);
            if (res < 0.0f)
                continue;
            vec_t.Add(res);
        }

        vec_t.Sort();
        if (vec_t.Count > 0)
            result = vec_t[0];

        return result;
    }

    float checkSingleTriCollision(Ray ray, Vector3[] triangle)
    {
        Vector3 E1 = triangle[1] - triangle[0];
        Vector3 E2 = triangle[2] - triangle[0];

        Vector3 P = Vector3.Cross(ray.direction, E2);
        float det = Vector3.Dot(P, E1);

        Vector3 T;
        if (det > 0)
        {
            T = ray.origin - triangle[0];
        }
        else
        {
            T = triangle[0] - ray.origin;
            det *= -1.0f;
        }

        if (det < 0.00001f) //表示射线与三角面所在的平面平行，返回不相交
            return -1.0f;

        /******* 相交则判断 交点是否落在三角形面内 *********/
        float u = Vector3.Dot(P, T);
        if (u < 0.0f || u > det)
            return -1.0f;

        Vector3 Q = Vector3.Cross(T, E1);
        float v = Vector3.Dot(Q, ray.direction);
        if (v < 0.0f || u + v > det)
            return -1.0f;

        float t = Vector3.Dot(Q, E2);
        if (t < 0.0f)
            return -1.0f;

        return t / det;
    }

}


