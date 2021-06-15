using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace LimeYoutubeAPI.SpanParseSrc
{
    internal static class JSONSetup
    {
        public static readonly char BETWEN_KEY_AND_SENSE = ':';
        public static readonly char BETWEN = ',';
        public static readonly char VALUE_AND_KEY = '"';

        public static readonly JSONSeparator OBJECT = new JSONSeparator('{', '}', JType.Object);
        public static readonly JSONSeparator ARRAY = new JSONSeparator('[', ']', JType.Array);
        public static readonly JSONSeparator VALUE = new JSONSeparator(VALUE_AND_KEY, VALUE_AND_KEY, JType.Value);

        public static readonly ImmutableHashSet<char> MANAGE_CONSTRUCT = new[] { '\r', ' ', '\n', '\a', '\b', '\f', '\t', ',' }.ToImmutableHashSet();

        public static JSONSeparator ChooseSeparate(char OpenSimbol)
        {
            if (OpenSimbol == OBJECT.Open) return OBJECT;
            if (OpenSimbol == ARRAY.Open) return ARRAY;
            if (OpenSimbol == VALUE.Open) return VALUE;

            return null;
            //throw new ArgumentException($"{OpenSimbol} - not registred separator: try with {{ }} [ ] {'"'}");
        }
        public static int GetSeparatorIndx(this ReadOnlySpan<char> source, int searchstartIndx, char sep)
        {
            for (int i = searchstartIndx; i < source.Length; i++)
            {
                var symbol = source[i];
                if (symbol == sep) return i;
                if (MANAGE_CONSTRUCT.Contains(symbol)) continue;
                return -1;
            }
            return -1;
        }
        public static bool HasNextSense(this ReadOnlySpan<char> source, int searchstartIndx, out int betwenIndx)
        {
            for (int i = searchstartIndx; i < source.Length; i++)
            {
                var symbol = source[i];
                if (symbol == BETWEN)
                {
                    betwenIndx = i;
                    return true;
                }
                if (symbol == OBJECT.Close || symbol == ARRAY.Close)
                {
                    betwenIndx = i;
                    return false;
                }
                if (MANAGE_CONSTRUCT.Contains(symbol)) continue;

                betwenIndx = -1;
                return false;
            }
            betwenIndx = -1;
            return false;
        }
        public static int GetSenseSeparatorIndx(this ReadOnlySpan<char> source, int searchstartIndx, out JType type)
        {
            for (int i = searchstartIndx; i < source.Length; i++)
            {
                var symbol = source[i];
                var sep = ChooseSeparate(symbol);
                if (sep != null)
                {
                    type = sep.Type;
                    return i;
                }
                if (MANAGE_CONSTRUCT.Contains(symbol)) continue;

                type = JType.None;
                return -1;
            }
            type = JType.None;
            return -1;
        }
        public static int GetValueSeparatorIndx(this ReadOnlySpan<char> source, int searchstartIndx, bool open = false)
        {
            var found = source[searchstartIndx..].IndexOf(VALUE_AND_KEY) + searchstartIndx;

            if (source[found + 1] == VALUE_AND_KEY && !open)
                return source.GetValueSeparatorIndx(found + 2);

            return found;
        }
    }
    public interface MemoryHolder<T> where T:unmanaged
    {
        T GetByRef(int indx);
        int Add(T value);
    }
    public class MemoryContext
    {
        public virtual PoolArray<JObjectNode> ObjectRefMemory { get; } = new PoolArray<JObjectNode>();
        public virtual PoolArray<JArrayNode> ArrayRefMemory { get; } = new PoolArray<JArrayNode>();
        public void Release()
        {
            ObjectRefMemory.Release();
            ArrayRefMemory.Release();
        }
    }
    internal class JSONSeparator
    {
        public readonly char Open;
        public readonly char Close;
        public readonly JType Type;
        public JSONSeparator(char open, char close, JType type)
        {
            Open = open;
            Close = close;
            Type = type;
        }
    }
    public readonly ref struct JSpan
    {
        private readonly ReadOnlySpan<char> SourceSet;
        private readonly MemoryContext MemoryContext;

        private readonly JArrayNode Array;
        private readonly JObjectNode Object;

        public bool IsEmpty => SourceSet.IsEmpty;
        public bool IsArray => Array.ValueType != JType.None;
        public bool IsObject => Object.ValueType != JType.None;
        public bool IsValue =>
            IsArray && Array.ValueType == JType.Value ||
            IsObject && Object.ValueType == JType.Value;
        public bool IsWrongJSON => !(IsEmpty || IsArray || IsObject || IsValue);


        private JSpan(ReadOnlySpan<char> jsonCharSpan, MemoryContext context)
        {
            SourceSet = jsonCharSpan;
            MemoryContext = context;
            Array = default;
            Object = default;
        }
        private JSpan(ReadOnlySpan<char> jsonCharSpan, MemoryContext context, JArrayNode array) : this(jsonCharSpan, context)
        {
            Array = array;
        }
        private JSpan(ReadOnlySpan<char> jsonCharSpan, MemoryContext context, JObjectNode obj) : this(jsonCharSpan, context)
        {
            Object = obj;
        }
        public static JSpan Parse(ReadOnlySpan<char> jsonCharSpan, MemoryContext context)
        {
            var foundIndx = JSONSetup.GetSenseSeparatorIndx(jsonCharSpan, 0, out var type);
            if (type == JType.Object)
            {
                JObjectNode.Parse(jsonCharSpan, foundIndx + 1, context, out var objectRef, out var valueEndIndx);
                valueEndIndx = jsonCharSpan.GetSeparatorIndx(valueEndIndx + 1, JSONSetup.OBJECT.Close);
                var Object = new JObjectNode(JType.Object, objectRef, 0, 0, foundIndx, valueEndIndx, -1);
                return new JSpan(jsonCharSpan, context, Object);
            }
            if (type == JType.Array)
            {
                JArrayNode.Parse(jsonCharSpan, foundIndx + 1, 0, context, out var arrayRef, out var valueEndIndx);
                var Array = new JObjectNode(JType.Array, arrayRef, 0, 0, foundIndx, valueEndIndx, -1);
                return new JSpan(jsonCharSpan, context, Array);
            }
            return new JSpan(jsonCharSpan, context);
        }
        public JSpan this[ReadOnlySpan<char> key] => FindByKey(key);

        private JSpan FindByKey(ReadOnlySpan<char> key)
        {
            if (!(Array.ValueType == JType.Object || Object.ValueType == JType.Object)) return default;
            var valueRef = IsObject ? Object.Value : Array.Value;
            var workflow = MemoryContext.ObjectRefMemory.GetByRef(valueRef);
            if (workflow.GetByKey(key, SourceSet, MemoryContext.ObjectRefMemory, out var obj))
            {
                return new JSpan(SourceSet, MemoryContext, obj);
            }
            return new JSpan(SourceSet, MemoryContext);
        }

        public JSpan this[int indx] => FindByObjectIndex(indx);

        private JSpan FindByObjectIndex(int indx)
        {
            if (!(Array.ValueType == JType.Array || Object.ValueType == JType.Array)) return default;
            var valueRef = IsObject ? Object.Value : Array.Value;
            var workflow = MemoryContext.ArrayRefMemory.GetByRef(valueRef);
            if (workflow.GetByIndex(indx, MemoryContext.ArrayRefMemory, out var obj))
            {
                return new JSpan(SourceSet, MemoryContext, obj);
            }
            return new JSpan(SourceSet, MemoryContext);
        }

        private bool GetValue(out int startIndx, out int endIndx)
        {
            if (IsArray)
            {
                startIndx = Array.ValueStartIndx;
                endIndx = Array.ValueEndIndx;
                return true;
            }
            if (IsObject)
            {
                startIndx = Object.ValueStartIndx;
                endIndx = Object.ValueEndIndx;
                return true;
            }
            startIndx = -1;
            endIndx = -1;
            return false;
        }

        public override string ToString()
        {
            if (GetValue(out var startIndx, out var endIndx)) 
            {
                return new string(SourceSet[(startIndx + 1)..endIndx]);
            }
            else
                return string.Empty;
        }
        public string AsJsonString()
        {
            if (GetValue(out var startIndx, out var endIndx))
            {
                return new string(SourceSet[startIndx..(endIndx + 1)]);
            }
            else
                return string.Empty;
        }
        public T Deserialize<T>()
        {
            if (GetValue(out var startIndx, out var endIndx))
            {
                var encoding = Encoding.UTF8;
                var set = SourceSet[startIndx..endIndx];
                Span<byte> buffer = stackalloc byte[encoding.GetByteCount(set)];
                encoding.GetBytes(set, buffer);
                return JsonSerializer.Deserialize<T>(buffer);
            }
            else
                return default;
        }
    }
    public readonly struct JObjectNode
    {
        public readonly JType ValueType;
        public readonly int Value;
        public readonly int ValueStartIndx;
        public readonly int ValueEndIndx;

        public readonly int KeyIndxStart;
        public readonly int KeyIndxEnd;

        public readonly int Next;
        public JObjectNode(JType type, int valueRef, int keyStartIndx, int keyEndIndx, int valueStartIndx, int valueEndIndx, int next)
        {
            ValueType = type;
            Value = valueRef;
            ValueStartIndx = valueStartIndx;
            ValueEndIndx = valueEndIndx;

            KeyIndxStart = keyStartIndx;
            KeyIndxEnd = keyEndIndx;

            Next = next;
        }

        public static JObjectNode Parse(ReadOnlySpan<char> source, int startSearchIndx, MemoryContext memoryContext, out int memoryIndx, out int endValueIndx)
        {
            var KeyIndxStart = source.GetValueSeparatorIndx(startSearchIndx, true) + 1;
            var KeyIndxEnd = source.GetValueSeparatorIndx(KeyIndxStart);

            var tempIndx = source.GetSeparatorIndx(KeyIndxEnd + 1, JSONSetup.BETWEN_KEY_AND_SENSE);
            var ValueStartIndx = source.GetSenseSeparatorIndx(tempIndx + 1, out var ValueType);

            if (ValueType == JType.None) throw new InvalidOperationException();

            var Next = -1;
            var Value = -1;
            var ValueEndIndx = -1;

            if (ValueType == JType.Array)
            {
                JArrayNode.Parse(source, ValueStartIndx + 1, 0, memoryContext, out Value, out ValueEndIndx);
            }
            if (ValueType == JType.Object)
            {
                Parse(source, ValueStartIndx + 1, memoryContext, out Value, out ValueEndIndx);
                ValueEndIndx = source.GetSeparatorIndx(ValueEndIndx + 1, JSONSetup.OBJECT.Close);
            }
            if (ValueType == JType.Value)
            {
                ValueEndIndx = source.GetValueSeparatorIndx(ValueStartIndx + 1);
            }

            if (source.HasNextSense(ValueEndIndx + 1, out tempIndx))
            {
                Parse(source, tempIndx + 1, memoryContext, out Next, out endValueIndx);
            }
            else
            {
                endValueIndx = ValueEndIndx;
            }

            if (endValueIndx == -1)
            {
                throw new InvalidOperationException("wrongJson");
            }

            var self = new JObjectNode(ValueType, Value, KeyIndxStart, KeyIndxEnd, ValueStartIndx, ValueEndIndx, Next);
            memoryIndx = memoryContext.ObjectRefMemory.Add(self);
            return self;
        }

        public bool GetByKey(ReadOnlySpan<char> key, ReadOnlySpan<char> source, MemoryHolder<JObjectNode> refMemomry, out JObjectNode value)
        {
            if (!source[KeyIndxStart..KeyIndxEnd].SequenceEqual(key))
            {
                if (Next == -1)
                {
                    value = default;
                    return false;
                }
                else
                {
                    var next = refMemomry.GetByRef(Next);
                    return next.GetByKey(key, source, refMemomry, out value);
                }
            }
            else
            {
                value = this;
                return true;
            }
        }
    }
    public readonly struct JArrayNode
    {
        public readonly JType ValueType;
        public readonly int Value;
        public readonly int ValueStartIndx;
        public readonly int ValueEndIndx;

        public readonly int SelfIndex;

        public readonly int Next;

        public JArrayNode(JType type, int valueRef, int self, int valueStartIndx, int valueEndIndx, int next)
        {
            ValueType = type;
            Value = valueRef;
            ValueStartIndx = valueStartIndx;
            ValueEndIndx = valueEndIndx;

            SelfIndex = self;

            Next = next;
        }
        public static JArrayNode Parse(ReadOnlySpan<char> source, int startSearchIndx, int selfIndx, MemoryContext memoryContext, out int memoryIndx, out int endValueIndx)
        {
            var ValueStartIndx = source.GetSenseSeparatorIndx(startSearchIndx, out var ValueType);

            if (ValueType == JType.None) throw new InvalidOperationException();

            var Next = -1;
            var Value = -1;
            var ValueEndIndx = -1;

            if (ValueType == JType.Array)
            {
                Parse(source, ValueStartIndx + 1, 0, memoryContext, out Value, out ValueEndIndx);
            }
            if (ValueType == JType.Object)
            {
                JObjectNode.Parse(source, ValueStartIndx + 1, memoryContext, out Value, out ValueEndIndx);
                ValueEndIndx = source.GetSeparatorIndx(ValueEndIndx + 1, JSONSetup.OBJECT.Close);
            }
            if (ValueType == JType.Value)
            {
                ValueEndIndx = source.GetValueSeparatorIndx(ValueStartIndx + 1);
            }

            if (source.HasNextSense(ValueEndIndx + 1, out var tempIndx))
            {
                Parse(source, tempIndx + 1, selfIndx + 1, memoryContext, out Next, out endValueIndx);
            }
            else
            {
                endValueIndx = source.GetSeparatorIndx(ValueEndIndx + 1, JSONSetup.ARRAY.Close);
            }

            if (endValueIndx == -1)
            {
                throw new InvalidOperationException("wrongJson");
            }

            var self = new JArrayNode(ValueType, Value, selfIndx, ValueStartIndx, ValueEndIndx, Next);
            memoryIndx = memoryContext.ArrayRefMemory.Add(self);
            return self;
        }
        public bool GetByIndex(int indx, MemoryHolder<JArrayNode> refMemomry, out JArrayNode value)
        {
            if (SelfIndex != indx)
            {
                if (Next == -1)
                {
                    value = default;
                    return false;
                }
                else
                {
                    var next = refMemomry.GetByRef(Next);
                    return next.GetByIndex(indx, refMemomry, out value);
                }
            }
            else
            {
                value = this;
                return true;
            }
        }
    }
    public enum JType : byte
    {
        None = 0,
        Value,
        Array,
        Object
    }
}
