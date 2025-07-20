using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    private float _value;
    private Vector3 _test;

    private void Start()
    {
        var floatProcess = new Lerp<float>(ref _value, 0, 10, 10);
        Lerp<float>.Start(ref floatProcess);

        var vector3Process = new Lerp<Vector3>(ref _test, Vector3.zero, Vector3.one, 10);
        Lerp<Vector3>.Start(ref vector3Process);
    }

    private void Update()
    {
        Debug.Log(_value);
        Debug.Log(_test);
    }
}

public class LerpRunner : MonoBehaviour
{
    private void Update()
    {
        // TODO: Add more types here if needed
        Lerp<float>.UpdateAll();
        Lerp<Color>.UpdateAll();
        Lerp<Vector2>.UpdateAll();
        Lerp<Vector3>.UpdateAll();
        Lerp<Vector4>.UpdateAll();
        Lerp<Quaternion>.UpdateAll();
    }
}

public unsafe struct Lerp<T> where T : unmanaged
{
    private int _id;
    private T* _target;
    private T _start;
    private T _end;
    private float _elapsedTime;
    private float _duration;
    private delegate*<T, T, float, T> _lerp;
    private delegate*<float, float> _ease;
    private Action _onComplete;
    private bool _isRunning;
    private bool _isDone;

    public int Id => _id;

    public Lerp(ref T target, T start, T end, float duration, delegate*<float, float> ease = null, Action onComplete = null)
    {
        _id = _rollingId++;
        _target = (T*)UnsafeUtility.AddressOf(ref target);
        _start = start;
        _end = end;
        _elapsedTime = 0;
        _duration = duration;
        _ease = ease == null ? Ease.None : ease;
        _onComplete = onComplete;
        _isRunning = false;
        _isDone = false;

        _lerp = (delegate*<T, T, float, T>)LerpType.Float;
        if (typeof(T) == typeof(Color)) _lerp = (delegate*<T, T, float, T>)LerpType.Colour;
        if (typeof(T) == typeof(Vector2)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec2;
        if (typeof(T) == typeof(Vector3)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec3;
        if (typeof(T) == typeof(Vector4)) _lerp = (delegate*<T, T, float, T>)LerpType.Vec4;
        if (typeof(T) == typeof(Quaternion)) _lerp = (delegate*<T, T, float, T>)LerpType.Quat;
    }

    public void Update()
    {
        if (!_isRunning) return;
        if (_isDone) return;
        if (_elapsedTime < _duration)
        {
            var value = _lerp(_start, _end, _ease(_elapsedTime/_duration));
            *_target = value;
            _elapsedTime += Time.deltaTime;
            return;
        }
        *_target = _end;
        _onComplete?.Invoke();
        _isDone = true;
    }

    private static LerpRunner _runner;
    private static int _rollingId;
    private static int _processCount;
    private static Lerp<T>[] _runningProcesses = new Lerp<T>[64];

    private static void SetupRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("[CoroutineUtilityRunner]");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<LerpRunner>();
    }

    private static bool TryGetProcess(int id, ref Lerp<T> processRef)
    {
        for (int i = 0; i < _processCount; i++)
        {
            if (_runningProcesses[i]._id == id)
            {
                processRef = ref _runningProcesses[i];
                return true;
            }
        }
        return false;
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
        var isFound = TryGetProcess(process.Id, ref dummy);
        if (!isFound)
        {
            if (_processCount >= _runningProcesses.Length)
                Array.Resize(ref _runningProcesses, _runningProcesses.Length * 2);
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
        if (process._isRunning) return;
        process._isRunning = true;
        process._isDone = false;
        process._elapsedTime = 0;
    }

    public static void Stop(ref Lerp<T> process)
    {
        RemoveProcess(process.Id);
    }

    public static void Stop(int id)
    {
        RemoveProcess(id);
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

    public static void UpdateAll()
    {
        int index = 0;
        while (index < _processCount)
        {
            ref Lerp<T> proc = ref _runningProcesses[index];
            proc.Update();
            if (proc._isDone)
                RemoveProcess(index);
            else index++;
        }
    }

    private static void RemoveProcess(int id)
    {
        int index = 0;

        while (index < _processCount)
        {
            ref Lerp<T> proc = ref _runningProcesses[index];
            if (proc.Id != id) continue;

            // Remove from list by swapping with last element and reduce count
            _runningProcesses[index] = _runningProcesses[_processCount - 1];
            _processCount--;

            // Resize array if needed
            if (_processCount < _runningProcesses.Length / 3)
                Array.Resize(ref _runningProcesses, _runningProcesses.Length / 2);

            break;
        }
    }

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
}

public unsafe static class Ease
{
    // TODO: Add more easing functions
    private static float Default(float t) => t;
    public static delegate*<float, float> None = &Default;
}
