using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Managers
{
    public class ReporteManager : MonoBehaviour
    {
        [Header("UI de Reporte")]
        public TextMeshProUGUI textoResultado;
        public TextMeshProUGUI textoTurno;
        public TextMeshProUGUI textoPresupuesto;
        public TextMeshProUGUI textoPorcentajeAceptacion;
        public Slider sliderAceptacion;
        public TextMeshProUGUI textoPorcentajePerfil;
        public Slider sliderPerfil;

        [Header("Botones")]
        public Button botonGrafico;
        public Button botonSalir;

        void Start()
        {
            // 1. Leer los datos de GameState
            string resultado = GameState.resultadoJuego;
            int turno = GameState.ultimoTurno;
            int presupuesto = GameState.presupuestoRestante;
            float aceptacion = GameState.nivelAceptacion;
            float perfil = GameState.nivelPerfil;

            // 2. Aplicar esos datos a la UI
            if (textoResultado != null) textoResultado.text = resultado;
            if (textoTurno != null) textoTurno.text = "TURNO: " + turno.ToString();
            if (textoPresupuesto != null) textoPresupuesto.text = "PRESUPUESTO: " + presupuesto.ToString() + "$";
        
            if (textoPorcentajeAceptacion) textoPorcentajeAceptacion.text = "ACEPTACIÓN: " + (aceptacion * 100).ToString("F0") + "%";
            if (sliderAceptacion != null) sliderAceptacion.value = aceptacion;
            if (textoPorcentajePerfil) textoPorcentajePerfil.text = "PERFIL: " + (perfil * 100).ToString("F0") + "%";
            if (sliderPerfil != null) sliderPerfil.value = perfil;

            // CONECTAR BOTONES
            if (botonGrafico) botonGrafico.onClick.AddListener(() => SceneManager.LoadScene("GraficoScene"));
            if (botonSalir) botonSalir.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        }

    }
}