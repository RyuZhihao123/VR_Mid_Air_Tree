using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//using System.Mathf;

public class CheckPointInOut : MonoBehaviour {
    private Vector3[] points;

    private Vector3 pointOrigin = new Vector3(-0.25f, 0.05f, -0.25f);
    private Vector3 pointOffset = new Vector3(0.05f, 0.05f, 0.05f);
    private int pointSize = 8;

    bool[] isInOutSphere_0, isInOutSphere_1;
    //bool[] isShowPoints;
    
    /*****  Generate Tree 参数 ******/
    private List<Vector3> treeNodes;  //组成树的结点
    private List<Vector3> attractionPoints; //AP点
    private List<bool> isEnableAP;  //确认AP点是否可用，即是否被kill
    public float D = 0.05f; //Tree的新node的生长长度
    public int diRate = 5; //每个AP的influence radius的倍率，即di = diRate * D
    public int dkRate = 2; //每个AP的kill Distance的倍率，即dk = dkRate * D


    void Start () {

        points = new Vector3[pointSize * pointSize * pointSize];
        for(int x = 0, i = 0; x < pointSize; ++x)
        {
            for(int y = 0; y < pointSize; ++y)
            {
                for(int z = 0; z < pointSize; ++z, ++i)
                {
                    points[i] = new Vector3(x * pointOffset.x, y * pointOffset.y, z * pointOffset.z);
                    points[i] += pointOrigin;
                }
            }
        }

        /*********** Generate Tree 参数 ************/
        treeNodes = new List<Vector3>();
        treeNodes.Add(new Vector3(0.0f, -0.03f, 0.0f));
        treeNodes.Add(new Vector3(0.0f, -0.06f, 0.0f));
        treeNodes.Add(new Vector3(0.0f, -0.09f, 0.0f));

    }

    private void OnDrawGizmos()
    {
        
        if (!(points == null || isInOutSphere_0 == null))
        {
            for (int i = 0; i < points.Length; ++i)
            {
                //Debug.Log(pos);
                //Gizmos.color = Color.red;
                //Gizmos.DrawRay(points[i], points[i] + new Vector3(0.352f, 1.12f, 0.0f).normalized * 100);
                if (isInOutSphere_0[i])
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(points[i], new Vector3(0.003f, 0.003f, 0.003f));
                }
                //Gizmos.draw
            }

            for(int i = 0; i < attractionPoints.Count; ++i)
            {
                if (isEnableAP[i])
                {
                    Gizmos.color = Color.red;
                    
                    Gizmos.DrawCube(attractionPoints[i], new Vector3(0.015f, 0.015f, 0.015f));
                }
            }
        }

        if(this.treeNodes != null) //到时候加个按钮，当树生成完了，不画AP和其他points了
        {
            Gizmos.color = Color.yellow;
            //Debug.Log("OnDrawGizmos-> treeNodes.count: " + treeNodes.Count);
            foreach (Vector3 v in treeNodes)
            {
                Gizmos.DrawCube(v, new Vector3(0.01f, 0.01f, 0.01f));
                //Debug.Log(v);
            }
        }
        //if ()
        //    return;

