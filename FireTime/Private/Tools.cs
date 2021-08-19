using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FireTime.Private
{
    internal static class Extensions
    {
        internal static IEnumerable<int> GetIndexes(this string Src, string Match)
        {
            Match = Regex.Escape(Match);
            foreach (Match match in Regex.Matches(Src, Match))
                yield return match.Index;
        }

        internal static bool Have(this JToken Base, object Match)
        {
            if (Match == null) return false;
            var Selected = Base.Where(M => M.ToString() == Match.ToString());
            foreach (var STok in Selected)
                if ((Match.GetType().Name == "String" && STok.Type != JTokenType.String) ||
                    (STok.Type == JTokenType.String && Match.GetType().Name != "String")) return false;
            return Selected.Count() > 0;
        }

        internal static void Fill(this JArray Base, int Till)
        {
            if (Base.Count - 1 >= Till) return;
            else for (int I = Base.Count; I <= Till; I++) Base.Add(null);
        }

        internal static JObject GetOFromA(this JArray Base)
        {
            int Index = 0;
            var JToRet = new JObject();

            foreach (var I in Base.Children())
            {
                if (I.Type != JTokenType.Null)
                    JToRet.Add(Index.ToString(), Base[Index]);
                Index++;
            }

            return JToRet;
        }

        internal static JArray TryGetAFromO(this JObject JObj)
        {
            JArray ToRet = new JArray();
            var JProp = JObj.Properties();
            if (JProp.Count() < 1) return null;
            if (!JProp.IsJArrayType()) return null;

            for (int I = 0; I <= int.Parse(JProp.Last().Name); I++)
                if (JObj.ContainsKey(I.ToString()))
                    ToRet.Add(JObj.GetValue(I.ToString()));
                else ToRet.Add(null);
            return ToRet;
        }

        internal static void ChangeTO(this JToken Base, ref JToken Root, JToken Value)
        {
            if (Base == Root) Root = Value;
            else Base.Replace(Value);
        }

        internal static void TrimNull(this JArray Base)
        {
            var Iliterable = Base.Children().Reverse();
            foreach (var Trimable in Iliterable)
                if (Trimable.Type == JTokenType.Null) Base.Remove(Trimable);
                else break;
        }

        internal static bool IsJArrayType(this IEnumerable<JProperty> ToCheck)
            => ToCheck.Where(M => !int.TryParse(M.Name, out _)).Count() == 0;
    }

    internal class ServerData
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
