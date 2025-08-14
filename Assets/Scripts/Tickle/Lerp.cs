using System.Runtime.CompilerServices;
using Tickle.Easings;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Collections;
using Tickle.Collections;

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

        private void OnApplicationQuit()
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
        public Ease EaseType;
        public float ElapsedTime;
        public bool IsRunning;
        public bool IsDone;
        private bool* _doneHandle;

        public Lerp(int id, ref T target, T start, T end, float duration, Ease ease = Ease.None)
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
        public bool Update()
        {
            if (!IsRunning) return IsDone;
            if (IsDone) return IsDone;
            if (ElapsedTime <= Duration)
            {
                var value = LerpManager<T>.ApplyLerp(Start, End, EaseFunctions.Apply(ElapsedTime / Duration, EaseType));
                *Target = value;
                ElapsedTime += Time.deltaTime;
            }
            if (ElapsedTime > Duration)
                SetIsDone(true);
            return IsDone;
        }
    }

    public static unsafe class LerpManager<T> where T : unmanaged
    {
        private static bool _hasSetup = false;
        private static delegate*<T, T, float, T> _lerp;

#if ENABLE_BURST
        public enum LerpType { Float, Color, Vec2, Vec3, Vec4, Quat }
        private static LerpType _lerpType;
#endif
        
        // Note: _createdProcesses and _runningProcesses do not hold exact references
        // of the same Lerp<T> data, but copies. When a process is only created but
        // not running, we only check it from the _createdProcesses array. If a
        // process is running, we only check it from the _runningProcesses array.
        private static SparseSet<Lerp<T>> _createdProcesses;
        private static SparseSet<Lerp<T>> _runningProcesses;
        private static NativeArray<bool> _hasDoneProcesses;

        private static void Setup()
        {
            if (LerpRunner.Instance == null)
                new GameObject("[LerpRunner]").AddComponent<LerpRunner>();

            // TODO: Add more types here if needed
            if (typeof(T) == typeof(float)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Float;
            else if (typeof(T) == typeof(Color)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Colour;
            else if (typeof(T) == typeof(Vector2)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Vec2;
            else if (typeof(T) == typeof(Vector3)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Vec3;
            else if (typeof(T) == typeof(Vector4)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Vec4;
            else if (typeof(T) == typeof(Quaternion)) _lerp = (delegate*<T, T, float, T>)LerpFunc.Quat;

#if ENABLE_BURST
            if (typeof(T) == typeof(float)) _lerpType = LerpType.Float;
            else if (typeof(T) == typeof(Color)) _lerpType = LerpType.Color;
            else if (typeof(T) == typeof(Vector2)) _lerpType = LerpType.Vec2;
            else if (typeof(T) == typeof(Vector3)) _lerpType = LerpType.Vec3;
            else if (typeof(T) == typeof(Vector4)) _lerpType = LerpType.Vec4;
            else if (typeof(T) == typeof(Quaternion)) _lerpType = LerpType.Quat;
#endif
            _runningProcesses = new SparseSet<Lerp<T>>(64);
            _createdProcesses = new SparseSet<Lerp<T>>(64);
            _hasDoneProcesses = new NativeArray<bool>(1, Allocator.Persistent);

            _hasSetup = true;
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
            return _runningProcesses.TryGet(id, ref processRef);
        }

        public static bool TryGetCreatedProcess(int id, ref Lerp<T> processRef)
        {
            return _createdProcesses.TryGet(id,ref processRef);
        }

        public static int Create(ref T target, ref bool doneHandle, T start, T end, float duration, Ease ease = Ease.None)
        {
            if (!_hasSetup) Setup();
            var process = new Lerp<T>(_createdProcesses.GetFreeKey(), ref target, start, end, duration, ease);
            process.BindDoneHandle(ref doneHandle);
            _createdProcesses.Add(process);
            return process.Id;
        }

        public static int Create(ref T target, T start, T end, float duration, Ease ease = Ease.None)
        {
            bool dummy = false;
            return Create(ref target, ref dummy, start, end, duration, ease);
        }

        public static int Start(ref T target, T start, T end, float duration, Ease ease = Ease.None)
        {
            var pid = Create(ref target, start, end, duration, ease);
            Start(pid);
            return pid;
        }

        public static void Start(int id)
        {
            // TODO: We should remove the use of TryGetRunningProcess and the use of ref keyword.
            // Instead, put a list of processIds to be started/resumed/etc in their respective list
            // Then we update each lerp's internal state together during the update loop.
            // There is more complexity, but the flow of data will be cleaner and more debuggable
            // than throwing refs around.
            Lerp<T> process = default;
            var isRunningProcessFound = TryGetRunningProcess(id, ref process);
            if (!isRunningProcessFound)
            {
                var isCreatedProcessFound = TryGetCreatedProcess(id, ref process);
                if (!isCreatedProcessFound) return;
                *process.Target = process.Start;
                process.IsRunning = true;
                process.SetIsDone(false);
                process.ElapsedTime = 0;
                _runningProcesses.Add(process, process.Id);
            }
            else
            {
                *process.Target = process.Start;
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
            // This is an API method for manually stopping a lerp, i.e., not to
            // be called at the natural end of a lerp. We choose the nuclear
            // option, that is to immediately remove this lerp from running processes

            Lerp<T> process = default;
            if (!TryGetRunningProcess(id, ref process)) return;
            process.SetIsDone(true);
            _runningProcesses.Remove(id);
        }

        public static void Destroy(int id)
        {
            Stop(id);
            _createdProcesses.Remove(id);
        }

        public static void UpdateAll()
        {
            if (!_hasSetup) return;
            Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(_runningProcesses.GetDenseData());
            for (int i = 0; i < _runningProcesses.GetDataCount(); i++)
                _hasDoneProcesses[0] = ptr[i].Update();
        }

#if ENABLE_BURST

        [BurstCompile]
        public static void BurstUpdateAll()
        {
            var job = new LerpUpdateParallelJob() {
                DeltaTime = Time.deltaTime,
                Processes = _runningProcesses.GetDenseData(),
                TypeLerp = _lerpType,
                HasDoneProcesses = _hasDoneProcesses,
            };
            JobHandle handle = job.Schedule(_runningProcesses.GetDataCount(), _runningProcesses.GetDataCount());
            handle.Complete();
        }

        [BurstCompile]
        public struct LerpUpdateParallelJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Lerp<T>> Processes;
            public NativeArray<bool> HasDoneProcesses;
            public float DeltaTime;
            public LerpType TypeLerp;

            public void Execute(int i)
            {
                Lerp<T> process = Processes[i];
                if (!process.IsRunning || process.IsDone) return;

                if (process.ElapsedTime <= process.Duration)
                {
                    float t = process.ElapsedTime / process.Duration;
                    t = EaseFunctions.Apply(t, process.EaseType);

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
                if (process.ElapsedTime > process.Duration)
                {
                    process.SetIsDone(true);
                    HasDoneProcesses[0] = true;
                }

                Processes[i] = process;
            }
        }
#endif

        public static void CompactCreatedProcessArray()
        {
            if (!_hasSetup) return;
            _createdProcesses.Resize();
        }

        public static void CompactRunningProcessArray()
        {
            if (!_hasSetup) return;

            // TODO: Instead of just getting a true/false value, it would be
            // better to know the exact number of done processes, so we do not
            // have to loop through the whole list all the time.
            if (_hasDoneProcesses[0])
            {
                Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(_runningProcesses.GetDenseData());
                int index = 0;
                while (index < _runningProcesses.GetDataCount())
                {
                    if (!ptr[index].IsDone)
                    {
                        index++;
                        continue;
                    }
                    _runningProcesses.Remove(index);
                }
                _hasDoneProcesses[0] = false;
            }

            _runningProcesses.Resize();
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
            if (_hasSetup)
            {
                _runningProcesses.Dispose();
                _createdProcesses.Dispose();
                _hasDoneProcesses.Dispose();
            }
            _runningProcesses = default;
            _createdProcesses = default;
            _hasDoneProcesses = default;
        }
    }
}
