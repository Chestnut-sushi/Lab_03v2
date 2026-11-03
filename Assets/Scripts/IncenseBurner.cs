using UnityEngine;

// Put on a trigger volume placed over the incense burner.
// A Grabbable put in the burner (released inside the volume, or handed over by the player
// while looking at the burner) is consumed, the smoke starts and the candles light up.
[RequireComponent(typeof(Collider))]
public class IncenseBurner : MonoBehaviour
{
    [SerializeField] ParticleSystem smoke;
    [SerializeField] CandleManager candles;
    [SerializeField] SoundtrackPlayer soundtrack;
    [SerializeField] SoundtrackPlayer narration;

    public bool IsBurning => smoke != null && smoke.isPlaying;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerStay(Collider other)
    {
        var piece = other.GetComponentInParent<Grabbable>();
        if (piece == null || piece.IsHeld) return;
        Accept(piece);
    }

    public void Accept(Grabbable piece)
    {
        Destroy(piece.gameObject);
        if (smoke != null && !smoke.isPlaying) smoke.Play();
        if (candles != null) candles.IgniteAll();
        if (soundtrack != null) soundtrack.Play();
        if (narration != null) narration.Play();
    }
}
