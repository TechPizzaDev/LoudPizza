using System;

namespace LoudPizza
{
    public unsafe partial class SoLoud
    {
        /// <summary>
        /// Set global filters. Set to <see langword="null"/> to clear the filter.
        /// </summary>
        public void setGlobalFilter(uint aFilterId, Filter? aFilter)
        {
            if (aFilterId >= FiltersPerStream)
                return;

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
        public float getFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId)
        {
            if (aFilterId >= FiltersPerStream)
                return (int)SoLoudStatus.InvalidParameter;

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
        public void setFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aValue)
        {
            if (aFilterId >= FiltersPerStream)
                return;

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
        public void fadeFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aTo, double aTime)
        {
            if (aFilterId >= FiltersPerStream)
                return;

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
                            filterInstance.fadeFilterParameter(aAttributeId, aTo, aTime, mStreamTime);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Oscillate a live filter parameter. Use 0 for the global filters.
        /// </summary>
        public void oscillateFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aFrom, float aTo, double aTime)
        {
            if (aFilterId >= FiltersPerStream)
                return;

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
                            filterInstance.oscillateFilterParameter(aAttributeId, aFrom, aTo, aTime, mStreamTime);
                        }
                    }
                }
            }
        }
    }
}
