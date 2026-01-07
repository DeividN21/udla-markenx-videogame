using System.Collections.Generic;
using UnityEngine; // Necesario para JsonUtility y Debug

public static class MockDataFactory
{
    // JSON DE PRUEBA (Solo se usa si no hay API)
    private static string jsonEcoScenario = @"
    {
        ""dimensions"": [
            {
                ""id"": ""dim-price"",
                ""name"": ""PriceSensitivity"",
                ""displayName"": ""Sensibilidad al Precio"",
                ""description"": ""Importancia del ahorro"",
                ""consumerExpectation"": 0.4,
                ""productInitialOffer"": 0.5
            },
            {
                ""id"": ""dim-social"",
                ""name"": ""SocialRecognition"",
                ""displayName"": ""Estatus Social"",
                ""description"": ""Necesidad de reconocimiento"",
                ""consumerExpectation"": 0.6,
                ""productInitialOffer"": 0.3
            },
            {
                ""id"": ""dim-quality"",
                ""name"": ""QualityExpectation"",
                ""displayName"": ""Calidad Esperada"",
                ""description"": ""Exigencia de durabilidad"",
                ""consumerExpectation"": 0.8,
                ""productInitialOffer"": 0.4
            },
            {
                ""id"": ""dim-eco"",
                ""name"": ""EcoInterest"",
                ""displayName"": ""Interés Ecológico"",
                ""description"": ""Preocupación ambiental"",
                ""consumerExpectation"": 0.95,
                ""productInitialOffer"": 0.1
            },
            {
                ""id"": ""dim-ease"",
                ""name"": ""EaseOfUse"",
                ""displayName"": ""Facilidad de Uso"",
                ""description"": ""Simplicidad"",
                ""consumerExpectation"": 0.7,
                ""productInitialOffer"": 0.5
            }
        ],
        ""consumer"": {
            ""name"": ""Barry Seal"",
            ""age"": 30,
            ""budget"": 1200,
            ""targetAcceptanceScore"": 0.80
        },
        ""actions"": [
            {
                ""id"": ""act-pack-recycle"",
                ""name"": ""Empaque Reciclado"",
                ""description"": ""Cartón 100% reciclado."",
                ""cost"": 150,
                ""category"": ""ATRIBUTOS_PRODUCCION"",
                ""isInitiallyLocked"": false,
                ""effects"": [ { ""dimensionId"": ""dim-eco"", ""delta"": 0.30 } ]
            },
            {
                ""id"": ""act-bio-mat"",
                ""name"": ""Mat. Biodegradable"",
                ""description"": ""Se degrada en 30 días."",
                ""cost"": 200,
                ""category"": ""ATRIBUTOS_PRODUCCION"",
                ""isInitiallyLocked"": true,
                ""prerequisiteActionId"": ""act-pack-recycle"",
                ""effects"": [ { ""dimensionId"": ""dim-eco"", ""delta"": 0.40 }, { ""dimensionId"": ""dim-quality"", ""delta"": 0.10 } ]
            },
            {
                ""id"": ""act-local"",
                ""name"": ""Producción Local"",
                ""description"": ""Menor huella de carbono."",
                ""cost"": 100,
                ""category"": ""ATRIBUTOS_PRODUCCION"",
                ""isInitiallyLocked"": true,
                ""prerequisiteActionId"": ""act-pack-recycle"",
                ""effects"": [ { ""dimensionId"": ""dim-eco"", ""delta"": 0.15 } ]
            },
            {
                ""id"": ""act-carbon"",
                ""name"": ""Cert. Carbono Neutro"",
                ""description"": ""Sello internacional."",
                ""cost"": 250,
                ""category"": ""ATRIBUTOS_PRODUCCION"",
                ""isInitiallyLocked"": true,
                ""prerequisiteActionId"": ""act-bio-mat"",
                ""effects"": [ { ""dimensionId"": ""dim-eco"", ""delta"": 0.40 } ]
            },
            {
                ""id"": ""act-label"",
                ""name"": ""Etiqueta Verde"",
                ""description"": ""Look natural."",
                ""cost"": 100,
                ""category"": ""ATRIBUTOS_DISENO"",
                ""isInitiallyLocked"": false,
                ""effects"": [ { ""dimensionId"": ""dim-social"", ""delta"": 0.10 } ]
            },
            {
                ""id"": ""act-research-1"",
                ""name"": ""Encuesta General"",
                ""description"": ""Datos básicos."",
                ""cost"": 100,
                ""category"": ""EXPLORACION"",
                ""isInitiallyLocked"": false,
                ""effects"": []
            },
            {
                ""id"": ""act-research-2"",
                ""name"": ""Focus Group Eco"",
                ""description"": ""Valores ambientales."",
                ""cost"": 200,
                ""category"": ""EXPLORACION"",
                ""isInitiallyLocked"": true,
                ""prerequisiteActionId"": ""act-research-1"",
                ""effects"": []
            }
        ],
        ""events"": [
            {
                ""id"": ""evt-green-world"",
                ""title"": ""EL MUNDO SE VUELVE MÁS VERDE"",
                ""description"": ""Impulso global por la sostenibilidad."",
                ""effects"": [ { ""dimensionId"": ""dim-eco"", ""weightMultiplier"": 3.0 } ]
            }
        ]
    }";

