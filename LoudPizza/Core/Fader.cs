using System;

namespace LoudPizza
{
    public struct Fader
    {
        // Value to fade from
        public float mFrom;

        // Value to fade to
        public float mTo;

        // Delta between from and to
        public float mDelta;

        // Total time to fade
        public Time mTime;

        // Time fading started
        public Time mStartTime;

        // Time fading will end
        public Time mEndTime;

        // Current value. Used in case time rolls over.
        public float mCurrent;

        // Active flag; 0 means disabled, 1 is active, 2 is LFO, -1 means was active, but stopped
        public int mActive;

        // Set up LFO
        public void setLFO(float aFrom, float aTo, Time aTime, Time aStartTime)
        {
            mActive = 2;
            mCurrent = 0;
            mFrom = aFrom;
            mTo = aTo;
            mTime = aTime;
            mDelta = (aTo - aFrom) / 2;
            if (mDelta < 0)
                mDelta = -mDelta;
            mStartTime = aStartTime;
            mEndTime = MathF.PI * 2 / mTime;
        }

        // Set up fader
        public void set(float aFrom, float aTo, Time aTime, Time aStartTime)
        {
            mCurrent = mFrom;
            mFrom = aFrom;
            mTo = aTo;
            mTime = aTime;
            mStartTime = aStartTime;
            mDelta = aTo - aFrom;
            mEndTime = mStartTime + mTime;
            mActive = 1;
        }

        // Get the current fading value
        public float get(Time aCurrentTime)
        {
            if (mActive == 2)
            {
                // LFO mode
                if (mStartTime > aCurrentTime)
                {
                    // Time rolled over.
                    mStartTime = aCurrentTime;
                }
                double t = aCurrentTime - mStartTime;
                return (float)(Math.Sin(t * mEndTime) * mDelta + (mFrom + mDelta));

            }
            if (mStartTime > aCurrentTime)
            {
                // Time rolled over.
                // Figure out where we were..
                float p = (mCurrent - mFrom) / mDelta; // 0..1
                mFrom = mCurrent;
                mStartTime = aCurrentTime;
                mTime = mTime * (1 - p); // time left
                mDelta = mTo - mFrom;
                mEndTime = mStartTime + mTime;
            }
            if (aCurrentTime > mEndTime)
            {
                mActive = -1;
                return mTo;
            }
            mCurrent = (float)(mFrom + mDelta * ((aCurrentTime - mStartTime) / mTime));
            return mCurrent;
        }
    }
}
