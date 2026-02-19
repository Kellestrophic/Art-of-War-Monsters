using System;
using UnityEngine;

public class parallaxEffect : MonoBehaviour
{
    public Camera cam;
    public Transform followTarget;

    Vector2 startingPosition;

    float startingZ;

    Vector2 camMoveSinceStart => (Vector2)cam.transform.position - startingPosition;

    float zDistanceFromTarget => transform.position.z - followTarget.transform.position.z;

    float clippingPlane => (cam.transform.position.z + (zDistanceFromTarget > 0 ? cam.farClipPlane : cam.nearClipPlane));
    float parallaxFactor => Mathf.Abs(zDistanceFromTarget) / clippingPlane;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startingPosition = transform.position;
        startingZ = transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
         Vector2 camMovement = camMoveSinceStart;
    
    // Only apply X parallax
    float newX = startingPosition.x + camMovement.x * parallaxFactor;

    // Keep Y position fixed (no vertical parallax)
    transform.position = new Vector3(newX, startingPosition.y, startingZ);
}
    }

