using Tickle;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _test;

    private void Start()
    {
        var tickles = new ITickle[]
        {
            _test.LerpPosition(new Vector3(-5, 0, 0), new Vector3(5, 0, 0), 2),
            _test.LerpScale(1, 3, 5).OnComplete(() => Debug.Log("TEST"))
        };
        tickles.OnComplete(() => Debug.Log("COMPLETED"));
        tickles.Start();
    }
}