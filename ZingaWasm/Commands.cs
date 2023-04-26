using Microsoft.JSInterop;

namespace Zinga.Wasm
{
    public static class Commands
    {
        [JSInvokable]
        public static string CompileSuco(string suco, string variableTypesJson) => Lib.Commands.CompileSuco(suco, variableTypesJson);

        [JSInvokable]
        public static string RenderConstraintSvgs(string constraintTypesJson, string stateJson) =>
            Lib.Commands.RenderConstraintSvgs(constraintTypesJson, stateJson);

        [JSInvokable]
        public static string GenerateOutline(string regionsJson, int width, int height) => Lib.Commands.GenerateOutline(regionsJson, width, height);

        [JSInvokable]
        public static void SetupConstraints(string constraintTypesJson, string stateJson) => Lib.Commands.SetupConstraints(constraintTypesJson, stateJson);

        [JSInvokable]
        public static string CheckConstraints(string enteredDigitsJson, string constraintsJson) => Lib.Commands.CheckConstraints(enteredDigitsJson, constraintsJson);
    }
}
