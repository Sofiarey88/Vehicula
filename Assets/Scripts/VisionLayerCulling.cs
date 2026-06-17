using UnityEngine;

/// <summary>
/// Controla el renderizado de objetos del layer Vision basado en la visibilidad de cámara
/// </summary>
public class VisionLayerCulling : MonoBehaviour
{
    [SerializeField] private Renderer[] renderersToControl;
    [SerializeField] private Behaviour[] componentsToToggle; // Scripts, particulas, etc.
    [SerializeField] private bool disableComponents = true;

    private bool isVisible = false;

    private void Start()
    {
        // Obtener todos los renderers si no están asignados
        if (renderersToControl == null || renderersToControl.Length == 0)
        {
            renderersToControl = GetComponentsInChildren<Renderer>();
        }
    }

    private void OnBecameVisible()
    {
        if (isVisible) return;
        isVisible = true;
        SetActiveState(true);
    }

    private void OnBecameInvisible()
    {
        if (!isVisible) return;
        isVisible = false;
        SetActiveState(false);
    }

    private void SetActiveState(bool active)
    {
        // Activar/desactivar renderers
        foreach (var renderer in renderersToControl)
        {
            if (renderer != null)
            {
                renderer.enabled = active;
            }
        }

        // Opcional: desactivar componentes adicionales
        if (disableComponents && componentsToToggle != null)
        {
            foreach (var component in componentsToToggle)
            {
                if (component != null)
                {
                    component.enabled = active;
                }
            }
        }
    }
}