using System.Collections.Generic;
using System;
using Tickle.Lerp;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace Tickle
{
    public class Tickle<T> : ITickle where T : unmanaged
    {
        private int _lerpId;
        private bool _isDone;
        private Action _onComplete;

        protected T _value;
        public bool IsDone => _isDone;

        public Tickle(T start, T end, float duration, Ease.Type ease, Action onComplete)
        {
            if (TickleRunner.Instance == null) SetupRunner();
            _lerpId = LerpManager<T>.Create(ref _value, ref _isDone, start, end, duration, ease);
            _onComplete = onComplete;
        }

        public ITickle Start()
        {
            LerpManager<T>.Start(_lerpId);
            TickleRunner.AddTickle(this);
            return this;
        }

        public void Stop()
        {
            LerpManager<T>.Stop(_lerpId);
            TickleRunner.RemoveTickle(this);
        }

        public void Pause()
        {
            LerpManager<T>.Pause(_lerpId);
        }

        public void Resume()
        {
            LerpManager<T>.Resume(_lerpId);
        }

        private static void SetupRunner()
        {
            if (TickleRunner.Instance != null) return;
            new GameObject("[TickleRunner]").AddComponent<TickleRunner>();
        }

        public virtual void Update() { }

        public void OnComplete() => _onComplete?.Invoke();

        ~Tickle() => LerpManager<T>.Destroy(_lerpId);
    }

    public unsafe class TickleSimple<T> : Tickle<T> where T : unmanaged
    {
        private T* _target;

        public TickleSimple(ref T target, T start, T end, float duration, Ease.Type ease, Action onComplete) : 
            base(start, end, duration, ease, onComplete) => _target = (T*)UnsafeUtility.AddressOf(ref target);

        public override void Update() => *_target = _value;
    }

    public class TickleTransform<T> : Tickle<T> where T : unmanaged
    {
        protected Transform _transform;
        public TickleTransform(Transform transform, T start, T end, float duration, Ease.Type ease, Action onComplete) : base(start, end, duration, ease, onComplete) => _transform = transform;
    }

    public class TickleTransformPosition<T> : TickleTransform<Vector3>
    {
        public TickleTransformPosition(Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease, Action onComplete) : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.position = _value;
    }

    public class TickleTransformScale<T> : TickleTransform<Vector3>
    {
        public TickleTransformScale(Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease, Action onComplete) : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.localScale = _value;
    }

    public class TickleTransformRotation<T> : TickleTransform<Quaternion>
    {
        public TickleTransformRotation(Transform transform, Quaternion start, Quaternion end, float duration, Ease.Type ease, Action onComplete) : base(transform, start, end, duration, ease, onComplete) { }
        public override void Update() => _transform.rotation = _value;
    }

    public class TickleRunner : MonoBehaviour
    {
        public static TickleRunner Instance;

        // TODO: Implement object pooling to avoid GCs
        // TODO: Implement LitMotion's SparseSet for O(1) search, insert and removals
        private static List<ITickle> _toRemove = new List<ITickle>();
        private static List<ITickle> _tickles = new List<ITickle>();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Update()
        {
            for(int i = 0; i < _tickles.Count; i++)
            {
                var tickle = _tickles[i];
                tickle.Update();
                if (!tickle.IsDone) continue;
                _toRemove.Add(tickle);
                tickle.OnComplete();
            }

            for(int i = 0; i < _toRemove.Count; i++)
                _tickles.Remove(_toRemove[i]);
            _toRemove.Clear();
        }

        public static void AddTickle(ITickle tickle)
        {
            if (_tickles.Contains(tickle)) return;
            _tickles.Add(tickle);
        }

        public static void RemoveTickle(ITickle tickle)
        {
            if (!_tickles.Contains(tickle)) return;
            _tickles.Remove(tickle);
        }
    }

    public interface ITickle
    {
        bool IsDone { get; }
        void Update();
        void OnComplete();
        ITickle Start();
        void Stop();
        void Pause();
        void Resume();
    }

    public static class Tickler
    {
        public static Tickle<float> Lerp(this ref float floatRef, float start, float end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new TickleSimple<float>(ref floatRef, start, end, duration, ease, onComplete);

        public static Tickle<Vector3> LerpScale(this Transform transform, int start, int end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new TickleTransformScale<Vector3>(transform, Vector3.one * start, Vector3.one * end, duration, ease, onComplete);
    }
}