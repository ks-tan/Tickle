using System;
using Tickle.Lerp;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Tickle
{
    public static class Tickler
    {
        public static Tickle<float> Lerp(this ref float floatRef, float start, float end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new TickleSimple<float>(ref floatRef, start, end, duration, ease, onComplete);

        public static Tickle<Vector3> LerpScale(this Transform transform, int start, int end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new TickleTransformScale<Vector3>(transform, Vector3.one * start, Vector3.one * end, duration, ease, onComplete);

        public static Tickle<Vector3> LerpPosition(this Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new TickleTransformPosition<Vector3>(transform.transform, start, end, duration, ease, onComplete);

        public static ITickle[] Combine(params ITickle[] tickles)
            => tickles;

        public static ITickle[] Combine(this ITickle[] tickles, ITickle other)
        {
            var newTickles = new ITickle[tickles.Length + 1];
            for(int i = 0; i < tickles.Length; i++)
                newTickles[i] = tickles[i];
            newTickles[tickles.Length] = other;
            return newTickles;
        }

        public static ITickle[] Combine(this ITickle tickle, ITickle other) 
            => Combine(new ITickle[] { tickle, other });

        public static ITickle[] Start(this ITickle[] tickles)
        {
            for (int i = 0; i < tickles.Length; i++)
                tickles[i].Start();
            return tickles;
        }

        public static ITickle[] OnComplete(this ITickle[] tickles, Action onComplete)
        {
            var longestTickle = tickles[0];
            for(int i = 0; i < tickles.Length; i++)
            {
                var tickle = tickles[i];
                if (tickle.Duration <= longestTickle.Duration) continue;
                longestTickle = tickle;
            }
            longestTickle.OnComplete(onComplete);
            return tickles;
        }
    }

    public unsafe class TickleSimple<T> : Tickle<T> where T : unmanaged
    {
        private T* _target;

        public TickleSimple(ref T target, T start, T end, float duration, Ease.Type ease, Action onComplete) 
            : base(start, end, duration, ease, onComplete) => _target = (T*)UnsafeUtility.AddressOf(ref target);

        public override void Update() => *_target = _value;
    }

    public class TickleTransform<T> : Tickle<T> where T : unmanaged
    {
        protected Transform _transform;
        public TickleTransform(Transform transform, T start, T end, float duration, Ease.Type ease, Action onComplete) 
            : base(start, end, duration, ease, onComplete) => _transform = transform;
    }

    public class TickleTransformPosition<T> : TickleTransform<Vector3>
    {
        public TickleTransformPosition(Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease, Action onComplete) 
            : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.position = _value;
    }

    public class TickleTransformScale<T> : TickleTransform<Vector3>
    {
        public TickleTransformScale(Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease, Action onComplete) 
            : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.localScale = _value;
    }

    public class TickleTransformRotation<T> : TickleTransform<Quaternion>
    {
        public TickleTransformRotation(Transform transform, Quaternion start, Quaternion end, float duration, Ease.Type ease, Action onComplete) 
            : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.rotation = _value;
    }
}