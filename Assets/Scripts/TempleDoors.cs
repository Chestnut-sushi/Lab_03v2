using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Opens and closes the temple doors when either leaf is selected (ray, poke, grab or hand).
public class TempleDoors : MonoBehaviour
{
    [SerializeField] Transform doorLeft;
    [SerializeField] Transform doorRight;
    [SerializeField] float openAngle = 95f;
    [SerializeField] float degreesPerSecond = 60f;

    bool isOpen;
    Quaternion leftClosed, rightClosed, leftOpen, rightOpen;
    XRSimpleInteractable[] interactables;

    void Awake()
    {
        leftClosed = doorLeft.localRotation;
        rightClosed = doorRight.localRotation;
        leftOpen = leftClosed * Quaternion.Euler(0f, openAngle, 0f);
        rightOpen = rightClosed * Quaternion.Euler(0f, -openAngle, 0f);

        interactables = GetComponentsInChildren<XRSimpleInteractable>();
        foreach (var i in interactables)
            i.selectEntered.AddListener(OnSelected);
    }

    void OnDestroy()
    {
        if (interactables == null) return;
        foreach (var i in interactables)
            if (i != null) i.selectEntered.RemoveListener(OnSelected);
    }

    void OnSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args) => Toggle();

    public bool IsOpen => isOpen;

    // True only for the two door leaves themselves (not for the rest of the temple, which shares this root).
    public bool IsLeaf(Transform t) =>
        t == doorLeft || t == doorRight || t.IsChildOf(doorLeft) || t.IsChildOf(doorRight);

    public void Toggle() => isOpen = !isOpen;

    void Update()
    {
        float step = degreesPerSecond * Time.deltaTime;
        doorLeft.localRotation = Quaternion.RotateTowards(doorLeft.localRotation, isOpen ? leftOpen : leftClosed, step);
        doorRight.localRotation = Quaternion.RotateTowards(doorRight.localRotation, isOpen ? rightOpen : rightClosed, step);
    }
}
