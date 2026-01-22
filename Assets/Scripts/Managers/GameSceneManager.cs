using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;
using MarkenX.Api.Config;
using MarkenX.Api.Services;
using MarkenX.Api.Mappers;
using MarkenX.Api.Dtos;

public class GameSceneManager : MonoBehaviour
{
    // SINGLETON
    public static GameSceneManager Instance { get; private set; }

    // ESTADO DEL JUEGO
    private int presupuestoActual;
    private float aceptacionActual;
    private int turnoActual;
    private string noticiaTituloActual;
    private string noticiaDetalleActual;

    // LOGICA INTERNA
    private PartidaDataPayload partidaData;
    private List<EscenarioPerfilSubfactor> perfilActualizado;

    // Historial de compras (para bloquear botones)
    private HashSet<int> accionesCompradasTotal = new HashSet<int>();
    private List<Accion> accionesCompradasEsteTurno = new List<Accion>();

    // ÁRBOL DE HABILIDADES Y DESCUBRIMIENTO
    private HashSet<int> accionesDesbloqueadas = new HashSet<int>();
    private HashSet<int> subfactoresDescubiertos = new HashSet<int>();

    // HISTORIAL DE TURNOS (para POST al backend)
    private List<TurnHistoryDto> historialTurnosApi = new List<TurnHistoryDto>();

    // Estado Global para MainMenu
    public bool juegoTerminado = false;

    // CONFIGURACIÓN
    [Header("Configuración")]
    [Tooltip("Si está activo, usa datos mock locales en lugar de la API. En WebGL con token, se ignora y usa API real.")]
    public bool usarModoSimulacion = true;
    [Range(0f, 1f)]
    public float probabilidadEvento = 0.40f; // Probabilidad alta

    /// <summary>
    /// Determina si se debe usar el modo simulación.
    /// En WebGL compilado con token válido, siempre usa API real (modo producción).
    /// En Editor o sin token, respeta el valor de usarModoSimulacion.
    /// </summary>
    private bool DebeUsarModoSimulacion()
    {
        // Si estamos en modo producción (WebGL con token), nunca usar simulación
        if (ApiConfig.Instance != null && ApiConfig.Instance.IsProductionMode)
        {
            Debug.Log("[GameSceneManager] Modo producción detectado (WebGL + token). Usando API real.");
            return false;
        }

        // En otros casos, respetar la configuración del Inspector
        return usarModoSimulacion;
    }

    [Header("Estado de Carga")]
    [SerializeField] private bool _isLoading = false;
    [SerializeField] private string _loadError = "";

    public bool IsLoading => _isLoading;
    public string LoadError => _loadError;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    // CARGA INICIAL
    public void IniciarPartida(string idAsignacion)
    {
        juegoTerminado = false; // Reiniciar estado
        StartCoroutine(CargarReglasCoroutine(idAsignacion));
    }

    IEnumerator CargarReglasCoroutine(string idAsignacion)
    {
        _isLoading = true;
        _loadError = "";

        // Esperar a que ApiConfig esté listo para determinar el modo
        while (ApiConfig.Instance == null || !ApiConfig.Instance.IsConfigLoaded)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Determinar si usar modo simulación basado en el entorno
        bool modoSimulacion = DebeUsarModoSimulacion();

        if (modoSimulacion)
        {
            // MODO OFFLINE - Usar datos mock locales
            Debug.Log("[GameSceneManager] Modo simulación activo, usando MockDataFactory");
            yield return new WaitForSeconds(0.5f);
            partidaData = MockDataFactory.GetMockData();
            InicializarJuego();
            _isLoading = false;
            SceneManager.LoadScene("GameScene");
            yield break;
        }

        // MODO ONLINE - Cargar desde API REST
        Debug.Log("[GameSceneManager] Modo producción activo, cargando desde API");

        // Determinar el scenarioId a usar
        string scenarioId = !string.IsNullOrEmpty(idAsignacion) ? idAsignacion : ApiConfig.Instance.ScenarioId;

        if (string.IsNullOrEmpty(scenarioId))
        {
            _loadError = "No se especificó un scenarioId válido";
            _isLoading = false;
            Debug.LogError($"[GameSceneManager] {_loadError}");
            yield break;
        }

        // Llamar al servicio de API
        bool requestComplete = false;
        bool requestSuccess = false;

        ScenarioApiService.Instance.GetScenarioById(scenarioId,
            // onSuccess
            (ScenarioDetailResponse response) =>
            {
                partidaData = ScenarioDataMapper.ToPartidaDataPayload(response);
                requestSuccess = partidaData != null;
                if (!requestSuccess)
                    _loadError = "Error al mapear los datos del escenario";
                requestComplete = true;
            },
            // onError
            (string error) =>
            {
                _loadError = error;
                requestComplete = true;
            }
        );

        // Esperar a que se complete el request
        while (!requestComplete)
        {
            yield return null;
        }

        _isLoading = false;

        if (requestSuccess)
        {
            InicializarJuego();
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            Debug.LogError($"[GameSceneManager] Error cargando escenario: {_loadError}");
        }
    }

