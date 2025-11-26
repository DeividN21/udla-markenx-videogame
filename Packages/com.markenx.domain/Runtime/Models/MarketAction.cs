using System;
using System.Collections.Generic;
using System.Linq;

namespace MyProject.Domain.Models
{
  /// <summary>
  /// Represents a market action that modifies product profile dimensions.
  /// Market actions simulate real-world business strategies such as promotions,
  /// advertising campaigns, product updates, or market events.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Each action consists of:
  /// <list type="bullet">
  ///   <item><description><b>Name:</b> Identifier for the action (e.g., "Black Friday Sale")</description></item>
  ///   <item><description><b>Description:</b> Explanation of what the action does</description></item>
  ///   <item><description><b>Cost:</b> Budget required to execute the action</description></item>
  ///   <item><description><b>Effects:</b> Dimensional changes (deltas) applied to the product</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Effects can be positive (increase) or negative (decrease):
  /// <list type="bullet">
  ///   <item><description>Positive: +0.20 to BrandAwareness (advertising campaign)</description></item>
  ///   <item><description>Negative: -0.15 to PricePerceived (discount promotion)</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Create a Black Friday promotion
  /// var blackFriday = new MarketAction(
  ///     "Black Friday Sale",
  ///     "50% discount on selected products",
  ///     cost: 50000m
  /// );
  /// 
  /// // Define effects
  /// blackFriday.AddEffect(pricePerceivedDef, -0.30f);  // Reduce price perception
  /// blackFriday.AddEffect(valueForMoneyDef, 0.20f);    // Increase value perception
  /// 
  /// // Apply to product
  /// blackFriday.Apply(product);
  /// </code>
  /// </example>
  public class MarketAction
  {
    /// <summary>
    /// Gets the name of the action.
    /// Should be concise and descriptive (e.g., "Holiday Discount", "Super Bowl Ad").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the detailed description of what this action does and its business rationale.
    /// </summary>
    /// <example>
    /// "Annual holiday promotion offering 30% discount to boost end-of-year sales"
    /// </example>
    public string Description { get; }

    /// <summary>
    /// Gets the estimated cost to execute this action.
    /// Used for ROI analysis and budget planning.
    /// </summary>
    /// <value>
    /// Cost in monetary units (e.g., USD). Can be zero for no-cost actions like organic social media.
    /// </value>
    public decimal Cost { get; }

    /// <summary>
    /// Gets the dictionary of dimensional effects this action produces.
    /// Each effect is a delta value that will be added to the product's dimension.
    /// </summary>
    private Dictionary<DimensionDefinition, float> _effects;

    /// <summary>
    /// Initializes a new market action with specified name, description, and cost.
    /// Effects must be added separately using <see cref="AddEffect"/>.
    /// </summary>
    /// <param name="name">Action name (e.g., "Holiday Sale")</param>
    /// <param name="description">Detailed description of the action</param>
    /// <param name="cost">Budget required to execute (default: 0)</param>
    /// <exception cref="ArgumentNullException">Thrown when name or description is null</exception>
    /// <exception cref="ArgumentException">Thrown when cost is negative</exception>
    public MarketAction(string name, string description, decimal cost = 0m)
    {
      if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentNullException(nameof(name));
      if (string.IsNullOrWhiteSpace(description))
        throw new ArgumentNullException(nameof(description));
      if (cost < 0)
        throw new ArgumentException("Cost cannot be negative", nameof(cost));

      Name = name;
      Description = description;
      Cost = cost;
      _effects = new Dictionary<DimensionDefinition, float>();
    }

    /// <summary>
    /// Adds or updates a dimensional effect for this action.
    /// Positive deltas increase the dimension, negative deltas decrease it.
    /// </summary>
    /// <param name="def">The dimension to modify</param>
    /// <param name="delta">The change amount (can be positive or negative)</param>
    /// <exception cref="ArgumentNullException">Thrown when def is null</exception>
    /// <example>
    /// <code>
    /// action.AddEffect(pricePerceivedDef, -0.20f);    // Reduce price by 20%
    /// action.AddEffect(brandAwarenessDef, 0.30f);     // Increase awareness by 30%
    /// action.AddEffect(socialRecognitionDef, 0.15f);  // Boost social status by 15%
    /// </code>
    /// </example>
    public void AddEffect(DimensionDefinition def, float delta)
    {
      if (def == null)
        throw new ArgumentNullException(nameof(def));

      _effects[def] = delta;
    }

    /// <summary>
    /// Removes an effect from this action.
    /// </summary>
    /// <param name="def">The dimension to remove</param>
    /// <returns>True if the effect was removed, false if it didn't exist</returns>
    public bool RemoveEffect(DimensionDefinition def)
    {
      return def != null && _effects.Remove(def);
    }

