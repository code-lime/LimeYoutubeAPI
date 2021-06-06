using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI
{
    public class PoolArray<T>
        where T : unmanaged
    {
        private T[] buffer;
        public int Length { get; private set; }
        public int Count { get; private set; } = 2;
        const int MaxSize = (int.MaxValue - 1) / 2;

        public PoolArray()
        {
            buffer = new T[Count];
        }
        public PoolArray(int initLength) : this()
        {
            SetSize(initLength);
        }
        private void SetSize(int newLength)
        {
            if (Count < newLength)
            {
                if (newLength > int.MaxValue)
                {
                    throw new OutOfMemoryException($"{newLength} bigger then {int.MaxValue}");
                }

                if (newLength > MaxSize & newLength <= int.MaxValue)
                {
                    Count = int.MaxValue;
                }
                else
                {
                    while (Count < newLength)
                    {
                        Count = Count << 1;
                    }
                }

                if (buffer.Length != Count)
                {
                    buffer = new T[Count];
                }
            }

            Length = newLength;
        }

        public void Compress()
        {
            int length = 2;
            while (length < Length)
            {
                length = length << 1;
            }

            Count = length;
            if (buffer.Length != length)
            {
                buffer = new T[length];
            }
        }

        public unsafe T* WriteByPointer(int length)
        {
            SetSize(length);
            fixed(T* pointer = buffer)
            {
                return pointer;
            }
        }
        public T[] Write(int length)
        {
            SetSize(length);
            return buffer;
        }

        public unsafe T* ReadByPointer()
        {
            fixed(T* pointer = buffer)
            {
                return pointer;
            }
        }
        public unsafe int ReadByPointer(out T* pointer)
        {
            fixed(T* point = buffer)
            {
                pointer = point;
                return Length;
            }
        }
        public ReadOnlySpan<T> Read()
        {
            return Read(0, Length);
        }
        public ReadOnlySpan<T> Read(int startIndx, int length)
        {
            if (startIndx + length > Length) throw new IndexOutOfRangeException();
            return buffer.AsSpan().Slice(startIndx, length);
        }
        public ReadOnlySpan<T> Read(int startIndx)
        {
            return Read(startIndx, Length - startIndx);
        }
    }
}
