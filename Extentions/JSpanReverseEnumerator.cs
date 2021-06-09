using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI
{
    public ref struct JSpanReverseEnumerator
    {
        readonly JSpan baseObj;
        int iterator;
        int count;
        public JSpanReverseEnumerator(JSpan baseObj)
        {
            this.baseObj = baseObj;
            Current = default;
            count = -1;
            iterator = -1;
        }

        public static implicit operator JSpanReverseEnumerator(JSpan span) => span.GetReverseEnumerator();
        public static implicit operator JSpan(JSpanReverseEnumerator span) => span.baseObj;

        public bool MoveNext()
        {
            if (iterator == -1) return false;
            Current = baseObj[iterator];
            if (Current.IsEmpty)
            {
                iterator = -1;
                Current = default;
                return false;
            }
            iterator--;
            return true;
        }
        public int Count()
        {
            if (count == -1)
            {
                var counter = 0;
                JSpan current = baseObj[0];
                while (!current.IsEmpty)
                {
                    counter++;
                }
                count = counter;
            }
            return count;
        }
        public void Reset()
        {
            iterator = Count() - 1;
        }
        public JSpan Current { get; private set; }
    }
}
