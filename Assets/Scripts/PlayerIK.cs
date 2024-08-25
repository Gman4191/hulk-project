using RootMotion.FinalIK;
using UnityEngine;

[RequireComponent(typeof(FullBodyBipedIK))]
[RequireComponent(typeof(PlayerController))]
public class PlayerIK : MonoBehaviour
{
    public float armRayDistance = 5.0f;
    public float yOffset = 2.0f;
    public float reachMultiplier = 2.0f;
    public float lookAtYOffset = 1.5f;
    public float lookAtWeightMultiplier = .9f;
    public float maxAngle = 90.0f;
    [Range(0.0f, 1.0f)]public float armIkStrength = .5f;
    [Range(0.0f, 1.0f)]public float reachArmIKWeight = .8f;
    public float reachArmSpeed = .01f;
    public float lookAtTurnSpeed = .1f;

    PlayerController playerController;
    FullBodyBipedIK ik;
    LookAtIK lookAtIK;
    RaycastHit rightArmHit;
    RaycastHit leftArmHit;

    IKEffector rightHand;
    IKEffector leftHand;
    
    bool isReachingWithRightArm;

    // Start is called before the first frame update
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        ik = GetComponent<FullBodyBipedIK>();
        lookAtIK = GetComponent<LookAtIK>();
        rightHand = ik.solver.effectors[6];
        leftHand = ik.solver.effectors[5];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(playerController.IsHanging)
        {

            Vector3 direction = transform.position - playerController.CameraT.position;
            
            float betweenAngle = Vector3.Angle(transform.forward, direction);

            // Get the direction from the player to the camera in local coordinates
            Vector3 localDirection = transform.InverseTransformDirection(playerController.CameraT.position - transform.position);

            isReachingWithRightArm = localDirection.x < 0;

            Vector3 lookAtTargetPosition = transform.position + lookAtYOffset * Vector3.up + playerController.CameraT.forward * reachMultiplier;
            float lookAtWeight = Mathf.Abs(betweenAngle) / 180.0f * lookAtWeightMultiplier;
            lookAtIK.solver.target.position = lookAtTargetPosition; 
            lookAtIK.solver.bodyWeight = lookAtWeight;
            lookAtIK.solver.headWeight = lookAtWeight;

            if(isReachingWithRightArm)
            {
                rightHand.positionWeight = lookAtWeight;
                rightHand.position = lookAtTargetPosition;
                leftHand.positionWeight = Mathf.Lerp(leftHand.positionWeight, reachArmIKWeight, reachArmSpeed);
                leftHand.position = leftHand.bone.transform.position;

            }
            else
            {
                leftHand.positionWeight = lookAtWeight;
                leftHand.position = lookAtTargetPosition;
                rightHand.positionWeight = Mathf.Lerp(rightHand.positionWeight, reachArmIKWeight, reachArmSpeed);
                rightHand.position = rightHand.bone.transform.position;
            }
        }
        else
        {
            lookAtIK.solver.bodyWeight = 0.0f;
            lookAtIK.solver.headWeight = 0.0f;
            // Reach out to close walls on the right side of the player
            if(Physics.Raycast(transform.position + Vector3.up * yOffset, transform.right, out rightArmHit, armRayDistance))
            {
                rightHand.position = rightArmHit.point;
                rightHand.positionWeight = Mathf.Lerp(rightHand.positionWeight, armIkStrength, .1f);

                Vector3 axis = Vector3.Cross(rightArmHit.normal, Vector3.right);

                rightHand.rotation = Quaternion.FromToRotation(axis, rightArmHit.normal);
                rightHand.rotationWeight = rightHand.positionWeight;
            }
            else
            {
                rightHand.positionWeight = Mathf.Lerp(rightHand.positionWeight, 0, .1f);
                rightHand.rotationWeight = rightHand.positionWeight;
            }

            // Reach out to walls on the left side of the player
            if(Physics.Raycast(transform.position + Vector3.up * yOffset, -transform.right, out leftArmHit, armRayDistance))
            {
                leftHand.position = leftArmHit.point;
                leftHand.positionWeight = Mathf.Lerp(leftHand.positionWeight, armIkStrength, .1f);

                // Vector3 axis = Vector3.Cross(leftArmHit.normal, Vector3.down);

                // leftHand.rotation = Quaternion.FromToRotation(axis, leftArmHit.normal);
                // leftHand.rotationWeight = leftHand.positionWeight;
            }
            else
            {
                leftHand.positionWeight = Mathf.Lerp(leftHand.positionWeight, 0, .1f);
                //leftHand.rotationWeight = leftHand.positionWeight;
            }
        }
    }

    // private void OnDrawGizmos() {
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawRay(transform.position + Vector3.up * yOffset, transform.right * armRayDistance);
    //     Gizmos.DrawRay(transform.position + Vector3.up * yOffset, -transform.right * armRayDistance);
    //     Gizmos.DrawCube(rightArmHit.point, Vector3.one * .1f);
    // }
}
