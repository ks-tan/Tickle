using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Collections;
using System;

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
        public int _id;
        public T* _target;
        public T _start;
        public T _end;
        public float _duration;
        public Ease.Type _easeType;

        public float _elapsedTime;
        public bool _isRunning;
        public bool _isDone;

        public Lerp(int id, ref T target, T start, T end, float duration, Ease.Type ease = Ease.Type.None)
        {
            _id = id;
            _target = (T*)UnsafeUtility.AddressOf(ref target);
            _start = start;
            _end = end;
            _elapsedTime = 0;
            _duration = duration;
            _easeType = ease;
            _isRunning = false;
            _isDone = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            if (!_isRunning) return;
            if (_isDone) return;
            if (_elapsedTime < _duration)
            {
                var value = LerpManager<T>.ApplyLerp(_start, _end, Ease.Apply(_elapsedTime / _duration, _easeType));
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
        private static delegate*<T, T, float, T> _lerp;
        private static NativeArray<Lerp<T>> _runningProcesses = new NativeArray<Lerp<T>>(64, Allocator.Persistent);

        private static void SetupRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("[LerpRunner]");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<LerpRunner>();

            // TODO: Add more types here if needed
            if (typeof(T) == typeof(float)) _lerp = (delegate*<T, T, float, T>)LerpType.Float;
            else if (typeof(T) == typeof(Color)) _lerp = (delegate*<T, T, float, T>)LerpType.Colour;
            else if (typeof(T) == typeof(Vector2)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec2;
            else if (typeof(T) == typeof(Vector3)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec3;
            else if (typeof(T) == typeof(Vector3)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec4;
            else if (typeof(T) == typeof(Quaternion)) _lerp = (delegate*<T, T, float, T>)LerpType.Quat;
        }

        public static bool TryGetRunningProcess(int id, ref Lerp<T> processRef)
        {
            Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(_runningProcesses);

            for (int i = 0; i < _processCount; i++)
            {
                if (ptr[i]._id == id)
                {
                    processRef = ptr[i];
                    return true;
                }
            }
            return false;
        }

        public static int Start(ref T target, T start, T end, float duration, Ease.Type ease = Ease.Type.None)
        {
            var process = new Lerp<T>(_rollingId++, ref target, start, end, duration, ease);
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
            var isFound = TryGetRunningProcess(process._id, ref dummy);
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
            var isFound = TryGetRunningProcess(id, ref process);
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
            var isFound = TryGetRunningProcess(id, ref process);
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
            var isFound = TryGetRunningProcess(id, ref process);
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
            var isFound = TryGetRunningProcess(id, ref process);
            if (!isFound) return;
            process._isDone = true;
        }

        public static void UpdateAll()
        {
            Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(_runningProcesses);

            for (int i = 0; i < _processCount; i++)
                ptr[i].Update();

            int index = 0;
            while (index < _processCount)
            {
                if (!ptr[index]._isDone)
                {
                    index++;
                    continue;
                }

                // Remove from list by swapping with last element and reduce count
                ptr[index] = ptr[_processCount - 1];
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

        // This cleans up leaky memory caused by processes which the owner for _target has been destroyed
        public static void CancelAllForTarget(void* target)
        {
            Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(_runningProcesses);
            for (int i = 0; i < _processCount; i++)
                if (ptr[i]._target == target)
                    ptr[i]._isDone = true;
        }

        public static T ApplyLerp(T a, T b, float t) => _lerp(a, b, t);
        
        private static unsafe class LerpType
        {
            // TODO: Add more types here if needed
            public static delegate*<float, float, float, float> Float = &Mathf.Lerp;
            public static delegate*<Color, Color, float, Color> Colour = &Color.Lerp;
            public static delegate*<Vector2, Vector2, float, Vector2> Vec2 = &Vector2.Lerp;
            public static delegate*<Vector3, Vector3, float, Vector3> Vec3 = &Vector3.Lerp;
            public static delegate*<Vector4, Vector4, float, Vector4> Vec4 = &Vector4.Lerp;
            public static delegate*<Quaternion, Quaternion, float, Quaternion> Quat = &Quaternion.Lerp;
        }

        // Runs automatically after a Domain Reload (when exiting Play Mode or recompiling scripts)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DomainReloadCleanup()
        {
            if (_runningProcesses.IsCreated)
                _runningProcesses.Dispose(); // Release NativeArray memory
            _runner = null; // Remove reference to GameObject
            _processCount = 0; // Reset active lerps
            _rollingId = 0; // Reset ID counter
        }
    }

    public unsafe static class Ease
    {
        public static float Apply(float t, Type type)
        {
            if (type == Type.None) return None(t);
            if (type == Type.OutExpo) return OutExpo(t);
            throw new NotSupportedException();
        }

        public enum Type { None, OutExpo }
        private static float None(float t) => t;
        private static float OutExpo(float t) => t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
    }
}
