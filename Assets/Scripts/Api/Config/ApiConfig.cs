using UnityEngine;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Runtime.InteropServices;

namespace MarkenX.Api.Config
{
    /// <summary>
    /// Configuración centralizada para la API REST.
    /// Maneja URL base, scenarioId dinámico, tokens de autenticación y configuración de entorno.
    /// </summary>
    public class ApiConfig : MonoBehaviour
    {
        public static ApiConfig Instance { get; private set; }

        [Header("Configuración de API")]
        [Tooltip("URL base de la API (sin trailing slash)")]
        [SerializeField] private string baseUrl = "http://localhost:8080";

        [Tooltip("Context path de la API")]
        [SerializeField] private string contextPath = "/api/v1";

        [Header("Configuración de Escenario")]
        [Tooltip("ID del escenario por defecto (usado si no se encuentra otra fuente)")]
        [SerializeField] private string defaultScenarioId = "";

        [Tooltip("ID del estudiante por defecto")]
        [SerializeField] private string defaultStudentId = "";

        [Header("Configuración de Red")]
        [Tooltip("Timeout para requests HTTP en segundos")]
        [SerializeField] private int requestTimeoutSeconds = 30;

        // Valores resueltos en runtime
        private string _resolvedScenarioId;
        private string _resolvedStudentId;
        private string _resolvedTaskId;
        private bool _configLoaded = false;

        // Token de autenticación (para integración con React/BFF)
        private string _gameToken;
        private int _tokenExpiresIn;
        private DateTime _tokenExpiresAt;

        // Eventos
        public event Action OnConfigLoaded;
        public event Action<string> OnTokenRefreshed;

        // Plugin JavaScript para comunicación WebGL
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void RequestTokenRefresh();
#endif

        #region Properties

        public string BaseUrl => baseUrl;
        public string ContextPath => contextPath;
        public string FullApiUrl => $"{baseUrl}{contextPath}";
        public int RequestTimeoutSeconds => requestTimeoutSeconds;

        public string ScenarioId => _resolvedScenarioId ?? defaultScenarioId;
        public string StudentId => _resolvedStudentId ?? defaultStudentId;
        public string TaskId => _resolvedTaskId ?? "";
        public bool IsConfigLoaded => _configLoaded;

        /// <summary>
        /// Token JWT para autenticación con el BFF.
        /// Null si no se proporcionó (modo desarrollo/simulación).
        /// </summary>
        public string GameToken => _gameToken;

        /// <summary>
        /// Indica si hay un token válido disponible.
        /// </summary>
        public bool HasValidToken => !string.IsNullOrEmpty(_gameToken);

        /// <summary>
        /// Indica si el token está próximo a expirar (menos de 2 minutos).
        /// </summary>
        public bool IsTokenExpiringSoon => HasValidToken && DateTime.Now > _tokenExpiresAt.AddMinutes(-2);

