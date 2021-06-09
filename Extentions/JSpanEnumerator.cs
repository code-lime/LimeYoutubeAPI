using System;
using System.Collections.Generic;
using System.Text;

namespace LimeYoutubeAPI
{
    public ref struct JSpanEnumerator
    {
        readonly JSpan baseObj;
        int iterator;
        int count;

        public static implicit operator JSpanEnumerator(JSpan span) => span.GetEnumerator();
        public static implicit operator JSpan(JSpanEnumerator span) => span.baseObj;

        public JSpanEnumerator(JSpan baseObj)
        {
            this.baseObj = baseObj;
            Current = default;
            iterator = 0;
            count = -1;
        }

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
            iterator++;
            return true;
        }

        public JSpan GetLast()
        {
            return baseObj[Count() - 1];
        }
        public int Count()
        {
            if (count == -1)
            {
                var iter = iterator;
                var counter = 0;
                Reset();
                while (MoveNext())
                {
                    counter++;
                }
                iterator = iter;
                count = counter;
            }
            return count;
        }
        public void Reset()
        {
            iterator = 0;
        }
        public JSpan Current { get; private set; }
    }
}
