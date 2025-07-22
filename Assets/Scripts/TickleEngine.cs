using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace Tickle.Engine
{
    public class LerpRunner : MonoBehaviour
    {
        private void Update()
        {
            // TODO: Add more types here if needed
            // TODO: These UpdateAlls can be run as Jobs!
            LerpManager<float>.UpdateAll();
            LerpManager<Color>.UpdateAll();
            LerpManager<Vector2>.UpdateAll();
            LerpManager<Vector3>.UpdateAll();
            LerpManager<Vector4>.UpdateAll();
            LerpManager<Quaternion>.UpdateAll();

            LerpManager<float>.CompactProcessesArray();
            LerpManager<Color>.CompactProcessesArray();
            LerpManager<Vector2>.CompactProcessesArray();
            LerpManager<Vector3>.CompactProcessesArray();
            LerpManager<Vector4>.CompactProcessesArray();
            LerpManager<Quaternion>.CompactProcessesArray();
        }
    }

    public unsafe struct Lerp<T> where T : unmanaged
    {
        // Note: Target owner should be a reference type for there to be no boxing
        public int _targetOwnerHash;
        public int _id;
        public T* _target;
        public T _start;
        public T _end;
        public float _duration;
        public delegate*<T, T, float, T> _lerp; // TODO: Turn this to enum, or this remains managed in the eyes of Mono
        public delegate*<float, float> _ease; // TODO: Turn this to enum, or this remains managed in the eyes of Mono

        public float _elapsedTime;
        public bool _isRunning;
        public bool _isDone;

        public Lerp(int id, int targetOwnerHash, ref T target, T start, T end, float duration, delegate*<float, float> ease = null)
        {
            _id = id;
            _target = (T*)UnsafeUtility.AddressOf(ref target);
            _start = start;
            _end = end;
            _elapsedTime = 0;
            _duration = duration;
            _ease = ease == null ? Ease.None : ease;
            _isRunning = false;
            _isDone = false;
            _targetOwnerHash = targetOwnerHash;

            _lerp = (delegate*<T, T, float, T>)LerpType.Float;
            if (typeof(T) == typeof(Color)) _lerp = (delegate*<T, T, float, T>)LerpType.Colour;
            if (typeof(T) == typeof(Vector2)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec2;
            if (typeof(T) == typeof(Vector3)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec3;
            if (typeof(T) == typeof(Vector4)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec4;
            if (typeof(T) == typeof(Quaternion)) _lerp = (delegate*<T, T, float, T>)LerpType.Quat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            if (!_isRunning) return;
            if (_isDone) return;
            var isTargetOwnerInvalid = _targetOwnerHash != -1 && !LerpManager<T>._hashToObject.ContainsKey(_targetOwnerHash);
            if (isTargetOwnerInvalid || _target == null)
            {
                _isDone = true;
                return;
            }
            if (_elapsedTime < _duration)
            {
                var value = _lerp(_start, _end, _ease(_elapsedTime / _duration));
                *_target = value;
                _elapsedTime += Time.deltaTime;
                return;
            }
            *_target = _end;
            _isDone = true;
        }
    }

    public static unsafe class LerpManager<T> where T : unmanaged
    {
        private static LerpRunner _runner;
        private static int _rollingId;
        private static int _processCount;

        private static NativeArray<Lerp<T>> _runningProcesses = new NativeArray<Lerp<T>>(64, Allocator.Persistent);
        public static Dictionary<int, object> _hashToObject = new Dictionary<int, object>();

        private static void SetupRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("[LerpRunner]");
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<LerpRunner>();
        }

        public static bool TryGetProcess(int id, ref Lerp<T> processRef)
        {
            for (int i = 0; i < _processCount; i++)
            {
                if (_runningProcesses[i]._id == id)
                {
                    processRef = _runningProcesses[i];
                    return true;
                }
            }
            return false;
        }

        public static int Start(object targetOwner, ref T target, T start, T end, float duration, delegate*<float, float> ease = null)
        {
            var targetOwnerHash = targetOwner == null ? -1 : targetOwner.GetHashCode();
            if (targetOwnerHash != -1)
                _hashToObject.Add(targetOwnerHash, targetOwner);
            var process = new Lerp<T>(_rollingId++, targetOwnerHash, ref target, start, end, duration, ease);
            Start(ref process);
            return process._id;
        }

        public static void Start(ref Lerp<T> process)
        {
            if (_runner == null)
                SetupRunner();

            if (process._isRunning) return;
            process._isRunning = true;
            process._isDone = false;
            process._elapsedTime = 0;

            Lerp<T> dummy = default;
            var isFound = TryGetProcess(process._id, ref dummy);
            if (!isFound)
            {
                if (_processCount >= _runningProcesses.Length)
                    ResizeProcessesArray(_runningProcesses.Length * 2);
                _runningProcesses[_processCount++] = process;
            }
        }

        public static void Start(int id)
        {
            if (_runner == null)
                SetupRunner();

            Lerp<T> process = default;
            var isFound = TryGetProcess(id, ref process);
            if (!isFound) return;
            process._isRunning = true;
            process._isDone = false;
            process._elapsedTime = 0;
        }

        public static void Resume(ref Lerp<T> process)
        {
            process._isRunning = true;
        }

        public static void Resume(int id)
        {
            Lerp<T> process = default;
            var isFound = TryGetProcess(id, ref process);
            if (!isFound) return;
            process._isRunning = true;
        }

        public static void Pause(ref Lerp<T> process)
        {
            process._isRunning = false;
        }

        public static void Pause(int id)
        {
            Lerp<T> process = default;
            var isFound = TryGetProcess(id, ref process);
            if (!isFound) return;
            process._isRunning = false;
        }

        public static void Stop(ref Lerp<T> process)
        {
            process._isDone = true;
        }

        public static void Stop(int id)
        {
            Lerp<T> process = default;
            var isFound = TryGetProcess(id, ref process);
            if (!isFound) return;
            process._isDone = true;
        }

        public static void UpdateAll()
        {
            foreach (var kvp in _hashToObject.ToList())
            {
                var obj = kvp.Value;
                if (obj != null) continue;
                _hashToObject.Remove(kvp.Key);
            }

            for (int i = 0; i < _processCount; i++)
                _runningProcesses[i].Update();

            int index = 0;
            while (index < _processCount)
            {
                if (!_runningProcesses[index]._isDone)
                {
                    index++;
                    continue;
                }

                // Remove from list by swapping with last element and reduce count
                _runningProcesses[index] = _runningProcesses[_processCount - 1];
                _processCount--;
            }
        }

        public static void CompactProcessesArray()
        {
            if (_processCount > _runningProcesses.Length / 3) return;
            ResizeProcessesArray(_runningProcesses.Length / 2);
        }

        public static void ResizeProcessesArray(int newSize)
        {
            var newArray = new NativeArray<Lerp<T>>(newSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            int elementsToCopy = _processCount;
            NativeArray<Lerp<T>>.Copy(_runningProcesses, newArray, elementsToCopy);
            _runningProcesses.Dispose();
            _runningProcesses = newArray;
        }
    }

    public static unsafe class LerpType
    {
        // TODO: Add more types here if needed
        public static delegate*<float, float, float, float> Float = &Mathf.Lerp;
        public static delegate*<Color, Color, float, Color> Colour = &Color.Lerp;
        public static delegate*<Vector2, Vector2, float, Vector2> Vec2 = &Vector2.Lerp;
        public static delegate*<Vector3, Vector3, float, Vector3> Vec3 = &Vector3.Lerp;
        public static delegate*<Vector4, Vector4, float, Vector4> Vec4 = &Vector4.Lerp;
        public static delegate*<Quaternion, Quaternion, float, Quaternion> Quat = &Quaternion.Lerp;
    }

    public unsafe static class Ease
    {
        // TODO: Add more easing functions
        private static float Default(float t) => t;
        public static delegate*<float, float> None = &Default;
    }
}
