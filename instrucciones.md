"""
SCRIPT / PROMPT PARA CLAUDE
Integración Unity (C#) ↔ API REST (Spring Boot)

================================================================
ROL
================================================================
Actúa como Senior Unity Developer + Backend Integrator, con
experiencia en:
- Unity (C#)
- Arquitectura cliente-servidor
- Integración REST (GET / POST)
- Clean Architecture
- Manejo de builds compilados (WebGL / Desktop)
- Documentación técnica (README)

================================================================
CONTEXTO DEL SISTEMA
================================================================
Estoy desarrollando un videojuego en Unity (C#) que debe integrarse
con una API REST en Spring Boot.

La API tiene context-path obligatorio:

/api/v1

Todos los endpoints comienzan con ese prefijo.

================================================================
OBJETIVO GENERAL
================================================================
Integrar el videojuego con la API para que:

1. Cargue la configuración del escenario por medio de un GET
2. Registre métricas de una sesión de juego por medio de un POST
3. Se reemplacen o validen los mocks actuales del videojuego
4. La integración sea robusta, desacoplada y documentada
5. El juego ya compilado pueda recibir dinámicamente el ID del escenario

================================================================
ENDPOINTS DISPONIBLES
================================================================

----------------
GET Escenario
----------------
GET /api/v1/scenarios/{id}

Controlador:
@GetMapping("/{id}")
public ResponseEntity<ScenarioDetailResponse> getById(@PathVariable String id) {
    var response = scenarioQueryUseCase.getById(new GetScenarioByIdQuery(id));
    return ResponseEntity.ok(response);
}

Respuesta (ScenarioDetailResponse):
{
  "id": "string",
  "title": "string",
  "description": "string",
  "consumer": {
    "id": "string",
    "name": "string",
    "age": 0,
    "budget": 0.0,
    "targetAcceptanceScore": 0.1
  },
  "dimensions": [
    {
      "id": "string",
      "name": "string",
      "displayName": "string",
      "description": "string",
      "consumerExpectation": 0.1,
      "productInitialOffer": 0.1
    }
  ],
  "actions": [
    {
      "id": "string",
      "name": "string",
      "description": "string",
      "cost": 0.0,
      "category": "string",
      "isInitiallyLocked": true,
      "prerequisiteActionId": "string",
      "effects": [
        {
          "dimensionId": "string",
          "delta": 0.1
        }
      ]
    }
  ],
  "events": [
    {
      "id": "string",
      "title": "string",
      "description": "string",
      "effects": [
        {
          "dimensionId": "string",
          "weightMultiplier": 0.1
        }
      ]
    }
  ]
}

----------------
POST Sesión de Juego
----------------
POST /api/v1/game-sessions

Controlador:
@PostMapping
@ResponseStatus(HttpStatus.CREATED)
public GameSessionResponseDTO registerGameSession(
    @RequestBody RegisterGameSessionRequestDTO dto
)

REQUEST CORREGIDO:
- Sin finalOutcome (se calcula en backend)
- BigDecimal representado como decimal
- ISO-8601 válido

{
  "taskId": "string",
  "studentId": "string",
  "sessionDate": "2026-01-07T13:12:15.568Z",
  "finalAcceptance": 0.1,
  "remainingBudget": 0.0,
  "totalTurnsUsed": 0,
  "profileDiscoveryPercentage": 0.1,
  "history": [
    {
      "turnNumber": 1,
      "acceptanceAtEnd": 0.1,
      "budgetAtEnd": 0.0,
      "eventOccurredTitle": "string",
      "actionsTakenIds": [
        "string"
      ]
    }
  ]
}

RESPONSE (GameSessionResponseDTO):
{
  "id": "string",
  "taskId": "string",
  "studentId": "string",
  "sessionDate": "2026-01-07T13:12:15.568Z",
  "finalAcceptance": 0.1,
  "remainingBudget": 0.0,
  "totalTurnsUsed": 0,
  "profileDiscoveryPercentage": 0.1,
  "finalOutcome": "GANASTE | PERDISTE",
  "history": [ ... ]
}

================================================================
TAREAS A REALIZAR
================================================================

A) Analizar el estado actual del videojuego
- Asumir uso de mocks locales
- Identificar qué estructuras coinciden con la API
- Proponer refactorización o eliminación
- Definir modelos C# alineados 1:1 con los DTOs

B) Modelado en Unity (C#)
- DTOs C# para ScenarioDetailResponse
- DTOs C# para GameSessionResponseDTO
- Servicios:
  - ScenarioApiService
  - GameSessionApiService
- Uso de UnityWebRequest
- Manejo de errores HTTP, timeouts y logging

C) Implementación de llamadas HTTP
- Construcción correcta del endpoint usando /api/v1
- Serialización / deserialización JSON
- Manejo de respuestas 200 y 201

D) Manejo dinámico del scenarioId en juego compilado
Presentar al menos 3 alternativas:
- Archivo config.json
- Argumentos de ejecución
- Query params (WebGL)
- PlayerPrefs
- Variables de entorno

Para cada alternativa:
- Pros
- Contras
- Casos de uso

Seleccionar una como recomendada y justificar técnicamente.

E) Flujo completo del juego
1. Inicio del juego
2. Obtención del escenario (GET)
3. Inicialización de dimensiones, acciones y eventos
4. Gameplay por turnos
5. Fin de la partida
6. Registro de métricas (POST)

F) README.md
Generar un README.md que incluya:
- Propósito del proyecto
- Configuración del endpoint base
- Cómo se define el scenarioId
- Flujo de integración Unity ↔ API
- Ejemplo de payload POST
- Consideraciones WebGL
- Errores comunes
- Requisitos del backend

================================================================
RESTRICCIONES
================================================================
- No inventar endpoints
- No modificar la estructura de los DTOs
- No usar librerías externas en Unity
- Mantener Clean Code y desacoplamiento
- El resultado debe ser directamente implementable

================================================================
FORMATO DE SALIDA ESPERADO
================================================================
1. Arquitectura propuesta
2. Modelos C#
3. Servicios HTTP
4. Estrategia para scenarioId
5. Flujo del juego
6. README.md

RESULTADO ESPERADO:
- Integración real con la API
- Eliminación de mocks
- Cambio de escenario sin recompilar
- Documentación clara y completa
"""
