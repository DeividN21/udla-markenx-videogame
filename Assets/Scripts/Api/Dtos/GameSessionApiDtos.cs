using System;
using System.Collections.Generic;

namespace MarkenX.Api.Dtos
{
    // =================================================================================
    // DTOs para POST /api/v1/game-sessions
    // Alineados 1:1 con la API REST de Spring Boot
    // =================================================================================

    /// <summary>
    /// Request body para registrar una sesión de juego.
    /// POST /api/v1/game-sessions
    /// </summary>
    [Serializable]
    public class RegisterGameSessionRequest
    {
        public string taskId;
        public string studentId;
        public string sessionDate; // ISO-8601: "2026-01-07T13:12:15.568Z"
        public float finalAcceptance;
        public float remainingBudget;
        public int totalTurnsUsed;
        public float profileDiscoveryPercentage;
        public List<TurnHistoryDto> history;
    }

    /// <summary>
    /// Historial de un turno individual.
    /// </summary>
    [Serializable]
    public class TurnHistoryDto
    {
        public int turnNumber;
        public float acceptanceAtEnd;
        public float budgetAtEnd;
        public string eventOccurredTitle;
        public List<string> actionsTakenIds;
    }

    /// <summary>
    /// Response del servidor al registrar una sesión.
    /// El backend calcula finalOutcome (GANASTE/PERDISTE).
    /// </summary>
    [Serializable]
    public class GameSessionResponse
    {
        public string id;
        public string taskId;
        public string studentId;
        public string sessionDate;
        public float finalAcceptance;
        public float remainingBudget;
        public int totalTurnsUsed;
        public float profileDiscoveryPercentage;
        public string finalOutcome; // "GANASTE" o "PERDISTE"
        public List<TurnHistoryDto> history;
    }
}
