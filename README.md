# MarkenX - Videojuego Educativo

## Descripción
Videojuego educativo desarrollado en Unity que simula escenarios de marketing para enseñar conducta del consumidor. El juego se integra con una API REST en Spring Boot para cargar escenarios dinámicamente y registrar métricas de las sesiones de juego.

## Requisitos
- **Unity**: 2022+ (probado en Unity 6000.2.10f1)
- **Backend**: API REST Spring Boot con context-path `/api/v1`
- **.NET**: Compatible con Unity (Mono o IL2CPP)

## Estructura del Proyecto

```
udla-markenx-videogame/
├── Assets/
│   ├── Scripts/
│   │   ├── Api/                    # Integración con API REST
│   │   │   ├── Config/
│   │   │   │   └── ApiConfig.cs    # Configuración centralizada
│   │   │   ├── Dtos/
│   │   │   │   ├── ScenarioApiDtos.cs      # DTOs para GET /scenarios
│   │   │   │   └── GameSessionApiDtos.cs   # DTOs para POST /game-sessions
│   │   │   ├── Mappers/
│   │   │   │   └── ScenarioDataMapper.cs   # Mapeo API → modelos internos
│   │   │   ├── Services/
│   │   │   │   ├── ScenarioApiService.cs   # Servicio GET escenarios
│   │   │   │   └── GameSessionApiService.cs # Servicio POST sesiones
│   │   │   └── ApiServicesInitializer.cs   # Inicializador de servicios
│   │   └── Managers/
│   │       ├── GameSceneManager.cs  # Manager principal del juego
│   │       ├── GameUIManager.cs     # Manager de UI
│   │       ├── GameState.cs         # Estado entre escenas
│   │       └── ApiDataModels.cs     # Modelos internos y mocks
│   ├── StreamingAssets/
│   │   └── config.json              # Configuración externa
│   └── Scenes/
│       ├── MainMenu.unity
│       ├── GameScene.unity
│       └── EndGameScene.unity
├── Packages/
│   └── com.markenx.domain/          # Modelos de dominio DDD
└── Docs/                            # Documentación teórica
```

---

## Integración con API REST

### Endpoints Utilizados

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/v1/scenarios/{id}` | Obtiene la configuración de un escenario |
| POST | `/api/v1/game-sessions` | Registra una sesión de juego |

### Configuración del Endpoint Base

La URL base de la API se configura de múltiples formas (en orden de prioridad):

1. **Query Params (WebGL)**: `?apiUrl=http://servidor:8080`
2. **Argumentos de línea de comando**: `--apiUrl=http://servidor:8080`
3. **Archivo config.json**: `StreamingAssets/config.json`
4. **Inspector de Unity**: En el componente `ApiConfig`

**Ejemplo config.json:**
```json
{
  "apiUrl": "http://localhost:8080",
  "scenarioId": "abc123",
  "studentId": "student001",
  "taskId": "task001"
}
```

---

## Estrategias para ScenarioId Dinámico

El sistema soporta múltiples formas de especificar el `scenarioId` sin recompilar el juego:

### 1. Query Parameters (WebGL) - **RECOMENDADA para Web**

```
https://tu-servidor.com/juego/?scenarioId=abc123&studentId=xyz789
```

| Pros | Contras |
|------|---------|
| Fácil integración con LMS | Solo funciona en WebGL |
| No requiere archivos adicionales | IDs visibles en URL |
| Perfecto para enlaces dinámicos | Límite de longitud de URL |

**Caso de uso**: Integración con plataformas educativas (Moodle, Canvas, etc.)

### 2. Argumentos de Línea de Comando (Desktop)

```bash
MarkenX.exe --scenarioId=abc123 --studentId=xyz789 --apiUrl=http://api.example.com
```

| Pros | Contras |
|------|---------|
| Flexible para automatización | Solo Desktop |
| Permite scripts de lanzamiento | Requiere acceso a terminal |
| Fácil testing | No amigable para usuarios finales |

