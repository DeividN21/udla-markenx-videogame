/*
 * TokenBridge.jslib
 *
 * IMPORTANTE: Este archivo es un PUENTE MINIMO entre Unity C# y JavaScript.
 *
 * - NO es JavaScript de navegador normal
 * - Se procesa durante el BUILD de Unity (Emscripten/Node.js)
 * - NO tiene acceso a: window, document, DOM, localStorage, etc.
 * - Solo debe declarar funciones que Unity puede invocar
 * - Toda la logica del navegador debe estar en index.html o React
 *
 * Ver: Docs/WEBGL_JSLIB_PATTERN.md para explicacion completa
 */
mergeInto(LibraryManager.library, {

    /**
     * Solicita a React (ventana padre) que refresque el token de autenticacion.
     *
     * Esta funcion:
     * 1. Verifica si existe una funcion global 'requestGameTokenRefresh' en el navegador
     * 2. Si existe, la invoca (esa funcion esta definida en index.html o React)
     * 3. Si no existe, no hace nada (fail silently para no romper el juego)
     *
     * El flujo completo es:
     * Unity C# -> RequestTokenRefresh() -> requestGameTokenRefresh() [index.html]
     *          -> postMessage a React -> React responde -> SendMessage a Unity
     */
    RequestTokenRefresh: function() {
        // Delegar a funcion global definida en index.html
        // La funcion global maneja toda la logica de postMessage
        if (typeof requestGameTokenRefresh === 'function') {
            requestGameTokenRefresh();
        }
    }

});
