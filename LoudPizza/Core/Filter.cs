
namespace LoudPizza
{
    public abstract class Filter
    {
        public enum PARAMTYPE
        {
            FLOAT_PARAM = 0,
            INT_PARAM,
            BOOL_PARAM
        }

        public virtual int getParamCount()
        {
            return 1; // there's always WET
        }

        public virtual string getParamName(uint aParamIndex)
        {
            return "Wet";
        }

        public virtual PARAMTYPE getParamType(uint aParamIndex)
        {
            return PARAMTYPE.FLOAT_PARAM;
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
