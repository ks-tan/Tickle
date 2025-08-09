#if ENABLE_BURST
using Unity.Burst;
using UnityEngine;
#endif

namespace Tickle.Easings
{
    public enum Ease
    {
        None, Reverse, 
        InQuad, OutQuad, BounceQuad, JumpQuad,
        OutElastic
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
            if (ease == Ease.OutElastic) return EaseOutElastic(t);
            return t;
        }

        private static float None(float t) => t;
        private static float Reverse(float t) => 1 - t;
        private static float EaseInQuad(float t) => t * t;
        private static float EaseOutQuad(float t) => Reverse(EaseInQuad(Reverse(t)));
        private static float BounceQuad(float t) => t < 0.5f ? EaseInQuad(t * 2) : EaseInQuad(Reverse(t) * 2);
        private static float JumpQuad(float t) => t < 0.5f ? EaseOutQuad(t * 2) : EaseOutQuad(Reverse(t) * 2);

        private static float EaseOutElastic(float t)
        {
            if (t < (1f / 2.75f)) return 7.5625f * t * t;
            else if (t < (2f / 2.75f)) return 7.5625f * (t -= (1.5f / 2.75f)) * t + 0.75f;
            else if (t < (2.5f / 2.75f)) return 7.5625f * (t -= (2.25f / 2.75f)) * t + 0.9375f;
            else return 7.5625f * (t -= (2.625f / 2.75f)) * t + 0.984375f;
        }
    }
}
