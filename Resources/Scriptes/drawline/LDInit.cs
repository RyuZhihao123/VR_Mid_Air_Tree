using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using System;
using System.Linq;



class TracingPoint
{
    //描点，包括固定的与移动的
    public TracingPoint()
    {
        vertexID = -1;
        w = 1.0f;
    }
    public TracingPoint(int vertexID, Vector3 movePos)
    {
        this.vertexID = vertexID;
        this.movePos = movePos;

        w = 1.0f;
    }

    public int vertexID; //描点的索引
    public float w;//描点的weight，具体数值的设计 暂搁置
    //public Vector3 oriPos; //描点的初始坐标
    public Vector3 movePos; //描点移动后的坐标
}

class LaplaceVertexInfo
{
    public LaplaceVertexInfo(int fullMeshID = -1, int areaMeshID = -1, Vector3 pos = new Vector3())
    {
        this.fullMeshID = fullMeshID;
        this.areaMeshID = areaMeshID;
        this.pos = pos;
    }

    public int fullMeshID;  //这个顶点在全局mesh的索引
    public int areaMeshID;  //在laplace变形区域mesh的索引
    public Vector3 pos;      //顶点位置
}

public class LaplaceDeformation
{
    MeshHalfEdge laplaceArea = new MeshHalfEdge();
    MeshHalfEdge overallArea = new MeshHalfEdge();



    List<TracingPoint> tracingPoints = new List<TracingPoint>(); //描点集合，包括固定的与移动的描点
    List<TracingPoint> fixedTracingPoints = new List<TracingPoint>(); //固定描点的集合
    List<TracingPoint> moveTracingPoints = new List<TracingPoint>(); //移动描点的集合

    GameObject meshObj;
    Matrix<float> calcA; //经过一系列转至求逆变化之后的A矩阵，方便多次计算变化后的mesh坐标
    Matrix<float> b;
    MatrixBuilder<float> mat;
    List<float> bValues;
    List<Vector3> D;
    List<LaplaceVertexInfo> laplaceVerticesInfo = new List<LaplaceVertexInfo>();

    public void init(GameObject obj)
    {
        this.meshObj = obj;
        //meshObj = GameObject.Find("LaplaceCube");
        overallArea.genMeshhalfEdge(this.meshObj);

    }

    ////List<int> tempTriangles = new List<int>();

    public void setDeformationArea(List<int> vertexIDsArray)
    {
        /*
         * 函数作用:
         *        重新定义laplace的变形区域，设置新的vertices与triangles，同时构建新的半边结构，
         *        将边界处划为固定描点
        *   
        * 参数:
        *        在已有的laplaceCubeObj的mesh基础上，的需要变形区域的mesh索引坐标
        * 
        * 返回:
        *   
        * 
        * 自我解释:
        *      
        */
        this.laplaceVerticesInfo.Clear();

        //0.构建新的半边结构，
        Mesh mesh = meshObj.GetComponent<MeshFilter>().mesh;

        List<bool> vertexTag = new List<bool>();//标记这个顶点是否应该被使用
        for (int i = 0; i < vertexIDsArray.Count; ++i)
        {
            vertexTag.Add(false);
        }

        int[] triangles = mesh.triangles;
        List<int> newTriangles = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            if (vertexIDsArray.Contains(a) && vertexIDsArray.Contains(b) && vertexIDsArray.Contains(c))
            {
                newTriangles.Add(vertexIDsArray.IndexOf(a)); //注意三角形索引坐标的转换
                newTriangles.Add(vertexIDsArray.IndexOf(b));
                newTriangles.Add(vertexIDsArray.IndexOf(c));

                vertexTag[vertexIDsArray.IndexOf(a)] = true; //激活顶点
                vertexTag[vertexIDsArray.IndexOf(b)] = true;
                vertexTag[vertexIDsArray.IndexOf(c)] = true;

            }
        }

        Dictionary<int, int> dic = new Dictionary<int, int>();//存 能用与不能用的顶点之间的索引对应关系
        for (int i = 0; i < vertexIDsArray.Count; ++i)
        {
            if (vertexTag[i])
            {
                dic.Add(i, dic.Count);
            }
        }

        //更新newTriangles
        for (int i = 0; i < newTriangles.Count; ++i)
        {
            newTriangles[i] = dic[newTriangles[i]];
        }


