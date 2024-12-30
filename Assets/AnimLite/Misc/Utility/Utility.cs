using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
//using UniJSON;
//using Newtonsoft.Json.Converters;
//using Newtonsoft.Json;

namespace AnimLite.Utility
{
    #nullable enable
    
    public static class Utility
    {

        public static void Then(this bool criteria, Action action)
        {
            if (criteria) action();
        }

        public static void NotThen(this bool criteria, Action action)
        {
            if (!criteria) action();
        }


        public static T Then<T>(this T src, Func<T, bool> criteria, Func<T, T> expression) =>
            criteria(src)
                ? expression(src)
                : src;


        public static ValueTask DisposeNullableAsync<T>(this T? obj) where T : IAsyncDisposable =>
            obj?.DisposeAsync() ?? new ValueTask();

        public static ValueTask InvokeNullableAsync(this Func<ValueTask>? f) =>
            f?.Invoke() ?? new ValueTask();

        public static ValueTask<T> InvokeNullableAsync<T>(this Func<T, ValueTask<T>>? f, T param) =>
            f?.Invoke(param) ?? new ValueTask<T>();



        //public static T CloneViaJson<T>(this T src)
        //{
        //    //var json = JsonUtility.ToJson(src);
        //    //return JsonUtility.FromJson<T>(json);
        //    var json = JsonConvert.SerializeObject(src);
        //    return 
        //}

    }

}
