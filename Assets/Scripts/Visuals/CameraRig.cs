using UnityEngine;

/// <summary>Keeps the camera's resting position and adds a short shake on request.</summary>
public class CameraRig : MonoBehaviour
{
    public Vector3 BasePosition;

    private float shakeStrength;
    private float shakeTime;
    private float shakeDuration;

    public void Shake(float strength, float duration)
    {
        shakeStrength = strength;
        shakeDuration = duration;
        shakeTime = duration;
    }

    private void LateUpdate()
    {
        Vector3 offset = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            float fade = shakeTime / shakeDuration;
            offset = (Vector3)(Random.insideUnitCircle * shakeStrength * fade);
        }
        transform.position = BasePosition + offset;
    }
}
