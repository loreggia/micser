using System;

namespace Micser.Common
{
    public class CircularBuffer<T>
    {
        private readonly object _addLock;
        private readonly T[] _data;

        public CircularBuffer(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            _addLock = new object();
            _data = new T[size];
            Size = size;
        }

        public bool IsFull { get; private set; }
        public int Position { get; private set; }
        public int Size { get; }

        public T this[int index]
        {
            get => index >= 0 && index < _data.Length ? _data[index] : throw new ArgumentOutOfRangeException(nameof(index));
            set
            {
                if (index < 0 || index >= _data.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                _data[index] = value;
            }
        }

        public void Add(T item)
        {
            lock (_addLock)
            {
                _data[Position] = item;

                Position++;

                if (Position == _data.Length)
                {
                    IsFull = true;
                }

                Position %= _data.Length;
            }
        }
    }
}