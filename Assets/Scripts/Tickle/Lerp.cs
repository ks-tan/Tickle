#define ENABLE_BURST

using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Collections;
using System;

#if ENABLE_BURST
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
#endif

namespace Tickle.Lerp
{
    public class LerpRunner : MonoBehaviour
    {
        public static LerpRunner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Update()
        {
            // TODO: Add more types here if needed
#if ENABLE_BURST
            LerpManager<float>.BurstUpdateAll();
            LerpManager<Color>.BurstUpdateAll();
            LerpManager<Vector2>.BurstUpdateAll();
            LerpManager<Vector3>.BurstUpdateAll();
            LerpManager<Vector4>.BurstUpdateAll();
            LerpManager<Quaternion>.BurstUpdateAll();
#else
            LerpManager<float>.UpdateAll();
            LerpManager<Color>.UpdateAll();
            LerpManager<Vector2>.UpdateAll();
            LerpManager<Vector3>.UpdateAll();
            LerpManager<Vector4>.UpdateAll();
            LerpManager<Quaternion>.UpdateAll();
#endif

            LerpManager<float>.CompactRunningProcessArray();
            LerpManager<Color>.CompactRunningProcessArray();
            LerpManager<Vector2>.CompactRunningProcessArray();
            LerpManager<Vector3>.CompactRunningProcessArray();
            LerpManager<Vector4>.CompactRunningProcessArray();
            LerpManager<Quaternion>.CompactRunningProcessArray();

            LerpManager<float>.CompactCreatedProcessArray();
            LerpManager<Color>.CompactCreatedProcessArray();
            LerpManager<Vector2>.CompactCreatedProcessArray();
            LerpManager<Vector3>.CompactCreatedProcessArray();
            LerpManager<Vector4>.CompactCreatedProcessArray();
            LerpManager<Quaternion>.CompactCreatedProcessArray();
        }

        private void OnDestroy()
        {
            LerpManager<float>.Cleanup();
            LerpManager<Color>.Cleanup();
            LerpManager<Vector2>.Cleanup();
            LerpManager<Vector3>.Cleanup();
            LerpManager<Vector4>.Cleanup();
            LerpManager<Quaternion>.Cleanup();
        }
    }

    public unsafe struct Lerp<T> where T : unmanaged
    {
        public int Id;
        public T* Target;
        public T Start;
        public T End;
        public float Duration;
        public Ease.Type EaseType;
        public float ElapsedTime;
        public bool IsRunning;
        public bool IsDone;
        private bool* _doneHandle;

        public Lerp(int id, ref T target, T start, T end, float duration, Ease.Type ease = Ease.Type.None)
        {
            Id = id;
            Target = (T*)UnsafeUtility.AddressOf(ref target);
            Start = start;
            End = end;
            ElapsedTime = 0;
            Duration = duration;
            EaseType = ease;
            IsRunning = false;
            IsDone = false;
            _doneHandle = null;
        }

        public void BindDoneHandle(ref bool doneHandle)
        {
            _doneHandle = (bool*)UnsafeUtility.AddressOf(ref doneHandle);
        }

        public void SetIsDone(bool isDone)
        {
            IsDone = isDone;
            if (_doneHandle != null)
                *_doneHandle = isDone;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            if (!IsRunning) return;
            if (IsDone) return;
            if (ElapsedTime < Duration)
            {
                var value = LerpManager<T>.ApplyLerp(Start, End, Ease.Apply(ElapsedTime / Duration, EaseType));
                *Target = value;
                ElapsedTime += Time.deltaTime;
                return;
            }
            *Target = End;
            SetIsDone(true);
        }
    }

    public static unsafe class LerpManager<T> where T : unmanaged
    {
        private static int _rollingId;
        private static delegate*<T, T, float, T> _lerp;

        // Note: _createdProcesses and _runningProcesses do not hold exact references
        // of the same Lerp<T> data, but copies. When a process is only created but
        // not running, we only check it from the _createdProcesses array. If a
        // process is running, we only check it from the _runningProcesses array.
        private static int _createdProcessCount;
        private static int _runningProcessCount;
        private static NativeArray<Lerp<T>> _createdProcesses;
        private static NativeArray<Lerp<T>> _runningProcesses;

