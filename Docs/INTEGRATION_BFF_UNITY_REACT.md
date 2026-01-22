# Integración Unity WebGL + React con Token Temporal

Este documento describe la arquitectura de autenticación y los pasos necesarios para la comunicación entre el videojuego Unity WebGL y el BFF (Backend For Frontend).

**Última actualización:** Enero 2026

---

## Estado de Implementación

| Componente | Estado | Notas |
|------------|--------|-------|
| **Unity WebGL** | ✅ COMPLETADO | Token, Auth headers, comunicación JS |
| **Backend (BFF)** | ⚠️ PENDIENTE | Endpoints de game-token |
| **React** | ⚠️ PENDIENTE | Página del juego con iframe |

---

## Arquitectura General

```
+------------------+                      +------------------+
|   React App      |  1. POST /auth/game-token              |
|   (Navegador)    | -------------------> |      BFF         |
+------------------+                      |  (Spring Boot)   |
        |                                 |  Puerto: 8082    |
        | 2. Pasa gameToken via URL       +------------------+
        v                                          ^
+------------------+                               |
|   Unity WebGL    |  3. Authorization: Bearer    |
|   (iframe)       | ----------------------------->
+------------------+
```

### Flujo Resumido

1. Usuario navega a la página del juego en React
2. React obtiene un `gameToken` del BFF (JWT temporal)
3. React carga Unity en un iframe pasando el token via URL
4. Unity lee el token y lo usa para autenticar peticiones a la API
5. Si el token está por expirar, Unity solicita refresh via postMessage
6. Al terminar, Unity envía los resultados al BFF

---

## Lo que YA está implementado en Unity

### Archivos modificados/creados:

| Archivo | Descripción |
|---------|-------------|
| `Assets/Scripts/Api/Config/ApiConfig.cs` | Lee `gameToken` de query params, detecta modo producción |
| `Assets/Scripts/Api/Services/ScenarioApiService.cs` | Agrega header `Authorization: Bearer` |
| `Assets/Scripts/Api/Services/GameSessionApiService.cs` | Agrega header `Authorization: Bearer` |
| `Assets/Plugins/WebGL/TokenBridge.jslib` | Comunicación JS para refresh de token |
| `Assets/Scripts/Managers/GameSceneManager.cs` | Detecta automáticamente modo producción vs simulación |

### Comportamiento automático:

| Entorno | Token presente | Modo |
|---------|----------------|------|
| Unity Editor | N/A | **Simulación** (usa mocks locales) |
| WebGL compilado | **Sí** | **Producción** (usa API real + auth) |
| WebGL compilado | No | **Simulación** (usa mocks locales) |

### Query Parameters que Unity lee:

| Parámetro | Requerido | Descripción |
|-----------|-----------|-------------|
| `scenarioId` | Sí | UUID del escenario a cargar |
| `studentId` | Sí | UUID del estudiante |
| `taskId` | Sí | UUID de la tarea/asignación |
| `apiUrl` | Sí | URL base del BFF (ej: `https://api.markenx.com`) |
| `gameToken` | Sí* | JWT para autenticación (*sin token usa modo simulación) |
| `tokenExpiresIn` | No | Segundos hasta expiración (default: 600) |

### Ejemplo de URL completa:

```
https://game.markenx.com/index.html?scenarioId=a74394ee-7360-4943-b19f-84be9f106e45&studentId=fc711ce9-cc33-4168-8110-bd4a710d278f&taskId=0925f141-fc36-4ef4-a661-b35820079585&apiUrl=https%3A%2F%2Fapi.markenx.com&gameToken=eyJhbGciOiJIUzI1NiJ9...&tokenExpiresIn=600
```

---

## Lo que FALTA implementar en React

### 1. Variables de entorno

Agregar en `.env` o `.env.production`:

```env
# URL donde está hosteado el build WebGL de Unity
VITE_GAME_URL=https://game.markenx.com

# URL base de la API (BFF)
VITE_API_BASE_URL=https://api.markenx.com/api/v1
```

### 2. Servicio de Game Token

**Crear archivo:** `src/services/gameTokenService.ts`

