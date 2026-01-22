using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MarkenX.Api.Config;

public class MainMenu : MonoBehaviour
{
    [Header("Botones de la Escena")]
    public Button buttonIniciar;
    public Button buttonSalir;

    [Header("Datos de Prueba (Solo si ApiConfig no está configurado)")]
    [Tooltip("Solo se usa si ApiConfig.ScenarioId está vacío")]
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

        // Determinar el scenarioId a usar (prioridad: ApiConfig > Inspector)
        string scenarioId = idAsignacionPrueba;

        if (ApiConfig.Instance != null && !string.IsNullOrEmpty(ApiConfig.Instance.ScenarioId))
        {
            scenarioId = ApiConfig.Instance.ScenarioId;
            Debug.Log($"[MainMenu] Usando ScenarioId de ApiConfig: {scenarioId}");
        }
        else
        {
            Debug.Log($"[MainMenu] Usando ScenarioId de prueba: {scenarioId}");
        }

        // Llama al Manager para que inicie la carga
        GameSceneManager.Instance.IniciarPartida(scenarioId);
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