using System.Collections.Generic;
using System;
using Tickle.Lerp;
using UnityEngine;
using Tickle.Easings;

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

        public Tickle(T start, T end, float duration, Ease ease, Action onComplete)
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
        private ITickle[] _tickles;
        public static implicit operator TickleSet(ITickle[] array) => new(array);
        public static implicit operator ITickle[](TickleSet set) => set._tickles;

        public TickleSet() => _tickles = new ITickle[0];

        public TickleSet(params ITickle[] tickles) => _tickles = tickles;

        public TickleSet Start() 
        {
            for (int i = 0; i < _tickles.Length; i++)
                _tickles[i].Start();
            return this;
        }

        public TickleSet OnComplete(Action onComplete)
        {
            var longestTickle = _tickles[0];
            for (int i = 0; i < _tickles.Length; i++)
            {
                var tickle = _tickles[i];
                if (tickle.Duration <= longestTickle.Duration) continue;
                longestTickle = tickle;
            }
            longestTickle.OnComplete(onComplete);
            return this;
        }

        public TickleSet Join(params ITickle[] tickles)
        {
            var combinedLength = _tickles.Length + tickles.Length;
            var newArray = new ITickle[combinedLength];
            for (int i = 0; i < _tickles.Length; i++)
                newArray[i] = _tickles[i];
            for (int i = 0; i < tickles.Length; i++)
                newArray[i + _tickles.Length] = tickles[i];
            _tickles = newArray;
            return this;
        }
    }

    public class TickleChain
    {
        private ITickle[][] _array;
        public static implicit operator TickleChain(ITickle[][] array) => new(array);
        public static implicit operator ITickle[][](TickleChain seq) => seq._array;

        public TickleChain() => _array = new ITickle[0][];

        public TickleChain(params ITickle[][] tickleSets)
        {
            _array = tickleSets;
            for (int i = 0; i < _array.Length - 1; i++)
                Chain(_array[i], _array[i + 1]);
        }

        public TickleChain Chain(params ITickle[][] tickleSets)
        {
            var combinedLength = _array.Length + tickleSets.Length;
            var newArray = new ITickle[combinedLength][];
            for (int i = 0; i < _array.Length; i++)
                newArray[i] = _array[i];
            for (int i = 0; i < tickleSets.Length; i++)
                newArray[i + _array.Length] = tickleSets[i];
            for (int i = 0; i < newArray.Length - 1; i++)
                Chain(newArray[i], newArray[i + 1]);
            _array = newArray;
            return this;
        }

        public TickleChain Chain(ITickle tickle)
        {
            var newArray = new ITickle[_array.Length + 1][];
            for (int i = 0; i < _array.Length; i++)
                newArray[i] = _array[i];
            newArray[newArray.Length - 1] = new ITickle[] { tickle };
            if (_array.Length > 0)
                Chain(newArray[newArray.Length - 2], newArray[newArray.Length - 1]);
            _array = newArray;
            return this;
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
            _array[0].Start();
            return this;
        }

        public void Stop()
        {
            for(int i = 0; i < _array.Length; i++)
                for (int j = 0; j < _array[i].Length; j++)
                    _array[i][j].Stop();
        }

        public void Pause()
        {
            for (int i = 0; i < _array.Length; i++)
                for (int j = 0; j < _array[i].Length; j++)
                    _array[i][j].Pause();
        }

        public void Resume()
        {
            for (int i = 0; i < _array.Length; i++)
                for (int j = 0; j < _array[i].Length; j++)
                    _array[i][j].Resume();
        }

        public TickleChain OnComplete(Action onComplete)
        {
            _array[_array.Length - 1].OnComplete(onComplete);
            return this;
        }
    }
}