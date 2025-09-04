using System;
using System.Collections.Generic;

namespace Prediction
{
    /// <summary>
    /// Interface for types that can anticipate their next value based on previous values.
    /// </summary>
    /// <typeparam name="T">The unmanaged type implementing this interface.</typeparam>
    public interface IAnticipatable<T> where T : unmanaged, IAnticipatable<T>
    {
        /// <summary>
        /// Predicts the next value based on previous and current values.
        /// </summary>
        /// <param name="previous">The previous value.</param>
        /// <param name="current">The current value.</param>
        /// <returns>The anticipated next value.</returns>
        T PredictNext(T previous, T current);
    }

    /// <summary>
    /// Maintains a history of values and can anticipate future values based on that history using pattern matching.
    /// </summary>
    /// <typeparam name="T">The unmanaged type implementing IAnticipatable&lt;T&gt;.</typeparam>
    public sealed class ValueAnticipator<T> where T : unmanaged, IAnticipatable<T>
    {
        private readonly Queue<T> _history;
        private readonly int _capacity;
        private float _predictWeight;
        private T _value;

        /// <summary>
        /// The current value of the anticipater. Setting it records the value into history automatically.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                if (_history.Count >= _capacity)
                    _history.Dequeue();
                _history.Enqueue(value);
            }
        }

        /// <summary>
        /// Initializes a new instance of ValueAnticipater with an initial value and history size.
        /// </summary>
        /// <param name="initial">The initial value.</param>
        /// <param name="historySize">The maximum number of historical values to keep.</param>
        public ValueAnticipator(T initial, int historySize = 15)
        {
            _capacity = historySize;
            _history = new Queue<T>(_capacity);
            Value = initial;
        }

        /// <summary>
        /// Predicts the next value based on historical patterns.
        /// </summary>
        /// <returns>The anticipated next value.</returns>
        public T Predict()
        {
            if (_history.Count < 2)
                return Value;

            T[] array = _history.ToArray();
            int bestMatchIndex = -1;
            int maxMatchLength = 0;

            for (int start = 0; start < array.Length - 1; start++)
            {
                int matchLength = 0;
                while (start + matchLength < array.Length - 1 &&
                       array[start + matchLength].Equals(array[array.Length - 1 - matchLength]))
                {
                    matchLength++;
                }
                if (matchLength > maxMatchLength)
                {
                    maxMatchLength = matchLength;
                    bestMatchIndex = start + matchLength;
                }
            }

            if (bestMatchIndex >= 0 && bestMatchIndex < array.Length)
                return array[bestMatchIndex];

            return array[array.Length - 1];
        }

        /// <summary>
        /// Smoothly predicts the value fading in from 0 to 1.
        /// </summary>
        /// <param name="step">The increment per call (0-1).</param>
        /// <returns>The anticipated blended value.</returns>
        public T PredictIn(float step = 0.1f)
        {
            _predictWeight += step;
            if (_predictWeight > 1f) _predictWeight = 1f;
            return Value.PredictNext(Value, Predict());
        }

        /// <summary>
        /// Smoothly predicts the value fading out from 1 to 0.
        /// </summary>
        /// <param name="step">The decrement per call (0-1).</param>
        /// <returns>The anticipated blended value.</returns>
        public T PredictOut(float step = 0.1f)
        {
            _predictWeight -= step;
            if (_predictWeight < 0f) _predictWeight = 0f;
            return Value.PredictNext(Value, Predict());
        }
    }
}
