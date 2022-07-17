using System;
using LoudPizza.Core;
using LoudPizza.Modifiers;

namespace LoudPizza
{
    public readonly partial struct SoLoudHandle
    {
        /// <inheritdoc cref="SoLoud.setGlobalFilter(uint, Filter?)"/>
        public void SetGlobalFilter(uint filterId, Filter? filter)
        {
            SoLoud.setGlobalFilter(filterId, filter);
        }

        /// <summary>
        /// Get a global live filter parameter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public float GetFilterParameter(uint filterId, uint attributeId)
        {
            return SoLoud.getFilterParameter(default, filterId, attributeId);
        }

        /// <summary>
        /// Set a global live filter parameter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public void SetFilterParameter(uint filterId, uint attributeId, float value)
        {
            SoLoud.setFilterParameter(default, filterId, attributeId, value);
        }

        /// <summary>
        /// Fade a global live filter parameter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public void FadeFilterParameter(uint filterId, uint attributeId, float to, Time time)
        {
            SoLoud.fadeFilterParameter(default, filterId, attributeId, to, time);
        }

        /// <summary>
        /// Oscillate a global live filter parameter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public void OscillateFilterParameter(uint filterId, uint attributeId, float from, float to, Time time)
        {
            SoLoud.oscillateFilterParameter(default, filterId, attributeId, from, to, time);
        }
    }
}
