using System;

namespace MyProject.Domain.Models
{
  /// <summary>
  /// Represents a single dimension used to evaluate products and consumer preferences.
  /// A dimension is a measurable attribute or psychological factor that influences purchasing decisions.
  /// Examples: "PriceSensitivity", "QualityExpectation", "SocialRecognition", "EaseOfUse".
  /// </summary>
  /// <remarks>
  /// This class serves as a template that defines what a dimension represents.
  /// It does NOT store actual values - those are stored separately in DimensionValue objects.
  /// Think of this as the "column header" in a spreadsheet, while DimensionValue contains the "cell data".
  /// </remarks>
  public class DimensionDefinition
  {
    /// <summary>
    /// Gets the unique identifier for this dimension.
    /// Should be concise and descriptive (e.g., "PriceSensitivity", "BrandLoyalty").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a human-readable explanation of what this dimension measures.
    /// Helps stakeholders understand the meaning and purpose of the dimension.
    /// </summary>
    /// <example>
    /// "Measures how much the consumer cares about the price of a product"
    /// </example>
    public string Description { get; }

    /// <summary>
    /// Initializes a new dimension definition.
    /// </summary>
    /// <param name="name">Unique name for the dimension (e.g., "PriceSensitivity")</param>
    /// <param name="description">Clear explanation of what this dimension represents</param>
    /// <exception cref="ArgumentNullException">Thrown when name or description is null</exception>
    public DimensionDefinition(string name, string description)
    {
      Name = name ?? throw new ArgumentNullException(nameof(name));
      Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>
    /// Returns a string representation of this dimension for debugging purposes.
    /// </summary>
    public override string ToString() => $"{Name}: {Description}";
  }
}