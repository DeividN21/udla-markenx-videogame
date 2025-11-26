using System;
using System.Collections.Generic;
using System.Linq;

namespace MyProject.Domain.Models
{
  /// <summary>
  /// Represents a product as a multidimensional profile vector.
  /// Contains a collection of <see cref="DimensionValue"/> entries that describe
  /// the product's characteristics, positioning, and market attributes.
  /// </summary>
  /// <remarks>
  /// <para>
  /// While <see cref="ConsumerProfile"/> describes what a consumer wants,
  /// ProductProfile describes what a product offers. Both use the same dimensional
  /// framework, enabling direct mathematical comparison for matching purposes.
  /// </para>
  /// <para>
  /// Product profiles can be dynamically modified by market actions such as:
  /// <list type="bullet">
  ///   <item><description>Promotions (discounts, special offers)</description></item>
  ///   <item><description>Campaigns (advertising, brand awareness)</description></item>
  ///   <item><description>Events (seasonal adjustments, market changes)</description></item>
  ///   <item><description>Repositioning (changing brand perception)</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// In marketing terms, product dimensions map to the 4Ps (Product, Price, Place, Promotion):
  /// <list type="bullet">
  ///   <item><description><b>Product:</b> Quality, EaseOfUse, Innovation, Durability</description></item>
  ///   <item><description><b>Price:</b> PricePerceived (0=free, 1=very expensive)</description></item>
  ///   <item><description><b>Place:</b> Availability, Accessibility, Distribution</description></item>
  ///   <item><description><b>Promotion:</b> BrandAwareness, SocialRecognition, MarketingReach</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var iPhone = new ProductProfile();
  /// iPhone.Set(pricePerceivedDef, 0.90f);      // Very expensive
  /// iPhone.Set(qualityDef, 0.95f);             // Excellent quality
  /// iPhone.Set(socialRecognitionDef, 0.95f);   // High status symbol
  /// iPhone.Set(easeOfUseDef, 0.90f);           // Very user-friendly
  /// 
  /// // Apply a promotion (10% discount)
  /// var discountAction = new DiscountAction(0.10f);
  /// iPhone.ApplyAction(discountAction);
  /// </code>
  /// </example>
  public class ProductProfile
  {
    /// <summary>
    /// Gets the dictionary mapping dimension definitions to their corresponding values.
    /// Each entry represents one aspect of what the product offers.
    /// </summary>
    public Dictionary<DimensionDefinition, DimensionValue> Dimensions { get; }

    /// <summary>
    /// Initializes a new empty product profile.
    /// Dimensions must be added using the <see cref="Set"/> method.
    /// </summary>
    public ProductProfile()
    {
      Dimensions = new Dictionary<DimensionDefinition, DimensionValue>();
    }

    /// <summary>
    /// Sets or updates a dimension value in the profile.
    /// If the dimension already exists, it will be replaced.
    /// </summary>
    /// <param name="def">The dimension definition to set</param>
    /// <param name="value">The normalized value (0–1, automatically clamped)</param>
    /// <exception cref="ArgumentNullException">Thrown when def is null</exception>
    /// <example>
    /// <code>
    /// product.Set(qualityDef, 0.85f);           // High quality product
    /// product.Set(pricePerceivedDef, 0.30f);    // Affordable price
    /// product.Set(innovationDef, 0.70f);        // Moderately innovative
    /// </code>
    /// </example>
    public void Set(DimensionDefinition def, float value)
    {
      if (def == null)
        throw new ArgumentNullException(nameof(def));

      Dimensions[def] = new DimensionValue(def, value);
    }

