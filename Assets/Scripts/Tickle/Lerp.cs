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
            LerpManager<float>.CompactProcessArray();
            LerpManager<Color>.CompactProcessArray();
            LerpManager<Vector2>.CompactProcessArray();
            LerpManager<Vector3>.CompactProcessArray();
            LerpManager<Vector4>.CompactProcessArray();
            LerpManager<Quaternion>.CompactProcessArray();
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

        private enum LerpType { Float, Color, Vec2, Vec3, Vec4, Quat }
        private LerpType _lerpType;

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

            _lerpType = LerpType.Float;
            if (typeof(T) == typeof(Color)) _lerpType = LerpType.Color;
            else if (typeof(T) == typeof(Vector2)) _lerpType = LerpType.Vec2;
            else if (typeof(T) == typeof(Vector3)) _lerpType = LerpType.Vec3;
            else if (typeof(T) == typeof(Vector4)) _lerpType = LerpType.Vec4;
            else if (typeof(T) == typeof(Quaternion)) _lerpType = LerpType.Quat;
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
        public void Update(float deltaTime)
        {
            if (!IsRunning || IsDone) return;
            
            if (ElapsedTime <= Duration)
            {
                float t = ElapsedTime / Duration;
                t = EaseFunctions.Apply(t, EaseType);

                // Directly write to target memory using the correct type
                if (_lerpType == LerpType.Float)
                {
                    float start = UnsafeUtility.As<T, float>(ref Start);
                    float end = UnsafeUtility.As<T, float>(ref End);
                    float result = math.lerp(start, end, t);
                    UnsafeUtility.WriteArrayElement(Target, 0, result);
                }
                else if (_lerpType == LerpType.Color)
                {
                    Color start = UnsafeUtility.As<T, Color>(ref Start);
                    Color end = UnsafeUtility.As<T, Color>(ref End);
                    Color result = Color.Lerp(start, end, t);
                    UnsafeUtility.WriteArrayElement(Target, 0, result);
                }
                else if (_lerpType == LerpType.Vec2)
                {
                    Vector2 start = UnsafeUtility.As<T, Vector2>(ref Start);
                    Vector2 end = UnsafeUtility.As<T, Vector2>(ref End);
                    Vector2 result = Vector2.Lerp(start, end, t);
                    UnsafeUtility.WriteArrayElement(Target, 0, result);
                }
                else if (_lerpType == LerpType.Vec3)
                {
                    Vector3 start = UnsafeUtility.As<T, Vector3>(ref Start);
                    Vector3 end = UnsafeUtility.As<T, Vector3>(ref End);
                    Vector3 result = Vector3.Lerp(start, end, t);
                    UnsafeUtility.WriteArrayElement(Target, 0, result);
                }
                else if (_lerpType == LerpType.Vec4)
                {
                    Vector4 start = UnsafeUtility.As<T, Vector4>(ref Start);
                    Vector4 end = UnsafeUtility.As<T, Vector4>(ref End);
                    Vector4 result = Vector4.Lerp(start, end, t);
                    UnsafeUtility.WriteArrayElement(Target, 0, result);
                }
                else if (_lerpType == LerpType.Quat)
                {
                    Quaternion start = UnsafeUtility.As<T, Quaternion>(ref Start);
                    Quaternion end = UnsafeUtility.As<T, Quaternion>(ref End);
                    Quaternion result = Quaternion.Lerp(start, end, t);
                    UnsafeUtility.WriteArrayElement(Target, 0, result);
                }

                ElapsedTime += deltaTime;
            }
            if (ElapsedTime > Duration)
                SetIsDone(true);
        }
    }

    public static unsafe class LerpManager<T> where T : unmanaged
    {
        private static bool _hasSetup = false;
        private static SparseSet<Lerp<T>> _processes;

        private static void Setup()
        {
            if (LerpRunner.Instance == null)
                new GameObject("[LerpRunner]").AddComponent<LerpRunner>();

            _processes = new SparseSet<Lerp<T>>(64);
            _hasSetup = true;
        }

        public static int Create(ref T target, ref bool doneHandle, T start, T end, float duration, Ease ease = Ease.None)
        {
            if (!_hasSetup) Setup();
            var process = new Lerp<T>(_processes.GetFreeKey(), ref target, start, end, duration, ease);
            process.BindDoneHandle(ref doneHandle);
            _processes.Add(process);
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
            var isProcessFound = _processes.TryGet(id, out Lerp<T>* process);
            if (!isProcessFound) return;
            *process->Target = process->Start;
            process->IsRunning = true;
            process->SetIsDone(false);
            process->ElapsedTime = 0;
        }

        public static void Resume(int id)
        {
            var isFound = _processes.TryGet(id, out Lerp<T>* process);
            if (!isFound) return;
            process->IsRunning = true;
        }

        public static void Pause(int id)
        {
            var isFound = _processes.TryGet(id, out Lerp<T>* process);
            if (!isFound) return;
            process->IsRunning = false;
        }

        public static void Stop(int id)
        {
            if (!_processes.TryGet(id, out Lerp<T>* process)) return;
            process->SetIsDone(true);
            process->IsRunning = false;
        }

        public static void Destroy(int id)
        {
            Stop(id);
            _processes.Remove(id);
        }

        public static void UpdateAll()
        {
            if (!_hasSetup) return;
            Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(_processes.GetDenseData());
            for (int i = 0; i < _processes.GetDataCount(); i++)
                ptr[i].Update(Time.deltaTime);
        }

#if ENABLE_BURST
        [BurstCompile]
        public static void BurstUpdateAll()
        {
            var job = new LerpUpdateParallelJob();
            job.DeltaTime = Time.deltaTime;
            job.Processes = _processes.GetDenseData();
            job.Schedule(_processes.GetDataCount(), _processes.GetDataCount()).Complete();
        }

        [BurstCompile]
        public struct LerpUpdateParallelJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Lerp<T>> Processes;
            public float DeltaTime;

            public void Execute(int i)
            {
                Lerp<T>* ptr = (Lerp<T>*)NativeArrayUnsafeUtility.GetUnsafePtr(Processes);
                ptr[i].Update(DeltaTime);
            }
        }
#endif

        public static void CompactProcessArray()
        {
            if (!_hasSetup) return;
            _processes.Resize();
        }

        public static void Cleanup()
        {
            if (!_hasSetup) return;
            _processes.Dispose();
            _processes = default;
        }
    }
}
