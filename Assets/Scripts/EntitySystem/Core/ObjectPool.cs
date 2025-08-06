using System;
using System.Collections.Generic;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Generic object pool for reducing garbage collection pressure
    /// </summary>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool = new();
        private readonly Func<T> _createFunc;
        private readonly Action<T> _resetAction;
        private readonly int _maxSize;
        private int _currentSize;

        public int AvailableCount => _pool.Count;
        public int TotalCreated => _currentSize;

        public ObjectPool(int maxSize = 100, Func<T> createFunc = null, Action<T> resetAction = null)
        {
            _maxSize = maxSize;
            _createFunc = createFunc ?? (() => new T());
            _resetAction = resetAction;
        }

        /// <summary>
        /// Get an object from the pool or create a new one
        /// </summary>
        public T Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            _currentSize++;
            return _createFunc();
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(T item)
        {
            if (item == null) return;

            if (_pool.Count < _maxSize)
            {
                _resetAction?.Invoke(item);
                _pool.Push(item);
            }
        }

        /// <summary>
        /// Clear the pool
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
            _currentSize = 0;
        }

        /// <summary>
        /// Pre-warm the pool with objects
        /// </summary>
        public void PreWarm(int count)
        {
            count = Mathf.Min(count, _maxSize);
            for (int i = 0; i < count; i++)
            {
                Return(_createFunc());
            }
        }
    }

    /// <summary>
    /// Specialized object pool for GameObjects
    /// </summary>
    public class GameObjectPool
    {
        private readonly Stack<GameObject> _pool = new();
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly int _maxSize;
        private int _currentSize;

        public int AvailableCount => _pool.Count;
        public int TotalCreated => _currentSize;

        public GameObjectPool(GameObject prefab, int maxSize = 50, Transform parent = null)
        {
            _prefab = prefab;
            _maxSize = maxSize;
            _parent = parent;
        }

        /// <summary>
        /// Get a GameObject from the pool or instantiate a new one
        /// </summary>
        public GameObject Get()
        {
            GameObject obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
                obj.SetActive(true);
            }
            else
            {
                obj = UnityEngine.Object.Instantiate(_prefab, _parent);
                _currentSize++;
            }

            return obj;
        }

        /// <summary>
        /// Return a GameObject to the pool
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (_pool.Count < _maxSize)
            {
                obj.SetActive(false);
                if (_parent != null)
                {
                    obj.transform.SetParent(_parent);
                }
                _pool.Push(obj);
            }
            else
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        /// <summary>
        /// Clear the pool and destroy all objects
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            _currentSize = 0;
        }

        /// <summary>
        /// Pre-warm the pool with GameObjects
        /// </summary>
        public void PreWarm(int count)
        {
            count = Mathf.Min(count, _maxSize);
            for (int i = 0; i < count; i++)
            {
                var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
                obj.SetActive(false);
                _pool.Push(obj);
                _currentSize++;
            }
        }
    }
}