using System;
using UnityEngine;

namespace SG.Dialogue.Events
{
    public abstract class BaseEventChannel<T> : ScriptableObject
    {
        private Action<T> _onRaised;

        public void Raise(T value) => _onRaised?.Invoke(value);

        public void RegisterListener(Action<T> listener) => _onRaised += listener;

        public void UnregisterListener(Action<T> listener) => _onRaised -= listener;
    }
}
