using System;
using System.Collections.Generic;
using MyProject.Domain.Models;

namespace MyProject.Domain.Services
{
  /// <summary>
  /// Computes similarity metrics between consumer and product profiles.
  /// Provides multiple distance calculation methods to measure how well a product
  /// matches a consumer's preferences and needs.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The distance metric is the mathematical foundation for product recommendation.
  /// It answers the question: "How compatible is this product with this consumer?"
  /// </para>
  /// <para>
  /// Available metrics:
  /// <list type="bullet">
  ///   <item><description><b>Manhattan Distance:</b> Sum of absolute differences (default, most intuitive)</description></item>
  ///   <item><description><b>Euclidean Distance:</b> Straight-line distance in multidimensional space</description></item>
  ///   <item><description><b>Cosine Similarity:</b> Measures angle between vectors (direction-focused)</description></item>
  ///   <item><description><b>Weighted Distance:</b> Allows prioritizing certain dimensions</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// All methods return an acceptance score between 0 and 1:
  /// <list type="bullet">
  ///   <item><description>1.0 = Perfect match (100%)</description></item>
  ///   <item><description>0.5 = Moderate match (50%)</description></item>
  ///   <item><description>0.0 = Complete mismatch (0%)</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public static class DistanceMetric
  {
    /// <summary>
    /// Computes acceptance using normalized Manhattan distance (L1 norm).
    /// This is the default and most intuitive method.
    /// </summary>
    /// <param name="consumer">The consumer profile (what they want)</param>
    /// <param name="product">The product profile (what it offers)</param>
    /// <returns>Acceptance score between 0 (no match) and 1 (perfect match)</returns>
    /// <remarks>
    /// <para><b>Algorithm:</b></para>
    /// <code>
    /// 1. For each dimension: distance = |consumer.value - product.value|
    /// 2. sum = sum of all distances
    /// 3. normalized = sum / count
    /// 4. acceptance = 1 - normalized
    /// </code>
    /// <para>
    /// Manhattan distance is ideal because:
    /// - Easy to understand (sum of differences)
    /// - Works well with normalized 0-1 values
    /// - Each dimension contributes equally
    /// - Fast to compute
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Consumer: [0.76, 0.60, 0.40, 0.50]
    /// Product:  [0.60, 0.70, 0.80, 0.40]
    /// 
    /// Distances: [0.16, 0.10, 0.40, 0.10]
    /// Sum: 0.76
    /// Average: 0.76 / 4 = 0.19
    /// Acceptance: 1 - 0.19 = 0.81 (81%)
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when consumer or product is null</exception>
    public static float ComputeAcceptance(ConsumerProfile consumer, ProductProfile product)
    {
      if (consumer == null)
        throw new ArgumentNullException(nameof(consumer));
      if (product == null)
        throw new ArgumentNullException(nameof(product));

      float sum = 0f;
      int count = 0;

      foreach (var dim in consumer.Dimensions)
      {
        if (product.Dimensions.TryGetValue(dim.Key, out var pValue))
        {
          float distance = Math.Abs(dim.Value.Value - pValue.Value);
          sum += distance;
          count++;
        }
      }

      // No common dimensions = no basis for comparison
      if (count == 0)
        return 0f;

      float normalized = sum / count;
      return Math.Clamp(1f - normalized, 0f, 1f);
    }

    /// <summary>
    /// Computes acceptance using Euclidean distance (L2 norm).
    /// This measures the straight-line distance in multidimensional space.
    /// </summary>
    /// <param name="consumer">The consumer profile</param>
    /// <param name="product">The product profile</param>
    /// <returns>Acceptance score between 0 and 1</returns>
    /// <remarks>
    /// <para><b>Formula:</b> distance = sqrt(sum((consumer[i] - product[i])²))</para>
    /// <para>
    /// Euclidean distance gives more weight to large differences because of squaring.
    /// Useful when you want to heavily penalize mismatches in key dimensions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Consumer: [0.8, 0.6]
    /// Product:  [0.6, 0.8]
    /// 
    /// distance = sqrt((0.8-0.6)² + (0.6-0.8)²)
    ///          = sqrt(0.04 + 0.04)
    ///          = sqrt(0.08) = 0.283
    /// normalized = 0.283 / sqrt(2) = 0.20
    /// acceptance = 1 - 0.20 = 0.80 (80%)
    /// </code>
    /// </example>
    public static float ComputeAcceptanceEuclidean(ConsumerProfile consumer, ProductProfile product)
    {
      if (consumer == null)
        throw new ArgumentNullException(nameof(consumer));
      if (product == null)
        throw new ArgumentNullException(nameof(product));

      float sumSquared = 0f;
      int count = 0;

      foreach (var dim in consumer.Dimensions)
      {
        if (product.Dimensions.TryGetValue(dim.Key, out var pValue))
        {
          float diff = dim.Value.Value - pValue.Value;
          sumSquared += diff * diff;
          count++;
        }
      }

      if (count == 0)
        return 0f;

      // Normalize by maximum possible distance (sqrt(count))
      float distance = (float)Math.Sqrt(sumSquared);
      float maxDistance = (float)Math.Sqrt(count);
      float normalized = distance / maxDistance;

      return Math.Clamp(1f - normalized, 0f, 1f);
    }

    /// <summary>
    /// Computes acceptance using cosine similarity.
    /// Measures the angle between consumer and product vectors.
    /// </summary>
    /// <param name="consumer">The consumer profile</param>
    /// <param name="product">The product profile</param>
    /// <returns>Similarity score between 0 and 1</returns>
    /// <remarks>
    /// <para><b>Formula:</b> similarity = (consumer · product) / (||consumer|| * ||product||)</para>
    /// <para>
    /// Cosine similarity focuses on the direction of preferences rather than magnitude.
    /// Two vectors pointing in the same direction have similarity = 1, even if magnitudes differ.
    /// Useful when the pattern of preferences matters more than absolute values.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Consumer: [0.8, 0.6, 0.4]  (high quality focus)
    /// Product:  [0.9, 0.7, 0.5]  (also high quality)
    /// 
    /// Even though magnitudes differ, both prioritize same dimensions.
    /// Cosine similarity ≈ 0.998 (nearly identical direction)
    /// </code>
    /// </example>
    public static float ComputeAcceptanceCosine(ConsumerProfile consumer, ProductProfile product)
    {
      if (consumer == null)
        throw new ArgumentNullException(nameof(consumer));
      if (product == null)
        throw new ArgumentNullException(nameof(product));

      float dotProduct = 0f;
      float consumerMagnitude = 0f;
      float productMagnitude = 0f;
      int count = 0;

      foreach (var dim in consumer.Dimensions)
      {
        if (product.Dimensions.TryGetValue(dim.Key, out var pValue))
        {
          float cVal = dim.Value.Value;
          float pVal = pValue.Value;

          dotProduct += cVal * pVal;
          consumerMagnitude += cVal * cVal;
          productMagnitude += pVal * pVal;
          count++;
        }
      }

      if (count == 0 || consumerMagnitude == 0 || productMagnitude == 0)
        return 0f;

      consumerMagnitude = (float)Math.Sqrt(consumerMagnitude);
      productMagnitude = (float)Math.Sqrt(productMagnitude);

      float cosineSimilarity = dotProduct / (consumerMagnitude * productMagnitude);
      return Math.Clamp(cosineSimilarity, 0f, 1f);
    }

    /// <summary>
    /// Computes weighted acceptance where certain dimensions have more importance.
    /// </summary>
    /// <param name="consumer">The consumer profile</param>
    /// <param name="product">The product profile</param>
    /// <param name="weights">Weights for each dimension (higher = more important)</param>
    /// <returns>Weighted acceptance score between 0 and 1</returns>
    /// <remarks>
    /// <para>
    /// Allows prioritizing dimensions based on business logic or user preferences.
    /// For example, PriceSensitivity might be 2x more important than SocialRecognition.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var weights = new Dictionary&lt;DimensionDefinition, float&gt;
    /// {
    ///     { priceSensitivityDef, 2.0f },    // Price is 2x important
    ///     { qualityDef, 1.5f },             // Quality is 1.5x important
    ///     { socialRecognitionDef, 0.5f }    // Social is 0.5x important
    /// };
    /// 
    /// float score = DistanceMetric.ComputeWeightedAcceptance(consumer, product, weights);
    /// </code>
    /// </example>
    public static float ComputeWeightedAcceptance(
      ConsumerProfile consumer,
      ProductProfile product,
      Dictionary<DimensionDefinition, float> weights)
    {
      if (consumer == null)
        throw new ArgumentNullException(nameof(consumer));
      if (product == null)
        throw new ArgumentNullException(nameof(product));
      if (weights == null)
        throw new ArgumentNullException(nameof(weights));

      float weightedSum = 0f;
      float totalWeight = 0f;

      foreach (var dim in consumer.Dimensions)
      {
        if (product.Dimensions.TryGetValue(dim.Key, out var pValue))
        {
          float distance = Math.Abs(dim.Value.Value - pValue.Value);
          float weight = weights.TryGetValue(dim.Key, out var w) ? w : 1.0f;

          weightedSum += distance * weight;
          totalWeight += weight;
        }
      }

      if (totalWeight == 0)
        return 0f;

      float normalized = weightedSum / totalWeight;
      return Math.Clamp(1f - normalized, 0f, 1f);
    }

    /// <summary>
    /// Computes a detailed breakdown of acceptance by dimension.
    /// Useful for explaining why a product matches or doesn't match.
    /// </summary>
    /// <param name="consumer">The consumer profile</param>
    /// <param name="product">The product profile</param>
    /// <returns>Detailed match report with per-dimension scores</returns>
    /// <example>
    /// <code>
    /// var report = DistanceMetric.ComputeDetailedMatch(consumer, product);
    /// Console.WriteLine($"Overall: {report.OverallAcceptance * 100:F1}%");
    /// foreach (var dim in report.DimensionScores)
    /// {
    ///     Console.WriteLine($"  {dim.Key.Name}: {dim.Value * 100:F1}%");
    /// }
    /// </code>
    /// </example>
    public static MatchReport ComputeDetailedMatch(ConsumerProfile consumer, ProductProfile product)
    {
      if (consumer == null)
        throw new ArgumentNullException(nameof(consumer));
      if (product == null)
        throw new ArgumentNullException(nameof(product));

      var report = new MatchReport
      {
        Consumer = consumer,
        Product = product,
        DimensionScores = new Dictionary<DimensionDefinition, float>()
      };

      float sum = 0f;
      int count = 0;

      foreach (var dim in consumer.Dimensions)
      {
        if (product.Dimensions.TryGetValue(dim.Key, out var pValue))
        {
          float distance = Math.Abs(dim.Value.Value - pValue.Value);
          float dimScore = 1f - distance;

          report.DimensionScores[dim.Key] = dimScore;
          sum += distance;
          count++;
        }
      }

      report.OverallAcceptance = count > 0 ? Math.Clamp(1f - (sum / count), 0f, 1f) : 0f;
      report.CommonDimensionsCount = count;

      return report;
    }

    /// <summary>
    /// Determines if the acceptance score meets a minimum threshold.
    /// </summary>
    /// <param name="consumer">The consumer profile</param>
    /// <param name="product">The product profile</param>
    /// <param name="threshold">Minimum acceptance (0-1, default 0.7 = 70%)</param>
    /// <returns>True if product is acceptable for this consumer</returns>
    /// <example>
    /// <code>
    /// bool isGoodMatch = DistanceMetric.IsAcceptable(consumer, product, 0.75f);
    /// if (isGoodMatch)
    /// {
    ///     RecommendProduct(product);
    /// }
    /// </code>
    /// </example>
    public static bool IsAcceptable(ConsumerProfile consumer, ProductProfile product, float threshold = 0.7f)
    {
      return ComputeAcceptance(consumer, product) >= threshold;
    }
  }
}
