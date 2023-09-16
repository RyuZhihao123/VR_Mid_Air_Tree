using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

public class Division {
    List<VertexInfo> verticesInfo = new List<VertexInfo>();
    List<HalfEdge> halfEdges = new List<HalfEdge>(); //原则上，halfEdges.count = 2 * vertiesInfo.count
    List<Face> faces = new List<Face>(); //halfedge 结构用到的faces

    //public int itnum = 2;
    // Use this for initialization


    public void buttonLoopSubDivision(Lobe lobe, int itnum = 2)
    {
        //Mesh mesh = divisionObj.GetComponent<MeshFilter>().mesh;
        //Lobe lobe = this.lobes[lobes.Count - 1];
        //foreach (Lobe lobe in this.lobes)
        //{

        Mesh mesh = lobe.lobeObj.GetComponent<MeshFilter>().mesh;


        List<Vector3> vertices = new List<Vector3>();
        for (int i = 0; i < mesh.vertices.Length; ++i)
            vertices.Add(mesh.vertices[i]);

        List<int> triangles = new List<int>();
        for (int i = 0; i < mesh.triangles.Length; ++i)
            triangles.Add(mesh.triangles[i]);

        //0.根据已有边，生成新的HalfEdge Structure
        genMeshhalfEdge(vertices, triangles);

        //int itnum = 2;
        /*
         * for 从这里开始 写迭代的循环条件
         */
        for (int i = 0; i < itnum; ++i)
        {
            int n = vertices.Count; //表示前n个vertex都是 old vertex， 后来的都是新添的

            //1. add new Vertices;
            //  这步之后 vertices会改变，有新的vertex值存进去
            addNewVertices(vertices);

            //2. 组合新的三角形
            //  这步之后 triangles会改变，有新的triangle值存进去
            triangles = combineNewTriangle(triangles);

            //3. update old Vertices的position
            //传入参数 vertices 与 n
            //updateOldVertices(n, vertices);
            genMeshhalfEdge(vertices, triangles); //更新半边结构

        }
        List<Vector2> newUvs = new List<Vector2>();
        for(int i = 0; i < vertices.Count; ++i)
        {
            newUvs.Add(new Vector2(0.0f, 0.0f));
        }

        //4. 将新vertices与新Triangles 赋给mesh网格
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = newUvs.ToArray();
        mesh.RecalculateNormals();

        //1.生成lobe的控制用 obj,移动控制用参数
        lobe.lobeOldMeshVertices = new Vector3[mesh.vertices.Length];
        mesh.vertices.CopyTo(lobe.lobeOldMeshVertices, 0);
        //}
    }


    void updateOldVertices(int num, List<Vector3> vertices)
    {
        /*
	     * 因为前 num 个存的是 old Vertex的索引，
		 * 且这时 已经有了一个新的半边结构 vertexinfo 也是最新的。
		 */

        for (int i = 0; i < num; ++i)
        {
            /*
			* 判断是内部点还是边界点
			* 根据 半边的faceid 是否为1 判断
			*/
            bool isInside = true; //假设是内部点
            List<int> dstInVertexID = new List<int>(); //存以i为起点的内部点的 的所有边的 另一个vertex的索引
            List<int> dstOutVertexID = new List<int>(); //存以i为起点的外部点的 的所有边的 另一个vertex的索引

            for (int j = 0; j < verticesInfo[i].oriHalfEdgesID.Count; ++j)
            {
                int halfedgeID = verticesInfo[i].oriHalfEdgesID[j];
                int oppoID = halfEdges[halfedgeID].oppoEdgeID;
                if (halfEdges[halfedgeID].faceID == -1 || halfEdges[oppoID].faceID == -1)
                {
                    isInside = false; // 说明是边界点
                    dstOutVertexID.Add(halfEdges[halfedgeID].endVertexID);
                }
                if (isInside)
                {
                    dstInVertexID.Add(halfEdges[halfedgeID].endVertexID);
                }
            }




            Vector3 pos = new Vector3();
            if (isInside)
            {
                int n = dstInVertexID.Count;
                float beita = 1.0f / n * (0.625f - Mathf.Pow(0.375f + 0.25f * Mathf.Cos(2 * 3.1415926f / n), 2));
                Vector3 t = new Vector3();
                for (int k = 0; k < n; ++k)
                {
                    t += vertices[dstInVertexID[k]];
                }
                pos = (1.0f - n * beita) * vertices[i] + beita * t;
            }
            else
            {
                pos = 0.75f * vertices[i] + 0.125f * (vertices[dstOutVertexID[0]] + vertices[dstOutVertexID[1]]);
            }


            vertices[i] = pos;
        }


        //List<Vector3> newPoses = new List<Vector3>(verticesInfo.Count);


    }

