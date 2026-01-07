using System.Collections.Generic;
using System;

// ===================================================================================
// 1. INPUT DATA (Estructura del JSON de Configuración)
// Las clases deben coincidir EXACTAMENTE con las claves del JSON que mande el backend.
// ===================================================================================

[System.Serializable]
public class GameScenarioConfig
{
    public List<DimensionConfig> dimensions;
    public ConsumerConfig consumer;
    public List<ActionConfig> actions;
    public List<EventConfig> events;
}

[System.Serializable]
public class DimensionConfig
{
    public string id;       // UUID
    public string name;     // "PriceSensitivity"
    public string displayName; // "Sensibilidad al precio" (para UI)
    public string description;
    public float consumerExpectation; // 0.0 - 1.0
    public float productInitialOffer; // 0.0 - 1.0
}

[System.Serializable]
public class ConsumerConfig
{
    public string name;
    public int age;
    public float budget;
    public float targetAcceptanceScore;
}

[System.Serializable]
public class ActionConfig
{
    public string id;       // UUID
    public string name;
    public string description;
    public float cost;
    public string category; // "PRODUCTION", "DESIGN", etc.
    public bool isInitiallyLocked;
    public string prerequisiteActionId; // UUID o null
    public List<EffectConfig> effects;
}

[System.Serializable]
public class EffectConfig
{
    public string dimensionId; // UUID de la dimensión a afectar
    public float delta;        // +0.15, -0.20, etc.
}

[System.Serializable]
public class EventConfig
{
    public string id;
    public string title;
    public string description;
    public List<EventEffectConfig> effects;
}

[System.Serializable]
public class EventEffectConfig
{
    public string dimensionId;
    public float weightMultiplier; // ej: 5.0
}

// ===================================================================================
// 2. OUTPUT DATA (Reporte de Resultados para el Docente)
// Esta estructura se convertirá en JSON al finalizar la partida.
// ===================================================================================

[System.Serializable]
public class GameSessionReport
{
    public string sessionDate;      // Fecha/Hora
    public string finalOutcome;     // "GANASTE" / "PERDISTE"
    public float finalAcceptance;   // 0.0 - 1.0
    public int remainingBudget;     
    public int totalTurnsUsed;
    public float profileDiscoveryPercentage;
    public List<TurnHistoryLog> history;
}

[System.Serializable]
public class TurnHistoryLog
{
    public int turnNumber;
    public float acceptanceAtEnd;
    public int budgetAtEnd;
    public List<string> actionsTakenIds; // Lista de IDs de acciones compradas en este turno
    public string eventOcurredTitle;     // Si hubo noticia, cuál fue
}

// ===================================================================================
// 3. RUNTIME CONTEXT (Lo que usa el juego mientras corre)
// Se mantiene esto para conectar la lógica interna.
// ===================================================================================

// Clase auxiliar para la UI de Unity
[System.Serializable]
public class AccionInfo 
{
    public int idAccion; // Se mapea el UUID a un int temporal para la UI vieja si es necesario
    public string originalUuid; // Guardamos el UUID real
    public string categoria; 
    public string nombreAccion;
    public string descripcion;
    public float costo;
    public bool esBloqueadaInicialmente; 
    public string idAccionRequeridaUuid; 
}

public class GameContext
{
    // Diccionarios para búsqueda rápida por UUID
    public Dictionary<string, DimensionDefinition> DimensionsMap; // UUID -> Definición Real
    public Dictionary<string, MarketAction> ActionsMap;           // UUID -> Acción Lógica

    // Perfiles Vivos
    public ConsumerProfile Consumer;
    public ProductProfile Product;
    
    // Lista visual para la UI
    public List<AccionInfo> AccionesVisuales;

    // Configuración de Eventos (Runtime)
    public List<EventConfig> EventosConfigurados;
    
    // Datos Generales
    public float PresupuestoInicial;
    public float AceptacionObjetivo;
    public string NombreConsumidor;
    public int EdadConsumidor;
}