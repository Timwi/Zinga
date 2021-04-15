using Microsoft.JSInterop;

namespace Zinga.Wasm
{
    public static class Commands
    {
        [JSInvokable]
        public static string CompileSuco(string suco, string variableTypesJson)
        {
            return Lib.Commands.CompileSuco(suco, variableTypesJson);
        }
    }
}
