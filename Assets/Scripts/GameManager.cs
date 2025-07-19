using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private LerpProcess<float> _floatProcess;
    private ProcessTicker _processTicker = new ProcessTicker();

    private void Start()
    {
        _floatProcess = new LerpProcess<float>(0, 10, 10, Mathf.Lerp, (t) => t);
        _floatProcess.Start();
        _processTicker.Add(ref _floatProcess);
    }

    private void Update()
    {
        _processTicker.Update();
        _floatProcess.Update();
    }
}

public enum ProcessType
{
    // TODO: Add more types here
    Simple,
    LerpFloat,
}

public sealed class ProcessTicker
{
    // Separate arrays for each process type
    private SimpleProcess[] _simpleProcesses = new SimpleProcess[64];
    private LerpProcess<float>[] _floatProcesses = new LerpProcess<float>[64];
    // Add arrays for other process types

    private int _simpleCount;
    private int _floatCount;
    // Add counts for other types

    public void Add(ref SimpleProcess process)
    {
        if (_simpleCount >= _simpleProcesses.Length)
            Array.Resize(ref _simpleProcesses, _simpleProcesses.Length * 2);

        _simpleProcesses[_simpleCount++] = process;
    }

    public void Add(ref LerpProcess<float> process)
    {
        if (_floatCount >= _floatProcesses.Length)
            Array.Resize(ref _floatProcesses, _floatProcesses.Length * 2);

        _floatProcesses[_floatCount++] = process;
    }

    public void Update()
    {
        UpdateSimpleProcesses();
        UpdateFloatProcesses();
        // Update other process types
    }

    private void UpdateSimpleProcesses()
    {
        int index = 0;
        while (index < _simpleCount)
        {
            ref SimpleProcess proc = ref _simpleProcesses[index];
            proc.Update();

            if (proc.IsDone)
            {
                // Swap with last element and reduce count
                _simpleProcesses[index] = _simpleProcesses[_simpleCount - 1];
                _simpleCount--;
            }
            else index++;
        }
    }

    private void UpdateFloatProcesses()
    {
        int index = 0;
        while (index < _floatCount)
        {
            ref LerpProcess<float> proc = ref _floatProcesses[index];
            proc.Update();

            if (proc.IsDone)
            {
                // Swap with last element and reduce count
                _floatProcesses[index] = _floatProcesses[_floatCount - 1];
                _floatCount--;
            }
            else index++;
        }
    }

    // Add similar update methods for other process types
}

public struct SimpleProcess 
{
    private float _elapsedTime;
    private float _duration;
    private Action _onComplete;
    private bool _isRunning;
    private bool _isDone;
    public bool IsDone => _isDone;

    public SimpleProcess(float duration, Action onComplete = null)
    {
        _elapsedTime = 0;
        _duration = duration;
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
            _elapsedTime += Time.deltaTime;
        _onComplete?.Invoke();
        _isDone = true;
    }
}

public struct LerpProcess<T>
{
    private T _value;
    private T _start;
    private T _end;
    private float _elapsedTime;
    private float _duration;
    private Func<T, T, float, T> _lerp;
    private Func<float, float> _easing;
    private Action _onComplete;
    private bool _isRunning;
    private bool _isDone;

    public T Value => _value;
    public bool IsRunning => _isRunning;
    public bool IsDone => _isDone;

    public LerpProcess(T start, T end, float duration, Func<T, T, float, T> lerp, Func<float, float> easing, Action onComplete = null)
    {
        _value = start;
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
            _value = _lerp(_start, _end, _easing(_elapsedTime/_duration));
            _elapsedTime += Time.deltaTime;
            return;
        }
        _onComplete?.Invoke();
        _isDone = true;
    }
}
