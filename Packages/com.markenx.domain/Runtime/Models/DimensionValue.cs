using System;

namespace MyProject.Domain.Models
{
  /// <summary>
  /// Represents the actual value (0–1) of a dimension for a specific consumer or product.
  /// While <see cref="DimensionDefinition"/> defines what a dimension means,
  /// this class stores the actual measurement or intensity for that dimension.
  /// </summary>
  /// <remarks>
  /// <para>All values are normalized to the range [0, 1] where:</para>
  /// <list type="bullet">
  ///   <item><description>0 = None/Minimum (e.g., free price, no interest)</description></item>
  ///   <item><description>0.5 = Average/Moderate</description></item>
  ///   <item><description>1 = Maximum/Very High (e.g., very expensive, extremely important)</description></item>
  /// </list>
  /// <para>Values outside this range are automatically clamped to ensure consistency.</para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var priceDef = new DimensionDefinition("PriceSensitivity", "How much the consumer cares about price");
  /// var priceValue = new DimensionValue(priceDef, 0.8f); // High price sensitivity
  /// 
  /// priceValue.Add(0.3f); // Value becomes 1.0 (clamped)
  /// priceValue.Add(-0.5f); // Value becomes 0.5
  /// </code>
  /// </example>
  public class DimensionValue
  {
    /// <summary>
    /// Gets the definition that describes what this dimension represents.
    /// Links this value to its conceptual meaning.
    /// </summary>
    public DimensionDefinition Definition { get; }

    /// <summary>
    /// Gets the current normalized value (always between 0 and 1 inclusive).
    /// </summary>
    /// <value>
    /// A float in the range [0, 1] where:
    /// <list type="bullet">
    ///   <item>0 = Minimum intensity (e.g., not important at all, free)</item>
    ///   <item>1 = Maximum intensity (e.g., extremely important, very expensive)</item>
    /// </list>
    /// </value>
    public float Value { get; private set; }

    /// <summary>
    /// Initializes a new dimension value with automatic clamping to [0, 1].
    /// </summary>
    /// <param name="definition">The dimension definition this value belongs to</param>
    /// <param name="value">The initial value (will be clamped to 0–1 range)</param>
    /// <exception cref="ArgumentNullException">Thrown when definition is null</exception>
    /// <example>
    /// <code>
    /// var qualityDef = new DimensionDefinition("Quality", "Product quality level");
    /// var qualityValue = new DimensionValue(qualityDef, 1.5f); // Clamped to 1.0
    /// </code>
    /// </example>
    public DimensionValue(DimensionDefinition definition, float value)
    {
      Definition = definition ?? throw new ArgumentNullException(nameof(definition));
      Value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Modifies the current value by adding a delta, automatically clamping the result to [0, 1].
    /// Useful for applying rules, adjustments, or simulating changes over time.
    /// </summary>
    /// <param name="delta">The amount to add (can be negative to subtract)</param>
    /// <example>
    /// <code>
    /// var brandLoyalty = new DimensionValue(brandDef, 0.5f);
    /// brandLoyalty.Add(0.2f);  // Now 0.7
    /// brandLoyalty.Add(-0.1f); // Now 0.6
    /// brandLoyalty.Add(0.8f);  // Now 1.0 (clamped)
    /// </code>
    /// </example>
    public void Add(float delta)
    {
      Value = Math.Clamp(Value + delta, 0f, 1f);
    }

    /// <summary>
    /// Sets the value directly, with automatic clamping to [0, 1].
    /// </summary>
    /// <param name="newValue">The new value to set</param>
    public void SetValue(float newValue)
    {
      Value = Math.Clamp(newValue, 0f, 1f);
    }

    /// <summary>
    /// Returns a string representation for debugging.
    /// </summary>
    public override string ToString() => $"{Definition.Name} = {Value:F2}";
  }
}