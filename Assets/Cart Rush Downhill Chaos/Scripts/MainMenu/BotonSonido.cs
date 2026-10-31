using UnityEngine;
using UnityEngine.EventSystems;

public class BotonSonido : MonoBehaviour
{
    public AudioSource reproductor;
    public AudioClip sonidoHover;
    public AudioClip sonidoClick;

    public void ReproducirClick()
    {
        if (sonidoClick != null && reproductor != null)
        {
            reproductor.PlayOneShot(sonidoClick);
        }
    }

    public void ReproducirHover()
    {
        if (sonidoHover != null && reproductor != null)
        {
            reproductor.PlayOneShot(sonidoHover);
        }
    }
}
