using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Face
{
    public Face(bool enable = true)
    {
        verticesID = new List<int>();

        this.enable = enable;
    }
    public Face(int a, int b, int c, bool enable = true)
    {
        verticesID = new List<int>(3);
        verticesID.Add(a);
        verticesID.Add(b);
        verticesID.Add(c);

        this.enable = enable;
    }

    public List<int> verticesID; //也就是三角形面，存这三个vertex的索引
    public bool enable; //incremental Remesh用
}

class HalfEdge
{
    public HalfEdge()
    {
        faceID = -1;
        w = -1.0f;
        newVertexID = -1;
        length = 0.0f;
    }
    public HalfEdge(int faceID, int oppoEdgeID, int originVertexID, int endVertexID, float length = 0.0f)
    {
        this.faceID = faceID;
        //this.nextEdgeID = nextEdgeID;
        this.oppoEdgeID = oppoEdgeID;
        this.originVertexID = originVertexID;
        this.endVertexID = endVertexID;

        w = -1.0f;
        newVertexID = -1;
        this.length = length;
    }

    public int faceID;
    //public int nextEdgeID; //生成外环 太麻烦了，而且loopDivision用不上next
    public int oppoEdgeID; //对面半边的索引
    public int originVertexID;
    public int endVertexID;

    public double w;//Laplace Deformation矩阵用
    public int newVertexID; //Loop SubDivision用，存储这个半边生成的 new Vertex的ID
    public float length;//increnmentalRemesh用，存储这个边的长度
    public bool feature;//increamentalRemesh是否是特征边
}

struct TT
{
    public TT(int faceID, int value)
    {
        this.faceID = faceID;
        this.value = value;
    }
    public int faceID;
    public int value;
}

class VertexInfo
{
    public VertexInfo()
    {
        oriHalfEdgesID = new List<int>();
    }

    public VertexInfo(int infoID, Vector3 pos, bool feature = false)
    {
        this.infoID = infoID;
        this.pos = pos;
        oriHalfEdgesID = new List<int>();

        this.enable = true;
        this.feature = feature;
        this.feaCount = -1;
    }
    public List<int> oriHalfEdgesID; //存所有 以这个vertexInfo索引为origin起点的 halfEdges的索引;
    public int infoID;
    public Vector3 pos;

    public bool enable; //incremental Remesh用
    public bool feature; //incremental remesh用，表示是否是特征点
    public int feaCount; //incremental remesh 用，表示特征点所属的组
}

/******************** 半边存储的mesh结构 ********************/
class MeshHalfEdge
{
    /*
     * 以半边结构存储的 mesh 
     */

    public List<VertexInfo> verticesInfo = new List<VertexInfo>();
    public List<HalfEdge> halfEdges = new List<HalfEdge>(); //原则上，halfEdges.count = 2 * vertiesInfo.count
    public List<Face> faces = new List<Face>(); //halfedge 结构用到的faces

    public int findEdge(int originVertexID, int endVertexID)
    {
        /*
         * 传进来 verticesInfo的两个索引，找到这个半边
         */
        int i = 0;
        for (; i < this.halfEdges.Count; ++i)
        {
            HalfEdge edge = this.halfEdges[i];
            if (edge.originVertexID == originVertexID && edge.endVertexID == endVertexID)
            {
                break;
            }

        }
        if (i == this.halfEdges.Count)
        {
            //Debug.Log("Error:findEdge");
            //resultEdge = new HalfEdge();
            return -1;
        }

        //resultEdge = this.halfEdges[i];
        //Debug.Log("resultEdge,  oriID: " + resultEdge.originVertexID + "     endID: " + resultEdge.endVertexID);
        //return true;
        //return this.halfEdges[i];
        return i;

    }

