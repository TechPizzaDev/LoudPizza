using System;

namespace LoudPizza.Core
{
    public struct Fader
    {
        /// <summary>
        /// Value to fade from.
        /// </summary>
        public float mFrom;

        /// <summary>
        /// Value to fade to.
        /// </summary>
        public float mTo;

        /// <summary>
        /// Delta between from and to.
        /// </summary>
        public float mDelta;

        /// <summary>
        /// Total time to fade.
        /// </summary>
        public Time mTime;

        /// <summary>
        /// Time fading started.
        /// </summary>
        public Time mStartTime;

        /// <summary>
        /// Time fading will end.
        /// </summary>
        public Time mEndTime;

        /// <summary>
        /// Current value. Used in case time rolls over.
        /// </summary>
        public float mCurrent;

        /// <summary>
        /// Active flag.
        /// </summary>
        public State mActive;

        /// <summary>
        /// Set up LFO.
        /// </summary>
        public void setLFO(float aFrom, float aTo, Time aTime, Time aStartTime)
        {
            mActive = State.LFO;
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

        /// <summary>
        /// Set up fader.
        /// </summary>
        public void set(float aFrom, float aTo, Time aTime, Time aStartTime)
        {
            mCurrent = mFrom;
            mFrom = aFrom;
            mTo = aTo;
            mTime = aTime;
            mStartTime = aStartTime;
            mDelta = aTo - aFrom;
            mEndTime = mStartTime + mTime;
            mActive = State.Active;
        }

        /// <summary>
        /// Get the current fading value.
        /// </summary>
        public float get(Time aCurrentTime)
        {
            if (mActive == State.LFO)
            {
                // LFO mode
                if (mStartTime > aCurrentTime)
                    // Time rolled over.
                    mStartTime = aCurrentTime;
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
                mActive = State.Inactive;
                return mTo;
            }
            mCurrent = (float)(mFrom + mDelta * ((aCurrentTime - mStartTime) / mTime));
            return mCurrent;
        }

        public enum State
        {
            Inactive = -1,
            Disabled = 0,
            Active = 1,
            LFO = 2,
        }
    }
}
