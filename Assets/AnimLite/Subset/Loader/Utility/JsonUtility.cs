using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AnimLite.Utility
{


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
    // ・ワイルドカードを含むときは、１つ１つマッチングして適合すれば上書きする
    // ・ _ から始まるものは実体化しないが、ほかのベースにはなりえる（その時は名前から _ を除外してマッチングする）
    // ・また、ワイルドカードを含めたものはマッチするものすべてのベースになりうる
    // ・_ は他の _ のベースにもなりうる
    // ・ただし前方にあるもののみ（それまでに辞書化されたものということ）
    // ・複数マッチする場合は最後にマッチするものを採用する
    public class DictionaryPopulativeConverter<TValue> : JsonConverter<Dictionary<string, TValue>>
        where TValue : class
    {

        public override Dictionary<string, TValue> ReadJson(
            JsonReader reader, Type objectType,
            Dictionary<string, TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dictionary = existingValue ?? new Dictionary<string, TValue>();
            var jToken = JToken.Load(reader);

            deserialize_normal_entries_();
            deserialize_wildcard_entries_();

            return dictionary;


            void deserialize_normal_entries_()
            {
                var basekeys = new List<string>();
                // 下記は下地になった dance scene もベースに採用する場合。わかりにくいのでやめる。（同一ファイル内でのみ上書き・ベースを行うとする）
                //var basekeys = dictionary.Keys
                //    .Where(x => is_basekey_(x))
                //    .Select(x => x)
                //    .ToList();

                var qEntry =
                    from thisprop in jToken.Children<JProperty>()
                    where
                        !thisprop.Name.IsWild()
                        ||
                        is_basekey_(thisprop.Name)
                    select (thisprop.Name, thisprop.Value)
                    ;
                foreach (var (thiskey, thisvalue) in qEntry)
                {
                    if (dictionary.TryGetValue(thiskey, out var value))
                    {
                        using var subReader = thisvalue.CreateReader();
                        serializer.Populate(subReader, value);
                    }
                    else if (find_base_(thiskey) is var basevalue && basevalue != null)
                    {
                        var newvalue = basevalue.CloneViaJson();
                        dictionary[thiskey] = newvalue;

                        using var subReader = thisvalue.CreateReader();
                        serializer.Populate(subReader, newvalue);
                    }
                    else
                    {
                        dictionary[thiskey] = thisvalue.ToObject<TValue>(serializer);
                    }

                    if (is_basekey_(thiskey)) basekeys.Add(thiskey);
                }

                return;


                bool is_basekey_(string keyname) =>
                    keyname[0] == '_';

                TValue? find_base_(string keyname)
                {
                    var targetkey = is_basekey_(keyname)
                        ? keyname
                        : "_" + keyname;
                    var qBase =
                        from _basekey in basekeys.AsEnumerable().Reverse()
                        where targetkey.Like(_basekey)
                        select _basekey
                        ;

                    var basekey = qBase.FirstOrDefault();
                    if (basekey == null) return null;

                    var basevalue = dictionary[basekey];
                    return basevalue;
                }
            }

            void deserialize_wildcard_entries_()
            {
                var qWild =
                    from thisprop in jToken.Children<JProperty>()
                    where thisprop.Name.IsWild()
                    let wildcard = thisprop.Name.ToWildcard()

                    from exist in dictionary
                    where exist.Key.Like(wildcard)

                    select (thisprop.Value, exist)
                    ;
                foreach (var (thisvalue, exist) in qWild)
                {
                    using var subReader = thisvalue.CreateReader();
                    serializer.Populate(subReader, exist.Value);
                }
            }
        }

        //public override Dictionary<string, TValue> ReadJson(
        //    JsonReader reader, Type objectType,
        //    Dictionary<string, TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
        //{
        //    var dictionary = existingValue ?? new Dictionary<string, TValue>();
        //    var jToken = JToken.Load(reader);

        //    foreach (var property in jToken.Children<JProperty>())
        //    {
        //        var thiskey = property.Name;
        //        var thisvalue = property.Value;

        //        if (!thiskey.IsWild())
        //        {
        //            if (dictionary.TryGetValue(thiskey, out var value))
        //            {
        //                using var subReader = thisvalue.CreateReader();
        //                serializer.Populate(subReader, value);
        //            }
        //            else
        //            {
        //                dictionary[thiskey] = thisvalue.ToObject<TValue>(serializer);
        //            }
        //        }
        //        else
        //        {
        //            var wildcard = thiskey.ToWildcard();

        //            foreach (var exist in dictionary)
        //            {
        //                if (!exist.Key.Like(wildcard)) continue;

        //                using var subReader = thisvalue.CreateReader();
        //                serializer.Populate(subReader, exist.Value);
        //            }
        //        }
        //    }
        //    return dictionary;
        //}


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
            (serializer.Deserialize<string>(reader)?.ToString() ?? (hasExistingValue ? existingValue : "")).ToPath();

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
                    Paths = (serializer.Deserialize<string[]>(reader) ?? new string[] { })
                        .Where(x => (x ?? "") != "")
                        .Select(x => x.ToPath())
                        .ToArray(),
                },

                JsonToken.String =>
                    (serializer.Deserialize<string>(reader)?.ToString() ?? "").ToPath(),

                _ =>
                    hasExistingValue
                        ? existingValue
                        : "".ToPath(),
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
