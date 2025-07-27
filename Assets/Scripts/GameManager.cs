using Tickle;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _test;

    private void Start()
    {
        var tickleSet = new TickleSet()
            .Join(_test.LerpPosition(new Vector3(-5, 0, 0), new Vector3(5, 0, 0), 2))
            .Join(_test.LerpScale(1, 3, 2).OnComplete(() => Debug.Log("TEST")));

        var tickleChain = new TickleChain()
            .Chain(tickleSet)
            .Chain(_test.LerpScale(3, 1, 5).OnComplete(() => Debug.Log("TEST")))
            .OnComplete(() => Debug.Log("FINISHED"))
            .Start();
    }
}