    /// <summary>
    /// Retrieves the dimension value for a specific definition.
    /// </summary>
    /// <param name="def">The dimension definition to look up</param>
    /// <returns>The dimension value if found, otherwise null</returns>
    /// <example>
    /// <code>
    /// var priceValue = product.Get(pricePerceivedDef);
    /// if (priceValue != null)
    /// {
    ///     Console.WriteLine($"Price level: {priceValue.Value}");
    /// }
    /// </code>
    /// </example>
    public DimensionValue Get(DimensionDefinition def)
    {
      return Dimensions.TryGetValue(def, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if the profile contains a specific dimension.
    /// </summary>
    /// <param name="def">The dimension definition to check</param>
    /// <returns>True if the dimension exists in the profile, false otherwise</returns>
    public bool HasDimension(DimensionDefinition def)
    {
      return def != null && Dimensions.ContainsKey(def);
    }

    /// <summary>
    /// Modifies an existing dimension value by adding a delta.
    /// If the dimension doesn't exist, it will be created with the delta as initial value.
    /// Commonly used by market actions to adjust product positioning.
    /// </summary>
    /// <param name="def">The dimension definition to modify</param>
    /// <param name="delta">The amount to add (can be negative)</param>
    /// <exception cref="ArgumentNullException">Thrown when def is null</exception>
    /// <example>
    /// <code>
    /// // Price reduction campaign
    /// product.Adjust(pricePerceivedDef, -0.15f);
    /// 
    /// // Quality improvement after product update
    /// product.Adjust(qualityDef, 0.10f);
    /// 
    /// // Brand awareness increase from advertising
    /// product.Adjust(brandAwarenessDef, 0.25f);
    /// </code>
    /// </example>
    public void Adjust(DimensionDefinition def, float delta)
    {
      if (def == null)
        throw new ArgumentNullException(nameof(def));

      if (Dimensions.TryGetValue(def, out var existing))
      {
        existing.Add(delta);
      }
      else
      {
        Dimensions[def] = new DimensionValue(def, delta);
      }
    }

    /// <summary>
    /// Applies a market action to modify this product profile.
    /// Market actions can represent promotions, campaigns, events, or any strategic change.
    /// </summary>
    /// <param name="action">The market action to apply</param>
    /// <exception cref="ArgumentNullException">Thrown when action is null</exception>
    /// <example>
    /// <code>
    /// // Black Friday promotion
    /// var blackFriday = new PromotionAction("BlackFriday", 
    ///     pricePerceivedDef, -0.30f);
    /// product.ApplyAction(blackFriday);
    /// 
    /// // Super Bowl advertising campaign
    /// var superBowlAd = new CampaignAction("SuperBowl",
    ///     brandAwarenessDef, 0.40f,
    ///     socialRecognitionDef, 0.20f);
    /// product.ApplyAction(superBowlAd);
    /// </code>
    /// </example>
    public void ApplyAction(MarketAction action)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      action.Apply(this);
    }

    /// <summary>
    /// Returns all dimension values as a read-only collection.
    /// Useful for iteration, comparison, and analysis.
    /// </summary>
    /// <returns>A collection of all dimension values in the profile</returns>
    public IEnumerable<DimensionValue> GetAllValues()
    {
      return Dimensions.Values;
    }

    /// <summary>
    /// Gets the number of dimensions in this profile.
    /// </summary>
    public int Count => Dimensions.Count;

    /// <summary>
    /// Creates a deep copy of this product profile.
    /// Useful for simulating market actions without modifying the original.
    /// </summary>
    /// <returns>A new ProductProfile with the same dimension values</returns>
    /// <example>
    /// <code>
    /// var original = GetProductProfile("iPhone");
    /// var simulation = original.Clone();
    /// 
    /// // Test promotion impact without affecting original
    /// simulation.ApplyAction(newPromotionAction);
    /// var projectedSales = CalculateSales(simulation);
    /// </code>
    /// </example>
    public ProductProfile Clone()
    {
      var clone = new ProductProfile();
      foreach (var kvp in Dimensions)
      {
        clone.Set(kvp.Key, kvp.Value.Value);
      }
      return clone;
    }

    /// <summary>
    /// Returns a string representation of the profile for debugging.
    /// </summary>
    /// <example>
    /// Output example:
    /// <code>
    /// ProductProfile (4 dimensions):
    ///   PricePerceived = 0.60
    ///   Quality = 0.70
    ///   SocialRecognition = 0.80
    ///   EaseOfUse = 0.40
    /// </code>
    /// </example>
    public override string ToString()
    {
      var dimensions = string.Join("\n  ",
        Dimensions.Values.Select(dv => $"{dv.Definition.Name} = {dv.Value:F2}"));
      return $"ProductProfile ({Count} dimensions):\n  {dimensions}";
    }
  }
}