**Caso de uso**: Laboratorios de computación, ejecución automatizada, CI/CD.

### 3. Archivo config.json (Multiplataforma) - **RECOMENDADA para Desktop**

Ubicación: `StreamingAssets/config.json`

```json
{
  "apiUrl": "http://localhost:8080",
  "scenarioId": "escenario-marketing-001",
  "studentId": "alumno-001",
  "taskId": "tarea-001"
}
```

| Pros | Contras |
|------|---------|
| Funciona en todas las plataformas | Requiere modificar archivo |
| Fácil de editar | Puede ser sobrescrito en actualizaciones |
| Persiste entre sesiones | Acceso a sistema de archivos necesario |

**Caso de uso**: Instalaciones en laboratorio, distribución en USB, configuración por institución.

### 4. PlayerPrefs

Guardado en registro del sistema/archivo local de Unity.

| Pros | Contras |
|------|---------|
| Persiste entre sesiones | Requiere UI para configurar |
| No necesita archivos externos | Diferente ubicación por plataforma |
| Fácil de implementar | No fácil de pre-configurar |

**Caso de uso**: Configuración inicial por usuario, recordar última sesión.

### 5. Variables de Entorno

```bash
export MARKENX_SCENARIO_ID=abc123
export MARKENX_API_URL=http://api.example.com
./MarkenX
```

| Pros | Contras |
|------|---------|
| Estándar en servidores | Configuración técnica |
| Bueno para contenedores | No funciona en WebGL |
| Separación de config y código | Complejidad para usuarios |

**Caso de uso**: Despliegue en contenedores Docker, servidores de juegos.

### Recomendación

| Plataforma | Estrategia Recomendada |
|------------|----------------------|
| **WebGL** | Query Parameters |
| **Windows/Mac/Linux** | config.json + Argumentos CLI |
| **Contenedores** | Variables de Entorno |

---

## Flujo de Integración Unity ↔ API

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   MainMenu      │     │   GameScene      │     │  EndGameScene   │
│                 │     │                  │     │                 │
│ 1. Cargar       │────►│ 4. Gameplay      │────►│ 7. Mostrar      │
│    ApiConfig    │     │    por turnos    │     │    resultados   │
│                 │     │                  │     │                 │
│ 2. GET scenario │     │ 5. Registrar     │     │                 │
│    /api/v1/     │     │    turnos en     │     │                 │
│    scenarios/   │     │    historial     │     │                 │
│    {id}         │     │                  │     │                 │
│                 │     │ 6. POST session  │     │                 │
│ 3. Mapear a     │     │    /api/v1/      │     │                 │
│    PartidaData  │     │    game-sessions │     │                 │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

### Detalle del Flujo

1. **Inicio del juego** (`MainMenu`)
   - `ApiServicesInitializer` crea los servicios de API
   - `ApiConfig` carga configuración (config.json, query params, etc.)

2. **Obtención del escenario** (`GameSceneManager.IniciarPartida`)
   - `ScenarioApiService.GetScenarioById()` hace GET al backend
   - `ScenarioDataMapper` convierte `ScenarioDetailResponse` a `PartidaDataPayload`

3. **Inicialización del juego**
   - Se cargan dimensiones, acciones y eventos del escenario
   - Se inicializa presupuesto, aceptación y árbol de habilidades

4. **Gameplay por turnos**
   - Jugador compra acciones y termina turnos
   - Cada turno se registra en `historialTurnosApi`

5. **Fin de la partida**
   - `GameSessionApiService.RegisterGameSession()` envía POST
   - Backend calcula `finalOutcome` (GANASTE/PERDISTE)

---

## Ejemplo de Payload POST

### Request: POST /api/v1/game-sessions

