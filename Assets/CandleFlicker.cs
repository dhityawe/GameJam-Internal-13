using UnityEngine;
using UnityEngine.Rendering.Universal; // <-- Penting untuk Light 2D

public class CandleFlicker : MonoBehaviour
{
    private Light2D candleLight;
    
    [Tooltip("Seberapa kuat kedipannya")]
    public float intensityFlicker = 0.2f;
    
    [Tooltip("Seberapa jauh jangkauan kedipannya")]
    public float radiusFlicker = 0.5f;

    [Tooltip("Seberapa cepat kedipannya")]
    public float flickerSpeed = 0.1f;

    private float baseIntensity;
    private float baseOuterRadius;
    private float timer;

    void Start()
    {
        candleLight = GetComponent<Light2D>();
        baseIntensity = candleLight.intensity;
        baseOuterRadius = candleLight.pointLightOuterRadius; // Gunakan ini untuk Spot Light 2D
        timer = Random.Range(0, flickerSpeed); // Acak timer awal
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Hanya update kedipan setiap interval flickerSpeed
        if (timer > flickerSpeed)
        {
            // Buat nilai acak baru
            float randomIntensity = Random.Range(
                baseIntensity - intensityFlicker, 
                baseIntensity + intensityFlicker
            );

            float randomRadius = Random.Range(
                baseOuterRadius - radiusFlicker, 
                baseOuterRadius + radiusFlicker
            );

            // Terapkan ke lampu
            candleLight.intensity = randomIntensity;
            candleLight.pointLightOuterRadius = randomRadius;

            // Reset timer
            timer = 0f;
        }
    }
}