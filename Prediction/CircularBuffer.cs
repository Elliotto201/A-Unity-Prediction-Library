using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prediction
{
    internal sealed class CircularBuffer<T>
    {
        private T[] buffer;
        private int capacity;

        public CircularBuffer(int capacity)
        {
            buffer = new T[capacity];
            this.capacity = capacity;
        }

        public void Add(T item, int index)
        {
            buffer[index % capacity] = item;
        }

        public T Get(int index)
        {
            return buffer[index % capacity];
        }
    }
}
