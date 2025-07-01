using NUnit.Framework;
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

        static public EmptyEnumerableStruct<T> Create() => new EmptyEnumerableStruct<T>();
    }

    public static class EmptyEnumerableExtension
    {
        public static EmptyEnumerableStruct<T> EmptyEnumerable<T>(this IEnumerable<T> src) => new EmptyEnumerableStruct<T>();
        public static IEnumerable<T> Box<T>(this EmptyEnumerableStruct<T> src) => src;

        //public static IEnumerable<T> SetEmptyEnumerable<T>(ref this IEnumerable<T> prop)
        //{
        //    return prop = prop.EmptyEnumerable();
        //}
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

        public static bool HasSingleElement<T>(this IEnumerable<T> src) =>
            src.Any() && src.Skip(1).IsEmpty();


        static public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>
            (this (IEnumerable<TKey> keys, IEnumerable<TValue> values) src)
        =>
            src.Zip().ToDictionary(x => x.Item1, x => x.Item2);



        public static IEnumerable<T> Do<T>(this IEnumerable<T> e, Action<T, int> f) =>
            e.Select((x, i) => { f(x, i); return x; })
            ;



        public static IEnumerable<T> Zip<T1, T2, T>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x, Func<T1, T2, T> f) =>
            Enumerable.Zip(x.src1, x.src2, f);

        public static IEnumerable<(T1, T2)> Zip<T1, T2>(this (IEnumerable<T1> src1, IEnumerable<T2> src2) x) =>
            (x.src1, x.src2).Zip((x, y) => (x, y));


        public static IAsyncEnumerable<T> Zip<T1, T2, T>(this (IAsyncEnumerable<T1> src1, IAsyncEnumerable<T2> src2) x, Func<T1, T2, T> f) =>
            AsyncEnumerable.Zip(x.src1, x.src2, f);

        public static IAsyncEnumerable<(T1, T2)> Zip<T1, T2>(this (IAsyncEnumerable<T1> src1, IAsyncEnumerable<T2> src2) x) =>
            (x.src1, x.src2).Zip((x, y) => (x, y));


        //public static IEnumerable<(T1, T2, T3)> Zip<T1, T2, T3>(
        //    this (IEnumerable<T1> a, IEnumerable<T2> b, IEnumerable<T3> c) x) =>
        //        ((x.a, x.b).Zip(), x.c).Zip((x, y) => (x.Item1, x.Item2, y));

        public static IEnumerable<(T1, T2, T3)> Zip<T1, T2, T3>(
            this (IEnumerable<T1> a, IEnumerable<T2> b, IEnumerable<T3> c) x)
        {
            var ea = x.a.GetEnumerator();
            var eb = x.b.GetEnumerator();
            var ec = x.c.GetEnumerator();
            while (ea.MoveNext() && eb.MoveNext() && ec.MoveNext())
            {
                yield return (ea.Current, eb.Current, ec.Current);
            }
        }





        public static IEnumerable<T> WhereIn<T, U>(this IEnumerable<T> src, IEnumerable<U> list, Func<T, U, bool> expression) =>
            src.Where(x => list.Where(y => expression(x, y)).Any());
        //list.Select(x => src.Where(y => expression(y, x)).Cast<T?>().FirstOrDefault())
        //    .Where(x => x is null);


        public static IEnumerable<string> WhereExtIn(this IEnumerable<string> src, IEnumerable<string> extensionlist) =>
            src.WhereIn(extensionlist, (x, y) => x.EndsWith(y, StringComparison.InvariantCultureIgnoreCase));

        public static IEnumerable<string> WhereExtIn(this IEnumerable<string> src, string extensions) =>
            src.WhereExtIn(extensions.Split(';'));


        public static IEnumerable<T> WhereExtIn<T>(this IEnumerable<T> src, IEnumerable<string> extensionlist, Func<T, string> conversion) =>
            src.WhereIn(extensionlist, (x, y) => conversion(x).EndsWith(y, StringComparison.InvariantCultureIgnoreCase));

        public static IEnumerable<T> WhereExtIn<T>(this IEnumerable<T> src, string extensions, Func<T, string> conversion) =>
            src.WhereExtIn(extensions.Split(';'), conversion);


        public static IEnumerable<string> WhereWildIn(this IEnumerable<string> src, IEnumerable<string> matchlist)
        {
            var wilds = matchlist.Select(x => x.ToWildcard()).ToArray();
            return src.WhereIn(wilds, (x, y) => x.Like(y));
        }
        public static IEnumerable<string> WhereWildIn(this IEnumerable<string> src, string matchs) =>
            src.WhereWildIn(matchs.Split(';'));

        public static IEnumerable<T> WhereWildIn<T>(this IEnumerable<T> src, IEnumerable<string> matchlist, Func<T, string> conversion)
        {
            var wilds = matchlist.Select(x => x.ToWildcard()).ToArray();
            return src.WhereIn(wilds, (x, y) => conversion(x).Like(y));
        }
        public static IEnumerable<T> WhereWildIn<T>(this IEnumerable<T> src, string matchs, Func<T, string> conversion) =>
            src.WhereWildIn(matchs.Split(';'), conversion);
    }

}
