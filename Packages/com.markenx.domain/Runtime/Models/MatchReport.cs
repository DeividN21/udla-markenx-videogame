using System.Collections.Generic;
using System.Linq;

namespace MyProject.Domain.Models
{
  /// <summary>
  /// Detailed report of how well a product matches a consumer.
  /// </summary>
  public class MatchReport
  {
    /// <summary>The consumer being analyzed</summary>
    public ConsumerProfile Consumer { get; set; }

    /// <summary>The product being evaluated</summary>
    public ProductProfile Product { get; set; }

    /// <summary>Overall acceptance score (0-1)</summary>
    public float OverallAcceptance { get; set; }

    /// <summary>Number of dimensions compared</summary>
    public int CommonDimensionsCount { get; set; }

    /// <summary>Per-dimension match scores</summary>
    public Dictionary<DimensionDefinition, float> DimensionScores { get; set; }

    /// <summary>
    /// Gets the best matching dimensions (sorted by score)
    /// </summary>
    public IEnumerable<KeyValuePair<DimensionDefinition, float>> GetBestMatches(int topN = 3)
    {
      return DimensionScores
        .OrderByDescending(kvp => kvp.Value)
        .Take(topN);
    }

    /// <summary>
    /// Gets the worst matching dimensions (sorted by score)
    /// </summary>
    public IEnumerable<KeyValuePair<DimensionDefinition, float>> GetWorstMatches(int bottomN = 3)
    {
      return DimensionScores
        .OrderBy(kvp => kvp.Value)
        .Take(bottomN);
    }

    public override string ToString()
    {
      return $"Match Report: {OverallAcceptance * 100:F1}% " +
             $"({CommonDimensionsCount} dimensions compared)";
    }
  }
}