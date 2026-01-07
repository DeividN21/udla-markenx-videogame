using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Linq; 
using System;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    // ESTADO DEL JUEGO
    private GameContext context; 
    private int turnoActual;
    private float aceptacionActual; 
    private int presupuestoActual; 
    
    private string noticiaTituloActual; 
    private string noticiaDetalleActual;

    // UI State
    private HashSet<int> accionesDesbloqueadas = new HashSet<int>();
    private HashSet<int> accionesCompradasTotal = new HashSet<int>(); 
    private List<AccionInfo> accionesCompradasEsteTurno = new List<AccionInfo>(); 
    private HashSet<int> subfactoresDescubiertos = new HashSet<int>(); 

    // Pesos Dinámicos
    private Dictionary<DimensionDefinition, float> pesosActuales;

    // HISTORIAL PARA REPORTE
    private List<TurnHistoryLog> historialPartida;

    public bool juegoTerminado = false;

    [Header("Configuración")]
    public bool usarModoSimulacion = true;
    public const int MAX_TURNOS = 5;

    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    public void IniciarPartida(string id)
    {
        juegoTerminado = false; 
        if (usarModoSimulacion) StartCoroutine(SimularCarga());
    }

    IEnumerator SimularCarga()
    {
        yield return new WaitForSeconds(0.1f);
        // Carga desde el JSON simulado en MockDataFactory
        context = MockDataFactory.GetEcoGameContext();
        InicializarEstado();
        LoadScene("GameScene");
    }

    void InicializarEstado()
    {
        turnoActual = 1;
        accionesCompradasTotal.Clear();
        accionesCompradasEsteTurno.Clear();
        accionesDesbloqueadas.Clear();
        subfactoresDescubiertos.Clear();
        noticiaTituloActual = "";
        noticiaDetalleActual = "";
        
        historialPartida = new List<TurnHistoryLog>(); // Iniciar historial

        presupuestoActual = (int)context.PresupuestoInicial;
        aceptacionActual = 0f;

        // INICIALIZAR PESOS (Todos a 1.0 por defecto, luego se ajustan)
        pesosActuales = new Dictionary<DimensionDefinition, float>();
        foreach(var dim in context.DimensionsMap.Values)
        {
            // Se podrían cargar pesos iniciales del JSON si existieran
            pesosActuales[dim] = 1.0f;
        }
        
        // Ajuste manual para demo ecológica si es necesario, o dejar que el motor decida.
        // Para mantener la demo ganadora, se subirá el peso de la dimensión ecológica si la encontramos
        var ecoDim = context.DimensionsMap.Values.FirstOrDefault(d => d.Name == "EcoInterest");
        if(ecoDim != null) pesosActuales[ecoDim] = 5.0f;

        if (context.AccionesVisuales != null)
        {
            foreach(var acc in context.AccionesVisuales.Where(a => !a.esBloqueadaInicialmente))
                accionesDesbloqueadas.Add(acc.idAccion);
        }
        
        RecalcularAceptacion(); // Cálculo inicial
    }

    public bool ComprarAccion(int idAccion)
    {
        if (!accionesDesbloqueadas.Contains(idAccion)) return false;
        if (accionesCompradasTotal.Contains(idAccion)) return false;

        AccionInfo infoVisual = context.AccionesVisuales.Find(a => a.idAccion == idAccion);
        if (infoVisual == null) return false;

        if (presupuestoActual >= infoVisual.costo)
        {
            presupuestoActual -= (int)infoVisual.costo;
            
            accionesCompradasEsteTurno.Add(infoVisual);
            accionesCompradasTotal.Add(idAccion);

            // LOGICA CORE: Buscar por UUID en el mapa de acciones
            if (context.ActionsMap.TryGetValue(infoVisual.originalUuid, out MarketAction accionLogica))
            {
                accionLogica.Apply(context.Product);
                Debug.Log($"Acción aplicada: {infoVisual.nombreAccion}");
            }

            // Desbloquear hijos
            var hijos = context.AccionesVisuales.Where(a => a.idAccionRequerida == idAccion);
            foreach (var h in hijos) accionesDesbloqueadas.Add(h.idAccion);

            // Exploración (Lógica visual simple por categoría)
            if (infoVisual.categoria == "EXPLORACION") 
            {
                // Descubrir una dimensión al azar que no esté descubierta
                int nextId = subfactoresDescubiertos.Count + 1; 
                subfactoresDescubiertos.Add(nextId * 100); // Se simulan los IDs 
            }

            return true;
        }
        return false;
    }

    public bool TerminarTurno(out string mensajeError)
    {
        if (accionesCompradasEsteTurno.Count == 0)
        {
            mensajeError = "¡Debes comprar al menos una acción antes de enviar el turno!";
            return false; 
        }

        mensajeError = "";
        
        // Registrar Turno en Historial
        RegistrarHistorialTurno();

        RecalcularAceptacion();
        
        // Lógica de Eventos Dinámica (Se busca en la lista de eventos del JSON)
        // Ejemplo: Si es turno 2, se aplica el primer evento disponible
        if (turnoActual == 2 && context.EventosConfigurados.Count > 0)
        {
            var evento = context.EventosConfigurados[0];
            noticiaTituloActual = evento.title;
            noticiaDetalleActual = evento.description;
            
            // Aplicar efectos del evento (Multiplicadores de Peso)
            foreach(var efecto in evento.effects)
            {
                if(context.DimensionsMap.TryGetValue(efecto.dimensionId, out var dimDef))
                {
                    if(pesosActuales.ContainsKey(dimDef))
                        pesosActuales[dimDef] *= efecto.weightMultiplier;
                }
            }
            RecalcularAceptacion();
        }
        else
        {
            noticiaTituloActual = "";
            noticiaDetalleActual = "";
        }

        turnoActual++;
        accionesCompradasEsteTurno.Clear(); 

        // Verificar Derrota
        if (presupuestoActual <= 0 || turnoActual > MAX_TURNOS)
        {
            if (aceptacionActual < context.AceptacionObjetivo)
                EjecutarFinDeJuego("PERDISTE");
        }

        return true; 
    }

    private void RecalcularAceptacion()
    {
        aceptacionActual = DistanceMetric.ComputeWeightedAcceptance(
            context.Consumer, 
            context.Product, 
            pesosActuales 
        );
    }

    private void RegistrarHistorialTurno()
    {
        var log = new TurnHistoryLog();
        log.turnNumber = turnoActual;
        log.budgetAtEnd = presupuestoActual;
        log.acceptanceAtEnd = aceptacionActual;
        log.eventOcurredTitle = noticiaTituloActual;
        log.actionsTakenIds = new List<string>();
        
        foreach(var acc in accionesCompradasEsteTurno)
        {
            log.actionsTakenIds.Add(acc.originalUuid);
        }
        
        historialPartida.Add(log);
    }

    public void EjecutarFinDeJuego(string resultado)
    {
        juegoTerminado = true;
        GameState.resultadoJuego = resultado;
        GameState.nivelAceptacion = aceptacionActual;
        GameState.presupuestoRestante = presupuestoActual;
        GameState.ultimoTurno = turnoActual;
        GameState.nivelPerfil = GetNivelPerfil();
        
        // GENERAR JSON FINAL
        string jsonReporte = GenerarReporteJSON(resultado);
        Debug.Log("<b>REPORTE FINAL JSON:</b> " + jsonReporte);
        // Aquí se conectará este JSON con la API:
        // ApiService.EnviarResultados(jsonReporte);

        LoadScene("EndGameScene");
    }

    private string GenerarReporteJSON(string resultado)
    {
        var reporte = new GameSessionReport();
        reporte.sessionDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        reporte.finalOutcome = resultado;
        reporte.finalAcceptance = aceptacionActual;
        reporte.remainingBudget = presupuestoActual;
        reporte.totalTurnsUsed = turnoActual;
        reporte.profileDiscoveryPercentage = GetNivelPerfil();
        reporte.history = historialPartida;

        return JsonUtility.ToJson(reporte, true);
    }

    // GETTERS
    public List<AccionInfo> GetAccionesDisponibles() => context.AccionesVisuales;
    public int GetPresupuestoActual() => presupuestoActual;
    public float GetAceptacionActual() => aceptacionActual * 100f; 
    public int GetTurnoActual() => turnoActual;
    public string GetNoticiaTitulo() => noticiaTituloActual;
    public string GetNoticiaDetalle() => noticiaDetalleActual;
    public bool IsAccionDesbloqueada(int id) => accionesDesbloqueadas.Contains(id);
    public bool IsAccionComprada(int id) => accionesCompradasTotal.Contains(id);
    public string GetNombreConsumidor() => context.NombreConsumidor;
    public int GetEdadConsumidor() => context.EdadConsumidor;
    public bool IsFactorDescubierto(int index) => subfactoresDescubiertos.Contains(index);
    public HashSet<int> GetSubfactoresDescubiertos() => subfactoresDescubiertos;

    public float GetNivelPerfil() {
        if (context == null || context.Consumer == null) return 0f;
        return (float)subfactoresDescubiertos.Count / 4.0f; 
    }

    public void QuitGame() { Application.Quit(); }

    public void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.CargarEscena(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}