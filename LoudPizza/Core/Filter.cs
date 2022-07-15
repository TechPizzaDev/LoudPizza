
namespace LoudPizza.Core
{
    public abstract class Filter
    {
        public enum ParamType
        {
            Float = 0,
            Int,
            Bool,
        }

        public virtual int getParamCount()
        {
            return 1; // there's always WET
        }

        public virtual string getParamName(uint aParamIndex)
        {
            return "Wet";
        }

        public virtual ParamType getParamType(uint aParamIndex)
        {
            return ParamType.Float;
        }

        public virtual float getParamMax(uint aParamIndex)
        {
            return 1;
        }

        public virtual float getParamMin(uint aParamIndex)
        {
            return 0;
        }

        public abstract FilterInstance createInstance();
    }
}
