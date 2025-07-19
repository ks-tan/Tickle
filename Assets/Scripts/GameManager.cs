using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private LerpProcess<float> _floatProcess;

    private void Start()
    {
        _floatProcess = new LerpProcess<float>(0, 10, 10, Mathf.Lerp, (t) => t);
    }

    private void Update()
    {
        if (!_floatProcess.IsRunning)
            _floatProcess.Start();
        _floatProcess.Update();
        Debug.Log(_floatProcess.Value);
    }
}

public struct Process 
{
    private float _elapsedTime;
    private float _duration;
    private Action _onComplete;
    private bool _isRunning;
    private bool _isDone;

    public Process(float duration, Action onComplete = null)
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
