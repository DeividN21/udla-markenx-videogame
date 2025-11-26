using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class AccionButton : MonoBehaviour
    {
        private AccionInfo miAccion;
        private GameUIManager uiManager;

        public Button miBoton;
        public TextMeshProUGUI miTexto;
        public Image miImagenFondo;

        public Color colorBloqueado = Color.gray;
        public Color colorDisponible = Color.white;
        public Color colorComprado = new Color(0.5f, 1f, 0.5f);

        public void Inicializar(AccionInfo accion, GameUIManager manager)
        {
            miAccion = accion;
            uiManager = manager;
            if(miTexto != null) miTexto.text = accion.nombreAccion;
        
            miBoton.onClick.RemoveAllListeners();
            miBoton.onClick.AddListener(() => uiManager.OnHexagonoClick(miAccion));
        }

        public void ActualizarVisual(bool desbloqueada, bool comprada)
        {
            miBoton.interactable = true; 
            if (miImagenFondo != null)
            {
                if (comprada) miImagenFondo.color = colorComprado;
                else miImagenFondo.color = desbloqueada ? colorDisponible : colorBloqueado;
            }
        }

        public int GetAccionID() => miAccion.idAccion;
        public string GetAccionCategoria() => miAccion.categoria;
    }
}