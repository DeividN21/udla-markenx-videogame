using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using MarkenX.Api.Config;
using MarkenX.Api.Dtos;

namespace MarkenX.Api.Services
{
    /// <summary>
    /// Servicio para consumir el endpoint GET /api/v1/scenarios/{id}
    /// Obtiene la configuración completa de un escenario desde el backend.
    /// </summary>
    public class ScenarioApiService : MonoBehaviour
    {
        public static ScenarioApiService Instance { get; private set; }

        private const string SCENARIOS_ENDPOINT = "/scenarios";

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

        #region Public Methods

        /// <summary>
        /// Obtiene un escenario por su ID desde la API.
        /// </summary>
        /// <param name="scenarioId">ID del escenario a obtener</param>
        /// <param name="onSuccess">Callback en caso de éxito</param>
        /// <param name="onError">Callback en caso de error</param>
        public void GetScenarioById(string scenarioId, Action<ScenarioDetailResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(scenarioId))
            {
                onError?.Invoke("El scenarioId no puede estar vacío");
                return;
            }

            StartCoroutine(GetScenarioCoroutine(scenarioId, onSuccess, onError));
        }

        /// <summary>
        /// Obtiene un escenario usando el scenarioId configurado en ApiConfig.
        /// </summary>
        public void GetConfiguredScenario(Action<ScenarioDetailResponse> onSuccess, Action<string> onError)
        {
            if (ApiConfig.Instance == null)
            {
                onError?.Invoke("ApiConfig no está inicializado");
                return;
            }

            string scenarioId = ApiConfig.Instance.ScenarioId;
            GetScenarioById(scenarioId, onSuccess, onError);
        }

        #endregion

        #region Private Methods

        private IEnumerator GetScenarioCoroutine(string scenarioId, Action<ScenarioDetailResponse> onSuccess, Action<string> onError)
        {
            // Verificar si el token está por expirar y solicitar refresh
            if (ApiConfig.Instance.IsTokenExpiringSoon)
            {
                ApiConfig.Instance.RequestTokenRefreshFromReact();
                yield return new WaitForSeconds(1f); // Esperar brevemente por el nuevo token
            }

            // Construir URL: /api/v1/scenarios/{id}
            string endpoint = $"{SCENARIOS_ENDPOINT}/{scenarioId}";
            string url = ApiConfig.Instance.BuildEndpointUrl(endpoint);

            Debug.Log($"[ScenarioApiService] GET {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Configurar timeout
                request.timeout = ApiConfig.Instance.RequestTimeoutSeconds;

                // Configurar headers
                request.SetRequestHeader("Accept", "application/json");

                // Agregar header de autorización si hay token disponible
                if (ApiConfig.Instance.HasValidToken)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {ApiConfig.Instance.GameToken}");
                    Debug.Log("[ScenarioApiService] Usando autenticación Bearer");
                }

                // Enviar request
                yield return request.SendWebRequest();

                // Procesar respuesta
                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProcessSuccessResponse(request, onSuccess, onError);
                }
                else
                {
                    ProcessErrorResponse(request, onError);
                }
            }
        }

        private void ProcessSuccessResponse(UnityWebRequest request, Action<ScenarioDetailResponse> onSuccess, Action<string> onError)
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"[ScenarioApiService] Response: {jsonResponse}");

            try
            {
                ScenarioDetailResponse scenario = JsonUtility.FromJson<ScenarioDetailResponse>(jsonResponse);

                if (scenario == null)
                {
                    onError?.Invoke("La respuesta del servidor no pudo ser parseada");
                    return;
                }

                // Validar datos mínimos
                if (string.IsNullOrEmpty(scenario.id))
                {
                    onError?.Invoke("El escenario recibido no tiene ID válido");
                    return;
                }

                Debug.Log($"[ScenarioApiService] Escenario cargado: {scenario.title} (ID: {scenario.id})");
                Debug.Log($"[ScenarioApiService] Dimensiones: {scenario.dimensions?.Count ?? 0}");
                Debug.Log($"[ScenarioApiService] Acciones: {scenario.actions?.Count ?? 0}");
                Debug.Log($"[ScenarioApiService] Eventos: {scenario.events?.Count ?? 0}");

                onSuccess?.Invoke(scenario);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioApiService] Error parseando JSON: {ex.Message}");
                onError?.Invoke($"Error parseando respuesta: {ex.Message}");
            }
        }

        private void ProcessErrorResponse(UnityWebRequest request, Action<string> onError)
        {
            string errorMessage;

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    errorMessage = $"Error de conexión: {request.error}. Verifique que el servidor esté activo.";
                    break;

                case UnityWebRequest.Result.ProtocolError:
                    errorMessage = GetHttpErrorMessage(request);
                    break;

                case UnityWebRequest.Result.DataProcessingError:
                    errorMessage = $"Error procesando datos: {request.error}";
                    break;

                default:
                    errorMessage = $"Error desconocido: {request.error}";
                    break;
            }

            Debug.LogError($"[ScenarioApiService] {errorMessage}");
            onError?.Invoke(errorMessage);
        }

        private string GetHttpErrorMessage(UnityWebRequest request)
        {
            long statusCode = request.responseCode;
            string responseBody = request.downloadHandler?.text ?? "";

            switch (statusCode)
            {
                case 400:
                    return $"Bad Request (400): Solicitud inválida. {responseBody}";
                case 401:
                    return "Unauthorized (401): No autorizado.";
                case 403:
                    return "Forbidden (403): Acceso denegado.";
                case 404:
                    return $"Not Found (404): El escenario no existe.";
                case 500:
                    return $"Internal Server Error (500): Error en el servidor. {responseBody}";
                case 502:
                    return "Bad Gateway (502): El servidor no está disponible.";
                case 503:
                    return "Service Unavailable (503): Servicio temporalmente no disponible.";
                default:
                    return $"HTTP Error {statusCode}: {request.error}. {responseBody}";
            }
        }

        #endregion
    }
}
