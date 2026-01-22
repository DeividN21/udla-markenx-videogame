using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MarkenX.Api.Config;
using MarkenX.Api.Dtos;

namespace MarkenX.Api.Services
{
    /// <summary>
    /// Servicio para consumir el endpoint POST /api/v1/game-sessions
    /// Registra las métricas de una sesión de juego en el backend.
    /// </summary>
    public class GameSessionApiService : MonoBehaviour
    {
        public static GameSessionApiService Instance { get; private set; }

        private const string GAME_SESSIONS_ENDPOINT = "/attempts";

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
        /// Registra una sesión de juego completa en el backend.
        /// </summary>
        /// <param name="request">Datos de la sesión a registrar</param>
        /// <param name="onSuccess">Callback en caso de éxito (recibe la respuesta con finalOutcome)</param>
        /// <param name="onError">Callback en caso de error</param>
        public void RegisterGameSession(RegisterGameSessionRequest request, Action<GameSessionResponse> onSuccess, Action<string> onError)
        {
            if (request == null)
            {
                onError?.Invoke("El request no puede ser null");
                return;
            }

            if (string.IsNullOrEmpty(request.taskId))
            {
                onError?.Invoke("El taskId es requerido");
                return;
            }

            if (string.IsNullOrEmpty(request.studentId))
            {
                onError?.Invoke("El studentId es requerido");
                return;
            }

            StartCoroutine(PostGameSessionCoroutine(request, onSuccess, onError));
        }

        /// <summary>
        /// Crea un request con los datos actuales del juego.
        /// </summary>
        public RegisterGameSessionRequest CreateRequestFromGameState(
            string taskId,
            string studentId,
            float finalAcceptance,
            float remainingBudget,
            int totalTurnsUsed,
            float profileDiscoveryPercentage,
            List<TurnHistoryDto> history)
        {
            return new RegisterGameSessionRequest
            {
                taskId = taskId,
                studentId = studentId,
                sessionDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                finalAcceptance = finalAcceptance,
                remainingBudget = remainingBudget,
                totalTurnsUsed = totalTurnsUsed,
                profileDiscoveryPercentage = profileDiscoveryPercentage,
                history = history ?? new List<TurnHistoryDto>()
            };
        }

        #endregion

        #region Private Methods

        private IEnumerator PostGameSessionCoroutine(RegisterGameSessionRequest request, Action<GameSessionResponse> onSuccess, Action<string> onError)
        {
            // Verificar si el token está por expirar y solicitar refresh
            if (ApiConfig.Instance.IsTokenExpiringSoon)
            {
                ApiConfig.Instance.RequestTokenRefreshFromReact();
                yield return new WaitForSeconds(1f); // Esperar brevemente por el nuevo token
            }

            // Construir URL: /api/v1/game-sessions
            string url = ApiConfig.Instance.BuildEndpointUrl(GAME_SESSIONS_ENDPOINT);

            // Serializar request a JSON
            string jsonBody = JsonUtility.ToJson(request);
            Debug.Log($"[GameSessionApiService] POST {url}");
            Debug.Log($"[GameSessionApiService] Body: {jsonBody}");

            // Crear request
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                // Configurar body
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                // Configurar headers
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Accept", "application/json");

                // Agregar header de autorización si hay token disponible
                if (ApiConfig.Instance.HasValidToken)
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {ApiConfig.Instance.GameToken}");
                    Debug.Log("[GameSessionApiService] Usando autenticación Bearer");
                }

                // Configurar timeout
                webRequest.timeout = ApiConfig.Instance.RequestTimeoutSeconds;

                // Enviar request
                yield return webRequest.SendWebRequest();

                // Procesar respuesta
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    ProcessSuccessResponse(webRequest, onSuccess, onError);
                }
                else
                {
                    ProcessErrorResponse(webRequest, onError);
                }
            }
        }

        private void ProcessSuccessResponse(UnityWebRequest request, Action<GameSessionResponse> onSuccess, Action<string> onError)
        {
            // Verificar código 201 Created
            if (request.responseCode != 201 && request.responseCode != 200)
            {
                Debug.LogWarning($"[GameSessionApiService] Código inesperado: {request.responseCode} (esperado: 201)");
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"[GameSessionApiService] Response ({request.responseCode}): {jsonResponse}");

            try
            {
                GameSessionResponse response = JsonUtility.FromJson<GameSessionResponse>(jsonResponse);

                if (response == null)
                {
                    onError?.Invoke("La respuesta del servidor no pudo ser parseada");
                    return;
                }

                Debug.Log($"[GameSessionApiService] Sesión registrada con ID: {response.id}");
                Debug.Log($"[GameSessionApiService] Resultado calculado por backend: {response.finalOutcome}");

                onSuccess?.Invoke(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSessionApiService] Error parseando JSON: {ex.Message}");
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

            Debug.LogError($"[GameSessionApiService] {errorMessage}");
            onError?.Invoke(errorMessage);
        }

        private string GetHttpErrorMessage(UnityWebRequest request)
        {
            long statusCode = request.responseCode;
            string responseBody = request.downloadHandler?.text ?? "";

            switch (statusCode)
            {
                case 400:
                    return $"Bad Request (400): Datos de sesión inválidos. {responseBody}";
                case 401:
                    return "Unauthorized (401): No autorizado.";
                case 403:
                    return "Forbidden (403): Acceso denegado.";
                case 404:
                    return "Not Found (404): El endpoint no existe. Verifique la URL.";
                case 409:
                    return $"Conflict (409): La sesión ya existe. {responseBody}";
                case 422:
                    return $"Unprocessable Entity (422): Datos inválidos. {responseBody}";
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
