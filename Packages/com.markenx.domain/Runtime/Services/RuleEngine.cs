using System;
using MyProject.Domain.Models;

namespace MyProject.Domain.Services
{
    /// <summary>
    /// Provides a simple rule-based mechanism for converting real-world documents 
    /// (e.g., DNI, payroll, psychological tests) into normalized dimension values.
    ///
    /// The engine applies formulas ("rules") that transform raw attributes such as 
    /// age, income, or personality traits into numerical dimensions used by the 
    /// consumer model (e.g., PriceSensitivity, SocialRecognition).
    ///
    /// This keeps dimension calculation flexible: each rule is independent and 
    /// no hard-coded conditional logic (if/else per dimension) is required. 
    /// New dimensions can be added simply by registering new rules.
    /// </summary>
    public static class RuleEngine
    {
        /// <summary>
        /// Applies demographic-related transformations based on the data found in the DNI.
        /// The method computes normalized values and stores them into the consumer profile.
        ///
        /// Rules used:
        /// - SocialRecognition: modeled by age, assuming younger individuals tend to
        ///   have stronger social-recognition needs. The result is normalized to [0,1].
        ///
        /// - PriceSensitivity: inversely related to income. Lower income yields higher 
        ///   sensitivity to price. The result is clamped to [0,1].
        ///
        /// These rules are intentionally simple examples; the real system can expand 
        /// them or register alternate formulas without modifying the RuleEngine logic.
        /// </summary>
        public static void ApplyDemographicRules(
            DNI dni,
            ConsumerProfile consumer,
            DimensionDefinition priceSensitivity,
            DimensionDefinition socialRecognition)
        {
            // Social recognition decreases as age increases (normalized 0-1).
            float ageFactor = Math.Clamp((100 - dni.Age) / 100f, 0f, 1f);
            consumer.Set(socialRecognition, ageFactor);

            // Lower income produces higher price sensitivity (normalized 0-1).
            float incomeFactor = Math.Clamp(1f - (dni.MonthlyIncome / 3000f), 0f, 1f);
            consumer.Set(priceSensitivity, incomeFactor);
        }
    }
}
