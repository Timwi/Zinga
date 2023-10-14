using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using RT.Json;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Database;

namespace Zinga
{
    public partial class ZingaPropellerModule
    {
        private static (ConstraintKind value, string name)[] _constraintKindCache;

        public HttpResponse PuzzleEditPage(HttpRequest req)
        {
            if (_constraintKindCache == null)
                lock (this)
                    _constraintKindCache ??= typeof(ConstraintKind)
                        .GetFields(BindingFlags.Static | BindingFlags.Public)
                        .Select(f => (value: (ConstraintKind) f.GetValue(null), name: f.GetCustomAttribute<ConstraintKindInfoAttribute>()?.Name))
                        .Where(tup => tup.name != null)
                        .ToArray();

            using var db = new Db();

            var constraintTypes = db.Constraints.Where(c => c.Public).ToDictionary(c => c.ConstraintID);

            var constraintTypesJson = constraintTypes.ToJsonDict(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToJson()).ToString();

            const bool autoCss = true;
            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE($"Editing: Sudoku by unknown"),
                    new RawTag(@"<script src='/_framework/blazor.webassembly.js' autostart='false'></script>"),

#if DEBUG
                    new SCRIPTLiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "EditPuzzle.js"))),
                    new STYLELiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Font.css"))),
                    autoCss ? Ut.NewArray<object>(
                        new STYLE { id = "auto-css" },
                        new SCRIPTLiteral($@"
                            (function() {{
                                let socket = new WebSocket('ws://{req.Url.Domain}:{req.Url.Port}/css-websocket');
                                socket.onopen = function()
                                {{
                                    socket.send('css');
                                    window.setInterval(function() {{ socket.send('css'); }}, 500);
                                }};
                                socket.onclose = function()
                                {{
                                    console.log('Socket closed.');
                                }};
                                socket.onmessage = function(msg)
                                {{
                                    document.getElementById('auto-css').innerText = msg.data.replace(/\r|\n/g, ' ');
                                }};
                            }})();
                        ")
                    ) : new STYLELiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Puzzle.css"))),
#else
                    new LINK { rel = "stylesheet", href = "/font" },
                    new SCRIPTLiteral(Resources.EditJs),
                    new STYLELiteral(Resources.Css),
#endif
                    new LINK { rel = "shortcut icon", type = "image/png", href = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAABAAAAAQAAQMAAABF07nAAAAABlBMVEUAAAD///+l2Z/dAAACFElEQVR42u3YsQ2AMBAEwZMIKINS3RplERm38ERvodn4gokvkSRJkiRJ2qHxFrqTXJXhk+SsDCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAJEmSJEmStslFAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8EPA8Q0gSZIkSZLUflD4iAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAANoBkiRJkiRJnS37yw5ZFqD7+QAAAABJRU5ErkJggg==" }),
                new BODY { class_ = "is-puzzle" }._(
                    new DIV { id = "topbar" }._(
                        new DIV { class_ = "title" }._("Sudoku"),
                        new DIV { class_ = "author" }._("by unknown")),
                    new DIV { id = "puzzle" }.Data("constrainttypes", constraintTypesJson)._(
                        new DIV { id = "puzzle-container", tabindex = 0, accesskey = "," }._(new RawTag($@"
                            <svg xmlns='http://www.w3.org/2000/svg' viewBox='-0.5 -0.5 10 11.2' text-anchor='middle' font-family='Bitter' id='puzzle-svg'>
                                <style></style>
                                <defs>
                                    <filter id='glow-blur'><feGaussianBlur stdDeviation='.1' /></filter>
                                    <filter id='constraint-selection-shadow' x='-1' y='-1' width='500%' height='500%' filterUnits='userSpaceOnUse'>
                                        <feMorphology in='SourceGraphic' operator='dilate' radius='.05' result='constraint-selection-shadow-1' />
                                        <feColorMatrix in='constraint-selection-shadow-1' type='matrix' values='0 0 0 0 .1 0 0 0 0 .39 0 0 0 0 .95 0 0 0 5 0' result='constraint-selection-shadow-2' />
                                        <feGaussianBlur in='constraint-selection-shadow-2' stdDeviation='.05' result='constraint-selection-shadow-3' />
                                        <feColorMatrix in='SourceGraphic' type='matrix' values='.52 0 0 0 0 0 .67 0 0 0 0 0 .97 0 0 0 0 0 5 0' result='constraint-selection-shadow-4' />
                                        <feComposite in='constraint-selection-shadow-4' in2='constraint-selection-shadow-3' />
                                    </filter>
                                    <marker id='selection-arrow-marker' orient='auto' overflow='visible'><path fill='hsl(220, 80%, 80%)' d='M-1-2.4 5 0-1 2.4z'/></marker>
                                </defs>
                                <defs id='constraint-defs'></defs>
                                <g id='bb-everything'>
                                    <g id='bb-buttons'><g id='bb-buttons-scaler' font-size='.55' text-anchor='middle'></g></g>
                                    <g id='constraint-svg-global'></g>

                                    <g id='bb-puzzle'>
                                        <g id='puzzle-cells'></g>

                                        <path id='puzzle-frame' fill='none' stroke='black' stroke-width='.05' />
                                        <path id='puzzle-lines' fill='none' stroke='black' stroke-width='.01' />

                                        <g id='constraint-svg'></g>
                                        <g id='outline-svg'></g>
                                        <g id='selection-arrows-svg' fill='none' stroke='hsl(220, 80%, 80%)' stroke-width='.03' marker-end='url(#selection-arrow-marker)'></g>
                                        <g id='multi-selects' fill='black'></g>
                                    </g>
                                </g>
                            </svg>")),
                        new DIV { id = "sidebar", tabindex = 0, accesskey = "." }._(
                            new DIV { class_ = "tabs" }._(
                                new DIV { class_ = "tab tab-puzzle", accesskey = "p", tabindex = -1 }.Data("tab", "puzzle")._("Puzzle".Accel('P')),
                                new DIV { class_ = "tab tab-constraints", accesskey = "c", tabindex = -1 }.Data("tab", "constraints")._("Constraints".Accel('C'))),
                            new DIV { class_ = "tabc", id = "tab-puzzle" }._(
                                new SECTION(
                                    new DIV { class_ = "label" }._("Title"),
                                    new DIV(new INPUT { type = itype.text, id = "puzzle-title-input" })),
                                new SECTION(
                                    new DIV { class_ = "label" }._("Author(s)"),
                                    new DIV(new INPUT { type = itype.text, id = "puzzle-author-input" })),
                                new SECTION(
                                    new DIV { class_ = "label" }._("Rules"),
                                    new DIV(new TEXTAREA { id = "puzzle-rules-input", accesskey = "/" })),
                                new SECTION(
                                    new DIV { class_ = "btns" }._(new BUTTON { id = "link-add", class_ = "mini-btn add", title = "Add a link (for example, to Logic Masters Germany)" }),
                                    new DIV { class_ = "label" }._("Links"),
                                    new TABLE { id = "links" }),
                                new SECTION(
                                    new DIV { class_ = "label" }._("Grid size"),
                                    new DIV { id = "grid-size" }._(
                                        new LABEL { for_ = "puzzle-width-input", accesskey = "w" }._("Width:".Accel('W')), " ", new INPUT { type = itype.number, min = "1", step = "1", id = "puzzle-width-input" }, " ",
                                        new LABEL { for_ = "puzzle-height-input", accesskey = "h" }._("Height:".Accel('H')), " ", new INPUT { type = itype.number, min = "1", step = "1", id = "puzzle-height-input" })),
                                new SECTION(
                                    new DIV { class_ = "btns" }._(new BUTTON { id = "value-add", class_ = "mini-btn add", title = "Add a new value" }),
                                    new DIV { class_ = "label" }._("Values and givens"),
                                    new DIV { id = "givens" },
                                    new DIV { id = "givens-presets", class_ = "buttons" }),
                                new SECTION(
                                    new DIV { class_ = "btns" }._(new BUTTON { id = "region-add", class_ = "mini-btn add", title = "Add a new region (Alt+R)", accesskey = "r" }),
                                    new DIV { class_ = "label" }._("Regions"),
                                    new DIV { id = "regions" },
                                    new DIV { id = "region-buttons", class_ = "buttons" }._(
                                        new SPAN { id = "region-presets" },
                                        new BUTTON { id = "region-remove-all" }._("Remove all"),
                                        new BUTTON { id = "region-fill" }._("Fill the rest")),
                                    new DIV { id = "region-options" }._(
                                        new DIV(new INPUT { id = "puzzle-rows-unique", type = itype.checkbox }, " ", new LABEL { for_ = "puzzle-rows-unique" }._("Rows must have unique values")),
                                        new DIV(new INPUT { id = "puzzle-columns-unique", type = itype.checkbox }, " ", new LABEL { for_ = "puzzle-columns-unique" }._("Columns must have unique values")))),
                                new SECTION(
                                    new DIV { class_ = "label" }._("Save"),
                                    new DIV { class_ = "save-section buttons" }._(
                                        new BUTTON { id = "puzzle-test", accesskey = "t" }._("Test puzzle".Accel('T')),
                                        new BUTTON { id = "puzzle-save", accesskey = "s" }._("Publish puzzle".Accel('s')),
                                        new DIV("Publishing...")))),
                            new DIV { class_ = "tabc", id = "tab-constraints" }._(
                                new SECTION { id = "constraints-section" }._(
                                    new DIV { class_ = "btns" }._(
                                        new BUTTON { id = "constraint-dup", class_ = "mini-btn", title = "Duplicate selected constraints (Ctrl+D)" },
                                        new BUTTON { id = "constraint-select-similar", class_ = "mini-btn", title = "Select constraints of the same type (Alt+M)", accesskey = "m" },
                                        new BUTTON { id = "constraint-move-up", class_ = "mini-btn", title = "Move selected constraints up in the list" },
                                        new BUTTON { id = "constraint-move-down", class_ = "mini-btn", title = "Move selected constraints down in the list" }),
                                    new DIV { class_ = "label" }._("Constraints"),
                                    new DIV { id = "constraint-list" }),
                                new SECTION { id = "constraint-code-section" }._(
                                    new DIV { class_ = "label" }._("Edit constraint code", new BUTTON { class_ = "expand", accesskey = "e" }),
                                    new DIV { class_ = "constraint-code" }._(
                                        new DIV { class_ = "label" }._("Name"),
                                        new DIV(new INPUT { type = itype.text, id = "constraint-code-name" }),
                                        new DIV { class_ = "label" }._("Kind"),
                                        new DIV(new SELECT { id = "constraint-code-kind" }._(_constraintKindCache.Select(tup => new OPTION { value = tup.value.ToString() }._(tup.name)))),
                                        new DIV { class_ = "label" }._("Properties", new BUTTON { id = "constraint-code-addvar", class_ = "mini-btn add", title = "Add a new property" }),
                                        new DIV(new TABLE { id = "constraint-code-variables" }),
                                        new DIV { class_ = "label" }._("Logic (Suco)"),
                                        new DIV(new TEXTAREA { id = "constraint-code-logic" }, new DIV { id = "reporting-logic", class_ = "reporting" }),
                                        new DIV { class_ = "label" }._("Code to generate SVG (Suco)"),
                                        new DIV(new TEXTAREA { id = "constraint-code-svg" }, new DIV { id = "reporting-svg", class_ = "reporting" }),
                                        new DIV { class_ = "label" }._("Code to generate SVG definitions (Suco)"),
                                        new DIV(new TEXTAREA { id = "constraint-code-svgdefs" }, new DIV { id = "reporting-svgdefs", class_ = "reporting" }))),
                                new SECTION { id = "constraint-add-section" }._(
                                    new DIV { class_ = "label" }._("Add a constraint"),
                                    new DIV { class_ = "main" }._(
                                        new UL(
                                            new LI("Select a set of cells in the grid."),
                                            new LI("Press a lower-case letter to add one of the common constraints listed below."),
                                            new LI("Press Shift with a letter to ", new A { href = "#", id = "constraint-search-link" }._("search for more constraints"), ".")),
                                        new HR(),
                                        new P("Shortcuts for common constraints:"),
                                        new TABLE(
                                            constraintTypes.Values.Where(c => c.Shortcut != null).OrderBy(c => c.Shortcut).Select(c => new TR(new TH(c.Shortcut), new TD(new A { href = "#", class_ = "constraint-shortcut" }.Data("id", c.ConstraintID)._(c.Name)))),
                                            new TR { id = "add-last-selected-constraint" }._(new TH('='), new TD(new A { href = "#", id = "constraint-shortcut-last" }._("Last selected constraint")))))))),
                        new DIV { class_ = "focus-catcher", tabindex = 0 }),

                    new DIV { id = "constraint-search" }._(
                            new DIV { id = "constraint-search-box" }._(new INPUT { id = "constraint-search-input", type = itype.text }),
                            new DIV { id = "constraint-results-box" },
                            new DIV { id = "constraint-button-row" }._(new BUTTON { type = btype.button, id = "constraint-search-ok" }._("OK"), new BUTTON { type = btype.button, id = "constraint-search-cancel" }._("Cancel"))))));
        }
    }
}