    /// <summary>
    /// Gets the delta value for a specific dimension.
    /// </summary>
    /// <param name="def">The dimension to query</param>
    /// <returns>The delta value if found, otherwise null</returns>
    public float? GetEffect(DimensionDefinition def)
    {
      if (def != null && _effects.TryGetValue(def, out var delta))
        return delta;
      return null;
    }

    /// <summary>
    /// Checks if this action has an effect on a specific dimension.
    /// </summary>
    /// <param name="def">The dimension to check</param>
    /// <returns>True if an effect exists for this dimension</returns>
    public bool HasEffect(DimensionDefinition def)
    {
      return def != null && _effects.ContainsKey(def);
    }

    /// <summary>
    /// Gets all effects as a read-only collection.
    /// </summary>
    /// <returns>Collection of dimension-delta pairs</returns>
    public IReadOnlyDictionary<DimensionDefinition, float> GetAllEffects()
    {
      return _effects;
    }

    /// <summary>
    /// Gets the number of dimensional effects this action has.
    /// </summary>
    public int EffectCount => _effects.Count;

    /// <summary>
    /// Applies this action's effects to a product profile.
    /// For each effect, the delta is added to the product's corresponding dimension.
    /// If a dimension doesn't exist in the product, it will be created with the delta as initial value.
    /// </summary>
    /// <param name="profile">The product profile to modify</param>
    /// <exception cref="ArgumentNullException">Thrown when profile is null</exception>
    /// <example>
    /// <code>
    /// var promotion = new MarketAction("Summer Sale", "Seasonal discount", 25000m);
    /// promotion.AddEffect(pricePerceivedDef, -0.25f);
    /// promotion.AddEffect(urgencyDef, 0.30f);
    /// 
    /// promotion.Apply(product);  // Modifies product in-place
    /// </code>
    /// </example>
    public void Apply(ProductProfile profile)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof(profile));

      foreach (var kvp in _effects)
      {
        if (profile.Dimensions.TryGetValue(kvp.Key, out var existingDimension))
        {
          // Modify existing dimension
          existingDimension.Add(kvp.Value);
        }
        else
        {
          // Create new dimension with delta as initial value
          profile.Set(kvp.Key, kvp.Value);
        }
      }
    }

    /// <summary>
    /// Simulates applying this action without actually modifying the product.
    /// Returns a cloned profile with the action applied.
    /// </summary>
    /// <param name="profile">The product profile to simulate on</param>
    /// <returns>A new profile with the action applied</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null</exception>
    /// <example>
    /// <code>
    /// var original = GetProduct("iPhone");
    /// var projected = action.Simulate(original);
    /// 
    /// // Compare before/after without affecting original
    /// Console.WriteLine($"Original price: {original.Get(priceDef).Value}");
    /// Console.WriteLine($"After action: {projected.Get(priceDef).Value}");
    /// </code>
    /// </example>
    public ProductProfile Simulate(ProductProfile profile)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof(profile));

      var clone = profile.Clone();
      Apply(clone);
      return clone;
    }

    /// <summary>
    /// Calculates the estimated return on investment (ROI) for this action.
    /// </summary>
    /// <param name="projectedRevenue">Expected revenue increase from this action</param>
    /// <returns>ROI as a percentage (e.g., 1.5 = 150% ROI)</returns>
    /// <example>
    /// <code>
    /// var action = new MarketAction("Ad Campaign", "TV spots", 100000m);
    /// var roi = action.CalculateROI(250000m);  // Expected $250k revenue
    /// Console.WriteLine($"ROI: {roi * 100:F1}%");  // Output: ROI: 150.0%
    /// </code>
    /// </example>
    public decimal CalculateROI(decimal projectedRevenue)
    {
      if (Cost == 0)
        return projectedRevenue > 0 ? decimal.MaxValue : 0;

      return (projectedRevenue - Cost) / Cost;
    }

    /// <summary>
    /// Returns a summary string for debugging and logging.
    /// </summary>
    /// <example>
    /// Output:
    /// <code>
    /// MarketAction: Black Friday Sale (Cost: $50,000.00)
    ///   Effects (3):
    ///     PricePerceived: -0.30
    ///     ValueForMoney: +0.20
    ///     Urgency: +0.40
    /// </code>
    /// </example>
    public override string ToString()
    {
      var effects = string.Join("\n    ",
        _effects.Select(kvp => $"{kvp.Key.Name}: {(kvp.Value >= 0 ? "+" : "")}{kvp.Value:F2}"));

      return $"MarketAction: {Name} (Cost: ${Cost:N2})\n" +
             $"  Description: {Description}\n" +
             $"  Effects ({EffectCount}):\n    {effects}";
    }
  }
}