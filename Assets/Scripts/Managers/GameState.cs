

// Esto NO es un MonoBehaviour, es una clase estática.
// Existe como un casillero global para guardar datos.

namespace Managers
{
    public static class GameState
    {
        // Aquí es donde GameUIManager va a guardar los datos
        // y ReporteManager los va a leer.
    
        public static string resultadoJuego; // "GANASTE" o "PERDISTE"
        public static int ultimoTurno;
        public static int presupuestoRestante;
        public static float nivelAceptacion; // (ej: 0.85 para 85%)
        public static float nivelPerfil;     // (ej: 0.30 para 30%)
    
        // Este es el dato que la IA calcula (ej: 850)
        public static int puntaje; 
    }
}