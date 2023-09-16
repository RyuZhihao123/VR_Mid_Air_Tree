using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity.Interaction;

//[RequireComponent(typeof(InteractionBehaviour))] //自动添加游戏部件，避免组装错误
public class delpoint : MonoBehaviour {

    // Use this for initialization
    private InteractionBehaviour _intObj;
    void Start()
    {
        _intObj = GetComponent<InteractionBehaviour>();
    }
    private void OnEnable()
    {
        _intObj = GetComponent<InteractionBehaviour>();

        /*
         * 这里有一个注意，如果是自定义的 custom constraint，一般使用OnPostPhysicalUpdate, 因为
         * 这个event是在FixedUpdate之后调用的，在其中更新interaction controller 和interaction objects
         */
        _intObj.OnGraspedMovement -= onGraspedMovement; // Prevent double-subscription.
        _intObj.OnGraspedMovement += onGraspedMovement;
        //_intObj.manager.OnPostPhysicalUpdate -= applyConstraint;
        //_intObj.manager.OnPostPhysicalUpdate += applyConstraint;
    }
    private void OnDisable()
    {
        _intObj.OnGraspedMovement -= onGraspedMovement;
        //_intObj.manager.OnPostPhysicalUpdate -= applyConstraint;
    }

    private void onGraspedMovement(Vector3 presolvedPos, Quaternion presolvedRot,
                                   Vector3 solvedPos, Quaternion solvedRot,
                                   List<InteractionController> graspingControllers)
    {
        // Project the vector of the motion of the object due to grasping along the world X axis.
        Vector3 movementDueToGrasp = solvedPos - presolvedPos;
        //float xAxisMovement = movementDueToGrasp.x;

        //print("onGraspedMovement");
        // Move the object back to its position before the grasp solve this frame,
        // then add just its movement along the world X axis.
        _intObj.rigidbody.position = presolvedPos;
        //_intObj.rigidbody.position += Vector3.right * xAxisMovement;
        _intObj.rigidbody.position += movementDueToGrasp;

        //_intObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    private void applyConstraint()
    {
        // This constraint forces the interaction object to have a positive X coordinate.
        print("applyCon");
        _intObj.rigidbody.position = new Vector3(0.0f, 0.0f, 0.0f);
    }
    // Update is called once per frame
    public void deleteDemo()
    {
        print("delete");
        transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }
}
