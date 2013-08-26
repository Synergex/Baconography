using System;
namespace BaconographyPortable
{
    public class CircularBuffer<T>
    {
        T[] _buffer;
        int _head;
        int _tail;
        int _length;
        int _bufferSize;
        object _lock = new object();

        public CircularBuffer(int bufferSize)
        {
            _buffer = new T[bufferSize];
            _bufferSize = bufferSize;
            _head = bufferSize - 1;
        }

        public bool IsEmpty
        {
            get { return _length == 0; }
        }

        public bool IsFull
        {
            get { return _length == _bufferSize; }
        }

        public T Dequeue()
        {
            lock (_lock)
            {
                if (IsEmpty) throw new InvalidOperationException("Queue exhausted");

                T dequeued = _buffer[_tail];
                _tail = NextPosition(_tail);
                _length--;
                return dequeued;
            }
        }

        private int NextPosition(int position)
        {
            return (position + 1) % _bufferSize;
        }

        public void Enqueue(T toAdd)
        {
            lock (_lock)
            {
                _head = NextPosition(_head);
                _buffer[_head] = toAdd;
                if (IsFull)
                    _tail = NextPosition(_tail);
                else
                    _length++;
            }
        }
    }
}