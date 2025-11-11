using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("Paneles Principales")]
    public GameObject panelConsumidor;      // Panel central con la barra de aceptación 
    public GameObject panelAtributos;     // Panel de hexágonos 
    public GameObject panelExploracion;   // (Aún no diseñado, pero se lo prepara)
    public GameObject panelPublicidad;    // (Aún no diseñado, pero se lo prepara)
    public GameObject panelPerfil;        // Panel de perfil 

    [Header("Escenas de Salida")]
    public string escenaMainMenu = "MainMenu"; // El nombre de la escena de menú
    public string escenaReporte = "EndGameScene"; // El nombre de la futura escena de "Ganaste"

    void Start()
    {
        // Al empezar el juego, solo se muestra el panel principal del consumidor
        ShowPanelConsumidor();
    }

    // FUNCIONES DE NAVEGACIÓN PRINCIPAL

    // Función para ocultar todos los paneles intercambiables
    private void OcultarTodosLosPaneles()
    {
        // Si el panel existe, lo desactiva
        if (panelConsumidor != null) panelConsumidor.SetActive(false);
        if (panelAtributos != null) panelAtributos.SetActive(false);
        if (panelExploracion != null) panelExploracion.SetActive(false);
        if (panelPublicidad != null) panelPublicidad.SetActive(false);
        if (panelPerfil != null) panelPerfil.SetActive(false);
    }

    // Funciones que llamarán a los botones
    public void ShowPanelConsumidor()
    {
        OcultarTodosLosPaneles();
        if (panelConsumidor != null) panelConsumidor.SetActive(true);
    }

    public void ShowPanelAtributos()
    {
        OcultarTodosLosPaneles();
        if (panelAtributos != null) panelAtributos.SetActive(true);
    }

    public void ShowPanelExploracion()
    {
        OcultarTodosLosPaneles();
        if (panelExploracion != null) panelExploracion.SetActive(true);
    }

    public void ShowPanelPublicidad()
    {
        OcultarTodosLosPaneles();
        if (panelPublicidad != null) panelPublicidad.SetActive(true);
    }

    public void ShowPanelPerfil()
    {
        OcultarTodosLosPaneles();
        if (panelPerfil != null) panelPerfil.SetActive(true);
    }

    // FUNCIONES DE BOTONES SUPERIORES
    public void BotonPausa()
    {
        // Time.timeScale = 0; // (Lógica de pausa)
        Debug.Log("Juego Pausado");
    }

    public void BotonInicio()
    {
        // Esta función llevará de vuelta al menú principal
        // O, a su vez, puede llamar a ShowPanelConsumidor()
        ShowPanelConsumidor();
    }

    // FUNCIONES DE FIN DE JUEGO
    public void EnviarTurno()
    {
        // 1. Aquí va la lógica para llamar al Modelo de IA
        // 2. Recibir el resultado (nuevo % de aceptación, presupuesto)
        // 3. Actualizar la UI
        
        Debug.Log("Turno enviado, esperando lógica de IA...");

        // 4. SIMULACIÓN DE VICTORIA:
        // Por ahora, CADA clic en "ENVIAR" es una victoria
        SceneManager.LoadScene(escenaReporte);
    }

    public void BotonIrAMenu()
    {
        // Time.timeScale = 1; // (Asegurarse de quitar la pausa)
        SceneManager.LoadScene(escenaMainMenu);
    }
}