        //for(int i = 0; ; ++i)
    } 
    public void GenerateTree()
    {
        Debug.Log("GenerateTree!");
        float Di = diRate * D;
        List<int>[] Sv = new List<int>[treeNodes.Count]; //表示影响第0个treeNodes的AP点的集合
        for(int i = 0; i < Sv.Length; ++i)
        {
            Sv[i] = new List<int>();
        }

        // (1)生成Sv，往其中加内容
        for(int i = 0; i < attractionPoints.Count; ++i)
        {
            if (!isEnableAP[i]) //如果这个AP被kill掉，则跳过
                continue;

            float minDis = (attractionPoints[i] - treeNodes[0]).magnitude;
            int minIndex = 0;
            for(int j = 1; j < treeNodes.Count; ++j)
            {
                Debug.Log("xxx");
                float dis = (attractionPoints[i] - treeNodes[j]).magnitude;
                if (dis < minDis)
                {
                    minDis = dis;
                    minIndex = j;
                }
            }
            if(minDis < Di)//如果在 influence radius内，则将这个AP加入指定node的SV集合里
            {
                Sv[minIndex].Add(i);
                Debug.Log("yyy");
            }
        }

        //(2)处理Sv,生成新的TreeNodes

        Debug.Log("Sv.length: " + Sv.Length);
        for(int i = 0; i < Sv.Length; ++i)
        {
            if (Sv[i].Count == 0) //如果该node的Sv集合为empty，则下一个
                continue;
            Debug.Log("zzz");
            //isKill = true;
            Vector3 genDir = new Vector3(0.0f, 0.0f, 0.0f);//新节点的生成方向
            for(int j = 0; j < Sv[i].Count; ++j)
            {
                genDir += attractionPoints[Sv[i][j]] - treeNodes[i];
            }
            genDir = genDir.normalized;
            print("add node-> treeNodes[i]: " + treeNodes[i] + " genDir: " + genDir + " D: " + D + " genDir.magnitude: " + genDir.magnitude);
            Debug.Log((treeNodes[i] + genDir * D).ToString("F4"));
            this.treeNodes.Add(treeNodes[i] + genDir * D);
            
        }

        //(3)根据条件，kill 小于Dk的AP
        float Dk = dkRate * D;
        for (int i = 0; i < attractionPoints.Count; ++i)
        {
            if (!isEnableAP[i]) //如果这个AP被kill掉，则跳过
                continue;

            float minDis = (attractionPoints[i] - treeNodes[0]).magnitude;
            for (int j = 1; j < treeNodes.Count; ++j)
            {
                float dis = (attractionPoints[i] - treeNodes[j]).magnitude;
                if (dis < minDis)
                {
                    minDis = dis;
                }
            }
            if (minDis < Dk)//如果小于Dk,则kill
            {
                isEnableAP[i] = false;
            }
        }

        /*
         * 根据已经标记出的红点，生成树
         * 1.需要一个 vector3[] treeNodes 点集
         * 2.重新划分一个 红点的AP List<Vector3> attractionPoints
         * 3.public float D = 0.5f或其他，表示新node的生长距离
         * 3.第一次迭代，
         *   (1)指定每个AP的influence radius为n*D，即Di
         *   for 循环AP集合，统计第i个AP(isEnableAP[i] = true)和每一个node的distance，找出最近的一个minDis和node的索引minIndex，
         *   若minDis<Di,则将这个minIndex加入List<List<int>> Sv; Sv[minIndex].add(i)
         *   (2)处理Sv, 
         *              bool isKill = false;
         *          for(int i = 0; i < Sv.Count; ++i){
         *            if(!Sv[i].isEmpty){
         *              Vector3 dir = new Vector3();
         *              for(int j = 0; j < Sv[i].Count; ++j){
         *                dir += attractionPoints[sv[i][j]] - treeNodes[i]
         *              }
         *              Vector3.Normalize(dir)//正交化
         *            
         *              treeNodes.add(treeNodes[i] + dir * D)
         *              isKill = true;
         *            }
         *            
         *          }
         *   (3)kill AP
         *     if(isKill){
         *       for(int i = 0; i < Sv.Count; ++i){
         *         if(!Sv[i].isEmpty){
         *            for(int j = 0; j < Sv[i].Count; ++j){
         *              for(int k = 0; k < treeNodes.Count; ++k){
         *                Vector3 dis = attractionPoints[sv[i][j]] - treeNodes[k];
         *                if(dis.magnitude < dk){ //通常dk应该小于di，我感觉的
         *                  来个bool数组， isEnableAP[sv[i][j]] = false;
         *                }
         *              }
         *            }
         *         }
         *       }
         *     }
         *   
         *  4.暂时不加treeNodes之间的父子关系，画出treeNodes这个点集合。
         *  5.按一次按钮，迭代一次
         */
    }

    public void GeneratePointCloud()
    {
        //temp
        //points = new Vector3[1];
        //points[0] = new Vector3(0.0f, 0.0f, 0.0f);
        //temp

        Ray[] rays = new Ray[points.Length];
        for(int i = 0; i < rays.Length; ++i)
        {
            rays[i].origin = points[i];
            rays[i].direction = new Vector3(0.0f, 1.0f, 0.0f).normalized;
        }

        isInOutSphere_0 = isPointInOrOutMesh("Sphere", rays);
        //isShowPoints = new bool[isInOutSphere_0.Length];
        /****** Gernerate Tree的参数初始化 ******/
        attractionPoints = new List<Vector3>();
        isEnableAP = new List<bool>();

        for(int i = 0; i < isInOutSphere_0.Length;++i)
        {
            //isShowPoints[i] = true;
            if (!isInOutSphere_0[i])
            {
                attractionPoints.Add(points[i]);
                isEnableAP.Add(true);
            }
        }

        //Debug.Log(attractionPoints.Count);
    }
    void Update()
    {
        //Ray ray = new Ray(new Vector3(0.0f, 0.45f, -3.0f), new Vector3(0.0f, -0.3f, 1.0f).normalized);
        //Debug.DrawLine(ray.origin, ray.origin + ray.direction * 100.0f, new Color(1.0f, 0.0f, 0.0f));
    }

    //void printMesh()
    bool[] isPointInOrOutMesh(string meshName, Ray[] ray)//如果在mesh外，返回true，在mesh内，返回false
    {
        //targetMesh = GameObject.Find("Sphere").GetComponent<DeformationToucher>().targetMesh;
        //targetMeshWorldVertices = GameObject.Find("Sphere").GetComponent<DeformationToucher>().getMeshWorldVertices();
        Vector3[] targetMeshWorldVertices = GameObject.Find(meshName).GetComponent<DeformationToucher>().getMeshWorldVertices();
        int[] targetMeshTriangles = GameObject.Find(meshName).GetComponent<DeformationToucher>().getMeshTriangles();

        List<Vector3[]> triangles = new List<Vector3[]>();
        for(int i = 0; i < targetMeshTriangles.Length; i += 3)
        {
            Vector3[] tri = new Vector3[3];
            tri[0] = targetMeshWorldVertices[targetMeshTriangles[i]];
            tri[1] = targetMeshWorldVertices[targetMeshTriangles[i+1]];
            tri[2] = targetMeshWorldVertices[targetMeshTriangles[i+2]];
            triangles.Add(tri);
        }

        /*
         * 判断所有射线与这个mesh交点的数量
         */
        bool[] result = new bool[ray.Length]; //如果在mesh外，返回true，在mesh内，返回false
        for(int i = 0; i < result.Length; ++i)
        {
            List<float> res_t = checkTriCollision(ray[i], triangles);
            int count = 0;
            for(int j = 0; j < res_t.Count; ++j)
            {
                if (res_t[j] > -1.0f)
                {
                    count++;
                    //Debug.Log("j:  " + j);
                }
            }
            if (count % 2 == 0)
                result[i] = true;
            else
                result[i] = false;

            //Debug.Log("count: " + count);
        }
        return result;
    }


    /*
     *  ray与组成Mesh的Triangeles的碰撞检测
     * 
     */
    List<float> checkTriCollision(Ray ray, List<Vector3[]> triangles)
    {
        List<float> vec_t = new List<float>();
        for(int i = 0; i < triangles.Count; ++i)
        {
            vec_t.Add(checkSingleTriCollision(ray, triangles[i]));
        }


        return vec_t;
    }

    float checkSingleTriCollision(Ray ray, Vector3[] triangle)
    {
        Vector3 E1 = triangle[1] - triangle[0];
        Vector3 E2 = triangle[2] - triangle[0];

        Vector3 P = Vector3.Cross(ray.direction, E2);
        float det = Vector3.Dot(P, E1);
        
        Vector3 T;
        if(det > 0)
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
