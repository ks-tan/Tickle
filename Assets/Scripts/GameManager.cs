using System;
using System.Collections;
using System.Collections.Generic;
using Tickle.Collections;
using Tickle.Engine;
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

    private void LinkedNodeTest()
    {
        LinkedNode<int> node1 = new LinkedNode<int>(1);
        LinkedNode<int> node2 = new LinkedNode<int>(2);
        LinkedNode<int> node3 = new LinkedNode<int>(3);

        LinkedNode<int>.AddAfter(&node1, &node2);
        LinkedNode<int>.AddAfter(&node2, &node3);
        LinkedNode<int>.Remove(&node3);

        LinkedNode<int>* currentNode = &node1;
        while (currentNode != null)
        {
            Debug.Log(currentNode->Value);
            currentNode = currentNode->Next;
        }
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