    void InicializarJuego()
    {
        presupuestoActual = partidaData.presupuestoInicial;
        aceptacionActual = 0;
        turnoActual = 1;
        noticiaTituloActual = "";
        noticiaDetalleActual = "";

        perfilActualizado = new List<EscenarioPerfilSubfactor>(partidaData.perfilConsumidor);
        accionesCompradasEsteTurno.Clear();
        accionesCompradasTotal.Clear();

        // Limpiar historial de turnos para la API
        historialTurnosApi.Clear();

        // Lógica Árbol: Desbloquear acciones iniciales (las que no tienen padre o idReq=0)
        accionesDesbloqueadas.Clear();
        foreach (var acc in partidaData.accionesDisponibles.Where(a => !a.esBloqueadaInicialmente))
            accionesDesbloqueadas.Add(acc.idAccion);

        // Lógica Perfil: Descubrir factores iniciales
        subfactoresDescubiertos.Clear();
        foreach (var fac in partidaData.perfilConsumidor.Where(p => p.esVisibleInicialmente))
            subfactoresDescubiertos.Add(fac.idSubfactor);
    }

    // LÓGICA DE JUEGO (ACCIONES)
    
    public bool ComprarAccion(int idAccion)
    {
        // 1. Verificar si está desbloqueada
        if (!accionesDesbloqueadas.Contains(idAccion)) return false;
        if (accionesCompradasTotal.Contains(idAccion)) return false; // Ya comprada

        Accion accion = partidaData.accionesDisponibles.Find(a => a.idAccion == idAccion);
        if (accion == null) return false;

        // 2. Verificar presupuesto
        if (presupuestoActual >= accion.costo)
        {
            presupuestoActual -= (int)accion.costo;
            accionesCompradasEsteTurno.Add(accion);
            accionesCompradasTotal.Add(idAccion); // Registrar compra permanente

            // 3. DESBLOQUEAR HIJOS (Lógica de Árbol)
            // Busca acciones cuyo requisito sea la acción que se acaba de comprar
            var hijos = partidaData.accionesDisponibles.Where(a => a.idAccionRequerida == accion.idAccion);
            foreach (var hijo in hijos)
            {
                accionesDesbloqueadas.Add(hijo.idAccion);
            }

            // 4. LÓGICA ESPECIAL: Si es exploración, descubre factores
            if (accion.idAccion == 13) DescubrirFactor(200);
            if (accion.idAccion == 14) DescubrirFactor(100);
            if (accion.idAccion == 15) DescubrirFactor(300); 
            if (accion.idAccion == 16) DescubrirFactor(400);

            return true;
        }
        return false;
    }

    private void DescubrirFactor(int idFactor)
    {
        if(!subfactoresDescubiertos.Contains(idFactor))
            subfactoresDescubiertos.Add(idFactor);
    }

    // LÓGICA DE TURNO (IA LOCAL)

