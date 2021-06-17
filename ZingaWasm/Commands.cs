using Microsoft.JSInterop;

namespace Zinga.Wasm
{
    public static class Commands
    {
        [JSInvokable]
        public static string CompileSuco(string suco, string variableTypesJson) => Lib.Commands.CompileSuco(suco, variableTypesJson);

        [JSInvokable]
        public static string RenderConstraintSvgs(string constraintTypesJson, string customConstraintTypesJson, string constraintsJson) =>
            Lib.Commands.RenderConstraintSvgs(constraintTypesJson, customConstraintTypesJson, constraintsJson);

        [JSInvokable]
        public static string GenerateOutline(string regionsJson) => Lib.Commands.GenerateOutline(regionsJson);

        [JSInvokable]
        public static void SetupConstraints(string givensJson, string constraintTypesJson, string customConstraintTypesJson, string constraintsJson) => Lib.Commands.SetupConstraints(givensJson, constraintTypesJson, customConstraintTypesJson, constraintsJson);

        [JSInvokable]
        public static string CheckConstraints(string enteredDigitsJson, string constraintsJson) => Lib.Commands.CheckConstraints(enteredDigitsJson, constraintsJson);
    }
}
