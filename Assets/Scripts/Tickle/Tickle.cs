using System.Collections.Generic;
using System;
using Tickle.Lerp;
using UnityEngine;

namespace Tickle
{
    public partial class Tickle<T> : ITickle where T : unmanaged
    {
        private int _lerpId;
        private bool _isDone;
        private float _duration;
        private Action _onComplete;
        private Action _startNextSet; // for chains

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

        public void AssignNextSetAction(Action startNextSet) => _startNextSet = startNextSet;

        public void InvokeNextSet() => _startNextSet?.Invoke();

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
                tickle.InvokeNextSet();
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
        ITickle Start();
        void Stop();
        void Pause();
        void Resume();
        ITickle OnComplete(Action onComplete);
        void InvokeOnComplete();
        void AssignNextSetAction(Action startNextSet);
        void InvokeNextSet();
    }

    public class TickleSet
    {
        public ITickle[] Tickles { get; }
        public static implicit operator TickleSet(ITickle[] array) => new(array);
        public static implicit operator ITickle[](TickleSet set) => set.Tickles;

        public TickleSet() => Tickles = new ITickle[0];

        public TickleSet(params ITickle[] tickles) => Tickles = tickles;

        public TickleSet Start() 
        {
            for (int i = 0; i < Tickles.Length; i++)
                Tickles[i].Start();
            return this;
        }

        public TickleSet OnComplete(Action onComplete)
        {
            var longestTickle = Tickles[0];
            for (int i = 0; i < Tickles.Length; i++)
            {
                var tickle = Tickles[i];
                if (tickle.Duration <= longestTickle.Duration) continue;
                longestTickle = tickle;
            }
            longestTickle.OnComplete(onComplete);
            return this;
        }

        public TickleSet Join(params ITickle[] tickles)
        {
            // TODO: This is broken! It should return THIS
            var combinedLength = Tickles.Length + tickles.Length;
            var newArray = new ITickle[combinedLength];
            for (int i = 0; i < Tickles.Length; i++)
                newArray[i] = Tickles[i];
            for (int i = 0; i < tickles.Length; i++)
                newArray[i + Tickles.Length] = tickles[i];
            return newArray;
        }
    }

    public class TickleChain
    {
        public ITickle[][] Array { get; }
        public static implicit operator TickleChain(ITickle[][] array) => new(array);
        public static implicit operator ITickle[][](TickleChain seq) => seq.Array;

        public TickleChain() => Array = new ITickle[0][];

        public TickleChain(params ITickle[][] tickleSets)
        {
            Array = tickleSets;
            for (int i = 0; i < Array.Length - 1; i++)
                Chain(Array[i], Array[i + 1]);
        }

        public TickleChain Chain(params ITickle[][] tickleSets)
        {
            // TODO: This is broken! It should return THIS
            var combinedLength = Array.Length + tickleSets.Length;
            var newArray = new ITickle[combinedLength][];
            for (int i = 0; i < Array.Length; i++)
                newArray[i] = Array[i];
            for (int i = 0; i < tickleSets.Length; i++)
                newArray[i + Array.Length] = tickleSets[i];
            for (int i = 0; i < newArray.Length - 1; i++)
                Chain(newArray[i], newArray[i + 1]);
            return newArray;
        }

        public TickleChain Chain(ITickle tickle)
        {
            // TODO: This is broken! It should return THIS
            var newArray = new ITickle[Array.Length + 1][];
            for (int i = 0; i < Array.Length; i++)
                newArray[i] = Array[i];
            newArray[newArray.Length - 1] = new ITickle[] { tickle };
            return newArray;
        }

        public void Chain(ITickle[] currentSet, ITickle[] nextSet)
        {
            var longestTickle = currentSet[0];
            for (int i = 0; i < currentSet.Length; i++)
            {
                var tickle = currentSet[i];
                if (tickle.Duration <= longestTickle.Duration) continue;
                longestTickle = tickle;
            }
            longestTickle.AssignNextSetAction(() => nextSet.Start());
        }

        // TODO: Implement Stop, Pause and Resume
        public TickleChain Start()
        {
            Array[0].Start();
            return this;
        }

        public TickleChain OnComplete(Action onComplete)
        {
            Array[Array.Length - 1].OnComplete(onComplete);
            return this;
        }
    }
}