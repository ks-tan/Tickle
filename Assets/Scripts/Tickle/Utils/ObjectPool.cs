using System;
using System.Collections.Generic;

namespace Tickle.Utils
{
    public class ObjectPool<T> where T : class
    {
        /// <summary>
        /// NOTE: Uninit by default 
        /// </summary>
        public static ObjectPool<T> Shared;

        private readonly Stack<T> _pooled;
        private readonly Func<T> _generator;

        public ObjectPool(Func<T> objectGenerator, bool thisAsShared = false)
        {
            _generator = objectGenerator ??
                throw new ArgumentNullException(nameof(objectGenerator));
            _pooled = new Stack<T>();
            Shared = thisAsShared ? this : null;
        }

        public T Rent() => _pooled.Count > 0 ? _pooled.Pop() : _generator();

        public void Return(T item) => _pooled.Push(item);
    }
}