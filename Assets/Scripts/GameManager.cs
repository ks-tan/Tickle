using Tickle;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    private float[] _values = new float[64000];

    private void Start()
    {
        for(int i = 0; i < _values.Length; i++)
        {
            _values[i] = 0;
            _values[i].Lerp(0, 10, 10).Start();
        }
    }

    private void Update()
    {
    }
}