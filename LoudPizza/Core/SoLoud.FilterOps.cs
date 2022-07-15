using System;

namespace LoudPizza.Core
{
    public unsafe partial class SoLoud
    {
        /// <summary>
        /// Set global filters. Set to <see langword="null"/> to clear the filter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aFilterId"/> is invalid.</exception>
        public void setGlobalFilter(uint aFilterId, Filter? aFilter)
        {
            if (aFilterId >= FiltersPerStream)
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
                    mFilterInstance[aFilterId] = aFilter.createInstance();
                }
            }
        }

        /// <summary>
        /// Get a live filter parameter. Use 0 for the global filters.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aFilterId"/> is invalid.</exception>
        public float getFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId)
        {
            if (aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                float ret = (int)SoLoudStatus.InvalidParameter;
                if (aVoiceHandle.Value == 0)
                {
                    FilterInstance? filterInstance = mFilterInstance[aFilterId];
                    if (filterInstance != null)
                    {
                        ret = filterInstance.getFilterParameter(aAttributeId);
                    }
                    return ret;
                }

                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch != null)
                {
                    FilterInstance? filterInstance = ch.mFilter[aFilterId];
                    if (filterInstance != null)
                    {
                        ret = filterInstance.getFilterParameter(aAttributeId);
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// Set a live filter parameter. Use 0 for the global filters.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aFilterId"/> is invalid.</exception>
        public void setFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aValue)
        {
            if (aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                if (aVoiceHandle.Value == 0)
                {
                    FilterInstance? filterInstance = mFilterInstance[aFilterId];
                    if (filterInstance != null)
                    {
                        filterInstance.setFilterParameter(aAttributeId, aValue);
                    }
                    return;
                }

                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        FilterInstance? filterInstance = ch.mFilter[aFilterId];
                        if (filterInstance != null)
                        {
                            filterInstance.setFilterParameter(aAttributeId, aValue);
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
            Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aTo, Time aTime)
        {
            if (aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                if (aVoiceHandle.Value == 0)
                {
                    FilterInstance? filterInstance = mFilterInstance[aFilterId];
                    if (filterInstance != null)
                    {
                        filterInstance.fadeFilterParameter(aAttributeId, aTo, aTime, mStreamTime);
                    }
                    return;
                }

                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        FilterInstance? filterInstance = ch.mFilter[aFilterId];
                        if (filterInstance != null)
                        {
                            filterInstance.fadeFilterParameter(aAttributeId, aTo, aTime, ch.mStreamTime);
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
            Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aFrom, float aTo, Time aTime)
        {
            if (aFilterId >= FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(aFilterId));
            }

            lock (mAudioThreadMutex)
            {
                if (aVoiceHandle.Value == 0)
                {
                    FilterInstance? filterInstance = mFilterInstance[aFilterId];
                    if (filterInstance != null)
                    {
                        filterInstance.oscillateFilterParameter(aAttributeId, aFrom, aTo, aTime, mStreamTime);
                    }
                    return;
                }

                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        FilterInstance? filterInstance = ch.mFilter[aFilterId];
                        if (filterInstance != null)
                        {
                            filterInstance.oscillateFilterParameter(aAttributeId, aFrom, aTo, aTime, ch.mStreamTime);
                        }
                    }
                }
            }
        }
    }
}
