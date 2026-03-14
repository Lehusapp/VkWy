using UnityEngine;

public class UIPulse : MonoBehaviour
{
    public float pulseSpeed = 4f;
    public float maxScale = 1.2f;
    public float minScale = 0.8f;

    void Update()
    {
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
