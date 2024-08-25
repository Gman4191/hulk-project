using UnityEngine;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour {

	[SerializeField] private PlayerController playerController;
	public bool lockCursor;
	public float mouseSensitivity = 10;
	public Transform target;
	public float dstFromTarget = 2;
	public float maxDstFromTarget = 2;
	public float fovSmoothAmount = .1f;
	public Vector2 pitchMinMax = new Vector2 (-40, 85);

	public float rotationSmoothTime = .12f;
	Vector3 rotationSmoothVelocity;
	Vector3 currentRotation;

	private float currentFOVOffset = 0.0f;
	

	[SerializeField]
	LayerMask environmentLayer;

	float yaw;
	float pitch;

	private Camera mainCamera;

	void Start() {
		if (lockCursor) {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		mainCamera = GetComponent<Camera>();
	}

	void LateUpdate () {
		yaw += Input.GetAxis ("Mouse X") * mouseSensitivity;
		pitch -= Input.GetAxis ("Mouse Y") * mouseSensitivity;
		pitch = Mathf.Clamp (pitch, pitchMinMax.x, pitchMinMax.y);

		currentRotation = Vector3.SmoothDamp (currentRotation, new Vector3 (pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
		transform.eulerAngles = currentRotation;

		float positionOffset = dstFromTarget + HandleFOV();
		transform.position = target.position - transform.forward * positionOffset;

		
		CutoutNearCamera();

	}

	private float HandleFOV()
	{
		float currentSpeed = Mathf.Min(playerController.Rigidbody.velocity.magnitude, playerController.MaxSpeed);
		currentFOVOffset = Mathf.Lerp(currentFOVOffset, currentSpeed / playerController.MaxSpeed, fovSmoothAmount);
		return currentFOVOffset * maxDstFromTarget;
	}

	void CutoutNearCamera()
	{
		Vector3 cutoutPos = mainCamera.WorldToViewportPoint(target.position);
		cutoutPos.y /= Screen.width / Screen.height;

		Vector3 offset = target.position - transform.position;
		RaycastHit[] raycastHits = Physics.RaycastAll(transform.position, offset, offset.magnitude, environmentLayer);

		for(int i = 0; i < raycastHits.Length; i++)
		{
			Material[] materials = raycastHits[i].transform.GetComponent<Renderer>().materials;
			foreach(Material mat in materials)
			{
				mat.SetVector("_CutoutPosition", cutoutPos);
				mat.SetFloat("_CutoutFalloff", 0.2f);
				mat.SetFloat("_CutoutSize", .5f);
			}

		}
	}

}
