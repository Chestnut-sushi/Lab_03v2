using UnityEngine;

// Put on something the desktop player can read (the hanging scroll). Looking at it from close up shows a prompt,
// a click opens the image full screen in front of the player (see DesktopFirstPersonController).
// Needs a collider on this object or below it.
public class ScrollReader : MonoBehaviour
{
    [SerializeField] Texture2D modalImage;

    public Texture2D ModalImage => modalImage;
}
