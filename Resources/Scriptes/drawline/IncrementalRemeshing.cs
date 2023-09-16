using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class IncrementalRemeshing  {

    // Use this for initialization
    MeshHalfEdge meshHE;
    Lobe lobe;
    //GameObject lo
	//void Start () {
 //       this.init("C:\\Users\\Administrator\\Desktop\\lobeMesh1.ply");

 //       this.Remesh(0.05f);

 //       this.Project_to_surface();

 //       Division divide = new Division();
 //       divide.buttonLoopSubDivision(2);
 //   }

    public void init(Lobe lobe)
    {
        /****** 接口待修改，待删除*****/

        this.lobe = lobe;

        List<List<Vector3>> vertices = new List<List<Vector3>>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < lobe.silParms.Count; ++i)
        {
            List<Vector3> vertex = new List<Vector3>();
            SilhouetteParm parm = lobe.silParms[i];
            for (int j = 0; j < parm.posIDs.Count; ++j)
            {
                vertex.Add(lobe.silPoses[parm.posIDs[j]]);
            }
            vertices.Add(vertex);
        }

        Mesh mesh = lobe.lobeObj.GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < mesh.triangles.Length; ++i)
        {
            triangles.Add(mesh.triangles[i]);
        }



        /******生成半边结构****/
        meshHE = new MeshHalfEdge();
        meshHE.init(vertices, triangles);

        this.Remesh(0.05f);

        this.Project_to_surface();
    }

    void Project_to_surface()
    {
        /*
         * 将新得到的半边结构的mesh 投影回原来的obj
         */
        //GameObject lobeObj = GameObject.Find("comLobeObj");
        //Mesh mesh = lobeObj.GetComponent<MeshFilter>().mesh;
        Mesh mesh = this.lobe.lobeObj.GetComponent<MeshFilter>().mesh;

        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        meshHE.getVerticesAndTriangles(newVertices, newTriangles);

        List<Vector2> newUVs = new List<Vector2>();
        for(int i = 0; i < newVertices.Count; ++i)
        {
            newUVs.Add(new Vector2(0.0f, 0.0f));
        }

        mesh.Clear();
        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.uv = newUVs.ToArray();
        mesh.RecalculateNormals();
    }

    void Remesh(float target_edge_length)
    {
        float low = 0.8f * target_edge_length;
        float high = 0.75f * target_edge_length;
        for (int i = 0; i < 4; ++i)
        {

            Split_long_edges(high);
            //meshHE.printTT();
            //Debug.Log(meshHE.verticesInfo.Count);
            //collapse_short_edges(low, high);
            //equalize_valences();
            tangential_relaxation();
        }
        collapse_short_edges(low, high);
        tangential_relaxation();
    }

    void tangential_relaxation()
    {
        this.Project_to_surface();

        //GameObject lobeObj = GameObject.Find("comLobeObj");
        //Mesh mesh = lobeObj.GetComponent<MeshFilter>().mesh;
        Mesh mesh = this.lobe.lobeObj.GetComponent<MeshFilter>().mesh;


        List<Vector3> normals = new List<Vector3>();     
        mesh.GetNormals(normals);

        List<Vector3> q = new List<Vector3>();
        for(int i = 0; i < meshHE.verticesInfo.Count; ++i)
        {
            VertexInfo info = meshHE.verticesInfo[i];
            Vector3 allPos = new Vector3(0.0f, 0.0f, 0.0f);
            for(int j = 0; j < info.oriHalfEdgesID.Count; ++j)
            {
                HalfEdge edge = meshHE.halfEdges[info.oriHalfEdgesID[j]];
                Vector3 pos = meshHE.verticesInfo[edge.endVertexID].pos;
                allPos += pos;
            }

            q.Add(allPos / info.oriHalfEdgesID.Count);
        }

        for (int i = 0; i < meshHE.verticesInfo.Count; ++i)
        {
            VertexInfo info = meshHE.verticesInfo[i];
            if (info.feature)//跳过特征点
                continue;
            info.pos = q[i] + Vector3.Dot(normals[i], info.pos - q[i]) * normals[i];
        }



        //    List<Vector3> newVertices = new List<Vector3>();
        //List<int> newTriangles = new List<int>();
        //meshHE.getVerticesAndTriangles(newVertices, newTriangles);


    }

    void equalize_valences()
    {

        /*
         * origin c0             AddTri   o          
         *                                |
         *        0                       |                
         *  o--ori--end--o         o   0  o  1  o     
         *        1                       |               
         *                                |
         *        c1                      o                  
         * 
         * 增添两个新的face，旧的两个face 关闭
         * 修正edge oppoedge两个边的走向
         * 修正四个旧的半边，
         * 修正四个顶点;
         */
        for (int i = 0; i < meshHE.halfEdges.Count; i += 2)
        {

            HalfEdge edge = meshHE.halfEdges[i];
            HalfEdge oppoEdge = meshHE.halfEdges[edge.oppoEdgeID];
            if (edge.feature || meshHE.verticesInfo[edge.originVertexID].feature ||meshHE.verticesInfo[edge.endVertexID].feature) //特征边不进行翻转flip
            {
                continue;
            }

            //0.0 找到这两个三角形
            Face face0 = meshHE.faces[edge.faceID];
            Face face1 = meshHE.faces[oppoEdge.faceID];

            //0.1 找到这两个三角形的第三个点
            List<int> ab = new List<int>();
            ab.Add(edge.originVertexID);
            ab.Add(edge.endVertexID);
            int c0 = face0.verticesID.Except(ab).ToArray()[0]; //拿到上半边所属三角形的 另一个vertex的索引
            int c1 = face1.verticesID.Except(ab).ToArray()[0]; //拿到下半边所属三角形的 另一个vertex的索引

            //0.2 衡量比较flip变化前后的 权
            int oldWeight = Mathf.Abs(meshHE.verticesInfo[edge.originVertexID].oriHalfEdgesID.Count - 6) +
                            Mathf.Abs(meshHE.verticesInfo[edge.endVertexID].oriHalfEdgesID.Count - 6) +
                            Mathf.Abs(meshHE.verticesInfo[c0].oriHalfEdgesID.Count - 6) +
                            Mathf.Abs(meshHE.verticesInfo[c1].oriHalfEdgesID.Count - 6);

            int newWeight = Mathf.Abs(meshHE.verticesInfo[edge.originVertexID].oriHalfEdgesID.Count-1 - 6) +
                            Mathf.Abs(meshHE.verticesInfo[edge.endVertexID].oriHalfEdgesID.Count-1 - 6) +
                            Mathf.Abs(meshHE.verticesInfo[c0].oriHalfEdgesID.Count+1 - 6) +
                            Mathf.Abs(meshHE.verticesInfo[c1].oriHalfEdgesID.Count+1 - 6);

            //0.3如果满足 ，则进行flip 翻转
            if(newWeight < oldWeight)
            {
                int faceCount = meshHE.faces.Count;

                //1.0 关闭旧的两个三角形,增加两个新的三角形
                meshHE.faces[edge.faceID].enable = false;
                meshHE.faces[oppoEdge.faceID].enable = false;

                meshHE.faces.Add(new Face(c1, c0, edge.originVertexID));
                meshHE.faces.Add(new Face(c1, edge.endVertexID, c0));

                //meshHE.faces.Add(new Face( c0,c1, edge.originVertexID));
                //meshHE.faces.Add(new Face(c1, edge.endVertexID, c0));
                //1.1 修正四个旧的半边
                int edgeID = meshHE.findEdge(edge.originVertexID, c0);
                if (meshHE.halfEdges[edgeID].faceID == edge.faceID)
                {
                    meshHE.halfEdges[edgeID].faceID = faceCount;
                }
                else
                {
                    meshHE.halfEdges[meshHE.halfEdges[edgeID].oppoEdgeID].faceID = faceCount;
                }

                edgeID = meshHE.findEdge(edge.originVertexID, c1);
                if (meshHE.halfEdges[edgeID].faceID == oppoEdge.faceID)
                {
                    meshHE.halfEdges[edgeID].faceID = faceCount;
                }
                else
                {
                    meshHE.halfEdges[meshHE.halfEdges[edgeID].oppoEdgeID].faceID = faceCount;
                }

                edgeID = meshHE.findEdge(edge.endVertexID, c0);
                if (meshHE.halfEdges[edgeID].faceID == edge.faceID)
                {
                    meshHE.halfEdges[edgeID].faceID = faceCount+1;
                }
                else
                {
                    meshHE.halfEdges[meshHE.halfEdges[edgeID].oppoEdgeID].faceID = faceCount+1;
                }

                edgeID = meshHE.findEdge(edge.endVertexID, c1);
                if (meshHE.halfEdges[edgeID].faceID == oppoEdge.faceID)
                {
                    meshHE.halfEdges[edgeID].faceID = faceCount+1;
                }
                else
                {
                    meshHE.halfEdges[meshHE.halfEdges[edgeID].oppoEdgeID].faceID = faceCount+1;
                }


                //1.2 修正两个边的走向与绑定face
                edge.faceID = faceCount;
                edge.originVertexID = c1;
                edge.endVertexID = c0;

                oppoEdge.faceID = faceCount + 1;
                oppoEdge.originVertexID = c0;
                oppoEdge.endVertexID = c1;

                //1.3 修正四个旧的顶点
                meshHE.verticesInfo[edge.originVertexID].oriHalfEdgesID.Remove(i);
                meshHE.verticesInfo[edge.endVertexID].oriHalfEdgesID.Remove(i+1);
                meshHE.verticesInfo[c1].oriHalfEdgesID.Add(i);

                meshHE.verticesInfo[c0].oriHalfEdgesID.Add(i + 1);


            }

        }

        meshHE.collection_garbage();
    }

 

    void collapse_short_edges(float low, float high)
    {
        /* 
         * 边遍历，一个一个过
         * 折叠边 edge ori->end
         * edge和oppoedge 关闭
         * ori顶点 关闭
         * edge的两个三角形 关闭
         * 遍历ori的剩余4个三角形 的ori点修正为end点
         * 修正ori的剩余四个半边指向 end点
         *  修正end点指向的半边集合，加上这四个半边，
         * 
         */
        //Debug.Log(Time.frameCount + ": collapse ");
        while (true)
        {
            //bool flag = true;
            HalfEdge edge = new HalfEdge();
            int i = 0;
            for (i = 0; i < meshHE.halfEdges.Count; ++i)
            {
              
                edge = meshHE.halfEdges[i];
                if (edge.feature || meshHE.verticesInfo[edge.originVertexID].feature) //特征边不能折,且特征点不能向外折
                {
                    continue;
                }


                if (edge.length < low)
                {
                    bool isCollapse = true;
                    VertexInfo endInfo = meshHE.verticesInfo[edge.endVertexID];
                    VertexInfo originInfo = meshHE.verticesInfo[edge.originVertexID];
                    for(int j = 0; j < originInfo.oriHalfEdgesID.Count; ++j)
                    {
                        HalfEdge targetEdge = meshHE.halfEdges[originInfo.oriHalfEdgesID[j]];
                        VertexInfo targetInfo = meshHE.verticesInfo[targetEdge.endVertexID];
                        if((targetInfo.pos - endInfo.pos).magnitude > high) //不能折这条边，会产生长度>high的边，判断下一条边
                        {
                            isCollapse = false;
                            break;
                        }
                    }

                    if (isCollapse)
                    {
                        //0. 关掉边的两个三角形
                        HalfEdge oppoEdge = meshHE.halfEdges[edge.oppoEdgeID];
                        meshHE.faces[edge.faceID].enable = false;
                        meshHE.faces[oppoEdge.faceID].enable = false;

                        ////3.修正c0 c1点的ori集合
                        //Face face0 = meshHE.faces[edge.faceID];
                        //Face face1 = meshHE.faces[oppoEdge.faceID];

                        //List<int> ab = new List<int>();
                        //ab.Add(edge.originVertexID);
                        //ab.Add(edge.endVertexID);
                        //int c0 = face0.verticesID.Except(ab).ToArray()[0]; //拿到上半边所属三角形的 另一个vertex的索引
                        //int c1 = face1.verticesID.Except(ab).ToArray()[0]; //拿到下半边所属三角形的 另一个vertex的索引
                        //meshHE.verticesInfo[c0].oriHalfEdgesID.Remove(meshHE.verticesInfo[c0].oriHalfEdgesID[0]);
                        //meshHE.verticesInfo[c1].oriHalfEdgesID.Remove(meshHE.verticesInfo[c1].oriHalfEdgesID[0]);


                        //1.关掉被折叠的顶点
                        originInfo.enable = false;

                        //2.修正折叠的这个顶点 所属的三角形，将ori顶点的索引改为end顶点的索引
                        for (int j = 0; j < originInfo.oriHalfEdgesID.Count; ++j)
                        {
                            HalfEdge targetEdge = meshHE.halfEdges[originInfo.oriHalfEdgesID[j]];
                            Face face = meshHE.faces[targetEdge.faceID];

                            face.verticesID[face.verticesID.IndexOf(originInfo.infoID)] = endInfo.infoID;

                            //HalfEdge toEdge = meshHE.halfEdges[targetEdge.oppoEdgeID];
                            //Face toFace = meshHE.faces[toEdge.faceID];

                            //toFace.verticesID[toFace.verticesID.IndexOf(originInfo.infoID)] = endInfo.infoID;

                        }



                        //Debug.Log(edge.originVertexID + "  ，  " + edge.endVertexID);

                        meshHE.collection_garbage(i);
                        
                        break;

                    }

                    
                }
            }

            if(i == meshHE.halfEdges.Count)
            {
                break;
            }
        }
    }

    void Split_long_edges(float high)
    {
        /*
        * 一次函数调用只split当前已存在的三角形，新split出来的暂时不管 
         * origin c0             AddTri   o          
         *                                |
         *        0                       |                
         *  o--ori--end--o         o   0  o  1  o     
         *        1                       |               
         *                                |
         *        c1                      o   
        */
        //Debug.Log("verinfoCount: " + meshHE.verticesInfo.Count + " faceCount:  " + meshHE.faces.Count);
        for (int i = 0; i < meshHE.halfEdges.Count; i += 2)
        {
            HalfEdge edge = meshHE.halfEdges[i];
            if (edge.length > high) {

                int faceCount = meshHE.faces.Count;
                int verticesCount = meshHE.verticesInfo.Count;
                //0.0 设置需要删除两个三角形为false
                HalfEdge oppoEdge = meshHE.halfEdges[edge.oppoEdgeID];
                Face face0 = meshHE.faces[edge.faceID];
                face0.enable = false;
                Face face1 = meshHE.faces[oppoEdge.faceID];
                face1.enable = false;


                //0.1 找到这两个三角形的第三个点
                List<int> ab = new List<int>();
                ab.Add(edge.originVertexID);
                ab.Add(edge.endVertexID);
                int c0 = face0.verticesID.Except(ab).ToArray()[0]; //拿到上半边所属三角形的 另一个vertex的索引
                int c1 = face1.verticesID.Except(ab).ToArray()[0]; //拿到下半边所属三角形的 另一个vertex的索引

                //0.2 增添一个新的vertex
                Vector3 midPos = (meshHE.verticesInfo[edge.originVertexID].pos + meshHE.verticesInfo[edge.endVertexID].pos) / 2.0f;
                VertexInfo midVertexInfo = new VertexInfo(meshHE.verticesInfo.Count, midPos);
                if (edge.feature)//如果是特征边，补两个东西
                {
                    midVertexInfo.feature = true;
                    midVertexInfo.feaCount = meshHE.verticesInfo[edge.originVertexID].feaCount;//归到同一组特征点里
                }
                meshHE.verticesInfo.Add(midVertexInfo);

                //0.3 增添四个面

                meshHE.faces.Add(new Face(c0, edge.originVertexID, verticesCount)); //tri0 ,
                meshHE.faces.Add(new Face(edge.endVertexID, c0, verticesCount));  // tri1;
                meshHE.faces.Add(new Face(edge.originVertexID, c1, verticesCount));  // tri2;
                meshHE.faces.Add(new Face(verticesCount, c1, edge.endVertexID));  // tri3;

                //meshHE.faces.Add(new Face(c0, verticesCount, edge.originVertexID)); //tri0 ,
                //meshHE.faces.Add(new Face(edge.endVertexID, verticesCount, c0));  // tri1;
                //meshHE.faces.Add(new Face(edge.originVertexID, verticesCount, c1));  // tri2;
                //meshHE.faces.Add(new Face(verticesCount,edge.endVertexID, c1));  // tri3;

                //0.4 修正四个旧的半边指向的faceID
                int res = meshHE.findEdge(c0, edge.originVertexID);
                if (res != -1)
                {
                    meshHE.halfEdges[res].faceID = faceCount;
                }

                res = meshHE.findEdge(edge.endVertexID, c0);
                if (res != -1)
                {
                    meshHE.halfEdges[res].faceID = faceCount + 1;
                }

                res = meshHE.findEdge(edge.originVertexID, c1);
                if (res != -1)
                {
                    meshHE.halfEdges[res].faceID = faceCount + 2;
                }

                res = meshHE.findEdge(c1, edge.endVertexID);
                if (res != -1)
                {
                    meshHE.halfEdges[res].faceID = faceCount + 3;

                }

            }

        }

        meshHE.collection_garbage();
    }

    
}
