using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Botones de la Escena")]
        public Button buttonIniciar;
        public Button buttonSalir; 

        [Header("Datos de Prueba")]
        // Este ID será reemplazado por el que venga del portal web,
        public string idAsignacionPrueba = "ASIGNACION_1"; 

        // Un texto para mostrar el estado
        //public TextMeshProUGUI textoEstado; 

        void Start()
        {
            if (GameSceneManager.Instance == null)
            {
                Debug.LogError("¡ERROR! No se encontró el 'GameSceneManager'");
                if (buttonIniciar != null) buttonIniciar.interactable = false;
                //if (textoEstado != null) textoEstado.text = "Error: GameSceneManager no encontrado.";
                return;
            }

            // CHEQUEAR SI EL JUEGO YA TERMINÓ
            if (GameSceneManager.Instance.juegoTerminado)
            {
                if(buttonIniciar) buttonIniciar.interactable = false;
                //if(textoEstado) textoEstado.text = "Asignación completada.";
            }
            else
            {
                if(buttonIniciar) 
                {
                    buttonIniciar.interactable = true;
                    buttonIniciar.onClick.AddListener(OnIniciarPartida);
                }
            }
        
            if (buttonSalir) buttonSalir.onClick.AddListener(OnSalirDelJuego);
        }

        public void OnIniciarPartida()
        {
            // Desactiva el botón para evitar doble clic
            if (buttonIniciar != null) buttonIniciar.interactable = false;
            //if (textoEstado != null) textoEstado.text = "Cargando reglas desde el backend...";

            // Llama al Manager (que es persistente) para que inicie la carga.
            // El Manager se encargará de llamar al backend y cargar la 'GameScene'.
            GameSceneManager.Instance.IniciarPartida(idAsignacionPrueba);
        }

        public void OnSalirDelJuego()
        {
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.QuitGame();
            }
            else
            {
                Application.Quit();
            }
        }
    }
}