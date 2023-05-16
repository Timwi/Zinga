using Microsoft.JSInterop;

namespace Zinga.Wasm
{
    public static class Commands
    {
        [JSInvokable]
        public static void SetupConstraints(string constraintTypesJson, string stateJson) => Lib.Commands.SetupConstraints(constraintTypesJson, stateJson);

        [JSInvokable]
        public static string CheckConstraints(string enteredDigitsJson) => Lib.Commands.CheckConstraints(enteredDigitsJson);

        [JSInvokable]
        public static string RenderPuzzleSvg(int width, int height, string regionsJson, bool rowsUnique, bool columnsUnique, string valuesJson, string constraintTypesJson, string customConstraintTypesJson, string constraintsJson) =>
            Lib.Commands.RenderPuzzleSvg(width, height, regionsJson, rowsUnique, columnsUnique, valuesJson, constraintTypesJson, customConstraintTypesJson, constraintsJson);

        // EDIT page only
        [JSInvokable]
        public static string GenerateOutline(string regionsJson, int width, int height) => Lib.Commands.GenerateOutline(regionsJson, width, height);

        [JSInvokable]
        public static string RenderConstraintSvgs(string constraintTypesJson, string stateJson) => Lib.Commands.RenderConstraintSvgs(constraintTypesJson, stateJson);
    }
}
