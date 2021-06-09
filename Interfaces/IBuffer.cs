using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI.Interfaces
{
    public interface IBuffer<T> where T: unmanaged 
    {
        public unsafe T* WriteByPointer(int length);
        public T[] Write(int length);
    }
}
