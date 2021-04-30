using Microsoft.JSInterop;

namespace Zinga.Wasm
{
    public static class Commands
    {
        [JSInvokable]
        public static string GetVersion() => "1.0";

        [JSInvokable]
        public static string GetLibVersion() => Lib.Commands.GetVersion();

        [JSInvokable]
        public static string CompileSuco(string suco, string variableTypesJson) => Lib.Commands.CompileSuco(suco, variableTypesJson);

        [JSInvokable]
        public static string RenderConstraintSvgs(string constraintTypesJson, string customConstraintTypesJson, string constraintsJson, int? editingConstraintTypeId, string editingConstraintTypeParameter) =>
            Lib.Commands.RenderConstraintSvgs(constraintTypesJson, customConstraintTypesJson, constraintsJson, editingConstraintTypeId, editingConstraintTypeParameter);

        [JSInvokable]
        public static string GenerateOutline(string regionsJson) => Lib.Commands.GenerateOutline(regionsJson);

        [JSInvokable]
        public static string CheckConstraints(string enteredDigitsJson, string constraintTypesJson, string customConstraintTypesJson, string constraintsJson) => Lib.Commands.CheckConstraints(enteredDigitsJson, constraintTypesJson, customConstraintTypesJson, constraintsJson);
    }
}
