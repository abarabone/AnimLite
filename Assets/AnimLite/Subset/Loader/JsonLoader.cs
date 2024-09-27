using AnimLite.DancePlayable;
using AnimLite.Vmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;
using Unity.VisualScripting;
using UnityEngine.AddressableAssets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AnimLite.Utility
{
    using AnimLite.Utility;
    using AnimLite.Utility.Linq;

    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using Newtonsoft.Json.Linq;
    using System.Reflection;
    using static AnimLite.DancePlayable.DanceGraphy2;


    public static class JsonLoader
    {


        public static async ValueTask<T> LoadJsonAsync<T>(
            this IArchive archive, PathUnit path, T jsondata, CancellationToken ct)
        {
            if (path.IsBlank()) return jsondata;

            if (archive is not null)
            {
                var json = await LoadErr.LoggingAsync(async () =>
                    path.ToZipEntryPath() switch
                    {
                        var entrypath when entrypath != "" =>
                            await archive.ExtractAsync(entrypath, s => DeserializeJsonAsync<T>(s, jsondata)),
                        _ =>
                            await archive.ExtractFirstEntryAsync(".json", s => DeserializeJsonAsync<T>(s, jsondata)),
                        // .json だけは .zip 自体の entry path を参照する。
                        // 他のメディアでは .json に記されたパスを entry path と解釈する。
                    });

                if (json is not null)
                    return json;

                if (archive.FallbackArchive is not null)
                    return await archive.FallbackArchive.LoadJsonAsync(path, jsondata, ct);
            }

            return await path.LoadJsonAsync<T>(jsondata, ct);
        }

        public static ValueTask<T> LoadJsonAsync<T>(this IArchive archive, PathUnit path, CancellationToken ct) where T:new() =>
            archive.LoadJsonAsync(path, new T(), ct);




        public static async ValueTask<T> LoadJsonAsync<T>(this PathUnit path, T jsondata, CancellationToken ct) =>
            await LoadErr.LoggingAsync(async () =>
        {

            ValueTask<Stream> openAsync_(PathUnit path) =>
                path.OpenStreamFileOrWebOrAssetAsync<TextAsset>(asset => asset.bytes, ct);


            if (path.IsBlank()) return jsondata;

            var (fullpath, queryString) = path.ToFullPath().DividToPathAndQueryString();
            fullpath.ThrowIfAccessedOutsideOfParentFolder();


            return fullpath.DividZipToArchiveAndEntry() switch
            {
                var (zippath, entrypath) when entrypath != "" =>
                    await openAsync_(zippath + queryString).UnzipAwait(entrypath, s => DeserializeJsonAsync<T>(s, jsondata)),
                var (zippath, _) when zippath != "" =>
                    await openAsync_(zippath + queryString).UnzipFirstEntryAwait(".json", s => DeserializeJsonAsync<T>(s, jsondata)),
                var (_, _) =>
                    await openAsync_(fullpath + queryString).UsingAwait(s => DeserializeJsonAsync<T>(s, jsondata)),
            };
        });

        public static ValueTask<T> LoadJsonAsync<T>(this PathUnit path, CancellationToken ct) where T : new() =>
            path.LoadJsonAsync(new T(), ct);




        static async ValueTask<T> DeserializeJsonAsync<T>(this Stream s, T jsondata)
        {
            using var r = new StreamReader(s);
            var json = await r.ReadToEndAsync();

            JsonConvert.PopulateObject(json, jsondata, JsonLoader.JsonOptions);
            return jsondata;
        }


        // System.Text.Json が一般的になったら変更しよう、コメントとかデフォルト値とか
        //static JsonSerializerOptions jsonOptions;
        //JsonLoader()
        //{
        //    jsonOptions = new
        //    {
        //        ReadCommentHandling = JsonCommentHandling.Skip,
        //    };
        //}


        static JsonLoader()
        {
            JsonOptions = new JsonSerializerSettings();
            JsonOptions.Converters.Add(new StringEnumConverter());
            JsonOptions.Converters.Add(new PathUnitConverter());
            JsonOptions.Converters.Add(new PathListConverter());
            JsonOptions.Converters.Add(new DictionaryPopulativeConverter<ModelDefineJson>());
            JsonOptions.Converters.Add(new DictionaryPopulativeConverter<DanceMotionDefineJson>());
            JsonOptions.Formatting = Formatting.Indented;
        }
        public static JsonSerializerSettings JsonOptions;
    }


    // ＜コンバーター覚え＞
    // reader はトークンを読み下すもの
    // オブジェクトは { } にくくられたもの、配列は [ ] にくくられたもの（JsonToken 列挙値を見ればいろいろわかる）
    // 文字列もプリミティブ
    // オブジェクト名も文字列に過ぎない
    // .Read() はカーソルを進める
    // .Value は現在のトークンを得る
    // serializer.Deserialize(reader) は reader でオブジェクトやプリミティブを読み下す処理を自動化するもの（当然カーソルは進む）
    // デシリアライズはカーソルが { の位置にある必要があり、完了時は } の位置になっている
    // 関数に入ってきたときは { の位置になっている

    // ディクショナリ同キーで PopulateObject() 対応にするコンバーター
    class DictionaryPopulativeConverter<TValue> : JsonConverter<Dictionary<string, TValue>>
        where TValue : class
    {

        public override Dictionary<string, TValue> ReadJson(
            JsonReader reader, Type objectType,
            Dictionary<string, TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dictionary = existingValue ?? new Dictionary<string, TValue>();

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                var key = reader.Value.ToString();

                reader.Read();

                if (dictionary.TryGetValue(key, out var value))
                {
                    serializer.Populate(reader, value);
                }
                else
                {
                    dictionary[key] = serializer.Deserialize<TValue>(reader);
                }
            }

            return dictionary;
        }


        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, Dictionary<string, TValue> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public class PathUnitConverter : JsonConverter<PathUnit>
    {
        public override PathUnit ReadJson(
            JsonReader reader, Type objectType, PathUnit existingValue, bool hasExistingValue, JsonSerializer serializer)
        =>
            (serializer.Deserialize<string>(reader)?.ToString() ?? "").ToPath();

        public override void WriteJson(
            JsonWriter writer, PathUnit value, JsonSerializer serializer)
        =>
            serializer.Serialize(writer, value.Value);
    }

    public class PathListConverter : JsonConverter<PathList>
    {
        public override PathList ReadJson(
            JsonReader reader, Type objectType, PathList existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return reader.TokenType switch
            {
                JsonToken.StartArray => new PathList
                {
                    Paths = serializer.Deserialize<string[]>(reader)
                        .Where(x => (x ?? "") != "")
                        .Select(x => x.ToPath()),
                },
                JsonToken.String =>
                    (serializer.Deserialize<string>(reader)?.ToString() ?? "").ToPath(),
                _ =>
                    "".ToPath(),
            };
        }

        public override void WriteJson(
            JsonWriter writer, PathList value, JsonSerializer serializer)
        {
            switch (value.Paths.ToArray())
            {
                case var x when x.Length == 1:
                    serializer.Serialize(writer, x[0].Value);
                    break;
                case var x:
                    serializer.Serialize(writer, x);
                    break;
                default:
                    break;
            };
        }
    }


    // 名前付き配列がほしいために、わざわざ辞書を使うのも無駄だなと思い、
    // キーバリューペアの配列から辞書と同じ json を書き出すコンバータも考えたが、
    // 結局 JObject などの内部オブジェクトで辞書を使ってそうなことに気づき、辞書でいいや…となった
    // ハッシュテーブルアルゴリズム的には、だいたいキーの倍のエントリを確保して衝突を防いでいるようなので、そんなに無駄には確保しないはず
    //public class KeyValuePairConverter<TKey, TValue> : JsonConverter<KeyValuePair<TKey, TValue>[]>
    //{
    //    public override void WriteJson(
    //        JsonWriter writer, KeyValuePair<TKey, TValue>[]? value, JsonSerializer serializer)
    //    {
    //        writer.WriteStartObject();
    //        foreach (var kvp in value!)
    //        {
    //            writer.WritePropertyName(kvp.Key!.ToString());
    //            serializer.Serialize(writer, kvp.Value);
    //        }
    //        writer.WriteEndObject();
    //    }

    //    public override KeyValuePair<TKey, TValue>[]? ReadJson(
    //        JsonReader reader, Type objectType, KeyValuePair<TKey, TValue>[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
    //    {
    //        var jObject = JObject.Load(reader);
    //        var keyValuePairs = new List<KeyValuePair<TKey, TValue>>();

    //        foreach (var property in jObject.Properties())
    //        {
    //            var key = (TKey)Convert.ChangeType(property.Name, typeof(TKey));
    //            var existingItem = Array.Find(existingValue, kvp => kvp.Key!.Equals(key));
    //            if (existingItem.Equals(default(KeyValuePair<TKey, TValue>)))
    //            {
    //                var value = property.Value.ToObject<TValue>(serializer);
    //                keyValuePairs.Add(new KeyValuePair<TKey, TValue>(key, value));
    //            }
    //            else
    //            {
    //                serializer.Populate(property.Value.CreateReader(), existingItem.Value);
    //                keyValuePairs.Add(existingItem);
    //            }
    //        }

    //        // existingValue に存在するが keyValuePairs には存在しないキーを追加
    //        foreach (var kvp in existingValue!)
    //        {
    //            if (!keyValuePairs.Exists(pair => pair.Key!.Equals(kvp.Key)))
    //            {
    //                keyValuePairs.Add(kvp);
    //            }
    //        }

    //        return keyValuePairs.ToArray();
    //    }
    //}


    // ないぶのエントリ確認関数
    //static int GetCapacity<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    //{
    //    FieldInfo fieldInfo = typeof(Dictionary<TKey, TValue>).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
    //    if (fieldInfo is null) return 0;

    //    Array entries = (Array)fieldInfo.GetValue(dictionary);
    //    return entries.Length;
    //}
}