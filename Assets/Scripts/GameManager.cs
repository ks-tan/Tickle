using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Update()
    {
        Ticker.Update();
    }
}

public static class Ticker
{
    static LerpProcess<float> _floatProcess = new LerpProcess<float>(0, 10, 10, Mathf.Lerp, (t) => t);

    public static void Update()
    {
        _floatProcess.Update();
        Debug.Log(_floatProcess.Value);
    }
}


public struct Process {

    private float _elapsedTime;
    private float _duration;
    private Action _onComplete;
    public bool IsRunning { get; private set; }
    public bool IsDone() => _elapsedTime >= _duration;
    public float Progress => _elapsedTime / _duration;

    public Process(float duration, Action onComplete = null)
    {
        IsRunning = false;
        _elapsedTime = 0;
        _duration = duration;
        _onComplete = onComplete;
    }

    public void Start()
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Update()
    {
        if (IsRunning) return;
        if (IsDone()) return;
        if (_elapsedTime < _duration)
            _elapsedTime += Time.deltaTime;
        _onComplete?.Invoke();
    }
}

public struct LerpProcess<T>
{
    public T Value { get; private set; }
    private T _start;
    private T _end;
    private Func<T, T, float, T> _lerp;
    private Func<float, float> _easing;
    private Process _process;

    public bool IsDone() => _process.IsDone();
    public void Start() => _process.Start();
    public void Stop() => _process.Stop();

    public LerpProcess(T start, T end, float duration, Func<T, T, float, T> lerp, Func<float, float> easing, Action onComplete = null)
    {
        _start = start;
        _end = end;
        _lerp = lerp;
        _easing = easing;
        _process = new Process(duration, onComplete);
        Value = start;
    }

    public void Update()
    {
        Value = _lerp(_start, _end, _easing(_process.Progress));
        _process.Update();
    }
}
