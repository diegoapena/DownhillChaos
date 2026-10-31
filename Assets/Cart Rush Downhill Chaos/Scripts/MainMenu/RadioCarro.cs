using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Si deseas mostrar el nombre de la canción en la UI

// Esta clase agrupa la información de cada emisora y será visible en el Inspector
[System.Serializable]
public class EmisoraRadio
{
    public string nombreEmisora;
    public AudioClip cancion;
}

public class RadioCarro : MonoBehaviour
{
    [Header("Configuración")]
    public AudioSource reproductorRadio;
    public TextMeshProUGUI textoUIEmisora; // Opcional

    [Header("Base de Datos de Emisoras")]
    public List<EmisoraRadio> emisoras = new List<EmisoraRadio>();
    private int indiceActual = 0;

    [Header("Input")]
    public InputActionReference accionScroll; // Referencia al evento del ratón

    private void OnEnable()
    {
        // Suscribimos el evento del scroll
        if (accionScroll != null)
        {
            accionScroll.action.Enable();
            accionScroll.action.performed += CambiarEmisora;
        }
    }

    private void OnDisable()
    {
        // Desuscribimos el evento para evitar errores de memoria
        if (accionScroll != null)
        {
            accionScroll.action.performed -= CambiarEmisora;
            accionScroll.action.Disable();
        }
    }

    private void Start()
    {
        if (emisoras.Count > 0)
        {
            ReproducirEmisora();
        }
    }

    private void CambiarEmisora(InputAction.CallbackContext context)
    {
        if (emisoras.Count == 0) return;

        // Leer el valor del scroll (Mouse Scroll en el Input System devuelve un Vector2)
        float scrollY = context.ReadValue<Vector2>().y;

        if (scrollY > 0) // Scroll hacia arriba (Siguiente emisora)
        {
            indiceActual++;
            if (indiceActual >= emisoras.Count) indiceActual = 0; // Vuelve al inicio si llega al final
            ReproducirEmisora();
        }
        else if (scrollY < 0) // Scroll hacia abajo (Emisora anterior)
        {
            indiceActual--;
            if (indiceActual < 0) indiceActual = emisoras.Count - 1; // Vuelve al final si está en el inicio
            ReproducirEmisora();
        }
    }

    private void ReproducirEmisora()
    {
        reproductorRadio.clip = emisoras[indiceActual].cancion;
        reproductorRadio.Play();

        // Actualizar la interfaz visual si tienes un texto asignado
        if (textoUIEmisora != null)
        {
            textoUIEmisora.text = emisoras[indiceActual].nombreEmisora;
        }
    }
}
