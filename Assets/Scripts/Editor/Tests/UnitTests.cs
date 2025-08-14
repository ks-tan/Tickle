using System;
using UnityEditor;
using UnityEngine;
using Tickle.Collections;

public static class UnitTests
{
    [MenuItem("Tickle/Test")]
    public static void RunTests()
    {
        Func<bool>[] tests =
        {
            SparseSetTests.InsertSingleAndGetSingle,
            SparseSetTests.InsertMultipleAndGetSingle,
            SparseSetTests.InsertSingleAndRemoveSingle,
            SparseSetTests.InsertMultipleAndRemoveSingle,
        };

        var failures = 0;

        foreach(var test in tests)
        {
            var isSuccess = test.Invoke();
            if (!isSuccess) failures++;
            Debug.Assert(isSuccess, $"Failed {test.Method.Name}");
        }

        if (failures == 0)
            Debug.Log("Passed all unit tests!");
    }
}

public static class SparseSetTests
{
    public static bool InsertSingleAndGetSingle()
    {
        var sparseSet = new SparseSet<int>(64);
        var index = sparseSet.Add(1);
        var data = -1;
        var isSuccess = sparseSet.TryGet(index, ref data) && data == 1;
        sparseSet.Dispose();
        return isSuccess;
    }

    public static bool InsertMultipleAndGetSingle()
    {
        var sparseSet = new SparseSet<int>(64);
        for (int i = 0; i < 64; i++)
            sparseSet.Add(i);
        var data = -1;
        var isSuccess = sparseSet.TryGet(5, ref data) && data == 5;
        sparseSet.Dispose();
        return isSuccess;
    }

    public static bool InsertSingleAndRemoveSingle()
    {
        var sparseSet = new SparseSet<int>(64);
        sparseSet.Add(10);
        sparseSet.Remove(0);
        var data = -1;
        var isSuccess = !sparseSet.TryGet(0, ref data);
        return isSuccess;
    }

    public static bool InsertMultipleAndRemoveSingle()
    {
        var sparseSet = new SparseSet<int>(64);
        for (int i = 0; i < 64; i++)
            sparseSet.Add(i);
        sparseSet.Remove(10);
        var data = -1;
        var isSuccess = !sparseSet.TryGet(10, ref data);
        return isSuccess;
    }
}