    public void TerminarTurno()
    {
        // 1. Calcular Impacto (Fórmula Ai * Wi)
        float impactoTurno = 0;
        foreach (Accion accion in accionesCompradasEsteTurno)
        {
            var reglas = partidaData.reglasImpacto.Where(r => r.idAccion == accion.idAccion);
            foreach (var regla in reglas)
            {
                var factor = perfilActualizado.Find(f => f.idSubfactor == regla.idSubfactor);
                if (factor != null)
                {
                    impactoTurno += regla.impacto * factor.peso;
                }
            }
        }

        aceptacionActual = Mathf.Clamp(aceptacionActual + impactoTurno, 0, 100);

        // 2. Procesar Eventos (Noticias)
        string eventoOcurrido = "";
        if (turnoActual == 2 && partidaData.eventosPosibles != null && partidaData.eventosPosibles.Count > 0)
        {
            var evento = partidaData.eventosPosibles[0];
            noticiaTituloActual = evento.tituloNoticia;
            noticiaDetalleActual = evento.detalleNoticia;
            eventoOcurrido = evento.tituloNoticia;

            var efecto = partidaData.efectosEventos.Find(e => e.idEvento == evento.idEvento);
            if (efecto != null)
            {
                var factorEco = perfilActualizado.Find(f => f.idSubfactor == efecto.idSubfactor);
                if (factorEco != null) factorEco.peso += efecto.modificadorPeso;
            }
        }
        else
        {
            noticiaTituloActual = "";
            noticiaDetalleActual = "";
        }

        // 3. Registrar turno en historial para API
        GuardarTurnoEnHistorial(eventoOcurrido);

        turnoActual++;
        accionesCompradasEsteTurno.Clear();
    }

    /// <summary>
    /// Guarda el estado actual del turno en el historial para enviar al backend.
    /// </summary>
    private void GuardarTurnoEnHistorial(string eventoOcurrido)
    {
        var turnHistory = new TurnHistoryDto
        {
            turnNumber = turnoActual,
            acceptanceAtEnd = aceptacionActual / 100f, // Normalizar a 0-1
            budgetAtEnd = presupuestoActual,
            eventOccurredTitle = eventoOcurrido,
            actionsTakenIds = accionesCompradasEsteTurno
                .Select(a => ScenarioDataMapper.GetApiActionId(a.idAccion))
                .ToList()
        };

        historialTurnosApi.Add(turnHistory);
    }

    // Función llamarla desde el botón "TERMINAR"
    public void EjecutarFinDeJuego(string resultado)
    {
        juegoTerminado = true;

        // Guardar estado local para UI
        GameState.resultadoJuego = resultado;
        GameState.ultimoTurno = turnoActual;
        GameState.presupuestoRestante = presupuestoActual;
        GameState.nivelAceptacion = aceptacionActual / 100f;
        GameState.nivelPerfil = GetNivelPerfil();

        // Enviar sesión al backend si estamos en modo producción (WebGL con token)
        if (ApiConfig.Instance != null && ApiConfig.Instance.IsProductionMode)
        {
            EnviarSesionAlBackend();
        }
        else
        {
            Debug.Log("[GameSceneManager] Modo simulación - No se envía sesión al backend");
        }

        SceneManager.LoadScene("EndGameScene");
    }

