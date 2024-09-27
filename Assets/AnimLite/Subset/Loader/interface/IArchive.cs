using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;
using System.IO;
using System.Collections.Concurrent;
using UnityEngine.Networking;
using UniVRM10;
using UnityEngine.AddressableAssets;
using UnityEngine.Scripting;// [Preserve] ‚Ì‚½‚ß
using System.Net.Http;
using System.IO.Compression;
using AnimLite.Vmd;
using System.Text;

namespace AnimLite.Utility
{


    public interface IArchive : IDisposable
    {
        T Extract<T>(PathUnit entryPath, Func<Stream, T> createAction);
        ValueTask<T> ExtractAsync<T>(PathUnit entryPath, Func<Stream, ValueTask<T>> createAction);

        T ExtractFirstEntry<T>(string extension, Func<Stream, T> convertAction);
        ValueTask<T> ExtractFirstEntryAsync<T>(string extension, Func<Stream, ValueTask<T>> convertAction);


        IArchive FallbackArchive { get; }

    }



}
