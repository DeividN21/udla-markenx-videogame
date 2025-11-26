using UnityEngine;
using UnityEngine.EventSystems;

// Interfaces para detectar el mouse
namespace Managers
{
    public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform rectTransform;
        private Vector3 originalScale;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Se agranda al pasar el mouse (Highlight)
            rectTransform.localScale = originalScale * 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Vuelve a normal
            rectTransform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Se encoge al hacer clic (Press feeling)
            rectTransform.localScale = originalScale * 0.9f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Rebota un poco
            rectTransform.localScale = originalScale * 1.1f;
        }
    }
}