    public void printTT()
    {
        //List<Face> tempFaces = new List<Face>();
        //tempFaces.Add(new Face(1, 2, 3));
        //tempFaces.Add(new Face(2, 4, 3));
        //tempFaces.Add(new Face(2, 4, 5));
        //Debug.Log("old:");
        Dictionary<string, List<int>> dic = new Dictionary<string, List<int>>();
        foreach (Face face in this.faces)
        //foreach (Face face in tempFaces)
        {
            string ab = face.verticesID[0] + "_" + face.verticesID[1];
            string bc = face.verticesID[1] + "_" + face.verticesID[2];
            string ca = face.verticesID[2] + "_" + face.verticesID[0];

            if (dic.ContainsKey(ab))
            {
                //dic[ab] = dic[ab] + 1;
                dic[ab].Add(this.faces.IndexOf(face));

            }
            else
            {
                List<int> t = new List<int>();
                t.Add(this.faces.IndexOf(face));
                dic.Add(ab, t);
            }

            if (dic.ContainsKey(bc))
            {
                dic[bc].Add(this.faces.IndexOf(face));

            }
            else
            {
                List<int> t = new List<int>();
                t.Add(this.faces.IndexOf(face));
                dic.Add(bc, t);
            }

            if (dic.ContainsKey(ca))
            {
                dic[ca].Add(this.faces.IndexOf(face));

            }
            else
            {
                List<int> t = new List<int>();
                t.Add(this.faces.IndexOf(face));
                dic.Add(ca, t);
            }

        }

        bool flag = false;
        foreach (var item in dic)
        {
            if (item.Value.Count > 1)
            {
                Debug.Log(item.Key + " , " + item.Value.Count);
                for (int i = 0; i < item.Value.Count; ++i)
                {
                    Face face = this.faces[item.Value[i]];
                    Debug.Log("faceID: " + item.Value[i] + "   , " + face.verticesID[0] + "  " + face.verticesID[1] + "   " + face.verticesID[2]);
                    Debug.DrawLine(verticesInfo[face.verticesID[0]].pos, verticesInfo[face.verticesID[1]].pos, new Color(1.0f, 0.0f, 0.0f), 1000);
                    Debug.DrawLine(verticesInfo[face.verticesID[1]].pos, verticesInfo[face.verticesID[2]].pos, new Color(1.0f, 0.0f, 0.0f), 1000);
                    Debug.DrawLine(verticesInfo[face.verticesID[2]].pos, verticesInfo[face.verticesID[0]].pos, new Color(1.0f, 0.0f, 0.0f), 1000);


                }
                flag = true;
            }
            //List<int> t = item.Key;
            ////for(int i = 0; i < t.Count; ++i)
            ////{
            //Debug.Log(t[0] + " " + t[1]);
            //}
        }

        if (flag)
            Debug.Log("-----");


        //foreach(VertexInfo info in this.verticesInfo)
        //{
        //    if(info.oriHalfEdgesID.Count == 2)
        //    {
        //        Debug.Log(info.infoID + ", 2222222!");
        //    }
        //}
        //Debug.Log("end");
    }

