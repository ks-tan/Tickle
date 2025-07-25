using Tickle;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    private Transform[] _transforms = new Transform[1000];

    private void Start()
    {
        for(int i = 0; i < _transforms.Length; i++)
        {
            var obj = new GameObject();
            obj.transform.LerpScale(1, 10, 10).Start();
            _transforms[i] = obj.transform;
        }
    }

    private void Update()
    {
        Debug.Log(_transforms[90].localScale);
    }
}