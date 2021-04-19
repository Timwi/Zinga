using System;
using System.Collections.Generic;
using System.Linq;
using RT.Json;
using Zinga.Suco;

namespace Zinga.Lib
{
    public static class ZingaUtil
    {
        public static Dictionary<string, object> ConvertVariableValues(JsonDict valuesJson, SucoVariable[] variables)
        {
            var dic = new Dictionary<string, object>();
            foreach (var variable in variables)
            {
                var jsonValue = valuesJson.Safe[variable.Name];
                dic[variable.Name] = convertVariableValue(variable.Type, jsonValue, null);
            }

            return dic;
        }

        private static object convertVariableValue(SucoType type, JsonValue j, int? position) => type switch
        {
            SucoBooleanType => j.GetBoolLenientSafe() ?? false,
            SucoCellType => new Cell(j.GetIntLenientSafe() ?? 0, position),
            SucoDecimalType => j.GetDoubleLenientSafe() ?? 0d,
            SucoIntegerType => j.GetIntLenientSafe() ?? 0,
            SucoStringType => j.GetStringLenientSafe() ?? "",
            SucoListType lst => (j.GetListSafe() ?? new JsonList()).Select((v, ix) => convertVariableValue(lst.Inner, v, ix + 1)).ToArray(),
            _ => throw new NotImplementedException($"Programmer has neglected to include code to deserialize “{type}”.")
        };
    }
}
