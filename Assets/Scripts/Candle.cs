using System.Collections;
using UnityEngine;

// A small candle that starts unlit. Ignite() turns its flame on (and its light, if it has one) after a delay,
// then the flame flickers.
public class Candle : MonoBehaviour
{
    [SerializeField] Transform flame;
    [SerializeField] Light glow;                 // optional: only some candles carry a real light
    [SerializeField] float flameGrowTime = 0.6f;
    [SerializeField] float lightFadeTime = 1.5f;
    [SerializeField] float flickerSpeed = 7f;

    float lightIntensity;
    float seed;
    Vector3 flameScale = Vector3.one;
    bool lit;

    public bool IsLit => lit;

    void Awake()
    {
        seed = Random.value * 100f;
        if (flame != null)
        {
            flameScale = flame.localScale;
            flame.gameObject.SetActive(false);
        }
        if (glow != null)
        {
            lightIntensity = glow.intensity;
            glow.enabled = false;
        }
    }

    public void Ignite(float delay)
    {
        if (!lit) StartCoroutine(IgniteRoutine(delay));
    }

    IEnumerator IgniteRoutine(float delay)
    {
        lit = true;
        yield return new WaitForSeconds(delay);

        if (flame != null) flame.gameObject.SetActive(true);
        if (glow != null) { glow.intensity = 0f; glow.enabled = true; }

        for (float t = 0f; t < Mathf.Max(flameGrowTime, lightFadeTime); t += Time.deltaTime)
        {
            float f = flameGrowTime > 0f ? Mathf.Clamp01(t / flameGrowTime) : 1f;
            float l = lightFadeTime > 0f ? Mathf.Clamp01(t / lightFadeTime) : 1f;
            if (flame != null) flame.localScale = flameScale * f;
            if (glow != null) glow.intensity = lightIntensity * Flicker() * l;
            yield return null;
        }
        if (flame != null) flame.localScale = flameScale;
    }

    float Flicker() => 0.85f + 0.3f * Mathf.PerlinNoise(seed, Time.time * flickerSpeed);

    void Update()
    {
        if (!lit) return;
        if (glow != null && glow.enabled) glow.intensity = lightIntensity * Flicker();
        if (flame != null && flame.gameObject.activeSelf)
            flame.localScale = new Vector3(flameScale.x, flameScale.y, flameScale.z * (0.9f + 0.2f * Mathf.PerlinNoise(seed + 40f, Time.time * flickerSpeed)));
    }
}
