using System;
using Tickle.Easings;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Tickle
{
    public static class Tickler
    {
        // TODO: Support more lerp operations

        public static Tickle<float> WaitForSeconds(float seconds, Action onComplete = null)
            => new Tickle<float>(0, 1, seconds, Ease.None, onComplete);

        public static Tickle<float> Lerp(this ref float floatRef, float start, float end, float duration, Ease ease = Ease.None, Action onComplete = null)
            => new TickleSimple<float>(ref floatRef, start, end, duration, ease, onComplete);

        public static Tickle<Vector3> LerpScale(this Transform transform, float start, float end, float duration, Ease ease = Ease.None, Action onComplete = null)
            => new TickleTransformScale<Vector3>(transform, Vector3.one * start, Vector3.one * end, duration, ease, onComplete);

        public static Tickle<Vector3> LerpPosition(this Transform transform, Vector3 start, Vector3 end, float duration, Ease ease = Ease.None, Action onComplete = null)
            => new TickleTransformPosition<Vector3>(transform, start, end, duration, ease, onComplete);

        public static Tickle<Quaternion> LerpQuaternion(this Transform transform, Vector3 eulerStart, Vector3 eulerEnd, float duration, Ease ease = Ease.None, Action onComplete = null)
            => new TickleTransformQuaternion<Quaternion>(transform, Quaternion.Euler(eulerStart), Quaternion.Euler(eulerEnd), duration, ease, onComplete);

        public static Tickle<Vector3> LerpRotation(this Transform transform, Vector3 eulerStart, Vector3 eulerEnd, float duration, Ease ease = Ease.None, Action onComplete = null)
            => new TickleTransformRotation<Vector3>(transform, eulerStart, eulerEnd, duration, ease, onComplete);

        public static ITickle[] Start(this ITickle[] tickles)
            => new TickleSet(tickles).Start();

        public static ITickle[] Join(this ITickle current, params ITickle[] tickles)
            => new TickleSet(current).Join(tickles);

        public static ITickle[] Join(this ITickle[] current, params ITickle[] tickles)
            => new TickleSet(current).Join(tickles);

        public static ITickle[] OnComplete(this ITickle[] tickles, Action onComplete)
            => new TickleSet(tickles).OnComplete(onComplete);

        public static ITickle[][] Start(this ITickle[][] chain)
            => new TickleChain(chain).Start();

        public static void Stop(this ITickle[][] chain)
            => new TickleChain(chain).Stop();

        public static ITickle[][] Chain(this ITickle[] current, params ITickle[] tickleSets)
            => new TickleChain(current).Chain(tickleSets);

        public static ITickle[][] Chain(this ITickle[][] current, params ITickle[] tickleSets)
            => new TickleChain(current).Chain(tickleSets);

        public static ITickle[][] OnComplete(this ITickle[][] chain, Action onComplete)
            => new TickleChain(chain).OnComplete(onComplete);
    }

    public unsafe class TickleSimple<T> : Tickle<T> where T : unmanaged
    {
        private T* _target;

        public TickleSimple(ref T target, T start, T end, float duration, Ease ease, Action onComplete) 
            : base(start, end, duration, ease, onComplete) => _target = (T*)UnsafeUtility.AddressOf(ref target);

        public override void Update() => *_target = _value;
    }

    public class TickleTransform<T> : Tickle<T> where T : unmanaged
    {
        protected Transform _transform;
        public TickleTransform(Transform transform, T start, T end, float duration, Ease ease, Action onComplete) 
            : base(start, end, duration, ease, onComplete) => _transform = transform;
    }

    public class TickleTransformPosition<T> : TickleTransform<Vector3>
    {
        public TickleTransformPosition(Transform transform, Vector3 start, Vector3 end, float duration, Ease ease, Action onComplete) 
            : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.localPosition = _value;
    }

    public class TickleTransformScale<T> : TickleTransform<Vector3>
    {
        public TickleTransformScale(Transform transform, Vector3 start, Vector3 end, float duration, Ease ease, Action onComplete) 
            : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.localScale = _value;
    }

    public class TickleTransformQuaternion<T> : TickleTransform<Quaternion>
    {
        public TickleTransformQuaternion(Transform transform, Quaternion start, Quaternion end, float duration, Ease ease, Action onComplete) 
            : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.localRotation = _value;
    }

    public class TickleTransformRotation<T> : TickleTransform<Vector3>
    {
        public TickleTransformRotation(Transform transform, Vector3 start, Vector3 end, float duration, Ease ease, Action onComplete)
            : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.localRotation = Quaternion.Euler(_value);
    }
}