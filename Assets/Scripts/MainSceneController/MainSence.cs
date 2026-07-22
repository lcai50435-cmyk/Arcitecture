using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main menu scene effect
/// </summary>
public class MainSence : MonoBehaviour
{
    public float offsetMultipliter = 1f; // Offset multiplier
    public float smoothTime = 0.3f; // Smoothing time

    private Vector3 startPosition; // Start position
    private Vector3 velocity; // Velocity

    void Start()
    {
        startPosition = transform.position; // Get the transform position of the object this script is attached to
    }

    void Update()
    {
        //Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);// Convert from screen space to viewport space
        ////
        //transform.position = Vector3.SmoothDamp(transform.position, startPosition + (offset * offsetMultipliter) ,ref velocity , smoothTime);
       
        Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        Vector3 targetPosition = startPosition + (Vector3)(offset * offsetMultipliter);
        targetPosition.z = transform.position.z;   // Keep the original Z value

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

    }
}
