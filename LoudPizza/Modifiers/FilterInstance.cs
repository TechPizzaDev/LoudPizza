using System;
using LoudPizza.Core;

namespace LoudPizza.Modifiers
{
    public abstract class FilterInstance : IDisposable
    {
        protected uint mNumParams;
        protected uint mParamChanged;
        protected float[] mParam;
        protected Fader[] mParamFader;

        public bool IsDisposed { get; private set; }

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

        public virtual void UpdateParams(Time time)
        {
            for (uint i = 0; i < mNumParams; i++)
            {
                if (mParamFader[i].mActive > 0)
                {
                    mParamChanged |= 1u << (int)i;
                    mParam[i] = mParamFader[i].get(time);
                }
            }
        }

        public virtual void Filter(Span<float> buffer, uint samples, uint bufferSize, uint channels, float sampleRate, Time time)
        {
            for (uint i = 0; i < channels; i++)
            {
                FilterChannel(
                    buffer.Slice((int)(i * bufferSize), (int)samples), sampleRate, time, i, channels);
            }
        }

        public abstract void FilterChannel(Span<float> buffer, float sampleRate, Time time, uint channel, uint channels);

        public virtual float GetFilterParameter(uint attributeId)
        {
            if (attributeId >= mNumParams)
                return 0;

            return mParam[attributeId];
        }

        public virtual void SetFilterParameter(uint attributeId, float value)
        {
            if (attributeId >= mNumParams)
                return;

            mParamFader[attributeId].mActive = 0;
            mParam[attributeId] = value;
            mParamChanged |= 1u << (int)attributeId;
        }

        public virtual void FadeFilterParameter(uint attributeId, float to, Time time, Time startTime)
        {
            if (attributeId >= mNumParams || time <= 0 || to == mParam[attributeId])
                return;

            mParamFader[attributeId].set(mParam[attributeId], to, time, startTime);
        }

        public virtual void OscillateFilterParameter(uint attributeId, float from, float to, Time time, Time startTime)
        {
            if (attributeId >= mNumParams || time <= 0 || from == to)
                return;

            mParamFader[attributeId].setLFO(from, to, time, startTime);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
