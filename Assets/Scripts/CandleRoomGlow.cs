using System.Collections.Generic;
using UnityEngine;

// The scene looks exactly as authored until the CandleManager starts lighting the candles. From then on the
// inside of the temple warms up and brightens (smoothly): the base colour of the renderers below the given
// roots is multiplied by a warm tint, through MaterialPropertyBlocks (the shared materials are untouched).
// No real-time light is needed, so it also works with the render pipeline's additional lights switched off.
public class CandleRoomGlow : MonoBehaviour
{
    [SerializeField] CandleManager candles;
    [SerializeField] Transform[] interiorRoots;                          // only seen from inside: full boost
    [SerializeField] Transform[] sharedRoots;                            // seen from both sides (temple walls): milder boost
    [SerializeField] Color interiorTint = new Color(1.9f, 1.55f, 1.15f);
    [SerializeField] Color sharedTint = new Color(1.35f, 1.18f, 1.0f);
    [SerializeField] float riseSeconds = 4f;

    struct Surface { public Renderer renderer; public int index; public Color color; public Color lit; }

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    readonly List<Surface> surfaces = new List<Surface>();
    MaterialPropertyBlock block;
    float progress;
    bool applied;

    void Awake()
    {
        if (candles == null) candles = GetComponent<CandleManager>();
        block = new MaterialPropertyBlock();
        Collect(interiorRoots, interiorTint);
        Collect(sharedRoots, sharedTint);
    }

    void Collect(Transform[] roots, Color tint)
    {
        if (roots == null) return;
        foreach (var root in roots)
        {
            if (root == null) continue;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r.GetComponentInParent<CandleManager>() != null) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].HasProperty(BaseColor)) continue;
                    var c = mats[i].GetColor(BaseColor);
                    surfaces.Add(new Surface { renderer = r, index = i, color = c, lit = new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a) });
                }
            }
        }
    }

    void Update()
    {
        if (candles == null || !candles.Started || progress >= 1f) return;
        progress = Mathf.Min(1f, progress + Time.deltaTime / Mathf.Max(0.01f, riseSeconds));
        float t = Mathf.SmoothStep(0f, 1f, progress);
        foreach (var s in surfaces)
        {
            if (s.renderer == null) continue;
            s.renderer.GetPropertyBlock(block, s.index);
            block.SetColor(BaseColor, Color.Lerp(s.color, s.lit, t));
            s.renderer.SetPropertyBlock(block, s.index);
        }
        applied = true;
    }

    void OnDestroy()
    {
        if (!applied) return;
        foreach (var s in surfaces)
            if (s.renderer != null) s.renderer.SetPropertyBlock(null, s.index);
    }
}
