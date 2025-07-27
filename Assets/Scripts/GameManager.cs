using Tickle;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _test;

    private void Start()
    {
        var chain = new TickleChain()
            .Chain(new ITickle[]
            {
                _test.LerpPosition(new Vector3(-5, 0, 0), new Vector3(5, 0, 0), 2),
                _test.LerpScale(1, 3, 2).OnComplete(() => Debug.Log("TEST"))
            })
            .Chain(_test.LerpScale(3, 1, 5).OnComplete(() => Debug.Log("TEST")))
            .OnComplete(() => Debug.Log("FINISHED"))
            .Start();
    }
}