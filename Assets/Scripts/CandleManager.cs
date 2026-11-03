using UnityEngine;

// Lights every Candle below this object, spreading outwards from the origin point (the incense burner).
public class CandleManager : MonoBehaviour
{
    [SerializeField] Transform origin;
    [SerializeField] float secondsPerMeter = 0.45f;
    [SerializeField] float jitter = 0.4f;

    bool started;

    public bool Started => started;

    public void IgniteAll()
    {
        if (started) return;
        started = true;

        Vector3 from = origin != null ? origin.position : transform.position;
        foreach (var candle in GetComponentsInChildren<Candle>(true))
        {
            float distance = Vector3.Distance(from, candle.transform.position);
            candle.Ignite(distance * secondsPerMeter + Random.Range(0f, jitter));
        }
    }
}
