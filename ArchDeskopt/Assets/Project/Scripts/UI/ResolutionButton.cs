using UnityEngine;
using TMPro;

public class ResolutionButton : MonoBehaviour
{
    [Header("Resoluciones")]
    [SerializeField] private Vector2Int[] resoluciones =
    {
        new Vector2Int(480, 640),
        new Vector2Int(600, 800),
        new Vector2Int(720, 960),
        new Vector2Int(900, 1200)
    };

    [Header("Texto")]
    [SerializeField] private TMP_Text textoResolucion;

    private int resolucionActual = 0;

    private void Start()
    {
        ActualizarTexto();
    }

    public void CambiarResolucion()
    {
        resolucionActual++;

        if (resolucionActual >= resoluciones.Length)
        {
            resolucionActual = 0;
        }

        Vector2Int nuevaResolucion = resoluciones[resolucionActual];

        if (Archaeo.Platform.WindowManager.Instance != null)
        {
            Archaeo.Platform.WindowManager.Instance.CambiarResolucion(
                nuevaResolucion.x,
                nuevaResolucion.y
            );
        }

        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (textoResolucion != null)
        {
            textoResolucion.text =
                resoluciones[resolucionActual].x +
                " x " +
                resoluciones[resolucionActual].y;
        }
    }
}