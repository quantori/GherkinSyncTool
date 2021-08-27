using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using TestRail.Types;

namespace TestRail.Utils
{
    /// <summary>Helper class for Json Objects</summary>
    internal static class JsonUtility
    {
        /// <summary>Merge two Json Objects</summary>
        /// <param name="obj1">object 1 to merge</param>
        /// <param name="obj2">object 2 to merge</param>
        /// <returns>a non null Json object (NOTE: may be empty)</returns>
        internal static JObject Merge(JObject obj1, JObject obj2)
        {
            if (null == obj1)
            {
                obj1 = new JObject();
            }

            if (null == obj2)
            {
                return obj1;
            }

            var token = obj2.First;

            while (null != token)
            {
                obj1.Add(token);
                token = token.Next;
            }

            return obj1;
        }

        /// <summary>Converts a JArray into a List of type T</summary>
        /// <param name="jarray">JArray to parse</param>
        /// <param name="parse">The method being used to parse the JArray</param>
        /// <returns>returns a list of objects corresponding to the json, empty list if nothing exists</returns>
        internal static List<T> ConvertJArrayToList<T>(JArray jarray, Func<JObject, T> parse) where T : BaseTestRailType
        {
            var list = new List<T>();

            if (null != jarray && null != parse)
            {
                list.AddRange(from JObject json in jarray select parse(json));
            }

            return list;
        }
    }
}
