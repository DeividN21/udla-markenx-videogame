using System;
using System.Collections.Generic;

namespace MarkenX.Api.Dtos
{
    // =================================================================================
    // DTOs para GET /api/v1/scenarios/{id}
    // Alineados 1:1 con la API REST de Spring Boot
    // =================================================================================

    [Serializable]
    public class ScenarioDetailResponse
    {
        public string id;
        public string title;
        public string description;
        public ConsumerDto consumer;
        public List<DimensionDto> dimensions;
        public List<ActionDto> actions;
        public List<EventDto> events;
    }

    [Serializable]
    public class ConsumerDto
    {
        public string id;
        public string name;
        public int age;
        public float budget;
        public float targetAcceptanceScore;
    }

    [Serializable]
    public class DimensionDto
    {
        public string id;
        public string name;
        public string displayName;
        public string description;
        public float consumerExpectation;
        public float productInitialOffer;
    }

    [Serializable]
    public class ActionDto
    {
        public string id;
        public string name;
        public string description;
        public float cost;
        public string category;
        public bool isInitiallyLocked;
        public string prerequisiteActionId;
        public List<ActionEffectDto> effects;
    }

    [Serializable]
    public class ActionEffectDto
    {
        public string dimensionId;
        public float delta;
    }

    [Serializable]
    public class EventDto
    {
        public string id;
        public string title;
        public string description;
        public List<EventEffectDto> effects;
    }

    [Serializable]
    public class EventEffectDto
    {
        public string dimensionId;
        public float weightMultiplier;
    }
}
