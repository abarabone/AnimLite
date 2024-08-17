using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;

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

    }

}