        /// <summary>
        /// Indica si estamos en modo producción (WebGL compilado con token válido).
        /// En este modo se usa la API real con autenticación.
        /// </summary>
        public bool IsProductionMode
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return HasValidToken;
#else
                return false;
#endif
            }
        }

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            StartCoroutine(LoadConfigurationCoroutine());
        }

        #endregion

        #region Configuration Loading

        /// <summary>
        /// Carga la configuración en el siguiente orden de prioridad:
        /// 1. Query params (WebGL)
        /// 2. Argumentos de línea de comando (Desktop)
        /// 3. Archivo config.json
        /// 4. Valores por defecto del Inspector
        /// </summary>
        private IEnumerator LoadConfigurationCoroutine()
        {
            // 1. Intentar Query Params (WebGL)
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                LoadFromQueryParams();
            }

            // 2. Intentar argumentos de línea de comando (Desktop)
            if (!HasValidScenarioId())
            {
                LoadFromCommandLineArgs();
            }

            // 3. Intentar archivo config.json
            if (!HasValidScenarioId())
            {
                yield return LoadFromConfigFile();
            }

            // 4. Usar valores por defecto si no se encontró nada
            if (!HasValidScenarioId())
            {
                _resolvedScenarioId = defaultScenarioId;
                _resolvedStudentId = defaultStudentId;
                Debug.LogWarning("[ApiConfig] Usando configuración por defecto del Inspector");
            }

            _configLoaded = true;
            LogConfiguration();
            OnConfigLoaded?.Invoke();
        }

        /// <summary>
        /// WebGL: Lee parámetros de la URL del navegador.
        /// Ejemplo: https://game.com/?scenarioId=abc123&studentId=xyz789&gameToken=jwt...
        /// </summary>
        private void LoadFromQueryParams()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string url = Application.absoluteURL;

            _resolvedScenarioId = GetQueryParam(url, "scenarioId");
            _resolvedStudentId = GetQueryParam(url, "studentId");
            _resolvedTaskId = GetQueryParam(url, "taskId");

            // También permite cargar baseUrl desde query params
            string customApiUrl = GetQueryParam(url, "apiUrl");
            if (!string.IsNullOrEmpty(customApiUrl))
            {
                baseUrl = Uri.UnescapeDataString(customApiUrl);
            }

            // Cargar token de autenticación si está presente
            _gameToken = GetQueryParam(url, "gameToken");
            string tokenExpiresInStr = GetQueryParam(url, "tokenExpiresIn");
            if (!string.IsNullOrEmpty(tokenExpiresInStr) && int.TryParse(tokenExpiresInStr, out int expiresIn))
            {
                _tokenExpiresIn = expiresIn;
                _tokenExpiresAt = DateTime.Now.AddSeconds(expiresIn);
            }
            else
            {
                _tokenExpiresIn = 600; // Default: 10 minutos
                _tokenExpiresAt = DateTime.Now.AddSeconds(600);
            }

            if (HasValidScenarioId())
            {
                Debug.Log("[ApiConfig] Configuración cargada desde Query Params");
                if (HasValidToken)
                {
                    Debug.Log($"[ApiConfig] Token de autenticación presente. Expira en: {_tokenExpiresIn}s");
                }
            }
