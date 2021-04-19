using Microsoft.JSInterop;

namespace Zinga.Wasm
{
    public static class Commands
    {
        [JSInvokable]
        public static string CompileSuco(string suco, string variableTypesJson) => Lib.Commands.CompileSuco(suco, variableTypesJson);

        [JSInvokable]
        public static string RenderConstraintSvgs(string constraintTypesJson, string constraintsJson) => Lib.Commands.RenderConstraintSvgs(constraintTypesJson, constraintsJson);
    }
}
