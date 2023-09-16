using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using Leap;
using Leap.Unity;



public class demo2 : MonoBehaviour {
    private LeapProvider provider;
    private Frame frame;

    private float smallestVelocity = 1.45f;
    public float deltaVelocity = 0.2f;
    public float rotateAngle = 5.0f; //旋转的速度
    // Use this for initialization
    void Start () {
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;
	}
	
	// Update is called once per frame
	void Update () {
        frame = provider.CurrentFrame;
        foreach (Hand hand in frame.Hands)
        {
            if (hand.IsLeft)
            {
                //print(hand.PalmVelocity.Magnitude);
                print(hand.PalmVelocity);
                if (isMoveLeft(hand))
                {
                    //print("LEFT:              " + hand.PalmVelocity);
                    transform.Rotate(new Vector3(0.0f, 1.0f, 0.0f), rotateAngle);
                }
                else if (isMoveRight(hand))
                {
                    transform.Rotate(Vector3.up, -rotateAngle);
                }
                else if (isMoveUp(hand))
                {
                    transform.Rotate(Vector3.right, rotateAngle);
                }
                else if (isMoveDown(hand))
                {
                    transform.Rotate(Vector3.right, -rotateAngle);
                }
                //transform.position = hand.PalmPosition.ToVector3() + hand.PalmNormal.ToVector3() * (transform.localScale.y * .5f + .02f);
                //transform.rotation = hand.Basis.CalculateRotation();

            }
        }

    }

    //向左挥手，左是负值，挥动的幅度和value有关，可以>1
    public bool isMoveLeft(Hand hand)
    {
        return hand.PalmVelocity.x < -deltaVelocity;
    }
    public bool isMoveRight(Hand hand)
    {
        return hand.PalmVelocity.x > deltaVelocity;
    }
    public bool isMoveUp(Hand hand)
    {
        return hand.PalmVelocity.y > deltaVelocity;
    }
    public bool isMoveDown(Hand hand)
    {
        return hand.PalmVelocity.y < -deltaVelocity;
    }
    //表示手是否固定不动
    public bool isStationary(Hand hand)
    {
        return hand.PalmVelocity.Magnitude < smallestVelocity;
    }
}
