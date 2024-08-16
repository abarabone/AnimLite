using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;



namespace AnimLite.Utility.Linq
{


    /// <summary>
    /// 
    /// </summary>
    public struct EmptyEnumerableStruct<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => new Enumerator();

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator();

        struct Enumerator : IEnumerator<T>
        {
            public T Current => default;

            object IEnumerator.Current => default;

            public void Dispose() { }

            public bool MoveNext() { return false; }
            public void Reset() { }
        }
    }

    public static class EmptyEnumerableExtension
    {
        public static EmptyEnumerableStruct<T> EmptyEnumerable<T>(this IEnumerable<T> src) => new EmptyEnumerableStruct<T>();
        public static IEnumerable<T> Box<T>(this EmptyEnumerableStruct<T> src) => src;
    }




    /// <summary>
    /// アロケーションなしに、単体の値/クラスを IEnumerable<T> で包む。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct EnumerableWrapStruct<T> : IEnumerable<T>
    {

        public T Value;

        public IEnumerator<T> GetEnumerator() => new Enumerator { Value = this.Value, isFirst = true };

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator { Value = this.Value, isFirst = true };

        struct Enumerator : IEnumerator<T>
        {
            public T Value;
            public bool isFirst;

            public T Current => this.Value;

            object IEnumerator.Current => this.Value;

            public void Dispose() { }

            public bool MoveNext()
            {
                var isValue = this.isFirst;
                this.isFirst = false;
                return isValue;
            }
            public void Reset() { }
        }
    }

    public static class EnumerableWrapStructExtension
    {
        public static EnumerableWrapStruct<T> WrapEnumerable<T>(this T value) => new EnumerableWrapStruct<T>() { Value = value };
        public static IEnumerable<T> Box<T>(this EnumerableWrapStruct<T> src) => src;
    }




    public static class LinqUtilityExtenstion
    {

        public static IEnumerable<T> Do<T>(this IEnumerable<T> e, Action<T, int> f) =>
            e.Select((x, i) => { f(x, i); return x; })
            ;



        public static IEnumerable<T> Zip<T1, T2, T>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x, Func<T1, T2, T> f) =>
            Enumerable.Zip(x.src1, x.src2, f);

        public static IEnumerable<(T1, T2)> Zip<T1, T2>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x) =>
            (x.src1, x.src2).Zip((x, y) => (x, y));

    }

}
