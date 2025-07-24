using System;
using System.Collections.Generic;
using Tickle.Engine;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _testTransform;

    private void Start()
    {
        _testTransform.LerpScale(1, 3, 10).Start();
    }
}

public class Tickle<T> where T : unmanaged
{
    private int _lerpId;
    private bool _isDone;
    private Action _onComplete;

    // This pair of properties come together. Their use is optional.
    // TODO: Put these in a struct for clarity
    private T _target;
    private Action<T> _setter;

    public Tickle(ref T target, T start, T end, float duration, Ease.Type ease, Action oncomplete)
    {
        if (TickleRunner.Instance == null) SetupRunner();
        _lerpId = LerpManager<T>.Create(ref target, ref _isDone, start, end, duration, ease);
        _onComplete = oncomplete;
    }

    public Tickle(Action<T> setter, T start, T end, float duration, Ease.Type ease, Action onComplete)
    {
        if (TickleRunner.Instance == null) SetupRunner();
        _setter = setter;
        _lerpId = LerpManager<T>.Create(ref _target, ref _isDone, start, end, duration, ease);
        _onComplete = onComplete;
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
            tickle._setter?.Invoke(tickle._target);
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
    public static Tickle<float> Lerp(this ref float floatRef, float start, float end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
        => new Tickle<float>(ref floatRef, start, end, duration, ease, onComplete);

    public static Tickle<Vector3> LerpScale(this Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
        => new Tickle<Vector3>(x => transform.localScale = x, start, end, duration, ease, onComplete);

    public static Tickle<Vector3> LerpScale(this Transform transform, float start, float end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
        => new Tickle<Vector3>(x => transform.localScale = x, Vector3.one * start, Vector3.one * end, duration, ease, onComplete);

    public static Tickle<Vector3> LerpPosition(this Transform transform, Vector3 start, Vector3 end, float duration, Ease.Type ease = Ease.Type.None, Action onComplete = null)
        => new Tickle<Vector3>(x => transform.position = x, start, end, duration, ease, onComplete);
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