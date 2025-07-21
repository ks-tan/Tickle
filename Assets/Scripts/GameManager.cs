using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    private float[] _values = new float[10000];

    private void Start()
    {
        LerpTest();
    }

    private void Update()
    {
        Debug.Log(_values[100]);
        Debug.Log(Time.deltaTime + "seconds, " + (1 / Time.deltaTime) + " fps");
    }

    // 150 fps
    private void LerpTest()
    {
        for (int i = 0; i < 10000; i++)
        {
            _values[i] = 0;
            Lerp<float>.Start(this, ref _values[i], start: 0, end: 10, duration: 10);
        }
    }

    // 60 fps
    private void CoroutineTest()
    {
        for (int i = 0; i < 10000; i++)
            StartCoroutine(routine(i, 0, 10, 10));

        IEnumerator routine(int index, float start, float end, float duration)
        {
            var elapsedTime = 0f;
            while (elapsedTime < duration) 
            {
                _values[index] = Mathf.Lerp(start, end, elapsedTime/duration);
                yield return null;
                elapsedTime += Time.deltaTime;
            }
            _values[index] = end;
        }
    }
}

public unsafe class Tickle<T> where T : unmanaged
{
    private int _lerpId;
    private T _value;
    private Action<T> _setter;
    private Action _onComplete;

    public Tickle(Action<T> setter, T start, T end, float duration, delegate*<float, float> ease, Action oncomplete)
    {
        var process = new Lerp<T>(this, ref _value, start, end, duration, ease);
        _lerpId = process.Id;
        _value = start;
        _setter = setter;
        _onComplete = oncomplete;
    }

    public void Start()
    {
        // TODO: We need to include Lerp<T> static function to get "created" processes
        // and remove from "created" processes when Tickle instance is finalized.

        //if (_runner == null) 
        //    SetupRunner();
        //Lerp<T>.Start(_lerpId);
        //if (!_tickles.Contains(this))
        //    _tickles.Add(this);
    }

    public bool IsDone()
    {
        Lerp<T> process = default;
        if (!Lerp<T>.TryGetProcess(_lerpId, ref process)) 
            return true;
        return process.IsDone;
    }

    private static TickleRunner _runner;
    private static List<Tickle<T>> _tickles;

    private static void SetupRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("[TickleRunner]");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<TickleRunner>();
    }

    public static void UpdateAll()
    {
        for(int i = _tickles.Count - 1; i >= 0; i--)
        {
            var tickle = _tickles[i];
            tickle._setter(tickle._value);
            if (!tickle.IsDone()) continue;
            tickle._onComplete?.Invoke();   
            _tickles.Remove(tickle);
        }
    }
}

public class TickleRunner : MonoBehaviour
{
    private void Update()
    {
        // TODO: Add more types here if needed
        Tickle<float>.UpdateAll();
        Tickle<Color>.UpdateAll();
        Tickle<Vector2>.UpdateAll();
        Tickle<Vector3>.UpdateAll();
        Tickle<Vector4>.UpdateAll();
        Tickle<Quaternion>.UpdateAll();
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
    // Note: Target owner should be a reference type for there to be no boxing
    private readonly object _targetOwner;
    private readonly int _id;
    private readonly T* _target;
    private readonly T _start;
    private readonly T _end;
    private readonly float _duration;
    private readonly delegate*<T, T, float, T> _lerp;
    private readonly delegate*<float, float> _ease;

    private float _elapsedTime;
    private bool _isRunning;
    private bool _isDone;

    public int Id => _id;
    public bool IsDone => _isDone;

    public Lerp(object targetOwner, ref T target, T start, T end, float duration, delegate*<float, float> ease = null)
    {
        _targetOwner = targetOwner;
        _id = _rollingId++;
        _target = (T*)UnsafeUtility.AddressOf(ref target);
        _start = start;
        _end = end;
        _elapsedTime = 0;
        _duration = duration;
        _ease = ease == null ? Ease.None : ease;
        _isRunning = false;
        _isDone = false;

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
        if (_targetOwner == null || _target == null)
        {
            _isDone = true;
            return;
        }
        if (_elapsedTime < _duration)
        {
            var value = _lerp(_start, _end, _ease(_elapsedTime/_duration));
            *_target = value;
            _elapsedTime += Time.deltaTime;
            return;
        }
        *_target = _end;
        _isDone = true;
    }

    private static LerpRunner _runner;
    private static int _rollingId;
    private static int _processCount;

    // TODO: We can use NativeArrays instead to access elements to make things
    // burst/jobs compatible. However, we will have to make Lerp<T> struct to be
    // a unmanaged type, which means all its members have to be value types only.
    // Currently, 'object _targetOwner' is the only member that is a reference
    // type and hence stopping Lerp<T> from being unmanaged. We can implement an
    // object tracker that assigns an ID to each object in a map (or some other
    // similar solutions).
    private static Lerp<T>[] _runningProcesses = new Lerp<T>[64];

    private static void SetupRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("[LerpRunner]");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<LerpRunner>();
    }

    // TODO: Need a faster way or this could lead to O(n^2)
    public static bool TryGetProcess(int id, ref Lerp<T> processRef)
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

    public static int Start(object targetOwner, ref T target, T start, T end, float duration, delegate*<float, float> ease = null)
    {
        var process = new Lerp<T>(targetOwner, ref target, start, end, duration, ease);
        Start(ref process);
        return process.Id;
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
        for(int i = 0; i < _processCount; i++)
            _runningProcesses[i].Update();

        int index = 0;
        while (index < _processCount)
        {
            ref Lerp<T> proc = ref _runningProcesses[index];

            if (!proc._isDone)
            {
                index++;
                continue;
            }

            // Remove from list by swapping with last element and reduce count
            _runningProcesses[index] = _runningProcesses[_processCount - 1];
            _processCount--;
        }

        // Resize array if needed
        if (_processCount < _runningProcesses.Length / 3)
            Array.Resize(ref _runningProcesses, _runningProcesses.Length / 2);
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
