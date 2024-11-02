using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UniJSON;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace AnimLite.Utility
{

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



        public static T Instantiate<T>(this T src)
        {
            var json = JsonUtility.ToJson(src);
            return JsonUtility.FromJson<T>(json);
        }

    }

}
