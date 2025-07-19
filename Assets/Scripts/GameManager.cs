using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private float _value;
    private Vector3 _test;

    private void Start()
    {
        var floatProcess = new LerpProcess<float>(x => _value = x, 0, 10, 10, Mathf.Lerp, (t) => t);
        floatProcess.Start();
        LerpProcess<float>.Add(ref floatProcess);

        var vector3Process = new LerpProcess<Vector3>(x => _test = x, Vector3.zero, Vector3.one, 10, Vector3.Lerp, (t) => t);
        vector3Process.Start();
        LerpProcess<Vector3>.Add(ref vector3Process);
    }

    private void Update()
    {
        LerpProcess<float>.UpdateAll();
        LerpProcess<Vector3>.UpdateAll();
        Debug.Log(_value);
        Debug.Log(_test);
    }
}

public struct LerpProcess<T>
{
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

    public bool IsRunning => _isRunning;
    public bool IsDone => _isDone;

    public LerpProcess(Action<T> setter, T start, T end, float duration, Func<T, T, float, T> lerp, Func<float, float> easing, Action onComplete = null)
    {
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

    public void Start()
    {
        _isRunning = true;
    }

    public void Stop()
    {
        _isRunning = false;
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

    private static LerpProcess<T>[] _processes = new LerpProcess<T>[64];
    private static int _processCount;

    public static void Add(ref LerpProcess<T> process)
    {
        if (_processCount >= _processes.Length)
            Array.Resize(ref _processes, _processes.Length * 2);
        _processes[_processCount++] = process;
    }

    public static void UpdateAll()
    {
        int index = 0;
        while (index < _processCount)
        {
            ref LerpProcess<T> proc = ref _processes[index];
            proc.Update();

            if (proc.IsDone)
            {
                // Swap with last element and reduce count
                _processes[index] = _processes[_processCount - 1];
                _processCount--;
            }
            else index++;
        }
    }
}
