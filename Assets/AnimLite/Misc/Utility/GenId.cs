using UnityEngine;
using System;
using System.Threading;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Burst;

namespace AnimLite.Utility
{

    public unsafe struct GenId : IDisposable
    {
        public int* p_counter { get; private set; }

        Allocator allocator;


        public static GenId Create(Allocator allocator = Allocator.Temp) => new GenId(allocator);
        
        public unsafe GenId(Allocator allocator)
        {
            this.allocator = allocator;
            this.p_counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), 4, allocator);
            *this.p_counter = 0;
        }


        public Concurrent ToConcurrent() => new Concurrent(this.p_counter);

        [BurstCompile]
        public unsafe struct Concurrent
        {
            [NativeDisableUnsafePtrRestriction]
            int* p_counter;

            public Concurrent(int* p_counter) => this.p_counter = p_counter;

            public int Generate() => Interlocked.Increment(ref *this.p_counter) - 1;

            public int Current => *this.p_counter;
        }


        public void Dispose()
        {
            UnsafeUtility.Free(this.p_counter, this.allocator);
            this.p_counter = null;
        }
    }

}
