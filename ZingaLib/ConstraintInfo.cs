using RT.Json;

namespace Zinga.Lib
{
    public sealed class ConstraintInfo
    {
        public int ID { get; private set; }
        public string ValuesJson { get; private set; }

        public ConstraintInfo(int id, string valuesJson)
        {
            ID = id;
            ValuesJson = valuesJson;
        }

        public JsonDict ToJson() => new()
        {
            ["type"] = ID,
            ["values"] = new JsonRaw(ValuesJson)
        };
    }
}