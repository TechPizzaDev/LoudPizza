namespace LoudPizza.Sources.Streaming
{
    // TODO: expose a general-purpose property change listener?
    internal interface IRelativePlaybackRateChangeListener
    {
        void RelativePlaybackRateChanged(float relativePlaybackSpeed);
    }
}