    public void collection_garbage(int tempEdgeID = -1)
    {
        /*
         * 收集关闭的 顶点和半边，
         * 
         * face里 有完整的 带有enable的三角形索引集合
         * vertexinfo 不能丢，因为有 所属组号和特征点的标记
         * 
         */

        //处理顶点

        List<VertexInfo> newVertexInfos = new List<VertexInfo>();


        Dictionary<int, int> indexMatchDic = new Dictionary<int, int>();
        int k = 0;
        for (int i = 0; i < this.verticesInfo.Count; ++i)
        {
            VertexInfo info = this.verticesInfo[i];
            if (info.enable)
            {
                //if(info.oriHalfEdgesID.Count == 2)
                //{
                //    delVertexInfos.Add(info);
                //    continue;
                //}
                VertexInfo newInfo = new VertexInfo(k, info.pos, info.feature);
                newInfo.feaCount = info.feaCount;
                newVertexInfos.Add(newInfo);

                indexMatchDic.Add(info.infoID, k);
                k++;
            }
        }
        ////处理坏掉的三角形
        //if (delVertexInfos.Count != 0) {
        //    for (int i = 0; i < this.faces.Count; ++i)
        //    {
        //        Face face = this.faces[i];
        //        for (int j = 0; j < delVertexInfos.Count; ++j)
        //        {
        //            if (face.verticesID.Contains(delVertexInfos[j].infoID))
        //            {
        //                face.enable = false;
        //            }
        //        }
        //    }
        //}
        //Debug.Log(Time.frameCount + ":  " + newVertexInfos.Count + "  " + indexMatchDic.Count); 
        //this.verticesInfo.Clear();
        //this.verticesInfo = newVertexInfos;
        //Debug.Log("old:");
        //this.printTT();
        //Debug.Log("end");
        //处理新的三角形face集合
        List<int> triangles = new List<int>();
        foreach (Face face in this.faces)
        {
            if (face.enable)
            {

                if (!indexMatchDic.ContainsKey(face.verticesID[2]))
                {
                    VertexInfo info = this.verticesInfo[face.verticesID[2]];
                    Debug.Log(info.feature + " " + info.feaCount + " " + info.infoID + " " + info.enable + " " + info.oriHalfEdgesID.Count);
                    Debug.Log(this.verticesInfo.Count + " - " + newVertexInfos.Count);
                    Debug.Log("errFaceID:" + this.faces.IndexOf(face) + "  ," + face.verticesID[0] + " " + face.verticesID[1] + " " + face.verticesID[2]);
                    //Debug.Log("445 to 690 faceID:" + this.halfEdges[findEdge(445, 690)].faceID);
                    HalfEdge tEdge = this.halfEdges[tempEdgeID];
                    Debug.Log("edge oriID: " + tEdge.originVertexID + "    endID: " + tEdge.endVertexID);
                    for (int i = 0; i < info.oriHalfEdgesID.Count; ++i)
                    {
                        HalfEdge edge = this.halfEdges[info.oriHalfEdgesID[i]];
                        Face tface = this.faces[edge.faceID];
                        Debug.Log("tEdge： oriID: " + edge.originVertexID + "    endID: " + edge.endVertexID + "   faceID:  " + edge.faceID + "   " + "   ,tface: " + tface.enable + "   " + tface.verticesID[0] + "  " + tface.verticesID[1] + "  " + tface.verticesID[2]);

                        //if(edge.length)
                    }
                    this.printTT();
                }
                triangles.Add(indexMatchDic[face.verticesID[0]]);
                triangles.Add(indexMatchDic[face.verticesID[1]]);
                triangles.Add(indexMatchDic[face.verticesID[2]]);

            }
        }
        this.verticesInfo = newVertexInfos;
        //处理新的半边结构
        this.faces.Clear();
        this.halfEdges.Clear();

        for (int i = 0; i < triangles.Count; i += 3) //三个三个过
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            faces.Add(new Face(a, b, c)); //增加一个Face
            //Debug.Log(i + "    a:" + a + "         b: " + b);

            doHalfEdge(a, b, faces.Count - 1);//处理a-b边
            doHalfEdge(b, c, faces.Count - 1);
            doHalfEdge(c, a, faces.Count - 1);
        }


        this.getEdgeLength(); //计算每个边的长度

        //this.printTT();

        //处理坏掉的三角形
        List<VertexInfo> delVertexInfos = new List<VertexInfo>(); //存坏掉的三角形的顶点
        for (int i = 0; i < this.verticesInfo.Count; ++i)
        {
            VertexInfo info = this.verticesInfo[i];
            if (info.oriHalfEdgesID.Count <= 2)
            {
                info.enable = false;
                delVertexInfos.Add(info);
                //Debug.Log("errInfo edgeCount:  " + info.oriHalfEdgesID.Count + "  , feature: " + info.feature);
            }
        }


