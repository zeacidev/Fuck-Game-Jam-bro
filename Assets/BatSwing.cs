using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the Bat object. Left mouse button swings the bat.
/// The swing is a small code-driven animation (wind up, sweep across, recover) pivoting at the handle.
/// The hit check happens mid-swing, when the bat passes in front of you.
/// It only works while the bat is equipped, because disabling the Bat GameObject also disables this script.
/// Hit objects with a Rigidbody get knocked away, and anything with an OnBatHit() method
/// (on the object or its parents) gets that method called.
/// </summary>
public class BatSwing : MonoBehaviour
{
    [Header("Aim")]
    [Tooltip("Where the swing is aimed from. Falls back to Camera.main.")]
    [SerializeField] private Transform aimOrigin;

    [Header("Swing")]
    [Tooltip("How far in front of the camera the bat reaches.")]
    [SerializeField] private float range = 2.5f;
    [Tooltip("Thickness of the swing area.")]
    [SerializeField] private float radius = 0.6f;
    [Tooltip("Extra seconds after a swing ends before you can swing again.")]
    [SerializeField] private float cooldown = 0.1f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Animation")]
    [Tooltip("Handle end of the bat, in the bat's own local units. The bat swings around this point.")]
    [SerializeField] private Vector3 pivotLocal = new Vector3(0f, -4.5f, 0f);
    [SerializeField] private float windupTime = 0.12f;
    [SerializeField] private float swingTime = 0.12f;
    [SerializeField] private float recoverTime = 0.2f;
    [Tooltip("How far the bat is pulled back (degrees) before the swing.")]
    [SerializeField] private float windupYaw = 35f;
    [Tooltip("Where the sweep ends (degrees). Negative = across to the left.")]
    [SerializeField] private float followThroughYaw = -150f;
    [Tooltip("How far the bat leans out to the side while swinging (degrees).")]
    [SerializeField] private float tilt = -80f;

    [Header("Hit effect")]
    [Tooltip("Impulse applied to hit Rigidbodies.")]
    [SerializeField] private float hitForce = 12f;
    [SerializeField] private bool logHits = true;

    [Header("Events")]
    public UnityEvent onSwing;
    public UnityEvent<Collider> onHit;

    private Vector3 restLocalPos;
    private Quaternion restLocalRot;
    private bool swinging;
    private float nextSwingTime;

    private void Awake()
    {
        restLocalPos = transform.localPosition;
        restLocalRot = transform.localRotation;

        if (aimOrigin == null && Camera.main != null)
            aimOrigin = Camera.main.transform;
    }

    private void OnDisable()
    {
        // Unequipped mid-swing: snap back to the resting pose
        swinging = false;
        ApplyPose(Quaternion.identity);
    }

    private void Update()
    {
        // Ignore the click that just re-captured the cursor
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (swinging || Time.time < nextSwingTime) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            StartCoroutine(SwingRoutine());
    }

    private IEnumerator SwingRoutine()
    {
        swinging = true;
        onSwing?.Invoke();

        // 1. Wind up: pull back and lean the bat out to the side (fast start, soft stop)
        Quaternion windup = Quaternion.Euler(0f, windupYaw, tilt);
        for (float t = 0f; t < 1f; t += Time.deltaTime / windupTime)
        {
            float e = 1f - (1f - t) * (1f - t);
            ApplyPose(Quaternion.Slerp(Quaternion.identity, windup, e));
            yield return null;
        }
        ApplyPose(windup);

        // 2. Swing: sweep across in front of the player (accelerates), hit check at the halfway point
        bool hitDone = false;
        for (float t = 0f; t < 1f; t += Time.deltaTime / swingTime)
        {
            float e = t * t;
            float yaw = Mathf.Lerp(windupYaw, followThroughYaw, e);
            ApplyPose(Quaternion.Euler(0f, yaw, tilt));

            if (!hitDone && t >= 0.5f)
            {
                hitDone = true;
                DoHitCheck();
            }
            yield return null;
        }
        Quaternion end = Quaternion.Euler(0f, followThroughYaw, tilt);
        ApplyPose(end);
        if (!hitDone) DoHitCheck();

        // 3. Recover back to the resting pose
        for (float t = 0f; t < 1f; t += Time.deltaTime / recoverTime)
        {
            ApplyPose(Quaternion.Slerp(end, Quaternion.identity, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }
        ApplyPose(Quaternion.identity);

        swinging = false;
        nextSwingTime = Time.time + cooldown;
    }

    // Rotates the bat by q (in the parent's space) around the handle, relative to its resting pose
    private void ApplyPose(Quaternion q)
    {
        Vector3 pivot = restLocalPos + restLocalRot * Vector3.Scale(pivotLocal, transform.localScale);
        transform.localRotation = q * restLocalRot;
        transform.localPosition = pivot + q * (restLocalPos - pivot);
    }

    private void DoHitCheck()
    {
        if (aimOrigin == null) return;

        Vector3 start = aimOrigin.position;
        Vector3 dir = aimOrigin.forward;
        Vector3 end = start + dir * range;

        Collider[] hits = Physics.OverlapCapsule(start, end, radius, hitMask, QueryTriggerInteraction.Ignore);
        Transform myRoot = transform.root;

        foreach (var col in hits)
        {
            if (col.transform.root == myRoot) continue; // don't hit ourselves

            if (col.attachedRigidbody != null)
                col.attachedRigidbody.AddForceAtPosition(dir * hitForce, col.ClosestPoint(start), ForceMode.Impulse);

            col.SendMessageUpwards("OnBatHit", SendMessageOptions.DontRequireReceiver);
            onHit?.Invoke(col);

            if (logHits) Debug.Log($"Bat hit {col.name}", col);

            if (col.CompareTag("JuleDekoration"))
            {
                Destroy(col.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = aimOrigin != null ? aimOrigin : (Camera.main != null ? Camera.main.transform : null);
        if (t == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(t.position, radius);
        Gizmos.DrawWireSphere(t.position + t.forward * range, radius);
        Gizmos.DrawLine(t.position, t.position + t.forward * range);
    }
}
