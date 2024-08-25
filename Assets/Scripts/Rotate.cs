using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.forward;
    public float rotateSpeed = 1;

    void FixedUpdate()
    {
        transform.Rotate(rotationAxis * rotateSpeed);
    }
}