    List<int> combineNewTriangle(List<int> triangles)
    {
        /*
		 * triangles 为传入参数，
		 * 根据旧的triangles 与新的vertices ，组合成新的三角形集合
		 * 
		 * 结合halfEdges集合与vertexInfo集合
		 */
        List<int> newTriangles = new List<int>();
        for (int i = 0; i < triangles.Count; i += 3)
        {
            //原则是 进来一个三角形3个顶点，返回4个三角形12个顶点索引
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            // d e f
            int d = halfEdges[isEdgeAdjacent(verticesInfo[a], b)].newVertexID; //找到由a指向b的 半边，然后找到这个半边上的new Vertex的索引
            int e = halfEdges[isEdgeAdjacent(verticesInfo[b], c)].newVertexID;
            int f = halfEdges[isEdgeAdjacent(verticesInfo[c], a)].newVertexID;

            //顺时针组合
            newTriangles.Add(a);
            newTriangles.Add(d);
            newTriangles.Add(f);

            newTriangles.Add(d);
            newTriangles.Add(b);
            newTriangles.Add(e);

            newTriangles.Add(f);
            newTriangles.Add(d);
            newTriangles.Add(e);

            newTriangles.Add(f);
            newTriangles.Add(e);
            newTriangles.Add(c);
        }

        return newTriangles;

    }

    void addNewVertices(List<Vector3> vertices)
    {
        /*
		 * 增加新的顶点，遍历所有半边，计算新的顶点位置。
		 * 根据每个边 指向面FaceID的数量 确定这个边是边界边还是内部边
		 * 定义：一个边有一个指向面为边界边
		 *               两个指向面为内部边
		 * 
		 * vertices 为传出参数，会包含已经新增的vertex
		 */
        for (int i = 0; i < halfEdges.Count; ++i)
        {
            HalfEdge edge = halfEdges[i]; //this 半边
            HalfEdge oppo = halfEdges[halfEdges[i].oppoEdgeID]; //this 半边的对边

            if (edge.newVertexID != -1) //说明这个半边已经有了 新的vertex
                continue;

            int n = 0;
            if (edge.faceID != -1) //如果这个半边有 指向面
                n += 1;
            if (oppo.faceID != -1) //如果这个半边的对边有 指向面
                n += 1;

            Vector3 a = vertices[edge.originVertexID];
            Vector3 b = vertices[edge.endVertexID];
            Vector3 pos = new Vector3();
            if (n == 1)
            {
                //说明这个边（注意，不是半边），是边界边
                pos = (a + b) / 2.0f;
            }
            else if (n == 2)
            {
                //内部边
                List<int> A = faces[edge.faceID].verticesID; //this 半边指向face的 三个vertex的索引
                List<int> B = faces[oppo.faceID].verticesID; //对边指向face的 三个vertex的索引

                //https://www.cnblogs.com/godbell/p/7535637.html 取A与B的差集，就是c点，B与A的差集就是d点；
                List<int> cID = A.Except(B).ToList(); //按原则，这个List 应该只有一个元素，就是c所在的索引
                Vector3 c = vertices[cID[0]];

                List<int> dID = B.Except(A).ToList();
                Vector3 d = vertices[dID[0]];

                //pos = 0.375f * (a + b) + 0.125f * (c + d);
                pos = (a + b) / 2.0f;
            }

            edge.newVertexID = vertices.Count; //绑定这个半边的 newVertex的索引
            oppo.newVertexID = vertices.Count; //绑定这个半边的对边的newVertex的索引

            vertices.Add(pos);

        }

    }



