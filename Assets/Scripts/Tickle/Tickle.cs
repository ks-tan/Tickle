using System.Collections.Generic;
using System;
using Tickle.Lerp;
using UnityEngine;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Tickle
{
    public partial class Tickle<T> : ITickle where T : unmanaged
    {
        private int _lerpId;
        private bool _isDone;
        private float _duration;
        private Action _onComplete;

        protected T _value;
        public bool IsDone => _isDone;
        public float Duration => _duration;

        public Tickle(T start, T end, float duration, Ease.Type ease, Action onComplete)
        {
            if (TickleRunner.Instance == null) SetupRunner();
            _lerpId = LerpManager<T>.Create(ref _value, ref _isDone, start, end, duration, ease);
            _duration = duration;
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

        public virtual void Update() { }

        public void InvokeOnComplete() => _onComplete?.Invoke();

        public ITickle OnComplete(Action onComplete)
        {
            _onComplete += onComplete;
            return this;
        }

        ~Tickle() => LerpManager<T>.Destroy(_lerpId);

        private static void SetupRunner()
        {
            if (TickleRunner.Instance != null) return;
            new GameObject("[TickleRunner]").AddComponent<TickleRunner>();
        }
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
                tickle.InvokeOnComplete();
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
        float Duration { get; }
        void Update();
        void InvokeOnComplete();
        ITickle Start();
        void Stop();
        void Pause();
        void Resume();
        ITickle OnComplete(Action onComplete);
    }

    public readonly struct TickleChain
    {
        public ITickle[,] Array { get; }
        public TickleChain(ITickle[,] array) => Array = array;
        public TickleChain(params ITickle[][] tickles) => Array = null; // TODO
        public TickleChain(params ITickle[] tickles) => Array = null; // TODO

        public static implicit operator TickleChain(ITickle[,] array) => new(array);
        public static implicit operator ITickle[,](TickleChain seq) => seq.Array;
    
        public TickleChain Chain(params ITickle[] tickle)
        {
            return this; // TOOD
        }

        public TickleChain Chain(params ITickle[][] tickles)
        {
            return this; // TODO
        }

        public TickleChain Chain(params TickleChain[] tickles)
        {
            return this; // TODO
        }
    }
}