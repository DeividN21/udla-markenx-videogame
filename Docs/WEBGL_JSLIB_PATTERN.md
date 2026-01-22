# Patron Correcto para .jslib en Unity WebGL

Este documento explica por que el build WebGL puede fallar con archivos `.jslib`
y como estructurar correctamente la comunicacion Unity WebGL <-> JavaScript/React.

---

## El Problema: "failure to execute js library"

### Sintoma

Al compilar Unity para WebGL, el build falla con:

```
failure to execute js library TokenBridge.jslib
```

### Codigo problematico (LO QUE ESTABA MAL)

```javascript
// TokenBridge.jslib - VERSION INCORRECTA

mergeInto(LibraryManager.library, {
    RequestTokenRefresh: function() {
        // ERROR: Usa 'window' que no existe en build-time
        if (window.parent && window.parent !== window) {
            window.parent.postMessage({ type: 'REFRESH_TOKEN_REQUEST' }, '*');
        }
    }
});

// ERROR CRITICO: IIFE que se ejecuta durante el build
(function() {
    // ERROR: 'window' no existe durante el build
    window.addEventListener('message', function(event) {
        // ERROR: 'window.unityInstance' no existe durante el build
        if (window.unityInstance) {
            window.unityInstance.SendMessage('ApiConfig', 'OnTokenRefreshedFromJS', data);
        }
    });
})();
```

---

## Por que falla: Build-time vs Runtime

### Concepto clave: Un .jslib NO es JavaScript de navegador

```
+------------------+     +------------------+     +------------------+
|   TU CODIGO      |     |   UNITY BUILD    |     |   NAVEGADOR      |
|   .jslib         | --> |   (Emscripten)   | --> |   (Chrome, etc)  |
+------------------+     +------------------+     +------------------+
        |                        |                        |
        |   Se EVALUA aqui       |                        |
        |   (Node.js)            |                        |
        +------------------------+                        |
                                                          |
                                 El .wasm y .js resultante|
                                 se EJECUTAN aqui         |
                                 +------------------------+
```

### Que existe en cada momento:

| API / Variable       | Build-time (Node.js) | Runtime (Navegador) |
|----------------------|----------------------|---------------------|
| `window`             | NO                   | SI                  |
| `document`           | NO                   | SI                  |
| `localStorage`       | NO                   | SI                  |
| `postMessage`        | NO                   | SI                  |
| `addEventListener`   | NO                   | SI                  |
| `unityInstance`      | NO                   | SI (despues de cargar) |
| `console.log`        | SI (Node)            | SI (Browser)        |
| `mergeInto`          | SI                   | NO                  |
| `LibraryManager`     | SI                   | NO                  |

### Por que el IIFE rompe el build:

```javascript
// Este codigo se EJECUTA durante el build de Unity
(function() {
    // Emscripten intenta evaluar esto en Node.js
    // Node.js no tiene 'window' -> ERROR -> Build falla
    window.addEventListener('message', ...);
})();
```

El IIFE (Immediately Invoked Function Expression) se ejecuta **inmediatamente**
cuando Emscripten procesa el archivo. Como Node.js no tiene `window`, el build falla.

---

## La Solucion: Separacion de Responsabilidades

### Principio: El .jslib es solo un PUENTE

```
+-------------+     +-------------+     +------------------+     +-------+
| Unity C#    | --> | .jslib      | --> | index.html       | --> | React |
| (Runtime)   |     | (Bridge)    |     | (Browser Logic)  |     |       |
+-------------+     +-------------+     +------------------+     +-------+
      |                   |                     |                    |
      |  DllImport        |  Delega a           |  postMessage       |
      |  "RequestToken    |  funcion global     |  al padre          |
      |   Refresh"        |  del navegador      |                    |
      +-------------------+---------------------+--------------------+
```

### Responsabilidades de cada capa:

| Capa | Responsabilidad | NO debe hacer |
|------|-----------------|---------------|
| **Unity C#** | Llamar funciones del .jslib via DllImport | Logica de JS |
| **.jslib** | Puente minimo: invocar funciones globales | Listeners, estado, DOM |
| **index.html** | Logica del navegador, listeners, postMessage | Logica de juego |
| **React** | Orquestar UI, tokens, comunicacion | Acceder a Unity directamente |

