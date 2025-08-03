#if ENABLE_BURST
using Unity.Burst;
#endif

namespace Tickle.Easings
{
    public enum Ease
    {
        None, Reverse, 
        InQuad, OutQuad, BounceQuad, JumpQuad
    }

    public static class EaseFunctions
    {
#if ENABLE_BURST
        [BurstCompile]
#endif
        public static float Apply(float t, Ease ease)
        {
            if (ease == Ease.None) return None(t);
            if (ease == Ease.Reverse) return Reverse(t);
            if (ease == Ease.InQuad) return EaseInQuad(t);
            if (ease == Ease.OutQuad) return EaseOutQuad(t);
            if (ease == Ease.BounceQuad) return BounceQuad(t);
            if (ease == Ease.JumpQuad) return JumpQuad(t);
            return t;
        }

        private static float None(float t) => t;
        private static float Reverse(float t) => 1 - t;
        private static float EaseInQuad(float t) => t * t;
        private static float EaseOutQuad(float t) => Reverse(EaseInQuad(Reverse(t)));
        private static float BounceQuad(float t) => t < 0.5f ? EaseInQuad(t * 2) : EaseInQuad(Reverse(t) * 2);
        private static float JumpQuad(float t) => t < 0.5f ? EaseOutQuad(t * 2) : EaseOutQuad(Reverse(t) * 2);
    }
}