#endif
        }

        /// <summary>
        /// Desktop: Lee argumentos de línea de comando.
        /// Ejemplo: Game.exe --scenarioId=abc123 --studentId=xyz789
        /// </summary>
        private void LoadFromCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                if (arg.StartsWith("--scenarioId="))
                    _resolvedScenarioId = arg.Substring("--scenarioId=".Length);
                else if (arg.StartsWith("--studentId="))
                    _resolvedStudentId = arg.Substring("--studentId=".Length);
                else if (arg.StartsWith("--taskId="))
                    _resolvedTaskId = arg.Substring("--taskId=".Length);
                else if (arg.StartsWith("--apiUrl="))
                    baseUrl = arg.Substring("--apiUrl=".Length);
            }

            if (HasValidScenarioId())
            {
                Debug.Log("[ApiConfig] Configuración cargada desde argumentos de línea de comando");
            }
        }

        /// <summary>
        /// Lee configuración desde StreamingAssets/config.json
        /// </summary>
        private IEnumerator LoadFromConfigFile()
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");

            // En WebGL usamos UnityWebRequest, en otras plataformas File.Exists
            if (Application.platform == RuntimePlatform.WebGLPlayer ||
                Application.platform == RuntimePlatform.Android)
            {
                using (UnityWebRequest request = UnityWebRequest.Get(configPath))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        ParseConfigJson(request.downloadHandler.text);
                        Debug.Log("[ApiConfig] Configuración cargada desde config.json (WebRequest)");
                    }
                }
            }
            else
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    ParseConfigJson(json);
                    Debug.Log("[ApiConfig] Configuración cargada desde config.json");
                }
            }
        }

        private void ParseConfigJson(string json)
        {
            try
            {
                ConfigFileData config = JsonUtility.FromJson<ConfigFileData>(json);

                if (!string.IsNullOrEmpty(config.scenarioId))
                    _resolvedScenarioId = config.scenarioId;
                if (!string.IsNullOrEmpty(config.studentId))
                    _resolvedStudentId = config.studentId;
                if (!string.IsNullOrEmpty(config.taskId))
                    _resolvedTaskId = config.taskId;
                if (!string.IsNullOrEmpty(config.apiUrl))
                    baseUrl = config.apiUrl;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ApiConfig] Error parseando config.json: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        private bool HasValidScenarioId()
        {
            return !string.IsNullOrEmpty(_resolvedScenarioId);
        }

        private string GetQueryParam(string url, string paramName)
        {
            if (string.IsNullOrEmpty(url)) return null;

            int queryStart = url.IndexOf('?');
            if (queryStart < 0) return null;

            string query = url.Substring(queryStart + 1);
            string[] pairs = query.Split('&');

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2 && keyValue[0] == paramName)
                {
                    return Uri.UnescapeDataString(keyValue[1]);
                }
            }

            return null;
        }

        private void LogConfiguration()
        {
            Debug.Log($"[ApiConfig] === Configuración Final ===");
            Debug.Log($"[ApiConfig] API URL: {FullApiUrl}");
            Debug.Log($"[ApiConfig] Scenario ID: {ScenarioId}");
            Debug.Log($"[ApiConfig] Student ID: {StudentId}");
            Debug.Log($"[ApiConfig] Task ID: {TaskId}");
            Debug.Log($"[ApiConfig] Timeout: {RequestTimeoutSeconds}s");
            Debug.Log($"[ApiConfig] Token presente: {HasValidToken}");
            Debug.Log($"[ApiConfig] Modo producción: {IsProductionMode}");
        }

        /// <summary>
        /// Construye la URL completa para un endpoint.
        /// </summary>
        public string BuildEndpointUrl(string endpoint)
        {
            // Asegurar que el endpoint empiece con /
            if (!endpoint.StartsWith("/"))
                endpoint = "/" + endpoint;

            return $"{FullApiUrl}{endpoint}";
        }

        /// <summary>
        /// Permite sobrescribir el scenarioId en runtime (útil para testing).
        /// </summary>
        public void SetScenarioId(string scenarioId)
        {
            _resolvedScenarioId = scenarioId;
            Debug.Log($"[ApiConfig] ScenarioId actualizado a: {scenarioId}");
        }

        /// <summary>
        /// Permite sobrescribir el studentId en runtime.
        /// </summary>
        public void SetStudentId(string studentId)
        {
            _resolvedStudentId = studentId;
        }

        #endregion

        #region Token Management

        /// <summary>
        /// Solicita a React que refresque el token vía postMessage.
        /// Solo funciona en WebGL compilado.
        /// </summary>
        public void RequestTokenRefreshFromReact()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[ApiConfig] Solicitando refresh de token a React...");
            RequestTokenRefresh();
#else
            Debug.Log("[ApiConfig] Token refresh solicitado (modo editor - ignorado)");
#endif
        }

        /// <summary>
        /// Llamado desde JavaScript cuando React proporciona un nuevo token.
        /// El parámetro es un JSON string con formato: {"token":"...", "expiresIn": 600}
        /// </summary>
        public void OnTokenRefreshedFromJS(string jsonData)
        {
            try
            {
                var tokenData = JsonUtility.FromJson<TokenRefreshData>(jsonData);
                if (!string.IsNullOrEmpty(tokenData.token))
                {
                    _gameToken = tokenData.token;
                    _tokenExpiresIn = tokenData.expiresIn > 0 ? tokenData.expiresIn : 600;
                    _tokenExpiresAt = DateTime.Now.AddSeconds(_tokenExpiresIn);

                    Debug.Log($"[ApiConfig] Token refrescado exitosamente. Nuevo expira en: {_tokenExpiresIn}s");
                    OnTokenRefreshed?.Invoke(_gameToken);
                }
                else
                {
                    Debug.LogWarning("[ApiConfig] Token refresh recibido pero el token está vacío");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ApiConfig] Error parseando token refresh: {ex.Message}");
            }
        }

        [Serializable]
        private class TokenRefreshData
        {
            public string token;
            public int expiresIn;
        }

        #endregion

        #region Config File Data

        [Serializable]
        private class ConfigFileData
        {
            public string apiUrl;
            public string scenarioId;
            public string studentId;
            public string taskId;
        }

        #endregion
    }
}