        //0.1找到新的顶点坐标
        List<Vector3> newVertices = new List<Vector3>();
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertexIDsArray.Count; ++i)
        {
            if (vertexTag[i])
            {
                newVertices.Add(vertices[vertexIDsArray[i]]);
                this.laplaceVerticesInfo.Add(new LaplaceVertexInfo(vertexIDsArray[i], dic[i], vertices[vertexIDsArray[i]]));
            }
        }

        //0.2构建新的半边结构
        laplaceArea.genMeshhalfEdge(newVertices, newTriangles);
        laplaceArea.getLaplaceW();

        //1. 将area的边界设为固定描点

        this.fixedTracingPoints.Clear();

        for (int i = 0; i < vertexIDsArray.Count; ++i)
        {
            if (!vertexTag[i])
                continue;

            /*
             * 这俩个其实是一个vertex的info信息，只是一个存的是在laplace 这个局部变形区域的info，一个存的是在整个obj这个大区域的info信息
             */
            VertexInfo info = laplaceArea.verticesInfo[dic[i]];
            VertexInfo infoA = overallArea.verticesInfo[vertexIDsArray[i]];

            if (info.oriHalfEdgesID.Count != infoA.oriHalfEdgesID.Count) //说明是边界点
            {
                this.fixedTracingPoints.Add(new TracingPoint(i, newVertices[dic[i]]));
            }
        }



    }

    public void setInitDeformation(List<int> moveVertexIDs)
    {
        /*
        * 函数作用:
                 设置固定描点，
                 计算 左边的A矩阵
        *   
        * 参数:
        *        在已有的laplaceCubeObj的mesh基础上，的需要变形区域的mesh索引坐标
        * 
        * 返回:
        *   
        * 
        * 自我解释:
        *        变形前的初始化阶段，
        *         已经捏住了移动描点，
        */


        this.tracingPoints.Clear();
        this.moveTracingPoints.Clear();

        //移动描点
        //List<TracingPoint> moveTracingPoints = new List<TracingPoint>();
        foreach (LaplaceVertexInfo info in this.laplaceVerticesInfo)
        {
            if (moveVertexIDs.Contains(info.fullMeshID))
            {
                moveTracingPoints.Add(new TracingPoint(info.areaMeshID, info.pos));
            }
        }

        //完整描点，包括固定与移动
        foreach (TracingPoint point in this.fixedTracingPoints)
            this.tracingPoints.Add(point);

        foreach (TracingPoint point in this.moveTracingPoints)
            this.tracingPoints.Add(point);

        this.calLaplaceMatA();
        D = getMatDelta(); //
    }

    public void setMovingDeformation(Vector3 offset)
    {
        /*
        * 函数作用:
        *   
        * 参数:
        *        
        * 
        * 返回:
        *   
        * 
        * 自我解释:
        *        拖着移动描点在移动的过程
        */
        this.tracingPoints.Clear();


        //完整描点，包括固定与移动
        foreach (TracingPoint point in this.fixedTracingPoints)
            this.tracingPoints.Add(point);

        foreach (TracingPoint point in this.moveTracingPoints)
        {
            this.tracingPoints.Add(new TracingPoint(point.vertexID, point.movePos + offset));
        }

        this.calLaplaceMatb();

        // 4.4计算mesh的new vertices位置
        Matrix<float> res = calcA * b;

        //List<Vector3> newVertices = new List<Vector3>();

        for (int i = 0; i < this.laplaceVerticesInfo.Count; ++i)
        {
            Vector3 v = new Vector3(res[i, 0], res[i, 1], res[i, 2]);
            this.laplaceVerticesInfo[i].pos = v;
            //newVertices.Add(v);
            //Debug.Log(Time.frameCount + "i: " + i + " " + this.laplaceVerticesInfo[i].areaMeshID);
        }

        foreach (TracingPoint point in this.tracingPoints)
        {
            this.laplaceVerticesInfo[point.vertexID].pos = point.movePos;
        }


        //Mesh mesh = meshObj.GetComponent<MeshFilter>().mesh;
        //Vector2[] newUVs = new Vector2[mesh.uv.Length];
        //mesh.uv.CopyTo(newUVs, 0);
        //mesh.Clear();
        //mesh.vertices = newVertices.ToArray();
        //mesh.triangles = tempTriangles.ToArray();
        //mesh.uv = newUVs;
        //mesh.RecalculateNormals();


        Mesh mesh = meshObj.GetComponent<MeshFilter>().mesh;
        List<Vector3> newVertices = new List<Vector3>();
        mesh.GetVertices(newVertices);

        foreach (LaplaceVertexInfo info in this.laplaceVerticesInfo)
        {
            newVertices[info.fullMeshID] = info.pos;
        }
        mesh.SetVertices(newVertices);
        mesh.RecalculateNormals();
    }

    void calLaplaceMatA() //计算laplace的左边矩阵
    {
        List<List<float>> L = getMatL();
        List<List<float>> H = getMatH();



        //4.矩阵组合，用数学库 mathNet进行计算
        mat = Matrix<float>.Build;


        //  4.1 填A矩阵
        int m = this.tracingPoints.Count;
        int n = laplaceArea.verticesInfo.Count;

        List<float> AValues = new List<float>();
        /*
         * 因为matnet的double 初始化 是从列开始的，
         * 所以填数据挺麻烦
         */
        for (int i = 0; i < L.Count; ++i)
        {

            for (int j = 0; j < L[i].Count; ++j)
            {
                AValues.Add(L[i][j]);
            }
        }
        for (int i = 0; i < H.Count; ++i)
        {

            for (int j = 0; j < H[i].Count; ++j)
            {
                AValues.Add(H[i][j]);
            }
        }

        var A = mat.Dense(n, n + m, AValues.ToArray());
        A = A.Transpose();

        // 4.2 求最终计算好的 A矩阵
        calcA = (A.Transpose() * A).Inverse() * A.Transpose();
    }

    void calLaplaceMatb()
    {
        /*
         * 
         * 计算laplace的右边矩阵与remesh网格
         * 
         */

        int m = this.tracingPoints.Count;
        int n = laplaceArea.verticesInfo.Count;


        //  4.3 填b矩阵
        //List<Vector3> D = getMatDelta(); //
        List<Vector3> h = getMath();

        bValues = new List<float>();
        foreach (Vector3 v in D)
        {
            bValues.Add(v.x);
            bValues.Add(v.y);
            bValues.Add(v.z);
        }
        foreach (Vector3 v in h)
        {
            bValues.Add(v.x);
            bValues.Add(v.y);
            bValues.Add(v.z);
        }
        b = mat.Dense(3, m + n, bValues.ToArray());
        b = b.Transpose();



    }
    /*
     * step:
     * 0. 食指带球，掠过需要变形的范围，找到边界设为固定描点
     * 1. 食指带球，接触到mesh时，该部分mesh变颜色，
     * 2. 食指拇指合并，球变颜色，进入按压状态，计算一次A矩阵，记录此时球的位置，同时部分mesh固定，不再改变，该mesh的pos随着球的offset改变而改变
     *     同时带通网格其他部分变形，实时改变b的delta laplace坐标与h的移动描点的新位置
     * 3. 松手则变形结束
     * 
     */

    public void testDemo1(Vector3 pos)
    {

        this.tracingPoints.Last().movePos = pos;

        //this.calLaplaceMatA();
        this.calLaplaceMatb();

    }






    private List<Vector3> getMath()
    {
        /*
         * b的 h矩阵，存描点变化后的坐标
         * 
         * 一个m*3 矩阵，m为描点的个数
         */

        List<Vector3> h = new List<Vector3>();

        foreach (TracingPoint point in this.tracingPoints)
        {
            h.Add(point.movePos);
        }


        return h;
    }

    private List<Vector3> getMatDelta()
    {
        /*
         * b的德尔塔矩阵，也就是 Laplace矩阵
         * 
         * 一个n*3 矩阵 存初始网格的Laplace坐标
         */


        List<Vector3> D = new List<Vector3>();

        List<Vector3> vertices = new List<Vector3>();
        foreach (LaplaceVertexInfo info in this.laplaceVerticesInfo)
        {
            vertices.Add(info.pos);
        }



        for (int i = 0; i < laplaceArea.verticesInfo.Count; ++i)
        {
            Vector3 d = vertices[i];

            VertexInfo info = laplaceArea.verticesInfo[i];
            foreach (int edgeID in info.oriHalfEdgesID)
            {
                HalfEdge edge = laplaceArea.halfEdges[edgeID];

                d -= ((float)edge.w * vertices[edge.endVertexID]);
            }
            D.Add(d);
        }


        return D;


    }

    private List<List<float>> getMatH()
    {
        /*
         * A的H矩阵，按一行一行往
         * 
         * 是个m*n的矩阵，m为描点的数量，包括固定描点与移动描点
         * 这块日后打算用VR来给定范围，
         * 现在假设是固定一个点，移动另一个点
         */
        int n = laplaceArea.verticesInfo.Count;
        List<List<float>> H = new List<List<float>>();

        foreach (TracingPoint point in this.tracingPoints)
        {
            float[] row = new float[n];
            row[point.vertexID] = point.w;

            H.Add(row.ToList());
        }
        return H;


    }

    private List<List<float>> getMatL()
    {
        /*
         * A的L矩阵， 按照 一行一行存的顺序
         * 
         * L应该是个n*n矩阵 n是mesh.vertices.count顶点个数
         */

        int n = laplaceArea.verticesInfo.Count;
        List<List<float>> L = new List<List<float>>();



        foreach (VertexInfo info in laplaceArea.verticesInfo)
        {
            float[] row = new float[n];
            //float sum = 0.0f;
            foreach (int edgeID in info.oriHalfEdgesID)
            {
                HalfEdge edge = laplaceArea.halfEdges[edgeID];
                row[edge.endVertexID] = -(float)edge.w;
                //sum += (float)edge.w;
            }
            //Debug.Log("sum w: " + sum);
            int oriID = laplaceArea.halfEdges[info.oriHalfEdgesID[0]].originVertexID;
            row[oriID] = 1.0f;
            L.Add(row.ToList());


        }



        return L;
    }


}
