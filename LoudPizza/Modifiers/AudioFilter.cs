using System;

namespace LoudPizza.Modifiers
{
    public abstract class AudioFilter : IDisposable
    {
        public enum ParamType
        {
            Float = 0,
            Int,
            Bool,
        }

        public bool IsDisposed { get; private set; }

        public virtual int GetParamCount()
        {
            return 1; // there's always WET
        }

        public virtual string GetParamName(uint paramIndex)
        {
            return "Wet";
        }

        public virtual ParamType GetParamType(uint paramIndex)
        {
            return ParamType.Float;
        }

        public virtual float GetParamMax(uint paramIndex)
        {
            return 1;
        }

        public virtual float GetParamMin(uint paramIndex)
        {
            return 0;
        }

        public abstract AudioFilterInstance CreateInstance();

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
