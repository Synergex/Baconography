using System;
using System.Collections.Generic;
namespace BaconographyPortable
{
    public class EndlessStack<T> where T : class
    {
        LinkedList<T> _data;
        int _headDiff;
        int _maxSize;

        public EndlessStack(int size)
        {
            _data = new LinkedList<T>();
            _headDiff = 0;
            _maxSize = size;
        }

        public void Push(T t)
        {
            _data.AddFirst(t);
            if (_data.Count > _maxSize)
            {
                _data.RemoveLast();
            }
        }

        public T Forward()
        {
            if (_headDiff > 0)
            {
                var lstNode = _data.First;
                for(int i = 1; lstNode != null && i < _headDiff; i++)
                {
                    lstNode = lstNode.Next;
                }
                _headDiff--;
                return lstNode != null ? lstNode.Value : null;
            }
            else
                return null;
        }

        public bool EmptyForward
        {
            get
            {
                return _headDiff == 0;
            }
        }


        public T Backward()
        {
            if (_headDiff < _data.Count)
            {
                var lstNode = _data.First;
                for (int i = 0; lstNode != null && i < _headDiff; i++)
                {
                    lstNode = lstNode.Next;
                }
                _headDiff++;
                return lstNode != null ? lstNode.Value : null;
            }
            else
                return null;
        }
    }
}