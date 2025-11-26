using System;
using System.Collections.Generic;
using System.Linq;

namespace MyProject.Domain.Models
{
    /// <summary>
    /// Represents a consumer as a multidimensional, mutable profile vector.
    /// Each entry maps a <see cref="DimensionDefinition"/> to its corresponding
    /// normalized <see cref="DimensionValue"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A profile is typically constructed by a Rule Engine that transforms
    /// consumer-related data (demographics, economic indicators, behavioral traits,
    /// interaction history, etc.) into numerical dimension values.
    /// </para>
    /// <para>
    /// The result is a mathematical representation of a consumer that can be
    /// compared with product profiles for matching, scoring, or recommendation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var profile = new ConsumerProfile();
    /// profile.Set(priceSensitivityDef, 0.76f);
    /// profile.Set(qualityExpectationDef, 0.60f);
    ///
    /// var value = profile.Get(priceSensitivityDef);
    /// Console.WriteLine(value?.Value);
    /// </code>
    /// </example>
    public class ConsumerProfile
    {
        /// <summary>
        /// Gets the internal mapping of dimension definitions to values.
        /// Note: this collection is mutable and does not guarantee ordering.
        /// </summary>
        public Dictionary<DimensionDefinition, DimensionValue> Dimensions { get; }

        /// <summary>
        /// Initializes an empty consumer profile.
        /// Dimensions can be populated using <see cref="Set"/>.
        /// </summary>
        public ConsumerProfile()
        {
            Dimensions = new Dictionary<DimensionDefinition, DimensionValue>();
        }

        /// <summary>
        /// Sets or replaces the value of a dimension in the profile.
        /// The value is automatically normalized to the 0–1 range.
        /// </summary>
        /// <param name="def">The dimension definition.</param>
        /// <param name="value">The value to assign.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="def"/> is null.</exception>
        public void Set(DimensionDefinition def, float value)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));

            Dimensions[def] = new DimensionValue(def, value);
        }

        /// <summary>
        /// Returns the value of a dimension, or <c>null</c> if the definition is not present.
        /// </summary>
        public DimensionValue Get(DimensionDefinition def)
        {
            return Dimensions.TryGetValue(def, out var value) ? value : null;
        }

        /// <summary>
        /// Indicates whether the profile contains the given dimension definition.
        /// </summary>
        public bool HasDimension(DimensionDefinition def)
        {
            return def != null && Dimensions.ContainsKey(def);
        }

        /// <summary>
        /// Adjusts an existing dimension by adding a delta. If the dimension does not exist,
        /// a new one is created with the delta as initial value.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="def"/> is null.</exception>
        public void Adjust(DimensionDefinition def, float delta)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));

            if (Dimensions.TryGetValue(def, out var existing))
                existing.Add(delta);
            else
                Dimensions[def] = new DimensionValue(def, delta);
        }

        /// <summary>
        /// Returns a live view of all dimension values.
        /// Modifications to the profile will be reflected in this sequence.
        /// </summary>
        public IEnumerable<DimensionValue> GetAllValues()
        {
            return Dimensions.Values;
        }

        /// <summary>
        /// Gets the total number of dimensions in the profile.
        /// </summary>
        public int Count => Dimensions.Count;

        /// <summary>
        /// Returns a human-readable representation of the profile.
        /// Ordering of dimensions is not guaranteed.
        /// </summary>
        public override string ToString()
        {
            var dimensions = string.Join("\n  ",
                Dimensions.Values.Select(dv => $"{dv.Definition.Name} = {dv.Value:F2}"));

            return $"ConsumerProfile ({Count} dimensions):\n  {dimensions}";
        }
    }
}