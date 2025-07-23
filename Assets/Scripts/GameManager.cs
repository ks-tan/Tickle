using System;
using System.Collections;
using System.Collections.Generic;
using Tickle.Engine;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    private float[] _values = new float[10000];

    private void Start()
    {
        TickleTest();
    }

    private void Update()
    {
        Debug.Log(_values[100]);
        Debug.Log(Time.deltaTime + "seconds, " + (1 / Time.deltaTime) + " fps");
    }

    private void TickleTest()
    {
        for (int i = 0; i < 10000; i++)
            _values[i].Lerp(0, 10, 10).Start();
    }

    // 200+ fps
    private void LerpTest()
    {
        for (int i = 0; i < 10000; i++)
        {
            _values[i] = 0;
            LerpManager<float>.Start(ref _values[i], start: 0, end: 10, duration: 10);
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
    private Action _onComplete;

    public Tickle(ref T target, T start, T end, float duration, Ease.Type ease, Action oncomplete)
    {
        if (_runner == null) SetupRunner();
        _lerpId = LerpManager<T>.Create(ref target, start, end, duration, ease);
        _onComplete = oncomplete;
    }

    public void Start()
    {
        LerpManager<T>.Start(_lerpId);
        if (!_tickles.Contains(this))
            _tickles.Add(this);
    }

    public bool IsDone()
    {
        // TODO: The source of the bottleneck. If only we can hold a reference/
        // pointer to the Lerp<T> running process so we can directly check for IsDone
        return false;
    }

    private static TickleRunner _runner;
    private static List<Tickle<T>> _tickles = new List<Tickle<T>>();

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
        foreach(var tickle in _tickles)
        {
            if (!tickle.IsDone()) continue;
            tickle._onComplete?.Invoke();
        }
    }

    //~Tickle() => LerpManager<T>.CancelAllForTarget(UnsafeUtility.AddressOf(ref _target));
}

public static class Tickler
{
    public static Tickle<float> Lerp(this ref float Float, float start, float end, float duration)
    => new Tickle<float>(ref Float, start, end, duration, Ease.Type.None, null);
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