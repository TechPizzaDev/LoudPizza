using System;

namespace LoudPizza
{
    public abstract unsafe class FilterInstance : IDisposable
    {
        protected uint mNumParams;
        protected uint mParamChanged;
        protected float[] mParam;
        protected Fader[] mParamFader;
        private bool _isDisposed;

        public FilterInstance(int paramCount)
        {
            mNumParams = (uint)paramCount;
            mParam = new float[mNumParams];
            mParamFader = new Fader[mNumParams];

            for (uint i = 0; i < mNumParams; i++)
            {
                mParam[i] = 0;
                mParamFader[i].mActive = 0;
            }
            mParam[0] = 1; // set 'wet' to 1
        }

        public virtual void updateParams(Time aTime)
        {
            for (uint i = 0; i < mNumParams; i++)
            {
                if (mParamFader[i].mActive > 0)
                {
                    mParamChanged |= 1u << (int)i;
                    mParam[i] = mParamFader[i].get(aTime);
                }
            }
        }

        public virtual void filter(float* aBuffer, uint aSamples, uint aBufferSize, uint aChannels, float aSamplerate, Time aTime)
        {
            for (uint i = 0; i < aChannels; i++)
            {
                filterChannel(aBuffer + i * aBufferSize, aSamples, aSamplerate, aTime, i, aChannels);
            }
        }

        public abstract void filterChannel(float* aBuffer, uint aSamples, float aSamplerate, Time aTime, uint aChannel, uint aChannels);
        
        public virtual float getFilterParameter(uint aAttributeId)
        {
            if (aAttributeId >= mNumParams)
                return 0;

            return mParam[aAttributeId];
        }
        
        public virtual void setFilterParameter(uint aAttributeId, float aValue)
        {
            if (aAttributeId >= mNumParams)
                return;

            mParamFader[aAttributeId].mActive = 0;
            mParam[aAttributeId] = aValue;
            mParamChanged |= 1u << (int)aAttributeId;
        }
        
        public virtual void fadeFilterParameter(uint aAttributeId, float aTo, Time aTime, Time aStartTime)
        {
            if (aAttributeId >= mNumParams || aTime <= 0 || aTo == mParam[aAttributeId])
                return;

            mParamFader[aAttributeId].set(mParam[aAttributeId], aTo, aTime, aStartTime);
        }

        public virtual void oscillateFilterParameter(uint aAttributeId, float aFrom, float aTo, Time aTime, Time aStartTime)
        {
            if (aAttributeId >= mNumParams || aTime <= 0 || aFrom == aTo)
                return;

            mParamFader[aAttributeId].setLFO(aFrom, aTo, aTime, aStartTime);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
