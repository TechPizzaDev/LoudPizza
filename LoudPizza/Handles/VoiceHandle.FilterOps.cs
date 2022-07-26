using System;
using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct VoiceHandle
    {
        /// <summary>
        /// Get a live filter parameter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public float GetFilterParameter(int filterId, int attributeId)
        {
            return SoLoud.getFilterParameter(Handle, filterId, attributeId);
        }

        /// <summary>
        /// Set a live filter parameter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public void SetFilterParameter(int filterId, int attributeId, float value)
        {
            SoLoud.setFilterParameter(Handle, filterId, attributeId, value);
        }

        /// <summary>
        /// Fade a live filter parameter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public void FadeFilterParameter(int filterId, int attributeId, float to, Time time)
        {
            SoLoud.fadeFilterParameter(Handle, filterId, attributeId, to, time);
        }

        /// <summary>
        /// Oscillate a live filter parameter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public void OscillateFilterParameter(int filterId, int attributeId, float from, float to, Time time)
        {
            SoLoud.oscillateFilterParameter(Handle, filterId, attributeId, from, to, time);
        }
    }
}
