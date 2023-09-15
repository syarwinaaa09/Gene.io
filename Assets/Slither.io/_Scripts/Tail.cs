using UnityEngine;

/// <summary>
/// Follows a target transform with a delay. Meant to mimic a snake movement. Is not a Networked Object.
/// </summary>
public class Tail : MonoBehaviour
{
    // Which networked owner these tails belong to
    public Transform networkedOwner;
    // The Transform object that this tail is following
    public Transform followTransform;

    [SerializeField, Tooltip("Represents the time delay between the GameObject and it's target")] private float delayTime = 0.1f;
    [SerializeField, Tooltip("The distance the GameObject should keep from it's target")] private float distance = 0.3f;
    [SerializeField, Tooltip("Movement lerp speed")] private float moveStep = 10f;

    private Vector3 _targetPosition;

    /// <summary>
    /// Move the tail towards the target with a delay.
    /// </summary>
    private void Update()
    {
        _targetPosition = followTransform.position - followTransform.forward * distance;
        _targetPosition += (transform.position - _targetPosition) * delayTime;
        _targetPosition.z = 0f;

        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * moveStep);
    }
}
