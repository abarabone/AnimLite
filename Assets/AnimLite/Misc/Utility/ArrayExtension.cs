using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace AnimLite.Utility
{
    public static class ArrayExtension
    {


        public static NativeArray<T> ToNativeArray<T>(this T[] src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged
        =>
            new NativeArray<T>(src, allocator);

        public static NativeArray<T> ToNativeArray<T>(this IEnumerable<T> src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged
        =>
            src.ToArray().ToNativeArray(allocator);



        public static NativeArray<T> ToNativeArrayOrNot<T>(this T[] src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged
        =>
            src.Length > 0
                ? src.ToNativeArray(allocator)
                : default;

        public static NativeArray<T> ToNativeArrayOrNot<T>(this IEnumerable<T> src, Allocator allocator = Allocator.Persistent)
            where T : unmanaged
        =>
            src.Any()
                ? src.ToNativeArray(allocator)
                : default;

    }
}
