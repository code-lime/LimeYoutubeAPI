using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using LimeYoutubeAPI.Interfaces;

namespace LimeYoutubeAPI
{
    public class PoolArray<T> : IBuffer<T>
        where T : unmanaged
    {
        private T[] buffer;
        public int Length { get; private set; }
        public int Count { get; private set; } = 2;
        private const int MAX_SIZE = (int.MaxValue - 1) / 2;

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
            if (newLength < 0) throw new ArgumentException($"{newLength} cant be less the zero");

            if (Count < newLength)
            {
                if (newLength > MAX_SIZE)
                {
                    Count = int.MaxValue;
                }
                else
                {
                    while (Count < newLength)
                    {
                        Count <<= 1;
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
                length <<= 1;
            }

            Count = length;
            if (buffer.Length != length)
            {
                buffer = new T[length];
            }
        }

        public void Clear()
        {
            Length = 0;
            Count = 2;
            buffer = new T[Count];
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