    /// <summary>
    /// Envía los datos de la sesión de juego al backend.
    /// </summary>
    private void EnviarSesionAlBackend()
    {
        if (GameSessionApiService.Instance == null || ApiConfig.Instance == null)
        {
            Debug.LogWarning("[GameSceneManager] Servicios API no disponibles, no se enviará la sesión");
            return;
        }

        string taskId = ApiConfig.Instance.TaskId;
        string studentId = ApiConfig.Instance.StudentId;

        if (string.IsNullOrEmpty(taskId))
        {
            taskId = ApiConfig.Instance.ScenarioId; // Fallback: usar scenarioId como taskId
        }

        if (string.IsNullOrEmpty(studentId))
        {
            Debug.LogWarning("[GameSceneManager] StudentId no configurado, usando 'anonymous'");
            studentId = "anonymous";
        }

        var request = GameSessionApiService.Instance.CreateRequestFromGameState(
            taskId: taskId,
            studentId: studentId,
            finalAcceptance: aceptacionActual / 100f,
            remainingBudget: presupuestoActual,
            totalTurnsUsed: turnoActual,
            profileDiscoveryPercentage: GetNivelPerfil(),
            history: historialTurnosApi
        );

        GameSessionApiService.Instance.RegisterGameSession(request,
            onSuccess: (response) =>
            {
                Debug.Log($"[GameSceneManager] Sesión registrada exitosamente. ID: {response.id}");
                Debug.Log($"[GameSceneManager] Resultado del backend: {response.finalOutcome}");

                // Actualizar resultado con el calculado por el backend
                if (!string.IsNullOrEmpty(response.finalOutcome))
                {
                    GameState.resultadoJuego = response.finalOutcome;
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[GameSceneManager] Error al registrar sesión: {error}");
            }
        );
    }

    // GETTERS PARA LA UI
    public PartidaDataPayload GetPartidaData() => partidaData;
    public int GetPresupuestoActual() => presupuestoActual;
    public float GetAceptacionActual() => aceptacionActual;
    public int GetTurnoActual() => turnoActual;
    public string GetNoticiaTitulo() => noticiaTituloActual;
    public string GetNoticiaDetalle() => noticiaDetalleActual;
    public bool IsAccionDesbloqueada(int id) => accionesDesbloqueadas.Contains(id);
    public bool IsAccionComprada(int id) => accionesCompradasTotal.Contains(id);
    public bool IsFactorDescubierto(int id) => subfactoresDescubiertos.Contains(id);
    public HashSet<int> GetSubfactoresDescubiertos() => subfactoresDescubiertos;

    public float GetNivelPerfil()
    {
        if (partidaData == null || partidaData.perfilConsumidor == null || partidaData.perfilConsumidor.Count == 0) 
            return 0f;
        
        return (float)subfactoresDescubiertos.Count / partidaData.perfilConsumidor.Count;
    }
    
    // UTILS
    public void QuitGame() { Application.Quit(); }

}


/* =================================================================================
   CÓDIGO ANTIGUO (COMENTADO)
   =================================================================================
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;

public class GameSceneManager : MonoBehaviour
{
    // SINGLETON
    public static GameSceneManager Instance { get; private set; }

    // ESTADO DEL JUEGO
    private int presupuestoActual;
    private float aceptacionActual;
    private int turnoActual;
    private string noticiaActual;
    private List<EscenarioPerfilSubfactor> perfilActualizado; // Perfil que puede ser modificado por eventos
    private List<Turno> historialTurnos = new List<Turno>();
    private List<Accion> accionesCompradasEsteTurno = new List<Accion>();

    // LÓGICA DE ÁRBOL DE HABILIDADES Y PERFIL
    private HashSet<int> accionesDesbloqueadas = new HashSet<int>();
    private HashSet<int> subfactoresDescubiertos = new HashSet<int>();

    // REGLAS DEL JUEGO (CARGADAS AL INICIO)
    private PartidaDataPayload partidaData; 

    // CONEXIÓN BACKEND
    private string apiUrl_CargarPartida = "http://URL_BACKEND/api/partida/iniciar";
    private string apiUrl_GuardarPartida = "http://URL_BACKEND/api/partida/guardar";

    [Header("Configuración de Juego")]
    [Range(0f, 1f)]
    public float probabilidadEventoNoticia = 0.25f;


    void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
    }

    // 1. LLAMADO DESDE MainMenu
    public void IniciarPartida(string idAsignacion)
    {
        StartCoroutine(CargarReglasCoroutine(idAsignacion));
    }

    // 2. CORUTINA DE CARGA
    IEnumerator CargarReglasCoroutine(string idAsignacion)
    {
        string urlConParams = $"{apiUrl_CargarPartida}?idAsignacion={idAsignacion}";
        using (UnityWebRequest request = UnityWebRequest.Get(urlConParams))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError("Error al cargar reglas: " + request.error);
            } else {
                string jsonResponse = request.downloadHandler.text;
                partidaData = JsonUtility.FromJson<PartidaDataPayload>(jsonResponse);

                // 3. INICIALIZAR EL ESTADO DEL JUEGO
                presupuestoActual = partidaData.presupuestoInicial;
                aceptacionActual = 0;
                turnoActual = 1;
                noticiaActual = "El mercado está estable.";
                perfilActualizado = new List<EscenarioPerfilSubfactor>(partidaData.perfilConsumidor);
                historialTurnos.Clear();
                accionesCompradasEsteTurno.Clear();

                // 4. INICIALIZAR LÓGICA DE ÁRBOL Y PERFIL
                accionesDesbloqueadas.Clear();
                subfactoresDescubiertos.Clear();
                foreach (var accion in partidaData.accionesDisponibles.Where(a => !a.esBloqueadaInicialmente)) {
                    accionesDesbloqueadas.Add(accion.idAccion);
                }
                foreach (var factor in partidaData.perfilConsumidor.Where(p => p.esVisibleInicialmente)) {
                    subfactoresDescubiertos.Add(factor.idSubfactor);
                }

                // 5. CARGAR LA ESCENA DEL JUEGO
                SceneManager.LoadScene("GameScene");
            }
        }
    }

    // MÉTODOS PÚBLICOS

    public PartidaDataPayload GetPartidaData() => partidaData;
    public int GetPresupuestoActual() => presupuestoActual;
    public float GetAceptacionActual() => aceptacionActual;
    public int GetTurnoActual() => turnoActual;
    public string GetNoticiaActual() => noticiaActual;
    public HashSet<int> GetSubfactoresDescubiertos() => subfactoresDescubiertos;
    
    // LOGICA ARBOL DE HABILIDADES
    public bool EstaAccionDesbloqueada(int idAccion)
    {
        return accionesDesbloqueadas.Contains(idAccion);
    }
    
    // LOGICA NIVEL PERFIL
    public float GetNivelPerfil()
    {
        if (partidaData == null || partidaData.perfilConsumidor.Count == 0) return 0f;
        return (float)subfactoresDescubiertos.Count / partidaData.perfilConsumidor.Count;
    }

    // Llamado por el botón "INVERTIR" en GameUIManager
    public bool ComprarAccion(int idAccion)
    {
        if (!EstaAccionDesbloqueada(idAccion)) {
            Debug.LogWarning("Intento de comprar acción BLOQUEADA");
            return false;
        }

        Accion accion = partidaData.accionesDisponibles.Find(a => a.idAccion == idAccion);
        if (accion == null) return false;

        if (presupuestoActual >= accion.costo)
        {
            presupuestoActual -= (int)accion.costo;
            accionesCompradasEsteTurno.Add(accion);

            // LOGICA ARBOL DE HABILIDADES: Desbloquear acciones siguientes
            foreach (var acc in partidaData.accionesDisponibles.Where(a => a.idAccionRequerida == accion.idAccion)) {
                accionesDesbloqueadas.Add(acc.idAccion);
            }

            // LOGICA NIVEL PERFIL: Si es de exploración, descubre un factor
            if (accion.categoria == "EXPLORACION") {
                DescubrirNuevoFactor();
            }

            Debug.Log($"Acción comprada: {accion.nombreAccion}");
            return true;
        }
        
        Debug.Log("¡Presupuesto insuficiente!");
        return false;
    }

    // Llamado por el botón "ENVIAR" en GameUIManager
    public void TerminarTurno()
    {
        float aceptacionCalculada = CalcularAceptacionLocal();
        aceptacionActual = aceptacionCalculada; 

        noticiaActual = ProcesarEventoNoticia(); // LOGICA DE EVENTOS

        GuardarTurnoActual(aceptacionCalculada, noticiaActual);

        turnoActual++;
        accionesCompradasEsteTurno.Clear();

        if (aceptacionActual >= partidaData.aceptacionObjetivo) {
            TerminarPartida("GANASTE");
        } else if (presupuestoActual <= 0 && aceptacionActual < partidaData.aceptacionObjetivo) {
            TerminarPartida("PERDISTE");
        }
    }

    // IA LOCAL)

    private float CalcularAceptacionLocal()
    {
        float aceptacionTotalTurno = 0;
        foreach (Accion accion in accionesCompradasEsteTurno)
        {
            List<AccionSubfactorImpacto> reglasParaEstaAccion = 
                partidaData.reglasImpacto.Where(r => r.idAccion == accion.idAccion).ToList();

            foreach (AccionSubfactorImpacto regla in reglasParaEstaAccion)
            {
                EscenarioPerfilSubfactor perfilFactor = 
                    perfilActualizado.Find(p => p.idSubfactor == regla.idSubfactor);

                if (perfilFactor != null)
                {
                    // A_i * W_i
                    float impactoCalculado = regla.impacto * perfilFactor.peso;
                    aceptacionTotalTurno += impactoCalculado;
                }
            }
        }
        return aceptacionActual + aceptacionTotalTurno;
    }

    // LOGICA NIVEL PERFIL
    private void DescubrirNuevoFactor()
    {
        // Encuentra el primer factor en el perfil que AÚN NO esté en el HashSet de descubiertos
        var factorOculto = partidaData.perfilConsumidor
            .FirstOrDefault(p => !subfactoresDescubiertos.Contains(p.idSubfactor));
            
        if (factorOculto != null)
        {
            subfactoresDescubiertos.Add(factorOculto.idSubfactor);
            Debug.Log($"¡Nuevo factor descubierto! ID: {factorOculto.idSubfactor}");
        }
    }

    // LOGICA DE EVENTOS
    private string ProcesarEventoNoticia()
    {
        if (Random.Range(0f, 1f) <= probabilidadEventoNoticia)
        {
            // Ocurre un evento
            var evento = partidaData.eventosPosibles[Random.Range(0, partidaData.eventosPosibles.Count)];
            var efectos = partidaData.efectosEventos.Where(e => e.idEvento == evento.idEvento);

            foreach (var efecto in efectos)
            {
                var factorEnPerfil = perfilActualizado.Find(p => p.idSubfactor == efecto.idSubfactor);
                if (factorEnPerfil != null) 
                {
                    // Modifica el peso del perfil
                    factorEnPerfil.peso += efecto.modificadorPeso;
                    Debug.Log($"Evento afectó a {factorEnPerfil.idSubfactor}, nuevo peso: {factorEnPerfil.peso}");
                }
            }
            return evento.detalleNoticia; // Devuelve la noticia para mostrarla
        }
        return "El mercado está estable."; // No pasa nada
    }


    // FIN DE PARTIDA

    private void GuardarTurnoActual(float aceptacion, string noticia)
    {
        Turno turno = new Turno {
            numeroTurno = turnoActual,
            aceptacionTurno = aceptacion,
            eventoNoticiaOcurrido = noticia,
            decisionesAccion = accionesCompradasEsteTurno.Select(a => a.idAccion).ToList()
        };
        historialTurnos.Add(turno);
    }
    
    private void TerminarPartida(string resultado)
    {
        GameState.resultadoJuego = resultado;
        GameState.ultimoTurno = turnoActual;
        GameState.presupuestoRestante = presupuestoActual;
        GameState.nivelAceptacion = aceptacionActual / 100.0f;
        GameState.nivelPerfil = GetNivelPerfil(); // LOGICA NIVEL PERFIL

        partidaData.historialTurnos = historialTurnos;
        StartCoroutine(GuardarPartidaCoroutine());

        SceneManager.LoadScene("EndGameScene");
    }

    IEnumerator GuardarPartidaCoroutine()
    {
        string jsonBody = JsonUtility.ToJson(partidaData);
        using (UnityWebRequest request = new UnityWebRequest(apiUrl_GuardarPartida, "POST"))
        {
            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError("Error al guardar partida: " + request.error);
            } else {
                Debug.Log("¡Partida guardada exitosamente!");
            }
        }
    }

    // Métodos de carga de escena
    public void LoadScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }
    public void QuitGame() {
        Application.Quit();
    }
}
================================================================================= */