using System.Collections.Generic;
using System;
using Tickle.Lerp;
using UnityEngine;
using UnityEditor.PackageManager;
using Unity.VisualScripting;

namespace Tickle
{
    public class Tickle<T> : ITickle where T : unmanaged
    {
        private int _lerpId;
        private bool _isDone;
        private Action _onComplete;

        // This pair of properties will only be used when we are trying to lerp
        // a target which we cannot get a ref handle on, for e.g. transform.localScale.
        // _target will act as a proxy for the target's value, and setter will define
        // how we may assign the value to the target.
        // Because delegates allocates to GC, we may ignore/avoid these properties
        // when possible.
        private T _target;
        private Action<T> _setter;

        public bool IsDone => _isDone;

        public Tickle(ref T target, T start, T end, float duration, Ease.Type ease, Action oncomplete)
        {
            if (TickleRunner.Instance == null) SetupRunner();
            _lerpId = LerpManager<T>.Create(ref target, ref _isDone, start, end, duration, ease);
            _onComplete = oncomplete;
        }

        public Tickle(Action<T> setter, T start, T end, float duration, Ease.Type ease, Action onComplete)
        {
            if (TickleRunner.Instance == null) SetupRunner();
            _setter = setter;
            _lerpId = LerpManager<T>.Create(ref _target, ref _isDone, start, end, duration, ease);
            _onComplete = onComplete;
        }

        public void Start()
        {
            LerpManager<T>.Start(_lerpId);
            TickleRunner.AddTickle(this);
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

        public void Update()
        {
            try
            {
                _setter?.Invoke(_target);
            }
            catch
            {
                Debug.LogError("Error invoking target setter. Target might have been destroyed.");
                LerpManager<T>.Destroy(_lerpId);
                _isDone = true;
            }
        }

        public void OnComplete() => _onComplete?.Invoke();

        ~Tickle() => LerpManager<T>.Destroy(_lerpId);
    }

    public class TickleRunner : MonoBehaviour
    {
        public static TickleRunner Instance;

        private static List<ITickle> _toRemove = new List<ITickle>();
        private static List<ITickle> _tickles = new List<ITickle>();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Update()
        {
            foreach (var tickle in _tickles)
            {
                tickle.Update();
                if (!tickle.IsDone) continue;
                _toRemove.Add(tickle);
                tickle.OnComplete();
            }

            foreach (var tickle in _toRemove)
                _tickles.Remove(tickle);
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
    }

    public static class Tickler
    {
        public static Tickle<float> Lerp(this ref float floatRef, float start, float end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new Tickle<float>(ref floatRef, start, end, duration, ease, onComplete);

        public static Tickle<Vector3> LerpScale(this Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new Tickle<Vector3>(x => transform.localScale = x, start, end, duration, ease, onComplete);

        public static Tickle<Vector3> LerpScale(this Transform transform, float start, float end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new Tickle<Vector3>(x => transform.localScale = x, Vector3.one * start, Vector3.one * end, duration, ease, onComplete);

        public static Tickle<Vector3> LerpPosition(this Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
            => new Tickle<Vector3>(x => transform.position = x, start, end, duration, ease, onComplete);
    }
}