        private static void Setup()
        {
            if (LerpRunner.Instance == null)
            {
                var go = new GameObject("[LerpRunner]");
                go.AddComponent<LerpRunner>();
                go.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            // TODO: Add more types here if needed
            if (typeof(T) == typeof(float)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Float;
            else if (typeof(T) == typeof(Color)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Colour;
            else if (typeof(T) == typeof(Vector2)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Vec2;
            else if (typeof(T) == typeof(Vector3)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Vec3;
            else if (typeof(T) == typeof(Vector4)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Vec4;
            else if (typeof(T) == typeof(Quaternion)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Quat;

            _runningProcesses = new NativeArray<Lerp<T>>(64, Allocator.Persistent);
            _createdProcesses = new NativeArray<Lerp<T>>(64, Allocator.Persistent);
        }

        private static bool TryGetProcess(int id, NativeArray<Lerp<T>> array, int count, ref Lerp<T> processRef)
        {
            Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(array);

            for (int i = 0; i < count; i++)
            {
                if (ptr[i].Id == id)
                {
                    processRef = ptr[i];
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetRunningProcess(int id, ref Lerp<T> processRef)
        {
            return TryGetProcess(id, _runningProcesses, _runningProcessCount, ref processRef);
        }

        public static bool TryGetCreatedProcess(int id, ref Lerp<T> processRef)
        {
            return TryGetProcess(id, _createdProcesses, _createdProcessCount, ref processRef);
        }

        public static int Create(ref T target, ref bool doneHandle, T start, T end, float duration, Ease.Type ease = Ease.Type.None)
        {
            if (!_createdProcesses.IsCreated)
                Setup();
            var process = new Lerp<T>(_rollingId++, ref target, start, end, duration, ease);
            process.BindDoneHandle(ref doneHandle);
            if (_createdProcessCount >= _createdProcesses.Length)
                ResizeCreatedProcessesArray(_createdProcesses.Length * 2);
            _createdProcesses[_createdProcessCount++] = process;
            return process.Id;
        }

        public static int Create(ref T target, T start, T end, float duration, Ease.Type ease = Ease.Type.None)
        {
            bool dummy = false;
            return Create(ref target, ref dummy, start, end, duration, ease);
        }

        public static int Start(ref T target, T start, T end, float duration, Ease.Type ease = Ease.Type.None)
        {
            var pid = Create(ref target, start, end, duration, ease);
            Start(pid);
            return pid;
        }

        public static void Start(int id)
        {
            Lerp<T> process = default;
            var isRunningProcessFound = TryGetRunningProcess(id, ref process);
            if (!isRunningProcessFound)
            {
                var isCreatedProcessFound = TryGetCreatedProcess(id, ref process);
                if (!isCreatedProcessFound) return;
                process.IsRunning = true;
                process.SetIsDone(false);
                process.ElapsedTime = 0;
                if (_runningProcessCount >= _runningProcesses.Length)
                    ResizeRunningProcessesArray(_runningProcesses.Length * 2);
                _runningProcesses[_runningProcessCount++] = process;
            }
            else
            {
                process.IsRunning = true;
                process.SetIsDone(false);
                process.ElapsedTime = 0;
            }
        }

        public static void Resume(int id)
        {
            Lerp<T> process = default;
            var isFound = TryGetRunningProcess(id, ref process);
            if (!isFound) return;
            process.IsRunning = true;
        }

        public static void Pause(int id)
        {
            Lerp<T> process = default;
            var isFound = TryGetRunningProcess(id, ref process);
            if (!isFound) return;
            process.IsRunning = false;
        }

        public static void Stop(int id)
        {
            Lerp<T> process = default;
            var isFound = TryGetRunningProcess(id, ref process);
            if (!isFound) return;
            process.SetIsDone(true);
        }

        public static void Destroy(int id)
        {
            Stop(id);

            Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(_createdProcesses);
            int index = 0;
            while (index < _createdProcessCount)
            {
                if (ptr[index].Id == id)
                {
                    // Remove from list by swapping with last element and reduce count
                    ptr[index] = ptr[_createdProcessCount - 1];
                    _createdProcessCount--;
                    break;
                }
                else index++;
            }
        }

        public static void UpdateAll()
        {
            if (!_runningProcesses.IsCreated) return;

            Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(_runningProcesses);

            for (int i = 0; i < _runningProcessCount; i++)
                ptr[i].Update();

            int index = 0;
            while (index < _runningProcessCount)
            {
                if (!ptr[index].IsDone)
                {
                    index++;
                    continue;
                }

                // Remove from list by swapping with last element and reduce count
                ptr[index] = ptr[_runningProcessCount - 1];
                _runningProcessCount--;
            }
        }

#if ENABLE_BURST
        public enum LerpType { Float, Color, Vec2, Vec3, Vec4, Quat }

        [BurstCompile]
        public static void BurstUpdateAll()
        {
            var type = LerpType.Float;
            if (typeof(T) == typeof(Color)) type = LerpType.Color;
            if (typeof(T) == typeof(Vector2)) type = LerpType.Vec2;
            if (typeof(T) == typeof(Vector3)) type = LerpType.Vec3;
            if (typeof(T) == typeof(Vector4)) type = LerpType.Vec4;
            if (typeof(T) == typeof(Quaternion)) type = LerpType.Quat;
            var job = new LerpUpdateParallelJob() {
                DeltaTime = Time.deltaTime,
                Processes = _runningProcesses,
                TypeLerp = type
            };
            JobHandle handle = job.Schedule(_runningProcessCount, 64); // 64 = batch size
            handle.Complete();
        }

        [BurstCompile]
        public struct LerpUpdateParallelJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Lerp<T>> Processes;
            public float DeltaTime;
            public LerpType TypeLerp;

            public void Execute(int i)
            {
                Lerp<T> process = Processes[i];
                if (!process.IsRunning || process.IsDone) return;

                if (process.ElapsedTime < process.Duration)
                {
                    float t = process.ElapsedTime / process.Duration;
                    t = Ease.Apply(t, process.EaseType);

                    // Directly write to target memory using the correct type
                    if (TypeLerp == LerpType.Float)
                    {
                        float start = UnsafeUtility.ReadArrayElement<float>(&process.Start, 0);
                        float end = UnsafeUtility.ReadArrayElement<float>(&process.End, 0);
                        float result = math.lerp(start, end, t);
                        UnsafeUtility.WriteArrayElement(process.Target, 0, result);
                    }
                    else if (TypeLerp == LerpType.Color)
                    {
                        Color start = UnsafeUtility.ReadArrayElement<Color>(&process.Start, 0);
                        Color end = UnsafeUtility.ReadArrayElement<Color>(&process.End, 0);
                        Color result = Color.Lerp(start, end, t);
                        UnsafeUtility.WriteArrayElement(process.Target, 0, result);
                    }
                    else if (TypeLerp == LerpType.Vec2)
                    {
                        Vector2 start = UnsafeUtility.ReadArrayElement<Vector2>(&process.Start, 0);
                        Vector2 end = UnsafeUtility.ReadArrayElement<Vector2>(&process.End, 0);
                        Vector2 result = Vector2.Lerp(start, end, t);
                        UnsafeUtility.WriteArrayElement(process.Target, 0, result);
                    }
                    else if (TypeLerp == LerpType.Vec3)
                    {
                        Vector3 start = UnsafeUtility.ReadArrayElement<Vector3>(&process.Start, 0);
                        Vector3 end = UnsafeUtility.ReadArrayElement<Vector3>(&process.End, 0);
                        Vector3 result = Vector3.Lerp(start, end, t);
                        UnsafeUtility.WriteArrayElement(process.Target, 0, result);
                    }
                    else if (TypeLerp == LerpType.Vec4)
                    {
                        Vector4 start = UnsafeUtility.ReadArrayElement<Vector4>(&process.Start, 0);
                        Vector4 end = UnsafeUtility.ReadArrayElement<Vector4>(&process.End, 0);
                        Vector4 result = Vector4.Lerp(start, end, t);
                        UnsafeUtility.WriteArrayElement(process.Target, 0, result);
                    }
                    else if (TypeLerp == LerpType.Quat)
                    {
                        Quaternion start = UnsafeUtility.ReadArrayElement<Quaternion>(&process.Start, 0);
                        Quaternion end = UnsafeUtility.ReadArrayElement<Quaternion>(&process.End, 0);
                        Quaternion result = Quaternion.Lerp(start, end, t);
                        UnsafeUtility.WriteArrayElement(process.Target, 0, result);
                    }

                    process.ElapsedTime += DeltaTime;
                }
                else
                {
                    // Direct copy for completion
                    UnsafeUtility.CopyStructureToPtr(ref process.End, process.Target);
                    process.SetIsDone(true);
                }

                Processes[i] = process;
            }
        }
#endif

        public static void CompactCreatedProcessArray()
        {
            if (_createdProcessCount < 64) return;
            if (_createdProcessCount > _createdProcesses.Length / 3) return;
            ResizeCreatedProcessesArray(_createdProcesses.Length / 2);
        }

        public static void CompactRunningProcessArray()
        {
            if (_runningProcessCount < 64) return;
            if (_runningProcessCount > _runningProcesses.Length / 3) return;
            ResizeRunningProcessesArray(_runningProcesses.Length / 2);
        }

        private static void ResizeCreatedProcessesArray(int newSize)
        {
            ResizeProcessesArray(newSize, ref _createdProcesses, _createdProcessCount);
        }

        private static void ResizeRunningProcessesArray(int newSize)
        {
            ResizeProcessesArray(newSize, ref _runningProcesses, _runningProcessCount);
        }

        private static void ResizeProcessesArray(int newSize, ref NativeArray<Lerp<T>> array, int elementsToCopy)
        {
            var newArray = new NativeArray<Lerp<T>>(newSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            NativeArray<Lerp<T>>.Copy(array, newArray, elementsToCopy);
            array.Dispose();
            array = newArray;
        }

        public static T ApplyLerp(T a, T b, float t) => _lerp(a, b, t);
        
        private static unsafe class LerpFunc
        {
            // TODO: Add more types here if needed
            public static delegate*<float, float, float, float> Float = &Mathf.Lerp;
            public static delegate*<Color, Color, float, Color> Colour = &Color.Lerp;
            public static delegate*<Vector2, Vector2, float, Vector2> Vec2 = &Vector2.Lerp;
            public static delegate*<Vector3, Vector3, float, Vector3> Vec3 = &Vector3.Lerp;
            public static delegate*<Vector4, Vector4, float, Vector4> Vec4 = &Vector4.Lerp;
            public static delegate*<Quaternion, Quaternion, float, Quaternion> Quat = &Quaternion.Lerp;
        }

        public static void Cleanup()
        {
            // Release NativeArray memory
            if (_runningProcesses.IsCreated) _runningProcesses.Dispose(); 
            if (_createdProcesses.IsCreated) _createdProcesses.Dispose();
        }
    }

    public static class Ease
    {
#if ENABLE_BURST
        [BurstCompile]
#endif
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