---

## Codigo Correcto

### 1. TokenBridge.jslib (PUENTE MINIMO)

```javascript
/*
 * SOLO declara funciones que Unity puede invocar.
 * NO usa window, document, ni APIs del navegador.
 * DELEGA a funciones globales definidas en index.html.
 */
mergeInto(LibraryManager.library, {

    RequestTokenRefresh: function() {
        // Verificar si la funcion global existe (definida en index.html)
        if (typeof requestGameTokenRefresh === 'function') {
            requestGameTokenRefresh();
        }
    }

});
```

### 2. index.html del Build WebGL (LOGICA DEL NAVEGADOR)

Despues de compilar Unity WebGL, agregar este script en el `<head>` del `index.html`:

```html
<script>
    // Variable global para la instancia de Unity
    var unityInstance = null;

    /**
     * Funcion global que el .jslib puede invocar.
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

    console.log('[TokenBridge] Inicializado correctamente');
</script>
```

### 3. Modificar la carga de Unity en index.html

Asegurarse de guardar la referencia a `unityInstance`:

```html
<script>
    createUnityInstance(canvas, config, (progress) => {
        // Mostrar progreso de carga
    }).then((instance) => {
        // IMPORTANTE: Guardar referencia global para que el listener pueda usarla
        unityInstance = instance;
        console.log('[Unity] Instancia cargada y guardada');
    }).catch((message) => {
        console.error('[Unity] Error cargando:', message);
    });
</script>
```

---

## Flujo Completo de Comunicacion

```
1. Unity necesita refrescar token
   |
   v
2. C#: ApiConfig.RequestTokenRefreshFromReact()
   |
   | [DllImport("__Internal")]
   | private static extern void RequestTokenRefresh();
   v
3. jslib: RequestTokenRefresh()
   |
   | if (typeof requestGameTokenRefresh === 'function')
   |     requestGameTokenRefresh();
   v
4. index.html: requestGameTokenRefresh()
   |
   | window.parent.postMessage({ type: 'REFRESH_TOKEN_REQUEST' }, '*');
   v
5. React: window.addEventListener('message', ...)
   |
   | Llama al BFF para obtener nuevo token
   v
6. React: iframe.contentWindow.postMessage({ type: 'REFRESH_TOKEN_RESPONSE', token: '...' })
   |
   v
7. index.html: window.addEventListener('message', ...)
   |
   | unityInstance.SendMessage('ApiConfig', 'OnTokenRefreshedFromJS', tokenData);
   v
8. Unity: ApiConfig.OnTokenRefreshedFromJS(string jsonData)
   |
   | Guarda el nuevo token
   v
9. Siguiente request usa el nuevo token
```

---

## Checklist: Lo que NO hacer en .jslib

| Mal | Bien |
|-----|------|
| `window.addEventListener(...)` | Listeners en index.html |
| `window.parent.postMessage(...)` | Delegar a funcion global |
| `document.getElementById(...)` | No acceder al DOM |
| `localStorage.setItem(...)` | No usar storage |
| `(function() { ... })()` | No usar IIFE |
| `var estado = {}` | No mantener estado |
| `setTimeout(...)` | No usar timers |
| `fetch(...)` | No hacer requests HTTP |

---

## Resumen

1. **El .jslib se procesa en build-time** (Node.js/Emscripten), no en runtime
2. **No existen APIs de navegador** durante el build
3. **El .jslib debe ser un puente minimo** que solo invoca funciones globales
4. **Toda la logica del navegador** va en index.html o React
5. **Usar funciones globales** como punto de contacto entre .jslib y el navegador

---

## Referencias

- [Unity Manual: WebGL Interacting with Browser Scripting](https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html)
- [Emscripten: Interacting with code](https://emscripten.org/docs/porting/connecting_cpp_and_javascript/Interacting-with-code.html)

---

*Documento creado: Enero 2026*
*Proyecto: MarkenX*
