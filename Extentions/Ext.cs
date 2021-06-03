using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI
{
    internal static class Ext
    {
        public static void UnregAll<T>(ref T _del) where T : Delegate
        {
            if (_del == null) return;
            foreach (Delegate d in _del.GetInvocationList())
                _del = (T)Delegate.Remove(_del, d);
        }
    }
}
