using UnityEngine;
using System.Collections.Generic;
using MarkenX.Api.Dtos;

namespace MarkenX.Api.Mappers
{
    /// <summary>
    /// Mapper para convertir DTOs de la API a modelos internos del juego.
    /// Mantiene compatibilidad con la estructura existente de PartidaDataPayload.
    /// </summary>
    public static class ScenarioDataMapper
    {
        // Diccionarios para mapeo de IDs (string API → int interno)
        private static Dictionary<string, int> _dimensionIdMap;
        private static Dictionary<string, int> _actionIdMap;
        private static Dictionary<string, int> _eventIdMap;

        // Diccionario inverso para convertir de vuelta (int interno → string API)
        private static Dictionary<int, string> _dimensionIdReverseMap;
        private static Dictionary<int, string> _actionIdReverseMap;

        #region Public Properties

        /// <summary>
        /// Obtiene el mapeo de IDs de acciones (string API → int interno).
        /// Útil para construir el historial de sesión.
        /// </summary>
        public static Dictionary<int, string> ActionIdReverseMap => _actionIdReverseMap;

        /// <summary>
        /// Obtiene el mapeo de IDs de dimensiones (string API → int interno).
        /// </summary>
        public static Dictionary<int, string> DimensionIdReverseMap => _dimensionIdReverseMap;

        #endregion

        #region Main Mapping Methods

        /// <summary>
        /// Convierte un ScenarioDetailResponse de la API a PartidaDataPayload interno.
        /// </summary>
        public static PartidaDataPayload ToPartidaDataPayload(ScenarioDetailResponse scenario)
        {
            if (scenario == null)
            {
                Debug.LogError("[ScenarioDataMapper] ScenarioDetailResponse es null");
                return null;
            }

            // Inicializar diccionarios de mapeo
            InitializeIdMappings(scenario);

            PartidaDataPayload payload = new PartidaDataPayload();

            // Mapear Consumer
            MapConsumer(scenario.consumer, payload);

            // Mapear Dimensions → perfilConsumidor
            MapDimensions(scenario.dimensions, payload);

            // Mapear Actions → accionesDisponibles + reglasImpacto
            MapActions(scenario.actions, payload);

            // Mapear Events → eventosPosibles + efectosEventos
            MapEvents(scenario.events, payload);

            // Inicializar historial vacío
            payload.historialTurnos = new List<Turno>();

            LogMappingResult(payload);

            return payload;
        }

        /// <summary>
        /// Convierte un ID de acción interno (int) a su ID de API (string).
        /// </summary>
        public static string GetApiActionId(int internalId)
        {
            if (_actionIdReverseMap != null && _actionIdReverseMap.TryGetValue(internalId, out string apiId))
                return apiId;
            return internalId.ToString();
        }

        /// <summary>
        /// Convierte un ID de dimensión interno (int) a su ID de API (string).
        /// </summary>
        public static string GetApiDimensionId(int internalId)
        {
            if (_dimensionIdReverseMap != null && _dimensionIdReverseMap.TryGetValue(internalId, out string apiId))
                return apiId;
            return internalId.ToString();
        }

        #endregion

        #region Private Mapping Methods

        private static void InitializeIdMappings(ScenarioDetailResponse scenario)
        {
            _dimensionIdMap = new Dictionary<string, int>();
            _actionIdMap = new Dictionary<string, int>();
            _eventIdMap = new Dictionary<string, int>();
            _dimensionIdReverseMap = new Dictionary<int, string>();
            _actionIdReverseMap = new Dictionary<int, string>();

            int idCounter = 100;

            // Mapear IDs de dimensiones
            if (scenario.dimensions != null)
            {
                foreach (var dim in scenario.dimensions)
                {
                    int internalId = TryParseIntOrAssign(dim.id, ref idCounter);
                    _dimensionIdMap[dim.id] = internalId;
                    _dimensionIdReverseMap[internalId] = dim.id;
                }
            }

            idCounter = 1;

            // Mapear IDs de acciones
            if (scenario.actions != null)
            {
                foreach (var action in scenario.actions)
                {
                    int internalId = TryParseIntOrAssign(action.id, ref idCounter);
                    _actionIdMap[action.id] = internalId;
                    _actionIdReverseMap[internalId] = action.id;
                }
            }

            idCounter = 1;

            // Mapear IDs de eventos
            if (scenario.events != null)
            {
                foreach (var evt in scenario.events)
                {
                    int internalId = TryParseIntOrAssign(evt.id, ref idCounter);
                    _eventIdMap[evt.id] = internalId;
                }
            }
        }

