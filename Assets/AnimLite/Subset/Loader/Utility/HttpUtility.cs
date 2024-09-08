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
using UnityEngine.Scripting;// [Preserve] のため
using System.Net.Http;
using System.IO.Compression;
using AnimLite.Vmd;
using System.Text;

namespace AnimLite.Utility
{





    // (Unity) Android で HttpClient で通信するとインターネット権限が自動でつかない問題
    // https://ikorin2.hatenablog.jp/entry/2024/03/30/025946
    [Preserve]
    internal sealed class MarkerForInternet : UnityWebRequest { }
    // 上記のコードをどこかに書いておけば、UnityWebRequest を使っている判定になり、自動でインターネット権限をつけてくれる。
    // UnityWebRequest を継承したクラスを定義しておいて、Preserve 属性を使って IL2CPP で消えないようにした。



    public static class HttpLoader
    {
        static bool isCreated = false;

        readonly public static HttpClient Client;


        static HttpLoader()
        {
            "http client created".ShowDebugLog();

            Client = new HttpClient();
            isCreated = true;
        }

        public static void Dispose()
        {
            if (isCreated) Client.Dispose();

            "http client disposed".ShowDebugLog();
        }
    }

    public static class WebLoaderUtility
    {

        public static async ValueTask<Stream> LoadFromWebAsync(this PathUnit url, CancellationToken ct)
        {
            //ct.ThrowIfCancellationRequested();

            //var content = await HttpLoader.Client.GetByteArrayAsync(url);

            //ct.ThrowIfCancellationRequested();

            //return new MemoryStream(content);


            using var response = await HttpLoader.Client.GetAsync(url);
            if (!response.IsSuccessStatusCode) throw new System.Net.Http.HttpRequestException($"response is not success status code. {url.Value}");

            ct.ThrowIfCancellationRequested();

            using var content = response.Content;

            return await content.ReadAsStreamAsync();
        }

    }




}
