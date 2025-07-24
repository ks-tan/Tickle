using Tickle;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _testTransform;

    private void Start()
    {
        _testTransform.LerpScale(1, 3, 10).Start();
    }
}