        if (delVertexInfos.Count != 0)
        {
            for (int i = 0; i < this.faces.Count; ++i)
            {
                Face face = this.faces[i];
                for (int j = 0; j < delVertexInfos.Count; ++j)
                {
                    if (face.verticesID.Contains(delVertexInfos[j].infoID))
                    {
                        face.enable = false;
                    }
                }
            }
            this.collection_garbage();
            //Debug.Log("error");
        }

    }

    public void init(List<List<Vector3>> vertices, List<int> triangles)
    {
        /*
         * 传入参数：
         *   vertices：为lobe所属的 一组手绘线条， 用这个为每个 特征点分组，以便日后可以 生成特征边
         */
        this.verticesInfo = new List<VertexInfo>(vertices.Count);
        for (int i = 0; i < vertices.Count; ++i)
        {
            for (int j = 0; j < vertices[i].Count; ++j)
            {
                VertexInfo info = new VertexInfo(this.verticesInfo.Count, vertices[i][j], true);
                info.feaCount = i;

                this.verticesInfo.Add(info);
            }
        }

        //1.Generate HalfEdge Structure

        for (int i = 0; i < triangles.Count; i += 3) //三个三个过
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            faces.Add(new Face(a, b, c)); //增加一个Face
            //Debug.Log(i + "    a:" + a + "         b: " + b);

            doHalfEdge(a, b, faces.Count - 1);//处理a-b边
            doHalfEdge(b, c, faces.Count - 1);
            doHalfEdge(c, a, faces.Count - 1);
        }


        this.getEdgeLength(); //计算每个边的长度
    }

    public void getVerticesAndTriangles(List<Vector3> vertices, List<int> triangles)
    {
        /*
         * 返回这个半边结构的 mesh的 顶点集合与三角形集合
         */

        vertices.Clear();
        triangles.Clear();
        //0.拿到顶点集合vertices
        Dictionary<int, int> indexMatchDic = new Dictionary<int, int>();
        int k = 0;
        foreach (VertexInfo info in this.verticesInfo)
        {
            //newVertices.Add(info.pos);
            if (info.enable)
            {
                vertices.Add(info.pos);
                indexMatchDic.Add(info.infoID, k++);
            }
        }


        //1.拿到三角形索引集合
        foreach (Face face in this.faces)
        {
            if (face.enable)
            {

                //triangles.Add(face.verticesID[0]);
                //triangles.Add(face.verticesID[1]);
                //triangles.Add(face.verticesID[2]);

                triangles.Add(indexMatchDic[face.verticesID[0]]);
                triangles.Add(indexMatchDic[face.verticesID[1]]);
                triangles.Add(indexMatchDic[face.verticesID[2]]);


            }

        }
    }

    public bool genMeshhalfEdge(GameObject meshObj) //另一个接口
    {
        Mesh mesh = meshObj.GetComponent<MeshFilter>().mesh;

        List<Vector3> verticesArr = new List<Vector3>();
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; ++i)
        {
            verticesArr.Add(vertices[i]);
        }



        List<int> trianglesArr = new List<int>();
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; ++i)
        {
            trianglesArr.Add(triangles[i]);
        }


        this.genMeshhalfEdge(verticesArr, trianglesArr);

        return true;
    }

    public bool genMeshhalfEdge(Vector3[] vertices, int[] triangles) //另一个接口
    {


        List<Vector3> verticesArr = new List<Vector3>();
        for (int i = 0; i < vertices.Length; ++i)
        {
            verticesArr.Add(vertices[i]);
        }



        List<int> trianglesArr = new List<int>();
        for (int i = 0; i < triangles.Length; ++i)
        {
            trianglesArr.Add(triangles[i]);
        }


        this.genMeshhalfEdge(verticesArr, trianglesArr);

        return true;
    }

    public bool genMeshhalfEdge(List<Vector3> vertices, List<int> triangles)
    {
        //Debug.Log("verticesCount:" + vertices)
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
            verticesInfo.Add(new VertexInfo(i, vertices[i]));
        }

        //1.Generate HalfEdge Structure

        for (int i = 0; i < triangles.Count; i += 3) //三个三个过
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            faces.Add(new Face(a, b, c)); //增加一个Face
            //Debug.Log(i + "    a:" + a + "         b: " + b);

            doHalfEdge(a, b, faces.Count - 1);//处理a-b边
            doHalfEdge(b, c, faces.Count - 1);
            doHalfEdge(c, a, faces.Count - 1);


        }

        //2.处理Laplace方阵中 wi的权值
        //getLaplaceW();

        //for(int i = 0; i < this.halfEdges.Count; ++i)
        //{
        //    Debug.Log("i: " + i + " w, " + this.halfEdges[i].w);
        //}

        return true;
    }

    public void getEdgeLength()
    {
        for (int i = 0; i < halfEdges.Count; ++i)
        {
            HalfEdge edge = halfEdges[i];
            Vector3 a = this.verticesInfo[edge.originVertexID].pos;
            Vector3 b = this.verticesInfo[edge.endVertexID].pos;

            edge.length = (a - b).magnitude;
            //Debug.Log(edge.length);
        }
    }

    public void getLaplaceW()
    {
        /*
         * 得到laplace 矩阵的权值
         * 单独放出来，是因为权值w有很多计算方法
         * 
         * 均值权值，余切权值 等等
         */

        ////0.余切权值
        //Vector3[] vertices = meshObj.GetComponent<MeshFilter>().mesh.vertices;

        //foreach (VertexInfo info in this.verticesInfo)
        //{
        //    for (int i = 0; i < info.oriHalfEdgesID.Count; ++i) //遍历每个顶点的 所有邻接半边(以这个顶点为起始方向的半边)
        //    {
        //        if (this.halfEdges[info.oriHalfEdgesID[i]].w.Equals(-1.0f)) //如果w是-1.0f,即w还未被赋值
        //        {
        //            HalfEdge edge = this.halfEdges[info.oriHalfEdgesID[i]];
        //            int oriID = edge.originVertexID;
        //            int endID = edge.endVertexID;

        //            //计算第一个三角形
        //            Face face1 = this.faces[edge.faceID];
        //            int otherID1 = -1;//face1的 第三个vertex的ID
        //            for (int j = 0; j < 3; ++j)
        //            {
        //                if (face1.verticesID[j] != oriID && face1.verticesID[j] != endID)
        //                {
        //                    otherID1 = face1.verticesID[j];
        //                }
        //            }
        //            //Debug.Log(oriID + " " + endID + " " + otherID1);
        //            double alpha = Vector3.Dot((vertices[oriID] - vertices[otherID1]).normalized, (vertices[endID] - vertices[otherID1]).normalized);
        //            //Debug.Log("alpha: " + alpha + " acos: " + Math.Acos(alpha));
        //            alpha = 1.0 / Math.Tan(Math.Acos(alpha));
        //            //Debug.Log("alpha: " + alpha);
        //            //计算第二个三角形
        //            HalfEdge oppoEdge = this.halfEdges[edge.oppoEdgeID];
        //            Face face2 = this.faces[oppoEdge.faceID];
        //            int otherID2 = -1;//face1的 第三个vertex的ID
        //            for (int j = 0; j < 3; ++j)
        //            {
        //                if (face2.verticesID[j] != oriID && face2.verticesID[j] != endID)
        //                {
        //                    otherID2 = face2.verticesID[j];
        //                }
        //            }

        //            double beita = Vector3.Dot((vertices[oriID] - vertices[otherID2]).normalized, (vertices[endID] - vertices[otherID2]).normalized);
        //            beita = 1.0 / Math.Tan(Math.Acos(beita));
        //            //计算余切弦值
        //            double w = (alpha + beita);
        //            edge.w = w;
        //            oppoEdge.w = w;
        //        }

        //    }

        //}

        ////权值归一化处理
        //foreach (VertexInfo info in this.verticesInfo)
        //{
        //    double sumW = 0.0f; //权值归一化处理
        //    //List<int> edgeIDnorm = new List<int>();// 权值归一化处理
        //    for (int i = 0; i < info.oriHalfEdgesID.Count; ++i) //遍历每个顶点的 所有邻接半边(以这个顶点为起始方向的半边)
        //    {
        //        HalfEdge edge = this.halfEdges[info.oriHalfEdgesID[i]]; ;
        //        sumW += edge.w;
        //    }

        //    for (int i = 0; i < info.oriHalfEdgesID.Count; ++i) //遍历每个顶点的 所有邻接半边(以这个顶点为起始方向的半边)
        //    {
        //        HalfEdge edge = this.halfEdges[info.oriHalfEdgesID[i]]; ;
        //        edge.w /= sumW;
        //    }
        //}

        //1.均值权值

        foreach (VertexInfo info in this.verticesInfo)
        {
            for (int i = 0; i < info.oriHalfEdgesID.Count; ++i) //遍历每个顶点的 所有邻接半边(以这个顶点为起始方向的半边)
            {

                if (this.halfEdges[info.oriHalfEdgesID[i]].w.Equals(-1.0f)) //如果w是-1.0f,即w还未被赋值
                {
                    HalfEdge edge = this.halfEdges[info.oriHalfEdgesID[i]];
                    double w = 1.0 / info.oriHalfEdgesID.Count;
                    edge.w = w;

                }

            }
        }

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


            //Debug.Log("a: " + a + "   b: " + b);
            //两个点都是同组特征点的边，都是特征边
            if (this.verticesInfo[a].feature && (this.verticesInfo[a].feaCount == this.verticesInfo[b].feaCount)) //两个点都是同组特征点的边，都是特征边
            {
                //if(!this.verticesInfo[b].feature)//从特征点出发的边，,且end点不是同组特征点的边，都是特征边
                //edge.feature = true; 
                //if (this.verticesInfo[a].feaCount == this.verticesInfo[b].feaCount) //两个点都是同组特征点的边，都是特征边
                //{
                edge.feature = true;
                edgeOppo.feature = true;
                //}
            }

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
