using System.Collections;
using System.Collections.Generic;
using System;
//using System.Math;
using UnityEngine;
using Leap;
using Leap.Unity;
using Object = UnityEngine.Object;

public class init : MonoBehaviour {
    private LeapProvider provider;
    private Frame frame;
    // Use this for initialization
    private GameObject obj;
    private List<GameObject> points_list = new List<GameObject>();
    private List<Vector3> pointsOriPos_list = new List<Vector3>();
    private Vector3 offset = new Vector3(0.1f, 0.1f, 0.1f);
    private int rank = 5; //5x5 得到点云的阶
    GameObject obstacle;

    public Transform PalmUIPivotAnchor;
    private bool isUIDisplay = false; //控制UI的控制台是否显示的state
    private bool isButRotate = false; //控制旋转按钮是否激活的state
    private bool isButExplode = false; //同，控制爆炸
    private bool isButPick = false; //同，控制是否选定扔掉小球
    void Start () {
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;
        /*
         我可以让 camera跟着转，拉近，obstacle就不动了吧，回来试试
         */
        //gb = new GameObject();
        obstacle = GameObject.Find("Obstacles");//得到parent类
        //Object point = Resources.Load("Prefabs/point", typeof(GameObject));
        Object point = Resources.Load("Prefabs/preSphere", typeof(GameObject));

        Vector3 origin = offset * (int)(-rank/2);//点云 起始的生成位置
        //print(origin);

        //obj = Instantiate(point, new Vector3(0, 0, 0),Quaternion.identity, obstacle.transform) as GameObject;

        for (int x = 0; x < rank; ++x)
        {
            for(int y = 0; y < rank; ++y)
            {
                for(int z = 0; z < rank; ++z)
                {
                    Vector3 pos = new Vector3(offset.x * x, offset.y * y, offset.z * z) + origin;
                    pointsOriPos_list.Add(pos);
                    obj = Instantiate(point, pos, Quaternion.identity, obstacle.transform) as GameObject;
                    points_list.Add(obj);
                }
            }
        }

        //obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //obj.transform.SetParent(obstacle.transform);
        //obj.AddComponent<Rigidbody>();
        //Instantiate()
    }

    // Update is called once per frame
    void Update () {
        /*
         * 放一个按钮的状态检测函数，时刻检测左手的ui控制台是否开启
         * 1：开启，且点击命令按钮，则右手开始执行旋转位移操作
         * 2：控制台关闭，则保持obstacles的当下状态不变
         */

        //0.控制isUIDisplay的状态机
        if (PalmUIPivotAnchor.transform.localScale.Equals(new Vector3(1.0f, 1.0f, 1.0f)))
        {
            isUIDisplay = true;
        }
        else
        {
            isUIDisplay = false;
            isButRotate = false;
            isButExplode = false;
            isButPick = false;
        }



        //1.LeapMotion Hand的操作
        Frame frame = provider.CurrentFrame;
        foreach (Hand hand in frame.Hands)
        {
            if (hand.IsRight)
            {
                //print(hand.PalmVelocity);
                //Debug.Log(hand.PalmVelocity);
                if (isUIDisplay )
                {
                    if (isButExplode)
                    {
                        explode(hand.GrabStrength);
                    }else if (isButRotate)
                    {
                        rotate(hand);
                    }else if(isButPick)
                    {
                        pickPoint(hand.PalmPosition.ToVector3());
                    }
                }

                //if()
                //if (Gesture.isCloseHand(hand))
                //{
                //    print("close");
                //    //obstacle.transform.position = hand.PalmPosition.ToVector3() + hand.PalmNormal.ToVector3() * (transform.localScale.y * .5f + .02f);
                //    //obstacle.transform.rotation = hand.Basis.CalculateRotation();
                //}else if (Gesture.isOpenFullHand(hand))
                //{
                //    print("open");
                //}

            }
        }
    }

    void pickPoint(Vector3 pos)
    {
        //print(pos);
        //print(points_list.Count);
        for (int i = 0; i < points_list.Count; ++i)
        {
            float dist = (points_list[i].GetComponent<Rigidbody>().position - pos).magnitude;
            if(dist < 1) //扯犊子 这技术反人类！！！！
            {
                points_list[i].transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            }
          //print(points_list[i].GetComponent<Rigidbody>().position);
        }
    }

    void explode(float rate) // 一个点云爆炸的特效
    {
        //intensity 为 0.0f -- 1.0f 的强度
        float intensity = 5.0f * rate;
        for(int i = 0; i < points_list.Count; ++i)
        {

            //points_list[i].transform.position = (1.0f + intensity) * pointsOriPos_list[i];
            /*
             * 使用RigidBody，而不是transform的position转换，是因为
             * transform会强制physX物体立即做一些沉重的负担计算，而rigidbody会在下一个physics simulation step中
             * 才更新位置，这比transform更快，因为后者会导致碰撞体重新计算他们相当于rigidbody的位置。
             */
            points_list[i].GetComponent<Rigidbody>().position = (1.0f + intensity) * pointsOriPos_list[i];

        }
    }

    float minActiveVelocity = 0.3f; //最小激活速度
    float rotateSpeed = 10.0f;
    float transSpeed = 1.0f;
    void rotate(Hand hand)
    {
        /*
         * 单方向的旋转
         */
        if (hand.PalmVelocity.x < -minActiveVelocity) {
            obstacle.transform.Rotate(new Vector3(0.0f, 1.0f, 0.0f), hand.PalmVelocity.x * rotateSpeed, Space.World);
        }else if (hand.PalmVelocity.y < -minActiveVelocity) {
            obstacle.transform.Rotate(new Vector3(1.0f, 0.0f, 0.0f), hand.PalmVelocity.y * rotateSpeed, Space.World);
            //obstacle.transform.Rotate()
        }else if(System.Math.Abs(hand.PalmVelocity.z) > minActiveVelocity)
        {
            obstacle.transform.position =  (obstacle.transform.position + new Vector3(0.0f, 0.0f, hand.PalmVelocity.z * transSpeed));
        }

    }
    void onDestroy()
    {
        foreach(var item in points_list)
        {
            Destroy(item);
        }
        points_list.Clear();
        //Destroy(obj);
    }

    public void activeButRotate()
    {
        print("Active Rotate");
        isButRotate = true;
    }

    public void activeButExplode()
    {
        print("Active Explode!");
        isButExplode = true;
    }
    public void activeButPick()
    {
        print("Active Pick!");
        isButPick = true;
    }
    public void demoDelete()
    {
        transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }
}
