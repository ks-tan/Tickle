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
            _values[i].Lerp(0, 10, 10, Ease.Type.None, () => Debug.Log("ISDONE")).Start();
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
    private bool _isDone;
    private Action _onComplete;

    public Tickle(ref T target, T start, T end, float duration, Ease.Type ease, Action oncomplete)
    {
        if (TickleRunner.Instance == null) SetupRunner();
        _lerpId = LerpManager<T>.Create(ref target, ref _isDone, start, end, duration, ease);
        _onComplete = oncomplete;
    }

    public void Start()
    {
        LerpManager<T>.Start(_lerpId);
        if (!_tickles.Contains(this))
            _tickles.Add(this);
    }

    private static List<Tickle<T>> _toRemove = new List<Tickle<T>>();
    private static List<Tickle<T>> _tickles = new List<Tickle<T>>();

    private static void SetupRunner()
    {
        if (TickleRunner.Instance != null) return;
        var go = new GameObject("[TickleRunner]");
        go.AddComponent<TickleRunner>();
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    public static void UpdateAll()
    {
        foreach(var tickle in _tickles)
        {
            if (!tickle._isDone) continue;
            _toRemove.Add(tickle);
            tickle._onComplete?.Invoke();
        }

        foreach (var tickle in _toRemove)
            _tickles.Remove(tickle);
        _toRemove.Clear();
    }

    //~Tickle() => LerpManager<T>.CancelAllForTarget(UnsafeUtility.AddressOf(ref _target));
}

public static class Tickler
{
    public static Tickle<float> Lerp(this ref float Float, float start, float end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
        => new Tickle<float>(ref Float, start, end, duration, ease, onComplete);
}

public class TickleRunner : MonoBehaviour
{
    public static TickleRunner Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

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