```typescript
interface GameTokenResponse {
  token: string;
  expiresIn: number; // segundos
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export const gameTokenService = {
  /**
   * Obtiene un token temporal para el juego Unity.
   * Requiere sesión activa (cookie de sesión).
   */
  getGameToken: async (): Promise<GameTokenResponse> => {
    const response = await fetch(`${API_BASE_URL}/auth/game-token`, {
      method: 'POST',
      credentials: 'include', // Importante: incluir cookies
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Error obteniendo game token: ${response.status}`);
    }

    return response.json();
  },

  /**
   * Refresca el token del juego antes de que expire.
   * Llamado cuando Unity envía postMessage solicitando refresh.
   */
  refreshGameToken: async (currentToken: string): Promise<GameTokenResponse> => {
    const response = await fetch(`${API_BASE_URL}/auth/game-token/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${currentToken}`,
      },
    });

    if (!response.ok) {
      throw new Error(`Error refrescando token: ${response.status}`);
    }

    return response.json();
  },
};
```

### 3. Componente de Página del Juego

**Crear archivo:** `src/pages/GamePage.tsx`

```typescript
import { useEffect, useState, useMemo, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { gameTokenService } from '../services/gameTokenService';
// Importar hook de sesión/auth según tu implementación
// import { useSession } from '../hooks/useSession';
// import { useTask } from '../hooks/useTask';

const GAME_URL = import.meta.env.VITE_GAME_URL;
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

interface GamePageProps {}

const GamePage: React.FC<GamePageProps> = () => {
  // Obtener IDs de la URL y contexto
  const { taskId } = useParams<{ taskId: string }>();
  // const { user } = useSession(); // Obtener usuario autenticado
  // const { task } = useTask(taskId); // Obtener datos de la tarea (incluye scenarioId)

  // Estados del token
  const [gameToken, setGameToken] = useState<string | null>(null);
  const [tokenExpiresAt, setTokenExpiresAt] = useState<number>(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Ref del iframe para comunicación
  const iframeRef = useRef<HTMLIFrameElement>(null);

  // TODO: Reemplazar con datos reales de tu aplicación
  const studentId = 'REEMPLAZAR_CON_USER_ID'; // user?.id
  const scenarioId = 'REEMPLAZAR_CON_SCENARIO_ID'; // task?.scenarioId

  // 1. Obtener token inicial al montar el componente
  useEffect(() => {
    const fetchToken = async () => {
      try {
        setLoading(true);
        const response = await gameTokenService.getGameToken();
        setGameToken(response.token);
        setTokenExpiresAt(Date.now() + response.expiresIn * 1000);
        setError(null);
      } catch (err) {
        console.error('Error obteniendo game token:', err);
        setError('No se pudo obtener autorización para el juego. Intenta recargar la página.');
      } finally {
        setLoading(false);
      }
    };

    fetchToken();
  }, []);

  // 2. Escuchar mensajes de Unity para refresh de token
  useEffect(() => {
    const handleMessage = async (event: MessageEvent) => {
      // Validar origen del mensaje (seguridad)
      // if (event.origin !== GAME_URL) return;

      if (event.data?.type === 'REFRESH_TOKEN_REQUEST') {
        console.log('[GamePage] Unity solicita refresh de token');

        if (!gameToken) {
          console.error('[GamePage] No hay token para refrescar');
          return;
        }

        try {
          const response = await gameTokenService.refreshGameToken(gameToken);
          setGameToken(response.token);
          setTokenExpiresAt(Date.now() + response.expiresIn * 1000);

          // Enviar nuevo token a Unity
          iframeRef.current?.contentWindow?.postMessage(
            {
              type: 'REFRESH_TOKEN_RESPONSE',
              token: response.token,
              expiresIn: response.expiresIn,
            },
            '*'
          );

          console.log('[GamePage] Token refrescado y enviado a Unity');
        } catch (err) {
          console.error('[GamePage] Error refrescando token:', err);

          // Notificar error a Unity
          iframeRef.current?.contentWindow?.postMessage(
            {
              type: 'REFRESH_TOKEN_ERROR',
              error: 'No se pudo refrescar el token',
            },
            '*'
          );
        }
      }
    };

    window.addEventListener('message', handleMessage);
    return () => window.removeEventListener('message', handleMessage);
  }, [gameToken]);

  // 3. Construir URL del juego con todos los parámetros
  const gameUrl = useMemo(() => {
    if (!taskId || !studentId || !scenarioId || !gameToken) {
      return null;
    }

    const params = new URLSearchParams();
    params.set('scenarioId', scenarioId);
    params.set('studentId', studentId);
    params.set('taskId', taskId);
    params.set('apiUrl', API_BASE_URL.replace('/api/v1', '')); // URL base sin context path
    params.set('gameToken', gameToken);
    params.set('tokenExpiresIn', String(Math.floor((tokenExpiresAt - Date.now()) / 1000)));

    return `${GAME_URL}/index.html?${params.toString()}`;
  }, [taskId, studentId, scenarioId, gameToken, tokenExpiresAt]);

  // Estados de UI
  if (loading) {
    return (
      <div className="game-loading">
        <p>Cargando juego...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="game-error">
        <p>{error}</p>
        <button onClick={() => window.location.reload()}>Reintentar</button>
      </div>
    );
  }

  if (!gameUrl) {
    return (
      <div className="game-error">
        <p>Faltan datos para cargar el juego. Verifica que la tarea existe.</p>
      </div>
    );
  }

  return (
    <div className="game-container" style={{ width: '100%', height: '100vh' }}>
      <iframe
        ref={iframeRef}
        src={gameUrl}
        title="MarkenX Game"
        width="100%"
        height="100%"
        style={{
          border: 'none',
          display: 'block',
        }}
        allow="fullscreen"
      />
    </div>
  );
};

export default GamePage;
```

### 4. Agregar ruta en React Router

```typescript
// En tu archivo de rutas (ej: App.tsx o routes.tsx)
import GamePage from './pages/GamePage';

// Agregar la ruta
<Route path="/game/:taskId" element={<GamePage />} />
```

### 5. Estilos CSS (opcional)

```css
.game-container {
  width: 100%;
  height: 100vh;
  overflow: hidden;
}

.game-loading,
.game-error {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  gap: 1rem;
}

.game-error {
  color: #dc3545;
}

.game-error button {
  padding: 0.5rem 1rem;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
```

---

## Lo que FALTA implementar en Backend (BFF)

### Endpoints requeridos:

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `POST /api/v1/auth/game-token` | POST | Genera JWT temporal para el juego |
| `POST /api/v1/auth/game-token/refresh` | POST | Refresca un JWT existente |

### 1. GameTokenController.java

```java
package com.udla.markenx.api.security.infrastructure.web;

import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.core.user.OAuth2User;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/v1/auth")
@CrossOrigin(origins = "${app.cors.allowed-origins}", allowCredentials = "true")
public class GameTokenController {

    private final GameTokenService gameTokenService;

    public GameTokenController(GameTokenService gameTokenService) {
        this.gameTokenService = gameTokenService;
    }

    @PostMapping("/game-token")
    public ResponseEntity<GameTokenResponse> generateGameToken(
            @AuthenticationPrincipal OAuth2User user) {

        if (user == null) {
            return ResponseEntity.status(401).build();
        }

        GameTokenResponse response = gameTokenService.generateToken(user);
        return ResponseEntity.ok(response);
    }

    @PostMapping("/game-token/refresh")
    public ResponseEntity<GameTokenResponse> refreshGameToken(
            @RequestHeader("Authorization") String authHeader) {

        if (authHeader == null || !authHeader.startsWith("Bearer ")) {
            return ResponseEntity.status(401).build();
        }

        String currentToken = authHeader.substring(7);

        try {
            GameTokenResponse response = gameTokenService.refreshToken(currentToken);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            return ResponseEntity.status(401).build();
        }
    }
}
```

### 2. GameTokenService.java

```java
package com.udla.markenx.api.security.application;

import io.jsonwebtoken.*;
import io.jsonwebtoken.security.Keys;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.oauth2.core.user.OAuth2User;
import org.springframework.stereotype.Service;

import javax.crypto.SecretKey;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.util.*;

@Service
public class GameTokenService {

    private final SecretKey secretKey;
    private final long tokenDurationSeconds;

    public GameTokenService(
            @Value("${app.game-token.secret}") String secret,
            @Value("${app.game-token.duration-seconds:600}") long durationSeconds) {
        this.secretKey = Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8));
        this.tokenDurationSeconds = durationSeconds;
    }

    public GameTokenResponse generateToken(OAuth2User user) {
        String userId = user.getAttribute("sub"); // o "preferred_username"
        String email = user.getAttribute("email");

        Instant now = Instant.now();
        Instant expiration = now.plusSeconds(tokenDurationSeconds);

        String token = Jwts.builder()
                .setSubject(userId)
                .claim("email", email)
                .setIssuedAt(Date.from(now))
                .setExpiration(Date.from(expiration))
                .signWith(secretKey, SignatureAlgorithm.HS256)
                .compact();

        return new GameTokenResponse(token, tokenDurationSeconds);
    }

    public GameTokenResponse refreshToken(String currentToken) {
        Claims claims = validateAndGetClaims(currentToken);

        // Verificar que no haya expirado hace más de 5 minutos
        Date expiration = claims.getExpiration();
        long expiredAgo = System.currentTimeMillis() - expiration.getTime();
        if (expiredAgo > 5 * 60 * 1000) {
            throw new RuntimeException("Token expirado hace más de 5 minutos");
        }

        Instant now = Instant.now();
        Instant newExpiration = now.plusSeconds(tokenDurationSeconds);

        String newToken = Jwts.builder()
                .setSubject(claims.getSubject())
                .claim("email", claims.get("email"))
                .setIssuedAt(Date.from(now))
                .setExpiration(Date.from(newExpiration))
                .signWith(secretKey, SignatureAlgorithm.HS256)
                .compact();

        return new GameTokenResponse(newToken, tokenDurationSeconds);
    }

    public Claims validateAndGetClaims(String token) {
        return Jwts.parserBuilder()
                .setSigningKey(secretKey)
                .build()
                .parseClaimsJws(token)
                .getBody();
    }
}
```

### 3. GameTokenResponse.java

```java
package com.udla.markenx.api.security.infrastructure.web;

public record GameTokenResponse(String token, long expiresIn) {}
```

### 4. Configuración application.yml

```yaml
app:
  game-token:
    # IMPORTANTE: Usar variable de entorno en producción
    secret: ${GAME_TOKEN_SECRET:clave-secreta-de-al-menos-32-caracteres-para-HS256}
    duration-seconds: 600  # 10 minutos
```

### 5. Agregar filtro de autenticación por Bearer token

Ver sección completa de `GameTokenAuthenticationFilter` en el documento original.

---

## Endpoints que Unity consume

### 1. Cargar Escenario

```
GET /api/v1/scenarios/{scenarioId}
Authorization: Bearer <gameToken>
```

**Response:**
```json
{
  "id": "uuid",
  "title": "Nombre del escenario",
  "description": "Descripción",
  "consumer": { ... },
  "dimensions": [ ... ],
  "actions": [ ... ],
  "events": [ ... ]
}
```

### 2. Registrar Intento (al terminar partida)

```
POST /api/v1/attempts
Authorization: Bearer <gameToken>
Content-Type: application/json
```

**Request:**
```json
{
  "taskId": "uuid",
  "studentId": "uuid",
  "sessionDate": "2026-01-22T15:30:00Z",
  "finalAcceptance": 85.5,
  "remainingBudget": 2500.00,
  "totalTurnsUsed": 12,
  "profileDiscoveryPercentage": 67.5,
  "history": [...]
}
```

**Response:**
```json
{
  "id": "uuid",
  "finalOutcome": "WIN",
  ...
}
```

---

## Configuración del Build WebGL (index.html)

**IMPORTANTE**: Después de compilar Unity para WebGL, se debe modificar el `index.html` generado para agregar el código de comunicación con React.

### Por qué es necesario

El archivo `.jslib` de Unity **NO puede contener lógica de navegador** (ver `Docs/WEBGL_JSLIB_PATTERN.md`).
Solo actúa como puente mínimo. Toda la lógica de `postMessage` y listeners debe estar en `index.html`.

### Código a agregar en el `<head>` de index.html

```html
<script>
    // ============================================================
    // BRIDGE DE COMUNICACION UNITY <-> REACT
    // Este codigo permite que Unity solicite refresh de token a React
    // ============================================================

    // Variable global para la instancia de Unity
    var unityInstance = null;

    /**
     * Funcion global que el .jslib invoca.
     * Envia postMessage a la ventana padre (React).
     */
    function requestGameTokenRefresh() {
        console.log('[TokenBridge] Solicitando refresh de token a React...');

        if (window.parent && window.parent !== window) {
            window.parent.postMessage({ type: 'REFRESH_TOKEN_REQUEST' }, '*');
        } else {
            console.warn('[TokenBridge] No hay ventana padre (no esta en iframe)');
        }
    }

    /**
     * Listener para recibir respuestas de React.
     * Envia el nuevo token a Unity via SendMessage.
     */
    window.addEventListener('message', function(event) {
        // Token refresh exitoso
        if (event.data && event.data.type === 'REFRESH_TOKEN_RESPONSE') {
            console.log('[TokenBridge] Token recibido de React');

            if (unityInstance) {
                var tokenData = JSON.stringify({
                    token: event.data.token,
                    expiresIn: event.data.expiresIn || 600
                });
                unityInstance.SendMessage('ApiConfig', 'OnTokenRefreshedFromJS', tokenData);
            } else {
                console.error('[TokenBridge] Unity no esta cargado todavia');
            }
        }

        // Error en refresh
        if (event.data && event.data.type === 'REFRESH_TOKEN_ERROR') {
            console.error('[TokenBridge] Error de React:', event.data.error);
        }
    });

    console.log('[TokenBridge] Bridge inicializado');
</script>
```

### Modificar la carga de Unity

En el mismo `index.html`, asegurarse de que al cargar Unity se guarde la referencia global:

```javascript
// Buscar donde se llama createUnityInstance y modificar:
createUnityInstance(canvas, config, (progress) => {
    // ... codigo de progreso existente ...
}).then((instance) => {
    // IMPORTANTE: Guardar referencia global
    unityInstance = instance;
    console.log('[Unity] Instancia cargada y guardada');
}).catch((message) => {
    console.error('[Unity] Error:', message);
});
```

### Checklist post-build

Después de cada build WebGL:

- [ ] Abrir `Build/index.html`
- [ ] Agregar el script de TokenBridge en el `<head>`
- [ ] Verificar que `createUnityInstance` guarde `unityInstance` globalmente
- [ ] Probar en iframe que postMessage funcione

---

## Comunicación Unity ↔ React (postMessage)

### Flujo completo:

```
Unity C#                    .jslib              index.html           React
    |                          |                     |                  |
    | RequestTokenRefresh()    |                     |                  |
    |------------------------->|                     |                  |
    |                          | requestGameToken    |                  |
    |                          | Refresh()           |                  |
    |                          |-------------------->|                  |
    |                          |                     | postMessage      |
    |                          |                     |----------------->|
    |                          |                     |                  |
    |                          |                     |   (refresh JWT)  |
    |                          |                     |                  |
    |                          |                     |<-----------------|
    |                          |                     | postMessage      |
    |                          |<--------------------|                  |
    |                          | SendMessage()       |                  |
    |<-------------------------|                     |                  |
    | OnTokenRefreshedFromJS() |                     |                  |
```

### Unity solicita refresh de token:

```javascript
// El .jslib llama a esta funcion global (definida en index.html):
requestGameTokenRefresh();

// Que internamente hace:
window.parent.postMessage({ type: 'REFRESH_TOKEN_REQUEST' }, '*');
```

### React responde con nuevo token:

```javascript
// React envía a Unity:
iframe.contentWindow.postMessage({
  type: 'REFRESH_TOKEN_RESPONSE',
  token: 'nuevo-jwt-token',
  expiresIn: 600
}, '*');
```

### En caso de error:

```javascript
iframe.contentWindow.postMessage({
  type: 'REFRESH_TOKEN_ERROR',
  error: 'Mensaje de error'
}, '*');
```

---

## Checklist de Implementación

### Unity (Videojuego) - ✅ COMPLETADO
- [x] `ApiConfig.cs` - Lee gameToken y tokenExpiresIn de query params
- [x] `ApiConfig.cs` - Propiedad `IsProductionMode` para detectar WebGL+token
- [x] `ApiConfig.cs` - Método `RequestTokenRefreshFromReact()` con DllImport
- [x] `ApiConfig.cs` - Método `OnTokenRefreshedFromJS()` para recibir nuevo token
- [x] `ScenarioApiService.cs` - Agrega header `Authorization: Bearer`
- [x] `GameSessionApiService.cs` - Agrega header `Authorization: Bearer`
- [x] `TokenBridge.jslib` - Plugin JS (bridge mínimo) para comunicación
- [x] `GameSceneManager.cs` - Detecta modo automáticamente

### Build WebGL (Post-compilación) - ⚠️ MANUAL
- [ ] Agregar script de TokenBridge en `index.html` (ver sección "Configuración del Build WebGL")
- [ ] Verificar que `createUnityInstance` guarde `unityInstance` globalmente

### Backend (BFF) - ⚠️ PENDIENTE
- [ ] Crear `GameTokenService` con generación y validación JWT
- [ ] Crear `GameTokenController` con endpoints `/game-token` y `/game-token/refresh`
- [ ] Crear `GameTokenAuthenticationFilter` para validar Bearer tokens
- [ ] Agregar configuración en `application.yml`
- [ ] Configurar CORS para permitir requests desde el dominio del juego

### Frontend (React) - ⚠️ PENDIENTE
- [ ] Crear `gameTokenService.ts`
- [ ] Crear `GamePage.tsx` con iframe y manejo de token
- [ ] Agregar listener de `postMessage` para refresh de token
- [ ] Agregar ruta `/game/:taskId`
- [ ] Configurar variables de entorno `VITE_GAME_URL` y `VITE_API_BASE_URL`

### Testing
- [ ] Verificar flujo completo en desarrollo local
- [ ] Probar expiración y refresh de token (esperar ~8 minutos)
- [ ] Verificar headers Authorization en Network tab
- [ ] Probar errores (token inválido, expirado, sin token)

---

## Troubleshooting

| Problema | Causa | Solución |
|----------|-------|----------|
| Unity no lee el token | No está en WebGL compilado | El token solo se lee en builds WebGL, no en Editor |
| "401 Unauthorized" | Token inválido o expirado | Verificar que React pase el token correctamente |
| CORS error | Backend no permite origen | Agregar dominio del juego a CORS config |
| postMessage no funciona | iframe en diferente origen | Verificar que se use `'*'` o el origen correcto |
| Modo simulación en WebGL | Token no presente en URL | Verificar que React pase `gameToken` en query params |
| "failure to execute js library" | .jslib usa APIs de navegador | Ver `Docs/WEBGL_JSLIB_PATTERN.md` |
| SendMessage no llega a Unity | `unityInstance` no está guardada | Verificar que index.html guarde la referencia global |

---

## Notas Importantes

1. **Seguridad del token**: El token se pasa via URL y es visible en la barra de direcciones. Esto es aceptable porque:
   - Son tokens de corta duración (10 min)
   - Solo permiten acceso a endpoints específicos del juego
   - Se usan sobre HTTPS

2. **Modo desarrollo**: En Unity Editor o sin token, el juego usa datos mock locales automáticamente.

3. **Compilación WebGL**: El juego debe compilarse como WebGL para que funcione la integración.

4. **Archivos .jslib**: Los archivos `.jslib` NO son JavaScript de navegador. Se procesan durante el build.
   Ver `Docs/WEBGL_JSLIB_PATTERN.md` para entender el patrón correcto.

5. **Post-build manual**: Después de cada build WebGL, se debe modificar `index.html` para agregar el código de comunicación.

---

*Documento actualizado: Enero 2026*
*Proyecto: MarkenX - Integración Unity + React*
