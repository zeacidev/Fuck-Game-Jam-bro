using UnityEngine;

/// Attach to the player (or camera). Press the throw key to launch a molotov.
public class MolotovThrower : MonoBehaviour
{
    [Header("References")]
    public Molotov molotovPrefab;
    public Transform throwPoint;          // empty child near the hand/camera

    [Header("Throw")]
    public KeyCode throwKey = KeyCode.G;
    public float throwForce = 14f;
    public float upwardBias = 0.25f;      // adds a bit of arc
    public float cooldown = 1.0f;

    float nextThrowTime;

    void Update()
    {
        if (Input.GetKeyDown(throwKey) && Time.time >= nextThrowTime)
        {
            Throw();
            nextThrowTime = Time.time + cooldown;
        }
    }

    void Throw()
    {
        Vector3 dir = (throwPoint.forward + Vector3.up * upwardBias).normalized;
        Molotov m = Instantiate(molotovPrefab, throwPoint.position, throwPoint.rotation);
        m.Launch(dir * throwForce);
    }
}
