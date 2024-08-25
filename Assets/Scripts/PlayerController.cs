using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {

	[Header("Movement Parameters")]
	public float walkSpeed = 6;
	public float runSpeed = 12;
	public float maxSpeed = 20;
	public float groundDrag = 5;
	public float airDrag = 1.75f;
	public float jumpHeight = 1;
	public float maxSlopeAngle = 60;
	public float airMultiplier = 0.3f;
	[Range(0,1)]
	public float airControlPercent;
	public float wallReachRange = 2.0f;
	public float jumpOfWallCooldown = 1.0f;
	public float turnSpeed = 0.1f;

	public float turnSmoothTime = 0.2f;
	private bool grounded;
	public float speedSmoothTime = 0.1f;

	bool isLanding = false;
	bool isJumpingOffWall = false;
	bool isHanging;
	float moveSpeed = 0;
	RaycastHit slopeHit;
	RaycastHit wallHitInfo;
	Animator animator;
	Transform cameraT;
	Rigidbody rb;
	float deltaTime;
	Vector3 spawnPosition;

	public bool IsHanging{get{return isHanging;}}
	public RaycastHit WallHitInfo{get{return wallHitInfo;}}
	public Transform CameraT{get{return cameraT;}}
	public Rigidbody Rigidbody{get{return rb;}}
	public float MaxSpeed{get{return maxSpeed;}}
	
	void Start ()
	{
		animator = GetComponent<Animator> ();
		cameraT = Camera.main.transform;
		rb = GetComponent<Rigidbody> ();
		spawnPosition = transform.position;
	}

	void Update ()
	{
		deltaTime = Time.deltaTime;
		Vector3 input = new Vector3 (Input.GetAxisRaw ("Horizontal"), 0.0f, Input.GetAxisRaw ("Vertical"));
		Vector3 inputDir = input.normalized;
		
		bool running = Input.GetKey (KeyCode.LeftShift);
		grounded = IsGrounded();
		isHanging = running && !grounded && CanReachWall() && !isJumpingOffWall;
		
		if(isHanging)
		{
			rb.useGravity = false;
			if (Input.GetKeyDown (KeyCode.Space)) 
			{
				isJumpingOffWall = true;
				JumpOffWall();
				Invoke("ResetJumpOffWall", jumpOfWallCooldown);
			}
		}
		else
		{
			rb.useGravity = true;
			Move (inputDir, running);

			if (Input.GetKeyDown (KeyCode.Space) && !isLanding) 
			{
				Jump ();
			}
		}

		// Increase or Decrease drag based on whether the player is in air or not
		if(grounded && !isLanding || isHanging)
		{
			rb.drag = groundDrag;
		}
		else
		{
			rb.drag = airDrag;
		}

		// Rotate the player towards the camera look direction when not landing
		if(!isLanding && !isHanging)
		{
			RotatePlayerToCamera();
		}
		else if(isLanding)
		{
			RotatePlayerToVelocity();
		}	

		// Set animator parameters
		if(animator)
		{
			int runningMultiplier = running ? 2 : 1;
			moveSpeed = Mathf.Lerp(moveSpeed, inputDir.magnitude != 0.0f ? 1.0f : 0.0f, 0.1f);
			animator.SetFloat ("MoveSpeed", moveSpeed * runningMultiplier);
			animator.SetFloat ("Horizontal",  input.x * runningMultiplier, 0.1f, Time.deltaTime);
			animator.SetFloat ("Vertical",  input.z * runningMultiplier,  0.1f, Time.deltaTime);
			animator.SetBool ("IsFalling", !grounded);
			animator.SetBool ("CanHang", isHanging);
		}

		// Safety Net
		if(transform.position.y < -215.0f)
		{
			rb.velocity = Vector3.zero;
			transform.position = spawnPosition;
		}

		// Quit Game
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}

	}

	void Move(Vector3 inputDir, bool running) 
	{
		float moveSpeed = running ? runSpeed : walkSpeed;
		Vector3 moveVector = transform.TransformDirection(inputDir) * moveSpeed * deltaTime;

		if(grounded && !isLanding)
			rb.AddForce(new Vector3(moveVector.x, rb.velocity.y, moveVector.z), ForceMode.Force);
		else if(!isLanding)
			rb.AddForce(new Vector3(moveVector.x, rb.velocity.y, moveVector.z) * airMultiplier, ForceMode.Force);

		rb.useGravity = !OnSlope();

		SpeedControl();
	}

	void RotatePlayerToCamera()
	{
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, cameraT.rotation.eulerAngles.y, 0), turnSpeed * deltaTime);
	}

	void RotatePlayerToVelocity()
	{
		Vector3 flatVelocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z); 
		if(flatVelocity != Vector3.zero)
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatVelocity), turnSpeed * deltaTime);
	}

	void Jump() {
		if (grounded) {
			float jumpVelocity = Mathf.Sqrt (2 * jumpHeight);
			rb.AddForce(Vector3.up * jumpVelocity, ForceMode.Impulse);

			if(animator)
				animator.SetTrigger("Jump");
		}
	}

	void JumpOffWall()
	{
		float jumpVelocity = Mathf.Sqrt(2 * jumpHeight);
		rb.AddForce(jumpVelocity * Vector3.up + cameraT.forward.normalized * jumpVelocity, ForceMode.Impulse);
	}

	bool IsGrounded()
	{
		return Physics.Raycast(transform.position, -Vector3.up, out slopeHit, 0.2f);
	}

	bool OnSlope()
	{
		if(!grounded) return false;

		float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
		return angle > maxSlopeAngle;
	}

	void SpeedControl()
	{
		Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

		if(flatVelocity.magnitude > maxSpeed)
		{
			Vector3 limitedVelocity = flatVelocity.normalized * maxSpeed;
			rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
		}
	}

	bool CanReachWall()
	{
		return Physics.Raycast(transform.position + Vector3.up * 2.0f, transform.forward, out wallHitInfo, wallReachRange);
	}

	void IsLanding()
	{
		isLanding = true;
	}
	void HasLanded()
	{
		isLanding = false;
	}

	void ResetJumpOffWall()
	{
		isJumpingOffWall = false;
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * wallReachRange);
	}
}
