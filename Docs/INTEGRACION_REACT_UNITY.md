# Guía de Integración: React + Unity WebGL

## Índice

1. [Análisis del Problema](#1-análisis-del-problema)
2. [Arquitectura Propuesta](#2-arquitectura-propuesta)
3. [Estrategia Técnica Recomendada](#3-estrategia-técnica-recomendada)
4. [Ejemplos Concretos](#4-ejemplos-concretos)
5. [Consideraciones Importantes](#5-consideraciones-importantes)
6. [Flujo Completo Paso a Paso](#6-flujo-completo-paso-a-paso)
7. [Recomendaciones Finales](#7-recomendaciones-finales)

---

## 1. Análisis del Problema

### 1.1 Por qué no es trivial

Integrar un videojuego Unity WebGL con una aplicación React presenta varios desafíos:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PROBLEMA CENTRAL                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│  React tiene datos dinámicos (taskId, studentId) que Unity necesita         │
│  PERO Unity ya está compilado como un paquete WebGL estático                │
│                                                                              │
│  React (runtime) ─────?────▶ Unity (compilado estático)                     │
│                                                                              │
│  ¿Cómo pasamos datos de una app dinámica a un binario compilado?            │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Puntos críticos:**
- Unity WebGL se compila a WebAssembly (.wasm) + JavaScript
- Los valores NO pueden estar "bakeados" en el código
- El juego necesita los IDs ANTES de hacer su primera llamada a la API
- Diferentes entornos (desarrollo, staging, producción) requieren diferentes URLs de API

### 1.2 Restricciones de WebGL

| Restricción | Impacto | Solución en el proyecto |
|-------------|---------|-------------------------|
| **No acceso a filesystem** | No puede leer archivos locales directamente | `UnityWebRequest` para StreamingAssets |
| **Single-threaded** | Bloqueos pueden congelar la UI | Uso de Coroutines (async) |
| **Sandboxed en iframe** | Restricciones de comunicación | postMessage / Query Params |
| **CORS obligatorio** | No puede llamar APIs sin headers apropiados | Backend debe configurar CORS |
| **No persiste estado** | localStorage limitado | Parámetros en URL |

### 1.3 Anti-patrones a evitar

```
❌ ANTI-PATRÓN 1: Hardcodear valores en Unity
   - Requiere recompilar para cada cambio
   - No escala a múltiples entornos

❌ ANTI-PATRÓN 2: Fetch inicial desde Unity sin IDs
   - Unity no sabe qué escenario cargar
   - Requiere lógica compleja de "espera"

❌ ANTI-PATRÓN 3: Comunicación bidireccional compleja React↔Unity
   - Difícil de debuggear
   - Acopla demasiado ambos sistemas

❌ ANTI-PATRÓN 4: Almacenar IDs sensibles en el frontend
   - Exposición de datos del usuario
   - Vulnerabilidad a manipulación

✅ PATRÓN CORRECTO: Query Parameters (implementado en el proyecto)
   - Simple y estándar web
   - Unity los lee de Application.absoluteURL
   - No requiere comunicación JS→Unity
   - Funciona desde el primer frame
```

---

## 2. Arquitectura Propuesta

### 2.1 Diagrama de Alto Nivel

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              ARQUITECTURA COMPLETA                               │
└─────────────────────────────────────────────────────────────────────────────────┘

                    ┌──────────────────────────────────────┐
                    │          USUARIO / NAVEGADOR          │
                    └──────────────────┬───────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                              PÁGINA REACT                                         │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  TaskDetailPage.tsx                                                         │  │
│  │  - Obtiene taskId de la ruta (/tasks/:taskId)                              │  │
│  │  - Obtiene studentId del contexto de autenticación                         │  │
│  │  - Obtiene scenarioId de la API (GET /tasks/{taskId})                      │  │
│  │  - Construye URL del juego con Query Params                                │  │
│  └───────────────────────────────────┬────────────────────────────────────────┘  │
│                                      │                                            │
│                                      ▼                                            │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  <iframe> o <UnityLoader>                                                   │  │
│  │  src="https://game.example.com/?taskId=X&studentId=Y&scenarioId=Z&apiUrl=" │  │
│  └───────────────────────────────────┬────────────────────────────────────────┘  │
└──────────────────────────────────────┼───────────────────────────────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                              UNITY WEBGL                                          │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  ApiConfig.cs (Singleton)                                                   │  │
│  │  1. Lee Application.absoluteURL                                            │  │
│  │  2. Parsea Query Params: ?taskId=X&studentId=Y&scenarioId=Z&apiUrl=...     │  │
│  │  3. Almacena en propiedades: TaskId, StudentId, ScenarioId, BaseUrl        │  │
│  │  4. Dispara evento OnConfigLoaded                                          │  │
│  └───────────────────────────────────┬────────────────────────────────────────┘  │
│                                      │                                            │
│                                      ▼                                            │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  ScenarioApiService.cs                                                      │  │
│  │  GET /api/v1/scenarios/{scenarioId}                                        │  │
│  │  → Carga reglas del juego, acciones, eventos, perfil del consumidor        │  │
│  └───────────────────────────────────┬────────────────────────────────────────┘  │
│                                      │                                            │
│                                      ▼                                            │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  [JUEGO EN EJECUCIÓN]                                                       │  │
│  │  → El estudiante juega la simulación                                       │  │
│  └───────────────────────────────────┬────────────────────────────────────────┘  │
│                                      │                                            │
│                                      ▼                                            │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  GameSessionApiService.cs                                                   │  │
│  │  POST /api/v1/attempts                                                     │  │
│  │  → Envía resultados: taskId, studentId, métricas, historial de turnos      │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                           BACKEND (Spring Boot)                                   │
│  ┌──────────────────────┐    ┌──────────────────────┐    ┌────────────────────┐  │
│  │ GET /scenarios/{id}  │    │ POST /attempts       │    │ Validaciones       │  │
│  │ - Retorna escenario  │    │ - Guarda sesión      │    │ - taskId existe?   │  │
│  │ - Acciones, eventos  │    │ - Calcula resultado  │    │ - studentId válido?│  │
│  │ - Perfil consumidor  │    │ - Retorna finalOutcome│    │ - Permisos OK?     │  │
│  └──────────────────────┘    └──────────────────────┘    └────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Responsabilidades de Cada Componente

| Componente | Responsabilidad | NO debe hacer |
|------------|-----------------|---------------|
| **React** | Autenticación, routing, obtener IDs, embeber juego | Lógica del juego |
| **Unity** | Lógica del juego, UI del juego, comunicación con API | Autenticación, validación de permisos |
| **Backend** | Persistencia, validación, cálculo de resultados, CORS | Lógica de juego (solo almacena) |

### 2.3 Flujo de Datos

```
┌─────────┐     ┌─────────┐     ┌─────────┐     ┌─────────┐
│  React  │────▶│  Unity  │────▶│ Backend │────▶│   DB    │
└─────────┘     └─────────┘     └─────────┘     └─────────┘
     │               │               │
     │  Query Params │  HTTP REST    │  SQL/NoSQL
     │               │               │
     ▼               ▼               ▼
  taskId          scenarioId       attempts
  studentId       API calls        scenarios
  scenarioId      game state       students
  apiUrl
```

---

## 3. Estrategia Técnica Recomendada

### 3.1 Cómo React Carga el Juego

Existen tres opciones principales. **Recomendamos la Opción A (iframe)** por su simplicidad y aislamiento:

#### Opción A: iframe (Recomendada)

```
┌─────────────────────────────────────────────────────────────────┐
│  VENTAJAS                           │  DESVENTAJAS              │
├─────────────────────────────────────┼───────────────────────────┤
│  ✅ Aislamiento total de contextos  │  ⚠️ Comunicación limitada │
│  ✅ Fácil de implementar            │  ⚠️ Carga como página     │
│  ✅ No conflictos de CSS/JS         │     completa              │
│  ✅ Seguridad por sandbox           │                           │
│  ✅ Funciona con Query Params       │                           │
└─────────────────────────────────────┴───────────────────────────┘
```

#### Opción B: Unity WebGL Loader (react-unity-webgl)

```
┌─────────────────────────────────────────────────────────────────┐
│  VENTAJAS                           │  DESVENTAJAS              │
├─────────────────────────────────────┼───────────────────────────┤
│  ✅ Integración más profunda        │  ⚠️ Más complejo          │
│  ✅ Comunicación JS↔Unity directa   │  ⚠️ Posibles conflictos   │
│  ✅ Control sobre eventos de carga  │  ⚠️ Dependencia externa   │
│                                     │  ⚠️ Requiere configuración│
│                                     │     adicional en Unity    │
└─────────────────────────────────────┴───────────────────────────┘
```

#### Opción C: Ventana/Pestaña nueva

```
┌─────────────────────────────────────────────────────────────────┐
│  VENTAJAS                           │  DESVENTAJAS              │
├─────────────────────────────────────┼───────────────────────────┤
│  ✅ Máximo aislamiento              │  ❌ UX fragmentada         │
│  ✅ Pantalla completa fácil         │  ❌ Usuario sale del flujo│
│                                     │  ❌ Difícil tracking       │
└─────────────────────────────────────┴───────────────────────────┘
```

### 3.2 Cómo React Pasa los Parámetros

#### Estrategia: Query Parameters (Ya implementada en Unity)

El juego Unity ya lee los Query Parameters desde `Application.absoluteURL`. React solo necesita construir la URL correctamente:

```
https://game.markenx.com/?scenarioId=abc&studentId=xyz&taskId=123&apiUrl=https%3A%2F%2Fapi.markenx.com
                         └──────────────────────────────────────────────────────────────────────────┘
                                                    Query Parameters
```

**Parámetros soportados:**

| Parámetro | Obligatorio | Descripción | Ejemplo |
|-----------|-------------|-------------|---------|
| `scenarioId` | Sí | UUID del escenario a cargar | `a74394ee-7360-4943-b19f-84be9f106e45` |
| `studentId` | Sí | UUID del estudiante | `fc711ce9-cc33-4168-8110-bd4a710d278f` |
| `taskId` | Sí | UUID de la tarea/asignación | `0925f141-fc36-4ef4-a661-b35820079585` |
| `apiUrl` | No* | URL base de la API | `https://api.markenx.com` |

*Si no se proporciona `apiUrl`, Unity usa el valor por defecto configurado en el Inspector.

### 3.3 Cómo Unity Recibe y Gestiona los Valores

#### Ubicación del código: `Assets/Scripts/Api/Config/ApiConfig.cs`

```
┌────────────────────────────────────────────────────────────────────────────┐
│                      JERARQUÍA DE PRIORIDAD EN UNITY                        │
│                      (de mayor a menor prioridad)                           │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  1️⃣  QUERY PARAMETERS (WebGL)                                              │
│      └─ Lee de Application.absoluteURL                                     │
│      └─ PRIORIDAD MÁXIMA - Usado para integración con React                │
│                                                                            │
│  2️⃣  ARGUMENTOS DE LÍNEA DE COMANDO (Desktop)                              │
│      └─ Lee de Environment.GetCommandLineArgs()                            │
│      └─ Útil para testing local: Game.exe --scenarioId=xxx                 │
│                                                                            │
│  3️⃣  ARCHIVO config.json (StreamingAssets)                                 │
│      └─ Lee de StreamingAssets/config.json                                 │
│      └─ Útil para builds de desarrollo                                     │
│                                                                            │
│  4️⃣  VALORES DEL INSPECTOR (Unity Editor)                                  │
│      └─ Campos [SerializeField] en ApiConfig                               │
│      └─ Fallback final                                                     │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

#### Flujo de inicialización en Unity:

```csharp
// ApiConfig.cs - Start()
void Start()
{
    StartCoroutine(LoadConfigurationCoroutine());
}

private IEnumerator LoadConfigurationCoroutine()
{
    // 1. WebGL? → Leer Query Params
    if (Application.platform == RuntimePlatform.WebGLPlayer)
    {
        LoadFromQueryParams();  // ← AQUÍ SE LEEN LOS PARÁMETROS DE REACT
    }

    // 2. ¿Falta scenarioId? → Intentar línea de comando
    if (!HasValidScenarioId())
    {
        LoadFromCommandLineArgs();
    }

    // 3. ¿Sigue faltando? → Leer config.json
    if (!HasValidScenarioId())
    {
        yield return LoadFromConfigFile();
    }

    // 4. ¿Nada funcionó? → Usar defaults del Inspector
    if (!HasValidScenarioId())
    {
        _resolvedScenarioId = defaultScenarioId;
    }

    _configLoaded = true;
    OnConfigLoaded?.Invoke();  // ← Notifica que está listo
}
```

---

## 4. Ejemplos Concretos

### 4.1 URL Generada por React

```
Escenario: El estudiante "Juan" (ID: fc711ce9) debe completar la tarea "Marketing Digital"
           (taskId: 0925f141) que usa el escenario "Lanzamiento Producto" (scenarioId: a74394ee)

URL Base del juego: https://game.markenx.com/

URL Completa:
https://game.markenx.com/?scenarioId=a74394ee-7360-4943-b19f-84be9f106e45&studentId=fc711ce9-cc33-4168-8110-bd4a710d278f&taskId=0925f141-fc36-4ef4-a661-b35820079585&apiUrl=https%3A%2F%2Fapi.markenx.com

Desglose:
┌──────────────────────────────────────────────────────────────────────────────┐
│ Base:        https://game.markenx.com/                                       │
│ scenarioId:  a74394ee-7360-4943-b19f-84be9f106e45                            │
│ studentId:   fc711ce9-cc33-4168-8110-bd4a710d278f                            │
│ taskId:      0925f141-fc36-4ef4-a661-b35820079585                            │
│ apiUrl:      https://api.markenx.com (URL-encoded)                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Código React Simplificado

#### Opción A: Usando iframe (Recomendada)

```tsx
// src/pages/TaskDetailPage.tsx
import React, { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useTask } from '../hooks/useTask';

// Configuración del juego
const GAME_CONFIG = {
  // URL donde está hosteado el build WebGL de Unity
  baseUrl: process.env.REACT_APP_GAME_URL || 'https://game.markenx.com',
  // URL de la API que el juego consumirá
  apiUrl: process.env.REACT_APP_API_URL || 'https://api.markenx.com',
};

interface GameEmbedProps {
  taskId: string;
  studentId: string;
  scenarioId: string;
}

/**
 * Componente que embebe el juego Unity WebGL
 */
const GameEmbed: React.FC<GameEmbedProps> = ({ taskId, studentId, scenarioId }) => {
  // Construir la URL del juego con Query Parameters
  const gameUrl = useMemo(() => {
    const params = new URLSearchParams({
      scenarioId,
      studentId,
      taskId,
      apiUrl: GAME_CONFIG.apiUrl,
    });

    return `${GAME_CONFIG.baseUrl}/?${params.toString()}`;
  }, [taskId, studentId, scenarioId]);

  return (
    <div className="game-container" style={{ width: '100%', height: '600px' }}>
      <iframe
        src={gameUrl}
        title="MarkenX Game"
        width="100%"
        height="100%"
        frameBorder="0"
        allow="fullscreen"
        style={{
          border: 'none',
          borderRadius: '8px',
          boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
        }}
      />
    </div>
  );
};

/**
 * Página de detalle de tarea que muestra el juego
 */
const TaskDetailPage: React.FC = () => {
  // Obtener taskId de la URL (React Router)
  const { taskId } = useParams<{ taskId: string }>();

  // Obtener studentId del contexto de autenticación
  const { user } = useAuth();
  const studentId = user?.id;

  // Obtener datos de la tarea (incluye scenarioId)
  const { task, isLoading, error } = useTask(taskId);

  // Estados de carga y error
  if (isLoading) {
    return <div className="loading">Cargando tarea...</div>;
  }

  if (error) {
    return <div className="error">Error al cargar la tarea: {error.message}</div>;
  }

  if (!task || !studentId || !taskId) {
    return <div className="error">Datos insuficientes para cargar el juego</div>;
  }

  return (
    <div className="task-detail-page">
      <header>
        <h1>{task.title}</h1>
        <p>{task.description}</p>
      </header>

      <main>
        <GameEmbed
          taskId={taskId}
          studentId={studentId}
          scenarioId={task.scenarioId}
        />
      </main>
    </div>
  );
};

export default TaskDetailPage;
```

#### Hook personalizado para obtener la tarea:

```tsx
// src/hooks/useTask.ts
import { useState, useEffect } from 'react';

interface Task {
  id: string;
  title: string;
  description: string;
  scenarioId: string;  // ← Este es el ID que Unity necesita para cargar el escenario
}

interface UseTaskResult {
  task: Task | null;
  isLoading: boolean;
  error: Error | null;
}

export const useTask = (taskId: string | undefined): UseTaskResult => {
  const [task, setTask] = useState<Task | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    if (!taskId) {
      setIsLoading(false);
      return;
    }

    const fetchTask = async () => {
      try {
        setIsLoading(true);
        const response = await fetch(`/api/v1/tasks/${taskId}`);

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const data = await response.json();
        setTask(data);
      } catch (err) {
        setError(err instanceof Error ? err : new Error('Error desconocido'));
      } finally {
        setIsLoading(false);
      }
    };

    fetchTask();
  }, [taskId]);

  return { task, isLoading, error };
};
```

#### Opción B: Usando react-unity-webgl

```tsx
// src/components/UnityGame.tsx
import React, { useCallback, useEffect } from 'react';
import { Unity, useUnityContext } from 'react-unity-webgl';

interface UnityGameProps {
  taskId: string;
  studentId: string;
  scenarioId: string;
  apiUrl: string;
}

const UnityGame: React.FC<UnityGameProps> = ({ taskId, studentId, scenarioId, apiUrl }) => {
  const { unityProvider, isLoaded, sendMessage, addEventListener, removeEventListener } = useUnityContext({
    loaderUrl: '/unity-build/Build/game.loader.js',
    dataUrl: '/unity-build/Build/game.data',
    frameworkUrl: '/unity-build/Build/game.framework.js',
    codeUrl: '/unity-build/Build/game.wasm',
  });

  // Enviar configuración a Unity cuando cargue
  useEffect(() => {
    if (isLoaded) {
      // Enviar cada parámetro usando SendMessage
      // Requiere que Unity tenga métodos públicos para recibirlos
      sendMessage('ApiConfig', 'SetScenarioId', scenarioId);
      sendMessage('ApiConfig', 'SetStudentId', studentId);
      sendMessage('ApiConfig', 'SetTaskId', taskId);
      sendMessage('ApiConfig', 'SetApiUrl', apiUrl);
    }
  }, [isLoaded, sendMessage, scenarioId, studentId, taskId, apiUrl]);

  // Escuchar eventos de Unity (opcional)
  const handleGameEnd = useCallback((result: string) => {
    console.log('Juego terminado con resultado:', result);
    // Aquí podrías actualizar el estado de React, navegar, etc.
  }, []);

  useEffect(() => {
    addEventListener('GameEnded', handleGameEnd);
    return () => removeEventListener('GameEnded', handleGameEnd);
  }, [addEventListener, removeEventListener, handleGameEnd]);

  return (
    <div className="unity-container">
      {!isLoaded && <div className="loading-overlay">Cargando juego...</div>}
      <Unity
        unityProvider={unityProvider}
        style={{ width: '100%', height: '600px' }}
      />
    </div>
  );
};

export default UnityGame;
```

> **Nota:** La Opción B requiere modificaciones adicionales en Unity para exponer métodos como `SetTaskId()`.
> La Opción A (iframe con Query Params) funciona con el código actual sin modificaciones.

### 4.3 Código Unity (C#) - Ya Implementado

El siguiente código **ya existe** en el proyecto. Se muestra aquí como referencia:

```csharp
// Assets/Scripts/Api/Config/ApiConfig.cs (extracto relevante)

/// <summary>
/// WebGL: Lee parámetros de la URL del navegador.
/// Ejemplo: https://game.com/?scenarioId=abc123&studentId=xyz789&taskId=task001&apiUrl=https://api.com
/// </summary>
private void LoadFromQueryParams()
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // Obtiene la URL completa del navegador
    string url = Application.absoluteURL;

    // Parsea cada parámetro de la URL
    _resolvedScenarioId = GetQueryParam(url, "scenarioId");
    _resolvedStudentId = GetQueryParam(url, "studentId");
    _resolvedTaskId = GetQueryParam(url, "taskId");

    // También permite override de la URL de la API
    string customApiUrl = GetQueryParam(url, "apiUrl");
    if (!string.IsNullOrEmpty(customApiUrl))
    {
        baseUrl = Uri.UnescapeDataString(customApiUrl);
    }

    if (HasValidScenarioId())
    {
        Debug.Log("[ApiConfig] Configuración cargada desde Query Params");
    }
#endif
}

/// <summary>
/// Extrae un parámetro específico de una URL con query string.
/// </summary>
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
            // Decodifica caracteres URL-encoded (%20, %3A, etc.)
            return Uri.UnescapeDataString(keyValue[1]);
        }
    }

    return null;
}
```

### 4.4 Uso de los Parámetros en el Juego

```csharp
// Assets/Scripts/Managers/GameSceneManager.cs (extracto)

/// <summary>
/// Envía los datos de la sesión de juego al backend al terminar.
/// </summary>
private void EnviarSesionAlBackend()
{
    // Obtener IDs de ApiConfig (que los leyó de Query Params)
    string taskId = ApiConfig.Instance.TaskId;
    string studentId = ApiConfig.Instance.StudentId;

    // Fallback: usar scenarioId como taskId si no está definido
    if (string.IsNullOrEmpty(taskId))
    {
        taskId = ApiConfig.Instance.ScenarioId;
    }

    // Crear request con los datos del juego
    var request = GameSessionApiService.Instance.CreateRequestFromGameState(
        taskId: taskId,
        studentId: studentId,
        finalAcceptance: aceptacionActual / 100f,
        remainingBudget: presupuestoActual,
        totalTurnsUsed: turnoActual,
        profileDiscoveryPercentage: GetNivelPerfil(),
        history: historialTurnosApi
    );

    // Enviar al backend
    GameSessionApiService.Instance.RegisterGameSession(request,
        onSuccess: (response) => {
            Debug.Log($"Sesión registrada. Resultado: {response.finalOutcome}");
        },
        onError: (error) => {
            Debug.LogError($"Error: {error}");
        }
    );
}
```

---

## 5. Consideraciones Importantes

### 5.1 Seguridad

#### Exposición de IDs en la URL

```
⚠️ RIESGO: Los IDs son visibles en la URL del navegador

URL: https://game.com/?taskId=xxx&studentId=yyy&scenarioId=zzz
                       └─────────────────────────────────────┘
                              Visible en historial, logs, etc.
```

**Mitigaciones implementadas/recomendadas:**

| Mitigación | Responsable | Estado |
|------------|-------------|--------|
| Usar UUIDs (no IDs secuenciales) | Backend | ✅ Implementado |
| Validar permisos en cada request | Backend | 🔲 Pendiente |
| No exponer datos sensibles en URL | Frontend | ✅ Solo IDs |
| HTTPS obligatorio | Infraestructura | 🔲 Configurar |
| Tokens de sesión cortos | Backend | 🔲 Considerar |

#### Validaciones Obligatorias en Backend

```java
// Ejemplo pseudocódigo para Spring Boot

@PostMapping("/api/v1/attempts")
public ResponseEntity<?> registerAttempt(@RequestBody AttemptRequest request,
                                         @AuthenticationPrincipal User user) {

    // 1. Validar que el estudiante existe
    if (!studentService.exists(request.getStudentId())) {
        return ResponseEntity.status(404).body("Estudiante no encontrado");
    }

    // 2. Validar que la tarea existe
    Task task = taskService.findById(request.getTaskId());
    if (task == null) {
        return ResponseEntity.status(404).body("Tarea no encontrada");
    }

    // 3. Validar que el estudiante tiene permiso para esta tarea
    if (!taskService.isAssignedTo(request.getTaskId(), request.getStudentId())) {
        return ResponseEntity.status(403).body("No autorizado para esta tarea");
    }

    // 4. Validar que la tarea no está vencida
    if (task.getDueDate().isBefore(LocalDateTime.now())) {
        return ResponseEntity.status(400).body("La tarea ha vencido");
    }

    // 5. Procesar el intento
    return attemptService.save(request);
}
```

### 5.2 CORS (Cross-Origin Resource Sharing)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ESCENARIO CORS                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   React App                    Unity WebGL                   Backend API     │
│   https://app.markenx.com      https://game.markenx.com     https://api...  │
│         │                            │                            │          │
│         │                            │──── GET /scenarios/xxx ───▶│          │
│         │                            │◀─────── CORS Check ────────│          │
│         │                            │                            │          │
│         │                            │──── POST /attempts ───────▶│          │
│         │                            │◀─────── CORS Check ────────│          │
│                                                                              │
│   El backend DEBE permitir requests desde game.markenx.com                  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Configuración CORS en Spring Boot:**

```java
// CorsConfig.java
@Configuration
public class CorsConfig implements WebMvcConfigurer {

    @Override
    public void addCorsMappings(CorsRegistry registry) {
        registry.addMapping("/api/**")
            .allowedOrigins(
                "https://app.markenx.com",      // React app
                "https://game.markenx.com",     // Unity WebGL
                "http://localhost:3000",        // React dev
                "http://localhost:8080"         // Unity local testing
            )
            .allowedMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .allowedHeaders("*")
            .allowCredentials(true)
            .maxAge(3600);
    }
}
```

### 5.3 Diferencias entre Entornos

```
┌──────────────┬─────────────────────────────┬──────────────────────────────┐
│   Entorno    │        Desarrollo           │        Producción            │
├──────────────┼─────────────────────────────┼──────────────────────────────┤
│ React URL    │ http://localhost:3000       │ https://app.markenx.com      │
│ Unity URL    │ http://localhost:8080       │ https://game.markenx.com     │
│ API URL      │ http://localhost:8082       │ https://api.markenx.com      │
│ CORS         │ Permisivo (*)               │ Restrictivo (dominios)       │
│ HTTPS        │ No requerido                │ Obligatorio                  │
│ Logs         │ Verbose (Debug.Log)         │ Mínimos (errores)            │
├──────────────┼─────────────────────────────┼──────────────────────────────┤
│ config.json  │ Valores de desarrollo       │ NO incluir (usar Query Params│
│              │ (para testing local)        │ exclusivamente)              │
└──────────────┴─────────────────────────────┴──────────────────────────────┘
```

**Variables de entorno React (.env):**

```env
# .env.development
REACT_APP_API_URL=http://localhost:8082
REACT_APP_GAME_URL=http://localhost:8080

# .env.production
REACT_APP_API_URL=https://api.markenx.com
REACT_APP_GAME_URL=https://game.markenx.com
```

---

## 6. Flujo Completo Paso a Paso

### Desde: Usuario abre la tarea en React
### Hasta: Unity ejecuta POST /attempts usando el taskId correcto

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 1: Usuario navega a la página de tarea                                    │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  Usuario → Navegador → https://app.markenx.com/tasks/0925f141-fc36-4ef4-a661    │
│                                                       └──────────────────────┘   │
│                                                              taskId              │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 2: React extrae el taskId y obtiene datos                                  │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  const { taskId } = useParams();           // "0925f141-fc36-4ef4-a661"          │
│  const { user } = useAuth();               // { id: "fc711ce9-cc33-4168" }       │
│  const { task } = useTask(taskId);         // { scenarioId: "a74394ee-7360" }    │
│                                                                                  │
│  React ahora tiene:                                                             │
│  • taskId = "0925f141-fc36-4ef4-a661-b35820079585"                               │
│  • studentId = "fc711ce9-cc33-4168-8110-bd4a710d278f"                            │
│  • scenarioId = "a74394ee-7360-4943-b19f-84be9f106e45"                           │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 3: React construye la URL del juego                                        │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  const params = new URLSearchParams({                                           │
│    scenarioId: "a74394ee-7360-4943-b19f-84be9f106e45",                           │
│    studentId: "fc711ce9-cc33-4168-8110-bd4a710d278f",                            │
│    taskId: "0925f141-fc36-4ef4-a661-b35820079585",                               │
│    apiUrl: "https://api.markenx.com"                                            │
│  });                                                                            │
│                                                                                  │
│  const gameUrl = `https://game.markenx.com/?${params.toString()}`;              │
│                                                                                  │
│  URL resultante:                                                                │
│  https://game.markenx.com/?scenarioId=a74394ee...&studentId=fc711ce9...&...     │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 4: React renderiza el iframe con el juego                                  │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  <iframe src={gameUrl} width="100%" height="600px" />                           │
│                                                                                  │
│  El navegador carga el build WebGL de Unity desde game.markenx.com              │
│  La URL completa (con Query Params) está en la barra de direcciones del iframe  │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 5: Unity se inicializa y lee los Query Params                              │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  // ApiConfig.cs - Start()                                                      │
│  void Start() {                                                                 │
│      StartCoroutine(LoadConfigurationCoroutine());                              │
│  }                                                                              │
│                                                                                  │
│  // LoadConfigurationCoroutine()                                                │
│  if (Application.platform == RuntimePlatform.WebGLPlayer) {                     │
│      LoadFromQueryParams();  // ← Lee la URL                                    │
│  }                                                                              │
│                                                                                  │
│  // LoadFromQueryParams()                                                       │
│  string url = Application.absoluteURL;                                          │
│  // url = "https://game.markenx.com/?scenarioId=a74394ee...&..."                │
│                                                                                  │
│  _resolvedScenarioId = GetQueryParam(url, "scenarioId"); // "a74394ee..."       │
│  _resolvedStudentId = GetQueryParam(url, "studentId");   // "fc711ce9..."       │
│  _resolvedTaskId = GetQueryParam(url, "taskId");         // "0925f141..."       │
│  baseUrl = GetQueryParam(url, "apiUrl");                 // "https://api..."    │
│                                                                                  │
│  _configLoaded = true;                                                          │
│  OnConfigLoaded?.Invoke();  // ← Notifica que la config está lista              │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 6: Unity carga el escenario desde la API                                   │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  // GameSceneManager.cs - CargarReglasCoroutine()                               │
│                                                                                  │
│  // Espera a que ApiConfig esté listo                                           │
│  while (!ApiConfig.Instance.IsConfigLoaded) {                                   │
│      yield return new WaitForSeconds(0.1f);                                     │
│  }                                                                              │
│                                                                                  │
│  // Obtiene el scenarioId                                                       │
│  string scenarioId = ApiConfig.Instance.ScenarioId;  // "a74394ee..."           │
│                                                                                  │
│  // Llama a la API                                                              │
│  ScenarioApiService.Instance.GetScenarioById(scenarioId, onSuccess, onError);   │
│                                                                                  │
│  // HTTP Request:                                                               │
│  // GET https://api.markenx.com/api/v1/scenarios/a74394ee-7360-4943-...          │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 7: El estudiante juega                                                     │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  [MainMenu] → Usuario clickea "Iniciar Partida"                                 │
│  [GameScene] → Carga con las reglas del escenario                               │
│                                                                                  │
│  El estudiante:                                                                 │
│  • Compra acciones de marketing                                                 │
│  • Ve el nivel de aceptación cambiar                                            │
│  • Descubre el perfil del consumidor                                            │
│  • Experimenta eventos aleatorios                                               │
│                                                                                  │
│  GameSceneManager registra cada turno en historialTurnosApi                     │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 8: El juego termina y envía resultados                                     │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  // GameSceneManager.cs - EjecutarFinDeJuego()                                  │
│                                                                                  │
│  if (!usarModoSimulacion) {                                                     │
│      EnviarSesionAlBackend();                                                   │
│  }                                                                              │
│                                                                                  │
│  // EnviarSesionAlBackend()                                                     │
│  string taskId = ApiConfig.Instance.TaskId;       // "0925f141..."              │
│  string studentId = ApiConfig.Instance.StudentId; // "fc711ce9..."              │
│                                                                                  │
│  var request = new RegisterGameSessionRequest {                                 │
│      taskId = taskId,                                                           │
│      studentId = studentId,                                                     │
│      finalAcceptance = 0.75f,                                                   │
│      remainingBudget = 5000,                                                    │
│      totalTurnsUsed = 8,                                                        │
│      history = historialTurnosApi                                               │
│  };                                                                             │
│                                                                                  │
│  // HTTP Request:                                                               │
│  // POST https://api.markenx.com/api/v1/attempts                                 │
│  // Body: { taskId: "0925f141...", studentId: "fc711ce9...", ... }              │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 9: Backend procesa y responde                                              │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  Backend recibe el POST:                                                        │
│  1. Valida taskId existe y está asignado al estudiante                          │
│  2. Valida studentId existe                                                     │
│  3. Calcula finalOutcome basado en finalAcceptance vs targetAcceptanceScore     │
│  4. Guarda el intento en la base de datos                                       │
│  5. Retorna respuesta con el resultado calculado                                │
│                                                                                  │
│  Response (201 Created):                                                        │
│  {                                                                              │
│    "id": "attempt-uuid-12345",                                                  │
│    "taskId": "0925f141-fc36-4ef4-a661-b35820079585",                             │
│    "studentId": "fc711ce9-cc33-4168-8110-bd4a710d278f",                          │
│    "finalOutcome": "GANASTE",  // Calculado por backend                         │
│    "finalAcceptance": 0.75,                                                     │
│    ...                                                                          │
│  }                                                                              │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PASO 10: Unity muestra el resultado final                                       │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  onSuccess: (response) => {                                                     │
│      GameState.resultadoJuego = response.finalOutcome;  // "GANASTE"            │
│  }                                                                              │
│                                                                                  │
│  SceneManager.LoadScene("EndGameScene");                                        │
│                                                                                  │
│  [EndGameScene muestra: "¡GANASTE!" con estadísticas]                           │
│                                                                                  │
│  ┌──────────────────────────────────────────────────────────────────┐           │
│  │                        ¡GANASTE!                                 │           │
│  │                                                                  │           │
│  │  Turnos utilizados: 8                                            │           │
│  │  Presupuesto restante: $5,000                                    │           │
│  │  Nivel de aceptación: 75%                                        │           │
│  │  Perfil descubierto: 80%                                         │           │
│  │                                                                  │           │
│  │                    [VOLVER AL MENÚ]                              │           │
│  └──────────────────────────────────────────────────────────────────┘           │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Recomendaciones Finales

### 7.1 Buenas Prácticas

#### Para React:

```
✅ DO:
  • Validar que todos los IDs existen antes de construir la URL
  • Usar URLSearchParams para construir query strings (maneja encoding)
  • Mostrar estado de carga mientras el iframe carga
  • Usar variables de entorno para URLs (no hardcodear)
  • Manejar errores de carga del iframe

❌ DON'T:
  • Concatenar strings manualmente para la URL
  • Asumir que el juego cargó correctamente sin validar
  • Exponer tokens de autenticación en la URL
  • Usar window.open() para abrir el juego (fragmenta UX)
```

#### Para Unity:

```
✅ DO:
  • Siempre tener fallbacks (config.json → defaults)
  • Loguear la configuración final para debugging
  • Validar que los IDs no están vacíos antes de llamar a la API
  • Usar Uri.UnescapeDataString() al leer Query Params

❌ DON'T:
  • Hardcodear URLs de producción
  • Asumir que los Query Params siempre existirán
  • Ignorar errores de API silenciosamente
  • Bloquear el main thread esperando respuestas
```

#### Para Backend:

```
✅ DO:
  • Validar TODOS los IDs en cada request
  • Configurar CORS correctamente para cada entorno
  • Retornar errores descriptivos (no solo códigos)
  • Loguear intentos fallidos (seguridad)

❌ DON'T:
  • Confiar ciegamente en los IDs que llegan
  • Usar CORS wildcard (*) en producción
  • Exponer stack traces en errores
  • Permitir requests sin autenticación apropiada
```

### 7.2 Preparación para Integraciones Futuras

#### LMS (Learning Management System)

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  FUTURO: Integración con LMS (Moodle, Canvas, etc.)                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  El diseño actual ya soporta esto porque:                                       │
│                                                                                  │
│  1. Los IDs son externos (UUID) - no dependen de una base de datos específica   │
│  2. Query Params funcionan con LTI (Learning Tools Interoperability)            │
│  3. La API REST es desacoplada - puede ser llamada desde cualquier cliente      │
│                                                                                  │
│  Para integrar con un LMS:                                                      │
│  • El LMS genera el taskId, studentId basado en su propia lógica                │
│  • El LMS construye la URL del juego igual que React                            │
│  • El backend mapea los IDs del LMS a su modelo interno                         │
│                                                                                  │
│  Parámetro adicional sugerido para LMS:                                         │
│  • lmsProvider: "moodle" | "canvas" | "blackboard"                              │
│  • ltiVersion: "1.3"                                                            │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

#### SSO (Single Sign-On)

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  FUTURO: Integración con SSO (OAuth2, SAML)                                     │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  Opción A: Token en Query Params (menos seguro, más simple)                     │
│  URL: .../?studentId=xxx&authToken=jwt_token_here                               │
│                                                                                  │
│  Opción B: Cookie compartida (más seguro, requiere mismo dominio)               │
│  - React autentica y setea cookie                                               │
│  - Unity WebGL envía cookie automáticamente (si mismo dominio)                  │
│                                                                                  │
│  Opción C: postMessage (más seguro, más complejo)                               │
│  - React envía token via postMessage después de cargar el iframe               │
│  - Unity escucha mensaje y almacena token                                       │
│                                                                                  │
│  Cambios necesarios en Unity:                                                   │
│  • Añadir header Authorization en requests HTTP                                 │
│  • Manejar respuestas 401 (token expirado)                                      │
│  • Implementar refresh token si es necesario                                    │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 7.3 Checklist de Deployment

```
ANTES DE IR A PRODUCCIÓN:
                                                                        STATUS
□ Unity build con usarModoSimulacion = false                           [  ]
□ config.json tiene valores de producción O se elimina del build       [  ]
□ Variables de entorno configuradas en React                           [  ]
□ CORS configurado en backend para dominios de producción              [  ]
□ HTTPS habilitado en todos los servicios                              [  ]
□ Validaciones de permisos implementadas en backend                    [  ]
□ Logs configurados apropiadamente (no verbose en prod)                [  ]
□ Monitoreo/alertas configuradas para errores de API                   [  ]
□ Pruebas end-to-end ejecutadas                                        [  ]
□ Prueba de carga del juego en navegadores target                      [  ]
```

### 7.4 Troubleshooting Común

| Problema | Causa probable | Solución |
|----------|----------------|----------|
| Unity no lee los params | `#if UNITY_WEBGL` no activo | Verificar plataforma de build |
| CORS error en consola | Backend no permite el origen | Añadir origen a config CORS |
| "scenarioId is null" | URL mal formada | Verificar URLSearchParams |
| Juego no carga | Build path incorrecto | Verificar rutas en loader |
| API timeout | Backend no responde | Verificar URL y servidor |
| 404 en /scenarios | Endpoint incorrecto | Verificar contextPath |

---

## Resumen Ejecutivo

El videojuego MarkenX está **completamente preparado** para integrarse con una aplicación React. La arquitectura implementada:

1. **Lee Query Parameters** automáticamente en WebGL
2. **Tiene fallbacks** para desarrollo local (config.json, Inspector)
3. **Usa la configuración** para todas las llamadas API
4. **No requiere modificaciones** en Unity para funcionar con React

**Lo único que React necesita hacer es:**

```tsx
const gameUrl = `https://game.markenx.com/?scenarioId=${scenarioId}&studentId=${studentId}&taskId=${taskId}&apiUrl=${encodeURIComponent(apiUrl)}`;

<iframe src={gameUrl} width="100%" height="600px" />
```

Eso es todo. El resto ya está implementado en Unity.

---

*Documento generado: Enero 2026*
*Versión: 1.0*
*Proyecto: MarkenX Videogame*