    bool genMeshhalfEdge(List<Vector3> vertices, List<int> triangles)
    {
        /*
		 * vertices triangles为传进的参数
		 * verticesInfo halfEdges, faces为传出的参数（猜测C# 用的都是浅拷贝）
		 * 
		 * verticesInfo 设计为 其.count = vertices.length， 存储以这个 索引为 halfEdge origin的所有halfEdge的索引，
		 *   帮助构建 整体mesh网格的half结构，与之后loop subdivision用于判断每个vertex的邻接vertex数量之用
		 * 
		 */
        if (vertices.Count == 0 || triangles.Count == 0)
        {
            Debug.Log("genMeshHalfEdge ERROR:: vertices null!");
        }

        //0.清空传出参数，因为genMeshHalfEdge（）这个函数要用到很多次
        verticesInfo.Clear();
        halfEdges.Clear();
        faces.Clear();

        verticesInfo = new List<VertexInfo>(vertices.Count);
        for (int i = 0; i < vertices.Count; ++i)
        {
            verticesInfo.Add(new VertexInfo());
        }

        //1.Generate HalfEdge Structure
        for (int i = 0; i < triangles.Count; i += 3) //三个三个过
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            faces.Add(new Face(a, b, c)); //增加一个Face


            doHalfEdge(a, b, faces.Count - 1);//处理a-b边
            doHalfEdge(b, c, faces.Count - 1);
            doHalfEdge(c, a, faces.Count - 1);


        }


        return true;
    }

    void doHalfEdge(int a, int b, int faceID)
    {
        /*
		 *
		 */
        int res = isEdgeAdjacent(verticesInfo[a], b);

        if (res != -1)
        {
            //如果含有，则说明a-b这个 halfEdge已经构建
            halfEdges[res].faceID = faceID;  //更改这个halfEdge的指向的faceID
                                             //halfEdges[res].nextEdgeID = 
        }
        else
        {
            //不含有，则构建a-b 这个halfEdge
            int n = halfEdges.Count;
            //halfEdges.Add(new HalfEdge()); //先在总 halfEdges中占两个位置，分别是a-b边和其对边的 halfEdge
            //halfEdges.Add(new HalfEdge());


            HalfEdge edge = new HalfEdge(faceID, n + 1, a, b); // a-b 边，其自身索引为n，对边索引为n+1
            HalfEdge edgeOppo = new HalfEdge(-1, n, b, a);   //其对边 b-a 边，默认faceID为-1

            verticesInfo[a].oriHalfEdgesID.Add(n);
            verticesInfo[b].oriHalfEdgesID.Add(n + 1);


            halfEdges.Add(edge);
            halfEdges.Add(edgeOppo);


        }
    }


    int isEdgeAdjacent(VertexInfo info, int endVertexID)
    {
        /*
		 * 判断endVertexID 这个点 是否在这个 Vertexinfo中，即是否与这个vertex相邻
		 * 如果没有 返回-1
		 * 有则返回这个 由info指向endVertexID 的halfedge的索引
		 */
        int result = -1;

        for (int i = 0; i < info.oriHalfEdgesID.Count; ++i)
        {
            int end = halfEdges[info.oriHalfEdgesID[i]].endVertexID;
            if (end == endVertexID)
                return info.oriHalfEdgesID[i];
        }


        return result;
    }
}

