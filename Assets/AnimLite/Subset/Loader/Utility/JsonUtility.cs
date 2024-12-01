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


    // ���R���o�[�^�[�o����
    // reader �̓g�[�N����ǂ݉�������
    // �I�u�W�F�N�g�� { } �ɂ�����ꂽ���́A�z��� [ ] �ɂ�����ꂽ���́iJsonToken �񋓒l������΂��낢��킩��j
    // ��������v���~�e�B�u
    // �I�u�W�F�N�g����������ɉ߂��Ȃ�
    // .Read() �̓J�[�\����i�߂�
    // .Value �͌��݂̃g�[�N���𓾂�
    // serializer.Deserialize(reader) �� reader �ŃI�u�W�F�N�g��v���~�e�B�u��ǂ݉���������������������́i���R�J�[�\���͐i�ށj
    // �f�V���A���C�Y�̓J�[�\���� { �̈ʒu�ɂ���K�v������A�������� } �̈ʒu�ɂȂ��Ă���
    // �֐��ɓ����Ă����Ƃ��� { �̈ʒu�ɂȂ��Ă���





    // �f�B�N�V���i�����L�[�� PopulateObject() �Ή��ɂ���R���o�[�^�[
    // �E���C���h�J�[�h���܂ނƂ��́A�P�P�}�b�`���O���ēK������Ώ㏑������
    // �E _ ����n�܂���͎̂��̉����Ȃ����A�ق��̃x�[�X�ɂ͂Ȃ肦��i���̎��͖��O���� _ �����O���ă}�b�`���O����j
    // �E�܂��A���C���h�J�[�h���܂߂����̂̓}�b�`������̂��ׂẴx�[�X�ɂȂ肤��
    // �E_ �͑��� _ �̃x�[�X�ɂ��Ȃ肤��
    // �E�������O���ɂ�����̂̂݁i����܂łɎ��������ꂽ���̂Ƃ������Ɓj
    // �E�����}�b�`����ꍇ�͍Ō�Ƀ}�b�`������̂��̗p����
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
                // ���L�͉��n�ɂȂ��� dance scene ���x�[�X�ɍ̗p����ꍇ�B�킩��ɂ����̂ł�߂�B�i����t�@�C�����ł̂ݏ㏑���E�x�[�X���s���Ƃ���j
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

    // ���O�t���z�񂪂ق������߂ɁA�킴�킴�������g���̂����ʂ��ȂƎv���A
    // �L�[�o�����[�y�A�̔z�񂩂玫���Ɠ��� json �������o���R���o�[�^���l�������A
    // ���� JObject �Ȃǂ̓����I�u�W�F�N�g�Ŏ������g���Ă����Ȃ��ƂɋC�Â��A�����ł�����c�ƂȂ���
    // �n�b�V���e�[�u���A���S���Y���I�ɂ́A���������L�[�̔{�̃G���g�����m�ۂ��ďՓ˂�h���ł���悤�Ȃ̂ŁA����Ȃɖ��ʂɂ͊m�ۂ��Ȃ��͂�
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

    //        // existingValue �ɑ��݂��邪 keyValuePairs �ɂ͑��݂��Ȃ��L�[��ǉ�
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


    // �Ȃ��Ԃ̃G���g���m�F�֐�
    //static int GetCapacity<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    //{
    //    FieldInfo fieldInfo = typeof(Dictionary<TKey, TValue>).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
    //    if (fieldInfo is null) return 0;

    //    Array entries = (Array)fieldInfo.GetValue(dictionary);
    //    return entries.Length;
    //}



}
