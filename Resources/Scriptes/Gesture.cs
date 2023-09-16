using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
public class Gesture : MonoBehaviour {
    //public static Gesture _instance; //静态类
    const float deltaVelocity = 0.02f;
    const float deltaCloseFinger = 0.06f;
    public static bool isMoveRight(Hand hand)// 手划向右边
    {
        return hand.PalmVelocity.x > deltaVelocity;
    }

    public static bool isCloseHand(Hand hand)//是否握拳
    {
        int count = 0;
        for(int i = 0; i < hand.Fingers.Count; ++i)
        {
            Finger finger = hand.Fingers[i];
            if ((finger.TipPosition - hand.PalmPosition).Magnitude < deltaCloseFinger)
                count++;
        }


        return (count == 5);
    }

    public static bool isOpenFullHand(Hand hand)
    {
        return hand.GrabStrength == 0;
    }
    // Use this for initialization
    //   void Start () {

    //}

    //// Update is called once per frame
    //void Update () {

    //}
}
