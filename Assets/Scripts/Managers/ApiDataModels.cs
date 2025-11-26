using System.Collections.Generic;
using MyProject.Domain.Models;

namespace Managers
{
    // UI de Unity (Iconos, Textos, Botones)
    [System.Serializable]
    public class AccionInfo 
    {
        public int idAccion;
        public string categoria; // "ATRIBUTOS_PRODUCCION", etc.
        public string nombreAccion;
        public string descripcion;
        public float costo;
        public bool esBloqueadaInicialmente; 
        public int idAccionRequerida; 
    }

    public class GameContext
    {
        // Definiciones de Dimensiones
        public DimensionDefinition PriceSensitivity;
        public DimensionDefinition SocialRecognition;
        public DimensionDefinition QualityExpectation;
        public DimensionDefinition EaseOfUse;
        public DimensionDefinition EcoInterest;

        // Perfiles
        public ConsumerProfile Consumer;
        public ProductProfile Product;

        // ID Botón (Unity) -> Acción Lógica (Paquete)
        public Dictionary<int, MarketAction> ActionsMap;
    
        // Lista visual para la UI
        public List<AccionInfo> AccionesVisuales;

        // Configuración
        public float Presupuesto;
        public float AceptacionObjetivo;
        public string NombreConsumidor;
        public int EdadConsumidor;
    }
}