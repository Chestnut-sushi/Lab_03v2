using System.Collections;
using UnityEngine;

// Marks an object the desktop player can pick up and put down. Needs a Rigidbody and a collider.
// With startKinematic the object stays exactly where it was placed until it is picked up.
[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    [SerializeField] bool startKinematic;
    [SerializeField] float collisionRestoreDelay = 0.75f;

    Rigidbody body;
    Collider[] colliders;

    public bool IsHeld { get; private set; }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        if (startKinematic) body.isKinematic = true;
    }

    public void OnGrab(Collider playerCollider)
    {
        IsHeld = true;
        body.isKinematic = true;
        foreach (var c in colliders)
            Physics.IgnoreCollision(c, playerCollider, true);
    }

    public void OnRelease(Collider playerCollider)
    {
        IsHeld = false;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        StartCoroutine(RestoreCollision(playerCollider));
    }

    IEnumerator RestoreCollision(Collider playerCollider)
    {
        yield return new WaitForSeconds(collisionRestoreDelay);
        foreach (var c in colliders)
            if (c != null) Physics.IgnoreCollision(c, playerCollider, false);
    }

    public Bounds GetBounds()
    {
        var b = new Bounds(transform.position, Vector3.zero);
        foreach (var c in colliders) b.Encapsulate(c.bounds);
        return b;
    }
}
