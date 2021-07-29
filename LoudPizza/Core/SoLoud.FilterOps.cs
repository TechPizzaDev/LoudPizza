using System;

namespace LoudPizza
{
    public unsafe partial class SoLoud
    {
        // Set global filters. Set to NULL to clear the filter.
        public void setGlobalFilter(uint aFilterId, Filter? aFilter)
        {
            if (aFilterId >= FILTERS_PER_STREAM)
                return;

            lockAudioMutex_internal();
            mFilterInstance[aFilterId]?.Dispose();
            mFilterInstance[aFilterId] = null;

            mFilter[aFilterId] = aFilter;
            if (aFilter != null)
            {
                mFilterInstance[aFilterId] = aFilter.createInstance();
            }
            unlockAudioMutex_internal();
        }

        // Get a live filter parameter. Use 0 for the global filters.
        public float getFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId)
        {
            float ret = (int)SOLOUD_ERRORS.INVALID_PARAMETER;
            if (aFilterId >= FILTERS_PER_STREAM)
                return ret;

            if (aVoiceHandle.Value == 0)
            {
                lockAudioMutex_internal();
                FilterInstance? filterInstance = mFilterInstance[aFilterId];
                if (filterInstance != null)
                {
                    ret = filterInstance.getFilterParameter(aAttributeId);
                }
                unlockAudioMutex_internal();
                return ret;
            }

            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch != null)
            {
                FilterInstance? filterInstance = ch.mFilter[aFilterId];
                if (filterInstance != null)
                {
                    ret = filterInstance.getFilterParameter(aAttributeId);
                }
            }
            unlockAudioMutex_internal();

            return ret;
        }

        // Set a live filter parameter. Use 0 for the global filters.
        public void setFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aValue)
        {
            if (aFilterId >= FILTERS_PER_STREAM)
                return;

            if (aVoiceHandle.Value == 0)
            {
                lockAudioMutex_internal();
                FilterInstance? filterInstance = mFilterInstance[aFilterId];
                if (filterInstance != null)
                {
                    filterInstance.setFilterParameter(aAttributeId, aValue);
                }
                unlockAudioMutex_internal();
                return;
            }

            void body(Handle h)
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

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Fade a live filter parameter. Use 0 for the global filters.
        public void fadeFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aTo, double aTime)
        {
            if (aFilterId >= FILTERS_PER_STREAM)
                return;

            if (aVoiceHandle.Value == 0)
            {
                lockAudioMutex_internal();
                FilterInstance? filterInstance = mFilterInstance[aFilterId];
                if (filterInstance != null)
                {
                    filterInstance.fadeFilterParameter(aAttributeId, aTo, aTime, mStreamTime);
                }
                unlockAudioMutex_internal();
                return;
            }

            void body(Handle h)
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

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Oscillate a live filter parameter. Use 0 for the global filters.
        public void oscillateFilterParameter(Handle aVoiceHandle, uint aFilterId, uint aAttributeId, float aFrom, float aTo, double aTime)
        {
            if (aFilterId >= FILTERS_PER_STREAM)
                return;

            if (aVoiceHandle.Value == 0)
            {
                lockAudioMutex_internal();
                FilterInstance? filterInstance = mFilterInstance[aFilterId];
                if (filterInstance != null)
                {
                    filterInstance.oscillateFilterParameter(aAttributeId, aFrom, aTo, aTime, mStreamTime);
                }
                unlockAudioMutex_internal();
                return;
            }

            void body(Handle h)
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

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }
    }
}
