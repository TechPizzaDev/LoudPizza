using System;
using LoudPizza.Modifiers;
using LoudPizza.Sources;

namespace LoudPizza.Core
{
    public unsafe partial class SoLoud
    {
        /// <summary>
        /// Set global filters. Set to <see langword="null"/> to clear the filter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aFilterId"/> is invalid.</exception>
        public void setGlobalFilter(int aFilterId, AudioFilter? aFilter)
        {
            if ((uint)aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                mFilterInstance[aFilterId]?.Dispose();
                mFilterInstance[aFilterId] = null;

                mFilter[aFilterId] = aFilter;
                if (aFilter != null)
                {
                    mFilterInstance[aFilterId] = aFilter.CreateInstance();
                }
            }
        }

        /// <summary>
        /// Get a live filter parameter. Use 0 for the global filters.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aFilterId"/> is invalid.</exception>
        public float getFilterParameter(Handle aVoiceHandle, int aFilterId, int aAttributeId)
        {
            if ((uint)aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                float ret = (int)SoLoudStatus.InvalidParameter;
                if (aVoiceHandle == default)
                {
                    AudioFilterInstance? filterInstance = mFilterInstance[aFilterId];
                    if (filterInstance != null)
                    {
                        ret = filterInstance.GetFilterParameter(aAttributeId);
                    }
                    return ret;
                }

                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch != null)
                {
                    AudioFilterInstance? filterInstance = ch.GetFilter(aFilterId);
                    if (filterInstance != null)
                    {
                        ret = filterInstance.GetFilterParameter(aAttributeId);
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// Set a live filter parameter. Use 0 for the global filters.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aFilterId"/> is invalid.</exception>
        public void setFilterParameter(Handle aVoiceHandle, int aFilterId, int aAttributeId, float aValue)
        {
            if ((uint)aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                if (aVoiceHandle == default)
                {
                    AudioFilterInstance? filterInstance = mFilterInstance[aFilterId];
                    if (filterInstance != null)
                    {
                        filterInstance.SetFilterParameter(aAttributeId, aValue);
                    }
                    return;
                }

                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        AudioFilterInstance? filterInstance = ch.GetFilter(aFilterId);
                        if (filterInstance != null)
                        {
                            filterInstance.SetFilterParameter(aAttributeId, aValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fade a live filter parameter. Use 0 for the global filters.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aFilterId"/> is invalid.</exception>
        public void fadeFilterParameter(
            Handle aVoiceHandle, int aFilterId, int aAttributeId, float aTo, Time aTime)
        {
            if ((uint)aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                if (aVoiceHandle == default)
                {
                    AudioFilterInstance? filterInstance = mFilterInstance[aFilterId];
                    if (filterInstance != null)
                    {
                        filterInstance.FadeFilterParameter(aAttributeId, aTo, aTime, mStreamTime);
                    }
                    return;
                }

                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        AudioFilterInstance? filterInstance = ch.GetFilter(aFilterId);
                        if (filterInstance != null)
                        {
                            filterInstance.FadeFilterParameter(aAttributeId, aTo, aTime, ch.mStreamTime);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Oscillate a live filter parameter. Use 0 for the global filters.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aFilterId"/> is invalid.</exception>
        public void oscillateFilterParameter(
            Handle aVoiceHandle, int aFilterId, int aAttributeId, float aFrom, float aTo, Time aTime)
        {
            if ((uint)aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                if (aVoiceHandle == default)
                {
                    AudioFilterInstance? filterInstance = mFilterInstance[aFilterId];
                    if (filterInstance != null)
                    {
                        filterInstance.OscillateFilterParameter(aAttributeId, aFrom, aTo, aTime, mStreamTime);
                    }
                    return;
                }

                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        AudioFilterInstance? filterInstance = ch.GetFilter(aFilterId);
                        if (filterInstance != null)
                        {
                            filterInstance.OscillateFilterParameter(aAttributeId, aFrom, aTo, aTime, ch.mStreamTime);
                        }
                    }
                }
            }
        }
    }
}
