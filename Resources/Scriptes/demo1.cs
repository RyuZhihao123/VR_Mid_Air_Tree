using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;


public class demo1 : MonoBehaviour {
    LeapProvider provider;

	// Use this for initialization
	void Start () {
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;
    }
	
	// Update is called once per frame
	void Update () {
        Frame frame = provider.CurrentFrame; 
        foreach (Hand hand in frame.Hands) {
            if (hand.IsRight)
            {
                if (hand.GrabStrength > 0.8f) {
                    print("grab");
                  transform.position = hand.PalmPosition.ToVector3() + hand.PalmNormal.ToVector3() * (transform.localScale.y * .5f + .02f);
                  transform.rotation = hand.Basis.CalculateRotation();
                  
                }else if(hand.GrabStrength < 0.3f && hand.GrabStrength > 0.05f)
                {

                }

            }
        }
        

    }
}
