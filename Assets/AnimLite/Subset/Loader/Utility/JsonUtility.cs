﻿using System;
using System.Dynamic;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AnimLite.Utility
{


    public static class JsonSupplemetUtility
    {
        public static T CloneViaJson<T>(this T value)
        {
            if (value is null) return default;

            var json = JsonConvert.SerializeObject(value, SerializeOptions);

            return JsonConvert.DeserializeObject<T>(json, SerializeOptions);
        }

        public static T PopulateViaJson<T>(this T overridevalue, T basevalue)
        {
            if (overridevalue is null) return basevalue;

            var json = JsonConvert.SerializeObject(overridevalue, SerializeOptions);

            JsonConvert.PopulateObject(json, basevalue, SerializeOptions);

            return basevalue;
        }

        //public static T PopulateDefaultViaJson<T>(this T overridevalue) where T : new() =>
        //    PopulateViaJson(overridevalue, new T { });




//#if UNITY_IL2CPP || UNITY_WEBGL || UNITY_IOS || !USE_DYNAMIC

//        //public static T CloneViaJson<T>(object value)
//        //{
//        //    if (value is null) return default;

//        //    var json = JsonConvert.SerializeObject(value as object, SerializeOptions);

//        //    return JsonConvert.DeserializeObject<T>(json, SerializeOptions);
//        //}

//        //public static T PopulateViaJson<T>(object overridevalue, T basevalue)
//        //{
//        //    if (overridevalue is null) return basevalue;

//        //    //var json = JsonConvert.SerializeObject(overridevalue as object, setting);
//        //    var json = JsonConvert.SerializeObject(overridevalue as object, SerializeOptions);

//        //    JsonConvert.PopulateObject(json, basevalue, SerializeOptions);

//        //    return basevalue;
//        //}

//        //public static T PopulateDefaultViaJson<T>(object overridevalue) where T : new() =>
//        //    PopulateViaJson(overridevalue, new T { });

//        public static T PopulateDefaultViaJson<T>(JObject overridevalue) where T : new() =>
//            overridevalue.ToObject<T>();

//#else
//        public static T CloneViaJson<T>(dynamic value)
//        {
//            if (value is null) return default;

//            var json = JsonConvert.SerializeObject(value as object, SerializeOptions);

//            return JsonConvert.DeserializeObject<T>(json, SerializeOptions);
//        }

//        public static T PopulateViaJson<T>(dynamic overridevalue, T basevalue)
//        {
//            if (overridevalue is null) return basevalue;

//            var json = JsonConvert.SerializeObject(overridevalue as object), SerializeOptions);

//            JsonConvert.PopulateObject(json, basevalue, SerializeOptions);

//            return basevalue;
//        }

//        public static T PopulateDefaultViaJson<T>(dynamic overridevalue) where T : new() =>
//            PopulateViaJson(overridevalue, new T { });

//#endif



        public static T PopulateTo<T>(this JsonSerializer serializer, JsonReader reader, T value)
        {
            var obj = value as object;
            
            serializer.Populate(reader, obj);

            return (T)obj;
        }


        //static JsonSupplemetUtility()
        //{
        //    setting = new JsonSerializerSettings
        //    {
        //        // Vector3 などの normalize など循環を起こして実行時エラーとなるのを防止
        //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        //    };
        //}
        //static JsonSerializerSettings setting;


        // System.Text.Json が一般的になったら変更しよう、コメントとかデフォルト値とか
        //static JsonSerializerOptions jsonOptions;
        //JsonLoader()
        //{
        //    jsonOptions = new
        //    {
        //        ReadCommentHandling = JsonCommentHandling.Skip,
        //    };
        //}

        static JsonSupplemetUtility()
        {
            setSerializeOption();
            setDeserializeOption();
        }
        public static JsonSerializerSettings SerializeOptions;
        public static JsonSerializerSettings DeserializeOptions;

        static void setSerializeOption()
        {
            SerializeOptions = new JsonSerializerSettings();

            // Vector3 などの normalize など循環を起こして実行時エラーとなるのを防止
            SerializeOptions.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            SerializeOptions.DefaultValueHandling = DefaultValueHandling.Populate;
        }

        static void setDeserializeOption()
        {
            DeserializeOptions = new JsonSerializerSettings();

            DeserializeOptions.Converters.Add(new StringEnumConverter());
            DeserializeOptions.Converters.Add(new PathUnitConverter());
            DeserializeOptions.Converters.Add(new PathListConverter());
            DeserializeOptions.Converters.Add(new Numeric3Converter());
            DeserializeOptions.Converters.Add(new DictionaryPopulativeConverter<ModelDefineJson>());
            DeserializeOptions.Converters.Add(new DictionaryPopulativeConverter<DanceMotionDefineJson>());
            DeserializeOptions.Converters.Add(new JObjectMergeConverter());
            //JsonOptions.Converters.Add(new DynamicOptionConverter());
            //JsonOptions.Converters.Add(new DynamicJsonConverter());

            DeserializeOptions.Formatting = Formatting.Indented;

            // Vector3 などの normalize など循環を起こして実行時エラーとなるのを防止
            DeserializeOptions.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            SerializeOptions.DefaultValueHandling = DefaultValueHandling.Populate;
            //SerializeOptions.NullValueHandling = NullValueHandling.Ignore;
        }

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








    // Options.param ではなくて Options.Value.param になってしまうのでやめる

    //public struct DynamicOption
    //{
    //    public dynamic Value;

    //    public T ConvertTo<T>() => DynamicJson.Serialize<T>(this.Value);
    //    public T OverrideTo<T>(T value) => DynamicJson.Populate<T>(this.Value, value);
    //    public T OverrideDefault<T>() where T : new() => DynamicJson.PopulateDefault<T>(this.Value);

    //    //public static DynamicOption Default => new DynamicOption { Value = new ExpandoObject { } };
    //    public static DynamicOption Default => default;
    //}

    //public class DynamicOptionConverter : JsonConverter<DynamicOption>
    //{
    //    public override DynamicOption ReadJson(
    //        JsonReader reader, Type objectType, DynamicOption existingValue, bool hasExistingValue, JsonSerializer serializer)
    //    =>
    //        new DynamicOption
    //        {
    //            Value = hasExistingValue && existingValue.Value is not null
    //                ? serializer.PopulateTo(reader, existingValue.Value as object)
    //                : serializer.PopulateTo(reader, new ExpandoObject { })
    //        };


    //    public override void WriteJson(
    //        JsonWriter writer, DynamicOption value, JsonSerializer serializer)
    //    {
    //        serializer.Serialize(writer, value);
    //    }

    //}

    //public class DynamicJsonConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType) =>
    //        objectType == typeof(ExpandoObject) || objectType == typeof(JObject);


    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
    //        existingValue is not null
    //            ? serializer.PopulateTo(reader, existingValue)
    //            : serializer.PopulateTo(reader, new ExpandoObject { });

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
    //        serializer.Serialize(writer, value);
    //}




    //// オプションなど、json のまま受け取れるようにしようかと思ったが、間に変換が入る可能性がありそうなので、
    //// それなら dynamic でいいかと思った
    //// が、dynamic にしたらデフォルト値の扱いが面倒だったのでやめる

    //public struct JsonString
    //{
    //    public string Value;

    //    public T Deserialize<T>() where T : new()
    //    {
    //        var obj = new T { };
    //        JsonConvert.PopulateObject(this.Value ?? "{}", obj);
    //        return obj;
    //    }

    //    static public JsonString Default => new JsonString { Value = "{}" };
    //    static public implicit operator string (JsonString src) => src.Value;
    //}

    //public class JsonStringConverter : JsonConverter<JsonString>
    //{
    //    public override JsonString ReadJson(
    //        JsonReader reader, Type objectType, JsonString existingValue, bool hasExistingValue, JsonSerializer serializer)
    //    =>
    //        new JsonString
    //        {
    //            Value = JObject.Load(reader).ToString(),
    //        };


    //    public override void WriteJson(
    //        JsonWriter writer, JsonString value, JsonSerializer serializer)
    //    {

    //    }

    //}






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
                // 下記は下地になった dance scene もベースに採用する場合。わかりにくいのでやめる。（→ 同一ファイル内でのみ上書き・ベースを行うこととする）
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

                TValue find_base_(string keyname)
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


        //public override bool CanWrite => false;

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

    public class Numeric3Converter : JsonConverter<numeric3>
    {
        public override numeric3 ReadJson(
            JsonReader reader, Type objectType, numeric3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return hasExistingValue
                ? reader.TokenType switch
                {
                    JsonToken.Float => new numeric3(serializer.Deserialize<double>(reader)),

                    JsonToken.StartArray => populate_(serializer.Deserialize<double?[]>(reader), existingValue),

                    JsonToken.StartObject => serializer.PopulateTo<numeric3>(reader, existingValue),

                    _ => existingValue,
                }
                : reader.TokenType switch
                {
                    JsonToken.Float => new numeric3(serializer.Deserialize<double>(reader)),

                    JsonToken.StartArray => get_(serializer.Deserialize<double?[]>(reader)),
                
                    JsonToken.StartObject => new numeric3(serializer.Deserialize<Vector3>(reader)),

                    _ => default,
                };

            static numeric3 populate_(double?[] v, numeric3 exist)
            {
                if (v is null) return exist;
                if (v.Length > 0 && v[0].HasValue) exist.x = (float)v[0].Value;
                if (v.Length > 1 && v[1].HasValue) exist.y = (float)v[1].Value;
                if (v.Length > 2 && v[2].HasValue) exist.z = (float)v[2].Value;
                return exist;
            }
            static numeric3 get_(double?[] v) => new numeric3(
                v.Select(x => x ?? 0.0).ToArray()
                ??
                new double[] { });
        }

        public override void WriteJson(
            JsonWriter writer, numeric3 value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToVector3());
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



    public class JObjectMergeConverter : JsonConverter<JObject>
    {
        //public override bool CanConvert(Type objectType)
        //{
        //    return objectType == typeof(JObject);
        //}
        public override JObject ReadJson(
            JsonReader reader, Type objectType, JObject existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject newData = JObject.Load(reader);

            if (existingValue is JObject existingData)
            {
                existingData.Merge(newData);
                return existingData;
            }

            return newData;
        }

        public override void WriteJson(JsonWriter writer, JObject value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

}
