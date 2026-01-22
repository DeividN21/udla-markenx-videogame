using UnityEngine;
using MarkenX.Api.Config;
using MarkenX.Api.Services;

namespace MarkenX.Api
{
    /// <summary>
    /// Inicializador de servicios API.
    /// Debe colocarse en un GameObject de la escena inicial (MainMenu).
    /// Crea y configura todos los servicios necesarios para la integración con el backend.
    /// </summary>
    public class ApiServicesInitializer : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Si está activo, los servicios se inicializan automáticamente")]
        [SerializeField] private bool autoInitialize = true;

        [Header("Referencias (Opcionales)")]
        [Tooltip("Prefab de ApiConfig. Si es null, se crea uno por defecto")]
        [SerializeField] private GameObject apiConfigPrefab;

        private static bool _initialized = false;

        void Awake()
        {
            if (_initialized)
            {
                Destroy(gameObject);
                return;
            }

            if (autoInitialize)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Inicializa todos los servicios de API.
        /// Puede llamarse manualmente si autoInitialize está desactivado.
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                Debug.Log("[ApiServicesInitializer] Los servicios ya fueron inicializados");
                return;
            }

            Debug.Log("[ApiServicesInitializer] Inicializando servicios de API...");

            // Crear contenedor para los servicios
            GameObject servicesContainer = new GameObject("[API Services]");
            DontDestroyOnLoad(servicesContainer);

            // Agregar ApiConfig
            if (ApiConfig.Instance == null)
            {
                if (apiConfigPrefab != null)
                {
                    Instantiate(apiConfigPrefab, servicesContainer.transform);
                }
                else
                {
                    servicesContainer.AddComponent<ApiConfig>();
                }
            }

            // Agregar ScenarioApiService
            if (ScenarioApiService.Instance == null)
            {
                servicesContainer.AddComponent<ScenarioApiService>();
            }

            // Agregar GameSessionApiService
            if (GameSessionApiService.Instance == null)
            {
                servicesContainer.AddComponent<GameSessionApiService>();
            }

            _initialized = true;
            Debug.Log("[ApiServicesInitializer] Servicios de API inicializados correctamente");
        }

        /// <summary>
        /// Resetea el estado de inicialización (útil para testing).
        /// </summary>
        public static void ResetInitialization()
        {
            _initialized = false;
        }
    }
}
