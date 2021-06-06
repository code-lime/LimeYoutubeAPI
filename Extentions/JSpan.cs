using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace LimeYoutubeAPI
{
    public readonly ref struct JSpan
    {
        private readonly ReadOnlySpan<char> Set;
        public bool IsEmpty => Set.IsEmpty;
        public bool IsArray => !IsEmpty && Set[0] == ARRAY.Open && Set[Set.Length - 1] == ARRAY.Close;
        public bool IsObject => !IsEmpty && Set[0] == OBJECT.Open && Set[Set.Length - 1] == OBJECT.Close;
        public bool IsValue => !IsEmpty && Set[0] == VALUE.Open && Set[Set.Length - 1] == VALUE.Close;
        public bool IsWrongJSON => !(IsEmpty || IsArray || IsObject || IsValue);

        private static readonly char[] BETWEN_KEY_AND_SENSE = { '"', ':' };
        private static readonly char BETWEN = ',';

        private static readonly JSONSeparator OBJECT = new JSONSeparator('{', '}', true);
        private static readonly JSONSeparator ARRAY = new JSONSeparator('[', ']', true);
        private static readonly JSONSeparator VALUE = new JSONSeparator('"', '"', false);

        public JSpan(ReadOnlySpan<char> set)
        {
            Set = set;
        }

        public static JSpan Parse(ReadOnlySpan<char> set) => new JSpan(set);
        public static implicit operator ReadOnlySpan<char>(JSpan span) => span.Set;
        public static implicit operator JSpan(ReadOnlySpan<char> span) => new JSpan(span);
        public static implicit operator bool(JSpan span) => span.IsEmpty;

        public JSpan this[ReadOnlySpan<char> key] => FindByKey(key);
        public JSpan this[int indx] => FindByObjectIndex(indx);
        public string AsString() => new string(Set);
        public string AsStringValue() => new string(Set[1..(Set.Length - 1)]);
        public T Deserialize<T>()
        {
            var encoding = Encoding.UTF8;
            Span<byte> buffer = stackalloc byte[encoding.GetByteCount(Set)];
            encoding.GetBytes(Set, buffer);
            return JsonSerializer.Deserialize<T>(buffer);
        }

        private JSpan FindByKey(ReadOnlySpan<char> key)
        {
            if (!IsObject) return default;

            var indx = Set.IndexOf(key);
            if (indx == -1) return default;

            var resultSenseStartIndx = indx + key.Length + BETWEN_KEY_AND_SENSE.Length;
            var resultLength = SearchSenseLength(resultSenseStartIndx);

            if (resultLength == -1) return default;

            return new JSpan(Set.Slice(resultSenseStartIndx, resultLength));
        }

        private JSpan FindByObjectIndex(int indx)
        {
            if (!IsArray) return default;
            if (indx < 0) throw new IndexOutOfRangeException($"{indx} cant be less the zero");

            int startObjIndx = 1;
            int objLength;
            for (int i = 0; i < indx; i++)
            {
                if (startObjIndx >= Set.Length - 1) return default;
                objLength = SearchSenseLength(startObjIndx);
                startObjIndx += objLength + 1;
            }
            if (startObjIndx >= Set.Length) return default;
            objLength = SearchSenseLength(startObjIndx);
            return new JSpan(Set.Slice(startObjIndx, objLength));
        }

        private int SearchSenseLength(int startIndx)
        {
            var separator = ChooseSeparate(Set[startIndx]);
            if (separator is null) return -1;

            var area = Set.Slice(startIndx + 1);

            if (!separator.IsDeptSearch)
            {
                var found = area.IndexOf(separator.Close);
                if (found == -1)
                {
                    return -1;
                }
                else
                {
                    return found + 2;
                }
            }

            int dept = 1;
            int startSearchIndx = 0;

            while (dept != 0)
            {
                var closeIndx = area[startSearchIndx..].IndexOf(separator.Close) + startSearchIndx;
                if (closeIndx == -1) return -1;

                var newOpenIndx = area[startSearchIndx..closeIndx].IndexOf(separator.Open) + startSearchIndx;
                while (newOpenIndx > startSearchIndx)
                {
                    newOpenIndx++;
                    dept++;
                    newOpenIndx = area[newOpenIndx..closeIndx].IndexOf(separator.Open);
                }
                dept--;
                startSearchIndx = closeIndx + 1;
            }

            return startSearchIndx + 1;
        }

        private JSONSeparator ChooseSeparate(char OpenSimbol)
        {
            if (OpenSimbol == OBJECT.Open) return OBJECT;
            if (OpenSimbol == ARRAY.Open) return ARRAY;
            if (OpenSimbol == VALUE.Open) return VALUE;

            return null;

            //throw new ArgumentException($"{OpenSimbol} - not registred separator: try with {{ }} [ ] {'"'}");
        }

        private class JSONSeparator
        {
            public readonly char Open;
            public readonly char Close;
            public readonly bool IsDeptSearch;
            public JSONSeparator(char open, char close, bool isDeptSearch)
            {
                Open = open;
                Close = close;
                IsDeptSearch = isDeptSearch;
            }
        }
    }
}
