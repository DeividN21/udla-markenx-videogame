using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Managers
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("Paneles Principales")]
        public GameObject panelConsumidor;
        public GameObject panelAtributos;
        public GameObject panelExploracion;
        public GameObject panelPublicidad;
        public GameObject panelPerfil;
        public GameObject panelNoticiaOverlay;

        [Header("Elementos UI Estado")]
        public TextMeshProUGUI textoTurno;
        public TextMeshProUGUI textoPresupuesto;
        public Slider sliderAceptacion;
        public TextMeshProUGUI textoNoticiaTopBar;
        public TextMeshProUGUI textoPorcentajeAceptacion;

        [Header("Elementos UI Noticia Overlay")]
        public TextMeshProUGUI textoNoticiaTitulo;
        public TextMeshProUGUI textoNoticiaCuerpo;
        public Button botonCerrarNoticia;

        [Header("Contenedores Hexágonos")]
        public Transform contenedorAtributos;
        public Transform contenedorExploracion;
        public Transform contenedorPublicidad;
    
        [Header("Botones Categoría")]
        public Button botonCategoriaProduccion; 
        public Button botonCategoriaDiseno;
        public Button botonCategoriaPrecio;
        public Button botonCategoriaPlaza;

        [Header("Detalle Acción")]
        public GameObject panelDetalle;
        public TextMeshProUGUI detalleNombre;
        public TextMeshProUGUI detalleDescrip;
        public TextMeshProUGUI detallePrecio;
        public Button botonInvertir;

        [Header("Perfil UI")]
        public TextMeshProUGUI perfilNombre;
        public TextMeshProUGUI perfilEdad;
        public Slider perfilSlider;
        public TextMeshProUGUI textoPorcentajePerfil;
        public List<TextMeshProUGUI> textosFactores; 

        [Header("Control Final")]
        public Button buttonEnviar;
        public TextMeshProUGUI textoBotonEnviar;
        public Image imagenPersonaje;
        public Sprite spriteNormal;
        public Sprite spriteFeliz;
    
        [Header("Presupuesto (Multiples sitios)")]
        public TextMeshProUGUI textoPresupuestoPrincipal; // Group_Consumidor
        public List<TextMeshProUGUI> textosPresupuestoPaneles; 

        [Header("Alertas")]
        public GameObject alertaPerfil; 

        [Header("Recursos")]
        public GameObject hexagonoPrefab;
        public string escenaMainMenu = "MainMenu";

        private GameSceneManager manager;
        private AccionInfo accionSeleccionada;
        private int factoresDescubiertosPrevios = 0;

        void Start()
        {
            manager = GameSceneManager.Instance;
            if (manager == null) return;
        
            factoresDescubiertosPrevios = manager.GetSubfactoresDescubiertos().Count;

            if(botonCategoriaProduccion) botonCategoriaProduccion.onClick.AddListener(() => PoblarPanelAcciones("ATRIBUTOS_PRODUCCION"));
            if(botonCategoriaDiseno) botonCategoriaDiseno.onClick.AddListener(() => PoblarPanelAcciones("ATRIBUTOS_DISENO"));
            if(botonCategoriaPrecio) botonCategoriaPrecio.onClick.AddListener(() => PoblarPanelAcciones("ATRIBUTOS_PRECIO"));
            if(botonCategoriaPlaza) botonCategoriaPlaza.onClick.AddListener(() => PoblarPanelAcciones("ATRIBUTOS_PLAZA"));
        
            if(botonCerrarNoticia) botonCerrarNoticia.onClick.AddListener(CerrarNoticiaOverlay);
        
            if (botonInvertir) {
                botonInvertir.onClick.RemoveAllListeners();
                botonInvertir.onClick.AddListener(OnInvertirClick);
            }
            if (buttonEnviar) {
                buttonEnviar.onClick.RemoveAllListeners();
                buttonEnviar.onClick.AddListener(OnEnviarTurnoClick);
            }

            OcultarTodosLosPaneles();
            if (panelConsumidor) panelConsumidor.SetActive(true);
            if (panelDetalle) panelDetalle.SetActive(false);
            if (panelNoticiaOverlay) panelNoticiaOverlay.SetActive(false);
            if (alertaPerfil) alertaPerfil.SetActive(false);

            GenerarHexagonosEnContenedor("ATRIBUTOS_PRODUCCION", contenedorAtributos);
            GenerarHexagonosEnContenedor("ATRIBUTOS_DISENO", contenedorAtributos);
            GenerarHexagonosEnContenedor("ATRIBUTOS_PRECIO", contenedorAtributos);
            GenerarHexagonosEnContenedor("ATRIBUTOS_PLAZA", contenedorAtributos);
            GenerarHexagonosEnContenedor("EXPLORACION", contenedorExploracion);
            GenerarHexagonosEnContenedor("PUBLICIDAD", contenedorPublicidad);

            ActualizarEstadoUI();
            PoblarPanelAcciones("ATRIBUTOS_PRODUCCION");
        }

        void GenerarHexagonosEnContenedor(string categoria, Transform contenedor)
        {
            if (contenedor == null) return;
            var acciones = manager.GetAccionesDisponibles().Where(a => a.categoria == categoria).ToList();

            foreach (var acc in acciones) {
                GameObject hex = Instantiate(hexagonoPrefab, contenedor);
                AccionButton btn = hex.GetComponent<AccionButton>();
                if(btn != null) btn.Inicializar(acc, this);
            
                if (contenedor == contenedorAtributos) hex.SetActive(false); else hex.SetActive(true);
            }
        }
    
        void PoblarPanelAcciones(string categoriaMostrar)
        {
            if (contenedorAtributos == null) return;
            foreach (Transform child in contenedorAtributos) {
                AccionButton btn = child.GetComponent<AccionButton>();
                if (btn != null) {
                    bool coincide = (btn.GetAccionCategoria() == categoriaMostrar);
                    child.gameObject.SetActive(coincide);
                    if (coincide) { 
                        bool desbloqueada = manager.IsAccionDesbloqueada(btn.GetAccionID());
                        bool comprada = manager.IsAccionComprada(btn.GetAccionID());
                        btn.ActualizarVisual(desbloqueada, comprada);
                    }
                }
            }
        }

        public void ActualizarEstadoUI()
        {
            if (textoTurno) textoTurno.text = "TURNO " + manager.GetTurnoActual();
            string presu = "$" + manager.GetPresupuestoActual();
            if (textoPresupuesto) textoPresupuesto.text = presu;
            if (textoPresupuestoPrincipal) textoPresupuestoPrincipal.text = presu;
        
            if(textosPresupuestoPaneles != null) {
                foreach(var t in textosPresupuestoPaneles) if(t) t.text = presu;
            }
        
            float aceptacion = manager.GetAceptacionActual(); 
            if (sliderAceptacion) sliderAceptacion.value = aceptacion / 100f;
            if (textoPorcentajeAceptacion) textoPorcentajeAceptacion.text = aceptacion.ToString("F0") + "%";

            float nivelPerfil = manager.GetNivelPerfil();
            if (perfilSlider) perfilSlider.value = nivelPerfil;
            if (textoPorcentajePerfil) textoPorcentajePerfil.text = (nivelPerfil * 100).ToString("F0") + "%";

            if (aceptacion >= 80) {
                if (imagenPersonaje) imagenPersonaje.sprite = spriteFeliz;
                if (textoBotonEnviar) textoBotonEnviar.text = "TERMINAR";
            } else {
                if (imagenPersonaje) imagenPersonaje.sprite = spriteNormal;
                if (textoBotonEnviar) textoBotonEnviar.text = "ENVIAR";
            }

            int factoresAhora = manager.GetSubfactoresDescubiertos().Count;
            if (factoresAhora > factoresDescubiertosPrevios) {
                if (alertaPerfil) alertaPerfil.SetActive(true);
                factoresDescubiertosPrevios = factoresAhora;
            }

            string tituloNoticia = manager.GetNoticiaTitulo();
            if (!string.IsNullOrEmpty(tituloNoticia))
            {
                if (textoNoticiaTopBar) textoNoticiaTopBar.text = tituloNoticia;
                if (panelNoticiaOverlay && !panelNoticiaOverlay.activeSelf && manager.GetTurnoActual() == 3) {
                    panelNoticiaOverlay.SetActive(true);
                    if (textoNoticiaTitulo) textoNoticiaTitulo.text = tituloNoticia;
                    if (textoNoticiaCuerpo) textoNoticiaCuerpo.text = manager.GetNoticiaDetalle();
                }
            }
            else { if (textoNoticiaTopBar) textoNoticiaTopBar.text = "SIN NOTICIAS"; }
        
            ActualizarEstadoHexagonos();
            PoblarPerfil();
        }

        void ActualizarEstadoHexagonos()
        {
            var botones = FindObjectsByType<AccionButton>(FindObjectsSortMode.None);
            foreach(var btn in botones)
            {
                bool desbloqueada = manager.IsAccionDesbloqueada(btn.GetAccionID());
                bool comprada = manager.IsAccionComprada(btn.GetAccionID());
                btn.ActualizarVisual(desbloqueada, comprada);
            }
        }

        void PoblarPerfil()
        {
            if (perfilNombre) perfilNombre.text = manager.GetNombreConsumidor();
            if (perfilEdad) perfilEdad.text = manager.GetEdadConsumidor() + " AÑOS";

            SetTextoFactor(0, 100, "SUBCULTURA: ECOLÓGICO");
            SetTextoFactor(1, 200, "OCUPACIÓN: PILOTO");
            SetTextoFactor(2, 300, "ESTILO: MINIMALISTA");
            SetTextoFactor(3, 400, "MOTIVACIÓN: AHORRO");
        }

        void SetTextoFactor(int indexUI, int idFactor, string textoReal)
        {
            if (indexUI < textosFactores.Count)
            {
                if (manager.IsFactorDescubierto(idFactor)) 
                    textosFactores[indexUI].text = textoReal;
                else
                    textosFactores[indexUI].text = "??";
            }
        }

        // EVENTOS UI

        public void OnHexagonoClick(AccionInfo accion)
        {
            accionSeleccionada = accion;
            if (panelDetalle != null)
            {
                panelDetalle.SetActive(true);
                if (detalleNombre) detalleNombre.text = accion.nombreAccion;
                if (detalleDescrip) detalleDescrip.text = accion.descripcion;
                if (detallePrecio) detallePrecio.text = "$" + accion.costo;
            
                bool yaComprada = manager.IsAccionComprada(accion.idAccion);
                bool puedeComprar = manager.GetPresupuestoActual() >= accion.costo && manager.IsAccionDesbloqueada(accion.idAccion);
            
                if (botonInvertir) 
                {
                    botonInvertir.interactable = puedeComprar && !yaComprada;
                    var textoBtn = botonInvertir.GetComponentInChildren<TextMeshProUGUI>();
                    if(textoBtn) textoBtn.text = yaComprada ? "COMPRADO" : "INVERTIR";
                }
            }
        }

        public void OnInvertirClick()
        {
            if(accionSeleccionada != null)
            {
                if(manager.ComprarAccion(accionSeleccionada.idAccion))
                {
                    if (panelDetalle) panelDetalle.SetActive(false);
                    ActualizarEstadoUI();
                }
            }
        }

        public void OnEnviarTurnoClick()
        {
            if (manager.GetAceptacionActual() >= 80)
                manager.EjecutarFinDeJuego("GANASTE");
            else {
                manager.TerminarTurno();
                ActualizarEstadoUI();
            }
        }

        public void CerrarNoticiaOverlay()
        {
            if(panelNoticiaOverlay) panelNoticiaOverlay.SetActive(false);
        }
    
        public void ShowPanelPerfil() {
            OcultarTodosLosPaneles();
            if (panelPerfil) AnimatePanelOpening(panelPerfil);
            if (alertaPerfil) alertaPerfil.SetActive(false); // Apagar alerta
        }

        // NAVEGACIÓN
        private void OcultarTodosLosPaneles() {
            if (panelConsumidor) panelConsumidor.SetActive(false);
            if (panelAtributos) panelAtributos.SetActive(false);
            if (panelExploracion) panelExploracion.SetActive(false);
            if (panelPublicidad) panelPublicidad.SetActive(false);
            if (panelPerfil) panelPerfil.SetActive(false);
            if (panelDetalle) panelDetalle.SetActive(false);
        }
    
        public void AnimatePanelOpening(GameObject panel) {
            panel.SetActive(true);
            panel.transform.localScale = Vector3.zero;
            StartCoroutine(PopUpAnimation(panel.transform));
        }
    
        System.Collections.IEnumerator PopUpAnimation(Transform target) {
            float timer = 0; float duration = 0.2f;
            while (timer < duration) {
                timer += Time.deltaTime;
                float progress = timer / duration;
                float scale = Mathf.Sin(progress * Mathf.PI * 0.5f); 
                target.localScale = Vector3.one * scale;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        public void ShowPanelConsumidor() { OcultarTodosLosPaneles(); if (panelConsumidor) AnimatePanelOpening(panelConsumidor); }
        public void ShowPanelAtributos() { OcultarTodosLosPaneles(); if (panelAtributos) AnimatePanelOpening(panelAtributos); PoblarPanelAcciones("ATRIBUTOS_PRODUCCION"); }
        public void ShowPanelExploracion() { OcultarTodosLosPaneles(); if (panelExploracion) AnimatePanelOpening(panelExploracion); }
        public void ShowPanelPublicidad() { OcultarTodosLosPaneles(); if (panelPublicidad) AnimatePanelOpening(panelPublicidad); }
    
        public void BotonPausa() { Debug.Log("Pausa"); }
        public void BotonInicio() { ShowPanelConsumidor(); }
        public void BotonIrAMenu() { SceneManager.LoadScene(escenaMainMenu); }
    }
}