```json
{
  "taskId": "escenario-001",
  "studentId": "alumno-123",
  "sessionDate": "2026-01-07T15:30:45.123Z",
  "finalAcceptance": 0.85,
  "remainingBudget": 250.0,
  "totalTurnsUsed": 5,
  "profileDiscoveryPercentage": 0.75,
  "history": [
    {
      "turnNumber": 1,
      "acceptanceAtEnd": 0.15,
      "budgetAtEnd": 850.0,
      "eventOccurredTitle": "",
      "actionsTakenIds": ["action-001", "action-002"]
    },
    {
      "turnNumber": 2,
      "acceptanceAtEnd": 0.45,
      "budgetAtEnd": 600.0,
      "eventOccurredTitle": "El mundo se vuelve más verde",
      "actionsTakenIds": ["action-003"]
    }
  ]
}
```

### Response (201 Created)

```json
{
  "id": "session-uuid-12345",
  "taskId": "escenario-001",
  "studentId": "alumno-123",
  "sessionDate": "2026-01-07T15:30:45.123Z",
  "finalAcceptance": 0.85,
  "remainingBudget": 250.0,
  "totalTurnsUsed": 5,
  "profileDiscoveryPercentage": 0.75,
  "finalOutcome": "GANASTE",
  "history": [...]
}
```

---

## Consideraciones WebGL

### CORS
El backend debe configurar CORS para permitir requests desde el dominio donde se aloja el juego:

```java
@Configuration
public class CorsConfig {
    @Bean
    public WebMvcConfigurer corsConfigurer() {
        return new WebMvcConfigurer() {
            @Override
            public void addCorsMappings(CorsRegistry registry) {
                registry.addMapping("/api/**")
                    .allowedOrigins("https://tu-dominio-juego.com")
                    .allowedMethods("GET", "POST", "OPTIONS")
                    .allowedHeaders("*");
            }
        };
    }
}
```

### Limitaciones WebGL
- No soporta archivos locales (config.json se carga vía HTTP)
- Threading limitado (usar Coroutines)
- No soporta `System.IO.File` directamente

### Pruebas Locales WebGL
Para probar localmente con query params:
```
http://localhost:8000/index.html?scenarioId=test&apiUrl=http://localhost:8080
```

---

## Errores Comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `Connection Error` | Backend no disponible | Verificar que el servidor esté corriendo |
| `404 Not Found` | ScenarioId inválido | Verificar que el escenario existe en la BD |
| `CORS Error` (WebGL) | Backend no permite origen | Configurar CORS en Spring Boot |
| `Timeout` | Red lenta o servidor saturado | Aumentar `RequestTimeoutSeconds` en ApiConfig |
| `JSON Parse Error` | Respuesta malformada | Verificar estructura de DTOs |

---

## Modo Simulación (Offline)

Para desarrollo sin backend:

1. En `GameSceneManager`, activar `usarModoSimulacion = true`
2. El juego usará `MockDataFactory.GetMockData()`
3. No se enviarán datos al backend

---

## Requisitos del Backend

El backend debe implementar los siguientes contratos:

### GET /api/v1/scenarios/{id}
- Retornar `ScenarioDetailResponse` con todas las relaciones cargadas
- Manejar 404 si el escenario no existe

### POST /api/v1/game-sessions
- Aceptar `RegisterGameSessionRequest`
- Calcular `finalOutcome` basado en `finalAcceptance` y reglas de negocio
- Retornar `GameSessionResponse` con código 201

---

## Cómo Ejecutar

### Desarrollo (Modo Simulación)
1. Abrir proyecto en Unity
2. Asegurar `usarModoSimulacion = true` en `GameSceneManager`
3. Play en el Editor

### Producción (Con API)
1. Configurar `config.json` con la URL del backend
2. Desactivar `usarModoSimulacion` en `GameSceneManager`
3. Build para la plataforma deseada

### WebGL
1. Build WebGL
2. Desplegar en servidor web
3. Acceder con: `https://servidor/juego/?scenarioId=ID&studentId=STUDENT`

---

## Licencia

Proyecto educativo - UDLA Ecuador