    // Función para obtener el JSON de prueba (Testing)
    public static string GetMockJson() => jsonEcoScenario;

    // MOTOR DE PARSEO
    // Esta función recibe CUALQUIER string JSON
    // y devuelve el objeto GameContext listo para jugar.
    public static GameContext CrearContextoDesdeJSON(string jsonInput)
    {
        // 1. DESERIALIZAR
        GameScenarioConfig config = JsonUtility.FromJson<GameScenarioConfig>(jsonInput);
        
        if (config == null) {
            Debug.LogError("Error al parsear el JSON. Formato inválido.");
            return null;
        }

        GameContext ctx = new GameContext();
        ctx.DimensionsMap = new Dictionary<string, DimensionDefinition>();
        ctx.ActionsMap = new Dictionary<string, MarketAction>();
        ctx.AccionesVisuales = new List<AccionInfo>();
        ctx.EventosConfigurados = config.events ?? new List<EventConfig>();

        // 2. CONFIGURAR DIMENSIONES
        foreach(var dimConfig in config.dimensions)
        {
            var def = new DimensionDefinition(dimConfig.name, dimConfig.description);
            ctx.DimensionsMap.Add(dimConfig.id, def);
        }

        // 3. CONFIGURAR PERFILES
        ctx.Consumer = new ConsumerProfile();
        ctx.Product = new ProductProfile();

        foreach(var dimConfig in config.dimensions)
        {
            if(ctx.DimensionsMap.TryGetValue(dimConfig.id, out var def))
            {
                ctx.Consumer.Set(def, dimConfig.consumerExpectation);
                ctx.Product.Set(def, dimConfig.productInitialOffer);
            }
        }

        ctx.NombreConsumidor = config.consumer.name;
        ctx.EdadConsumidor = config.consumer.age;
        ctx.PresupuestoInicial = config.consumer.budget;
        ctx.AceptacionObjetivo = config.consumer.targetAcceptanceScore;

        // 4. CONFIGURAR ACCIONES
        foreach(var actConfig in config.actions)
        {
            // A. Lógica (Core)
            var actionLogic = new MarketAction(actConfig.name, actConfig.description, (decimal)actConfig.cost);
            
            // Añadir efectos a la acción lógica
            foreach(var effect in actConfig.effects)
            {
                if(ctx.DimensionsMap.TryGetValue(effect.dimensionId, out var dimDef))
                {
                    actionLogic.AddEffect(dimDef, effect.delta);
                }
            }
            ctx.ActionsMap.Add(actConfig.id, actionLogic);

            // B. Visual (Unity UI)
            // Se genera un ID numérico temporal (hash) para que la UI vieja funcione
            // pero se guarda el UUID real para la lógica.
            int tempId = Mathf.Abs(actConfig.id.GetHashCode()); 

            ctx.AccionesVisuales.Add(new AccionInfo {
                idAccion = tempId, // ID temporal para la UI existente
                originalUuid = actConfig.id, // El ID real de la API
                nombreAccion = actConfig.name,
                descripcion = actConfig.description,
                costo = actConfig.cost,
                categoria = actConfig.category,
                esBloqueadaInicialmente = actConfig.isInitiallyLocked,
                idAccionRequeridaUuid = actConfig.prerequisiteActionId
            });
        }

        // Post-proceso: Enlazar prerequisitos visuales usando los IDs temporales
        foreach(var visual in ctx.AccionesVisuales)
        {
            if(!string.IsNullOrEmpty(visual.idAccionRequeridaUuid))
            {
                var padre = ctx.AccionesVisuales.Find(a => a.originalUuid == visual.idAccionRequeridaUuid);
                if(padre != null) visual.idAccionRequerida = padre.idAccion;
            }
        }

        return ctx;
    }
}