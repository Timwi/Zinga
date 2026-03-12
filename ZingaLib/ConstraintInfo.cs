using RT.Json;

namespace Zinga.Lib
{
    public sealed class ConstraintInfo(int id, string valuesJson)
    {
        public int ID { get; private set; } = id;
        public string ValuesJson { get; private set; } = valuesJson;

        public JsonDict ToJson() => new()
        {
            ["type"] = ID,
            ["values"] = new JsonRaw(ValuesJson)
        };
    }
}