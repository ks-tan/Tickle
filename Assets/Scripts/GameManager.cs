using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private float _value;
    private Vector3 _test;

    private void Start()
    {
        var floatProcess = new LerpProcess<float>(x => _value = x, 0, 10, 10, Mathf.Lerp, (t) => t);
        LerpProcess<float>.Start(ref floatProcess);

        var vector3Process = new LerpProcess<Vector3>(x => _test = x, Vector3.zero, Vector3.one, 10, Vector3.Lerp, (t) => t);
        LerpProcess<Vector3>.Start(ref vector3Process);
    }

    private void Update()
    {
        Debug.Log(_value);
        Debug.Log(_test);
    }
}

public class LerpProcessRunner : MonoBehaviour
{
    private void Update()
    {
        // TODO: Add more types here if needed
        LerpProcess<float>.UpdateAll();
        LerpProcess<Color>.UpdateAll();
        LerpProcess<Vector2>.UpdateAll();
        LerpProcess<Vector3>.UpdateAll();
        LerpProcess<Vector4>.UpdateAll();
        LerpProcess<Quaternion>.UpdateAll();
    }
}

public struct LerpProcess<T>
{
    private int _id;
    private T _start;
    private T _end;
    private float _elapsedTime;
    private float _duration;
    private Func<T, T, float, T> _lerp;
    private Func<float, float> _easing;
    private Action<T> _setter;
    private Action _onComplete;
    private bool _isRunning;
    private bool _isDone;

    public int Id => _id;

    public LerpProcess(Action<T> setter, T start, T end, float duration, Func<T, T, float, T> lerp, Func<float, float> easing, Action onComplete = null)
    {
        _id = _rollingId++;
        _setter = setter;
        _start = start;
        _end = end;
        _elapsedTime = 0;
        _duration = duration;
        _lerp = lerp;
        _easing = easing;
        _onComplete = onComplete;
        _isRunning = false;
        _isDone = false;
    }

    public void Update()
    {
        if (!_isRunning) return;
        if (_isDone) return;
        if (_elapsedTime < _duration)
        {
            var value = _lerp(_start, _end, _easing(_elapsedTime/_duration));
            _setter(value);
            _elapsedTime += Time.deltaTime;
            return;
        }
        _setter(_end);
        _onComplete?.Invoke();
        _isDone = true;
    }

    private static LerpProcessRunner _runner;
    private static int _rollingId;
    private static int _processCount;
    private static LerpProcess<T>[] _processes = new LerpProcess<T>[64];

    private static void SetupRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("[CoroutineUtilityRunner]");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<LerpProcessRunner>();
    }

    private static bool TryGetProcess(int id, ref LerpProcess<T> processRef)
    {
        for (int i = 0; i < _processCount; i++)
        {
            if (_processes[i]._id == id)
            {
                processRef = ref _processes[i];
                return true;
            }
        }
        return false;
    }

    public static void Start(ref LerpProcess<T> process)
    {
        if (_runner == null) 
            SetupRunner();

        if (process._isRunning) return;
        process._isRunning = true;
        process._isDone = false;
        process._elapsedTime = 0;

        LerpProcess<T> dummy = default;
        var isFound = TryGetProcess(process.Id, ref dummy);
        if (!isFound)
        {
            if (_processCount >= _processes.Length)
                Array.Resize(ref _processes, _processes.Length * 2);
            _processes[_processCount++] = process;
        }
    }

    public static void Start(int id)
    {
        if (_runner == null) 
            SetupRunner();

        LerpProcess<T> process = default;
        var isFound = TryGetProcess(id, ref process);
        if (!isFound) return;
        if (process._isRunning) return;
        process._isRunning = true;
        process._isDone = false;
        process._elapsedTime = 0;
    }

    public static void Stop(ref LerpProcess<T> process)
    {
        Stop(process.Id);
    }

    public static void Stop(int id)
    {
        LerpProcess<T> process = default;
        var isFound = TryGetProcess(id, ref process);
        if (!isFound) return;
        // Remove from list by swapping with last element and reduce count
        _processes[process.Id] = _processes[_processCount - 1];
        _processCount--;
    }

    public static void Pause(ref LerpProcess<T> process)
    {
        process._isRunning = false;
    }

    public static void Pause(int id)
    {
        LerpProcess<T> process = default;
        var isFound = TryGetProcess(id, ref process);
        if (!isFound) return;
        process._isRunning = false;
    }

    public static void UpdateAll()
    {
        int index = 0;
        while (index < _processCount)
        {
            ref LerpProcess<T> proc = ref _processes[index];
            proc.Update();

            if (proc._isDone)
            {
                // Remove from list by swapping with last element and reduce count
                _processes[index] = _processes[_processCount - 1];
                _processCount--;
            }
            else index++;
        }
    }
}