        private static int TryParseIntOrAssign(string stringId, ref int counter)
        {
            if (int.TryParse(stringId, out int parsed))
                return parsed;
            return counter++;
        }

        private static void MapConsumer(ConsumerDto consumer, PartidaDataPayload payload)
        {
            if (consumer == null)
            {
                Debug.LogWarning("[ScenarioDataMapper] Consumer es null, usando valores por defecto");
                payload.nombreConsumidor = "Consumidor";
                payload.edadConsumidor = 25;
                payload.presupuestoInicial = 1000;
                payload.aceptacionObjetivo = 80f;
                return;
            }

            payload.nombreConsumidor = consumer.name ?? "Consumidor";
            payload.edadConsumidor = consumer.age;
            payload.presupuestoInicial = (int)consumer.budget;
            // targetAcceptanceScore viene como decimal (0.0-1.0), convertir a porcentaje
            payload.aceptacionObjetivo = consumer.targetAcceptanceScore * 100f;
        }

        private static void MapDimensions(List<DimensionDto> dimensions, PartidaDataPayload payload)
        {
            payload.perfilConsumidor = new List<EscenarioPerfilSubfactor>();

            if (dimensions == null || dimensions.Count == 0)
            {
                Debug.LogWarning("[ScenarioDataMapper] No hay dimensiones en el escenario");
                return;
            }

            foreach (var dim in dimensions)
            {
                int internalId = _dimensionIdMap.GetValueOrDefault(dim.id, 0);

                // consumerExpectation representa el peso/importancia para el consumidor
                // productInitialOffer representa la oferta inicial del producto
                // La diferencia determina cuánto hay que mejorar
                var perfilSubfactor = new EscenarioPerfilSubfactor
                {
                    idSubfactor = internalId,
                    peso = dim.consumerExpectation * 10f, // Escalar peso para compatibilidad
                    esVisibleInicialmente = false // Por defecto ocultos, se descubren con exploración
                };

                payload.perfilConsumidor.Add(perfilSubfactor);
            }
        }

        private static void MapActions(List<ActionDto> actions, PartidaDataPayload payload)
        {
            payload.accionesDisponibles = new List<Accion>();
            payload.reglasImpacto = new List<AccionSubfactorImpacto>();

            if (actions == null || actions.Count == 0)
            {
                Debug.LogWarning("[ScenarioDataMapper] No hay acciones en el escenario");
                return;
            }

            foreach (var actionDto in actions)
            {
                int internalId = _actionIdMap.GetValueOrDefault(actionDto.id, 0);

                // Mapear acción
                var accion = new Accion
                {
                    idAccion = internalId,
                    categoria = MapCategory(actionDto.category),
                    nombreAccion = actionDto.name ?? "",
                    descripcion = actionDto.description ?? "",
                    costo = actionDto.cost,
                    esBloqueadaInicialmente = actionDto.isInitiallyLocked,
                    idAccionRequerida = GetPrerequisiteActionId(actionDto.prerequisiteActionId)
                };

                payload.accionesDisponibles.Add(accion);

                // Mapear efectos → reglas de impacto
                if (actionDto.effects != null)
                {
                    foreach (var effect in actionDto.effects)
                    {
                        int dimensionId = _dimensionIdMap.GetValueOrDefault(effect.dimensionId, 0);

                        var regla = new AccionSubfactorImpacto
                        {
                            idAccion = internalId,
                            idSubfactor = dimensionId,
                            impacto = (int)(effect.delta * 100) // delta viene como decimal, escalar
                        };

                        payload.reglasImpacto.Add(regla);
                    }
                }
            }
        }

        private static int GetPrerequisiteActionId(string prerequisiteId)
        {
            if (string.IsNullOrEmpty(prerequisiteId))
                return 0;

            if (_actionIdMap.TryGetValue(prerequisiteId, out int internalId))
                return internalId;

            return 0;
        }

        private static string MapCategory(string apiCategory)
        {
            if (string.IsNullOrEmpty(apiCategory))
                return "OTROS";

            // Mapear categorías de la API a las categorías internas del juego
            string normalized = apiCategory.ToUpperInvariant().Trim();

            // Mapeo directo si coincide
            switch (normalized)
            {
                case "ATRIBUTOS_PRODUCCION":
                case "PRODUCCION":
                case "PRODUCTION":
                    return "ATRIBUTOS_PRODUCCION";

                case "ATRIBUTOS_DISENO":
                case "DISENO":
                case "DESIGN":
                    return "ATRIBUTOS_DISENO";

                case "ATRIBUTOS_PRECIO":
                case "PRECIO":
                case "PRICE":
                    return "ATRIBUTOS_PRECIO";

                case "ATRIBUTOS_PLAZA":
                case "PLAZA":
                case "PLACE":
                case "DISTRIBUTION":
                    return "ATRIBUTOS_PLAZA";

                case "EXPLORACION":
                case "EXPLORATION":
                case "RESEARCH":
                    return "EXPLORACION";

                case "PUBLICIDAD":
                case "ADVERTISING":
                case "PROMOTION":
                case "MARKETING":
                    return "PUBLICIDAD";

                default:
                    // Mantener categoría original en mayúsculas
                    return normalized;
            }
        }

        private static void MapEvents(List<EventDto> events, PartidaDataPayload payload)
        {
            payload.eventosPosibles = new List<Evento>();
            payload.efectosEventos = new List<EventoEfecto>();

            if (events == null || events.Count == 0)
            {
                Debug.Log("[ScenarioDataMapper] No hay eventos en el escenario");
                return;
            }

            foreach (var eventDto in events)
            {
                int internalId = _eventIdMap.GetValueOrDefault(eventDto.id, 0);

                // Mapear evento
                var evento = new Evento
                {
                    idEvento = internalId,
                    tituloNoticia = eventDto.title ?? "",
                    detalleNoticia = eventDto.description ?? ""
                };

                payload.eventosPosibles.Add(evento);

                // Mapear efectos del evento
                if (eventDto.effects != null)
                {
                    foreach (var effect in eventDto.effects)
                    {
                        int dimensionId = _dimensionIdMap.GetValueOrDefault(effect.dimensionId, 0);

                        var efecto = new EventoEfecto
                        {
                            idEvento = internalId,
                            idSubfactor = dimensionId,
                            modificadorPeso = effect.weightMultiplier
                        };

                        payload.efectosEventos.Add(efecto);
                    }
                }
            }
        }

        private static void LogMappingResult(PartidaDataPayload payload)
        {
            Debug.Log("[ScenarioDataMapper] === Mapeo Completado ===");
            Debug.Log($"[ScenarioDataMapper] Consumidor: {payload.nombreConsumidor}, {payload.edadConsumidor} años");
            Debug.Log($"[ScenarioDataMapper] Presupuesto: {payload.presupuestoInicial}");
            Debug.Log($"[ScenarioDataMapper] Objetivo Aceptación: {payload.aceptacionObjetivo}%");
            Debug.Log($"[ScenarioDataMapper] Subfactores: {payload.perfilConsumidor?.Count ?? 0}");
            Debug.Log($"[ScenarioDataMapper] Acciones: {payload.accionesDisponibles?.Count ?? 0}");
            Debug.Log($"[ScenarioDataMapper] Reglas de Impacto: {payload.reglasImpacto?.Count ?? 0}");
            Debug.Log($"[ScenarioDataMapper] Eventos: {payload.eventosPosibles?.Count ?? 0}");
            Debug.Log($"[ScenarioDataMapper] Efectos de Eventos: {payload.efectosEventos?.Count ?? 0}");
        }

        #endregion
    }
}
