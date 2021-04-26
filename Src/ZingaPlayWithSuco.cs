using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RT.Json;
using RT.Serialization;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Lib;
using Zinga.Suco;

namespace Zinga
{
    public partial class ZingaPropellerModule
    {
        private HttpResponse PlayWithSuco(HttpRequest req)
        {
            List<object> htmlBlocks = null;
            var code = req.Post["code"].Value;
            var environment = new SucoTypeEnvironment()
                // built-ins
                .DeclareVariable("cells", new SucoListType(new SucoListType(SucoCellType.Instance)));

            if (code != null)
            {
                htmlBlocks = new List<object>();

                object exceptionBox(Exception exc, (int start, int? end)[] highlights)
                {
                    var pieces = new List<object>();
                    var ix = 0;
                    for (var i = 0; i < highlights.Length; i++)
                    {
                        if (highlights[i].start > ix)
                            pieces.Add(code.Substring(ix, highlights[i].start - ix));
                        if (highlights[i].end == null)
                            pieces.Add(new STRONG { class_ = "single" });
                        else
                            pieces.Add(new STRONG(code.Substring(highlights[i].start, highlights[i].end.Value - highlights[i].start)));
                        ix = highlights[i].end ?? highlights[i].start;
                    }
                    if (code.Length > ix)
                        pieces.Add(code.Substring(ix));
                    return new DIV { class_ = "exception" }._(
                        new DIV { class_ = "message" }._(exc.Message),
                        new DIV { class_ = "type" }._(exc.GetType().Name),
                        new DIV { class_ = "code" }._(new PRE(pieces)));
                }

                object parseExceptionBox(SucoParseException exc) => exceptionBox(exc, (exc.Highlights?.OrderBy(h => h.StartIndex).Select(h => (start: h.StartIndex, end: h.EndIndex)).ToArray() ?? Enumerable.Empty<(int start, int? end)>()).Concat((start: exc.Index, end: null)).ToArray());
                object compileExceptionBox(SucoCompileException exc) => exceptionBox(exc, new[] { (start: exc.StartIndex, end: exc.EndIndex.Nullable()) });

                // Parse tree
                try
                {
                    var parseTree = SucoParser.ParseCode(code, environment, SucoContext.Svg);

                    object span(SucoNode node) => new SPAN { class_ = "node" }.Data("type", $"{Regex.Replace(node.GetType().Name, @"^Suco|Expression$", "")}{(node is SucoExpression expr ? $" — {expr.Type}" : null)}")._(visit(node));
                    IEnumerable<object> visit(SucoNode expr)
                    {
                        var properties = expr.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        var ix = expr.StartIndex;
                        foreach (var inner in properties.Where(p => typeof(SucoNode).IsAssignableFrom(p.PropertyType)).Select(p => (SucoNode) p.GetValue(expr))
                            .Concat(properties.Where(p => typeof(IEnumerable<SucoNode>).IsAssignableFrom(p.PropertyType)).SelectMany(p => (IEnumerable<SucoNode>) p.GetValue(expr)))
                            .Where(expr => expr != null)
                            .OrderBy(expr => expr.StartIndex))
                        {
                            yield return code.Substring(ix, inner.StartIndex - ix);
                            yield return span(inner);
                            ix = inner.EndIndex;
                        }
                        yield return code.Substring(ix, expr.EndIndex - ix);
                    }
                    htmlBlocks.Add(new PRE { class_ = "parse-tree" }._(span(parseTree)));
                }
                catch (SucoParseException pe) { htmlBlocks.Add(parseExceptionBox(pe)); }
            }

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE("Suco parse tree"),
                    new STYLELiteral(@"
                        body, * {
                            font-family: 'Roboto';
                        }
                        .parse-tree {
                            font-size: 18pt;
                            font-weight: 400;
                        }
                        .node {
                            display: inline-block;
                            padding: .5cm .1cm .1cm;
                            margin: 0 .1cm;
                            border: 1px solid transparent;
                            position: relative;
                            box-sizing: border-box;
                            background: white;
                        }
                        .node:hover {
                            border-color: black;
                            background: #def;
                        }
                        .node:hover::after {
                            content: attr(data-type);
                            position: absolute;
                            left: 0;
                            bottom: calc(100% + 1px);
                            font-size: 9pt;
                            font-weight: 300;
                            background: #bdf;
                            padding: 1px 4px 1px 1px;
                            border: 1px solid black;
                            border-bottom: none;
                            border-top-left-radius: .1cm;
                            border-top-right-radius: .1cm;
                            box-sizing: border-box;
                            z-index: 1;
                        }
                        textarea {
                            width: 100%;
                            height: 15em;
                        }
                        .exception {
                            border: 2px solid black;
                            border-radius: .25cm;
                            overflow: hidden;
                            margin-bottom: 1cm;
                        }
                            .exception .message {
                                background: #fdd;
                                padding: .1cm .25cm;
                                font-weight: bold;
                                text-align: center;
                            }
                            .exception .type {
                                background: #ffd;
                                font-size: 9pt;
                                padding: .05cm .1cm;
                                text-align: center;
                            }
                            .exception .code {
                                padding: .1cm .25cm;
                            }
                        strong {
                            background: #fdd;
                            padding: 0 .05cm;
                        }
                        pre.javascript {
                            white-space: pre-wrap;
                        }
                    ")),
                new BODY(
                    htmlBlocks,
                    new DIV(new FORM { method = method.post, action = "/tmp" }._(
                        new DIV(new TEXTAREA { accesskey = ",", name = "code" }._(code)),
                        new DIV(new BUTTON { type = btype.submit, accesskey = "p" }._("Parse")))))));
        }

        private HttpResponse DebugStuff(HttpRequest req)
        {
            var json =
new string[] { @"[null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,7,8,9,null,null,null,null,null,null,1,null,null,null,null,null,null,null,null,9,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null]", @"{""1"":{""global"":false,""kind"":""SingleCell"",""logic"":""a from ([cell]), $b diagonal: a.value != b.value"",""name"":""Anti-bishop (cell)"",""preview"":null,""public"":true,""svgdefs"":null,""svg"":""\""<path transform='translate({cell.x}, {cell.y})' opacity='.2' d='M.943.932H.826q0-.018-.03-.018L.742.92Q.666.929.647.929q-.059 0-.1-.026Q.509.878.5.834.492.878.452.904q-.04.025-.1.025-.018 0-.095-.01L.205.915q-.03 0-.03.018H.056V.907q0-.025.02-.052Q.096.83.134.815.174.798.23.798q.025 0 .07.006Q.331.81.343.81.375.81.39.803.404.795.423.767.353.755.33.732V.596Q.257.545.257.465q0-.04.02-.074.02-.033.05-.058L.473.21Q.428.189.428.14q0-.03.02-.05Q.47.067.5.067t.051.021Q.572.11.572.14.572.19.527.21q.138.116.162.14.025.022.04.053.014.03.014.063 0 .08-.073.131v.136Q.646.755.577.767.596.795.61.803.625.81.657.81.669.81.7.805.746.8.77.8q.084 0 .127.033.045.033.045.075zM.5.181q.017 0 .029-.012T.54.14Q.54.124.529.112T.5.1Q.483.1.471.112T.46.14q0 .017.012.029T.5.181zm.14.397Q.674.566.694.534q.02-.032.02-.07Q.714.41.642.347L.5.221.393.315Q.336.365.321.383.307.4.296.423q-.01.022-.01.042 0 .037.02.07.021.03.053.043Q.447.554.5.554q.052 0 .14.024zM.58.44H.515v.064h-.03V.44H.42V.41h.064V.347h.03V.41H.58zm.06.263V.675L.615.646.64.63V.608Q.57.584.5.584T.36.608v.02l.026.018L.36.675v.028Q.408.686.5.686q.092 0 .14.017zM.544.633L.5.662.457.634.5.605zm.08.094Q.592.715.5.715T.377.727Q.417.744.5.744q.083 0 .122-.017zm.29.175Q.895.828.773.828q-.017 0-.061.007Q.67.84.655.84.615.84.592.823.569.805.547.77H.515Q.515.9.647.9.663.899.736.89l.06-.006q.028 0 .046.018zM.484.77H.453Q.431.805.408.823.385.84.345.84.33.84.288.835.243.828.226.828q-.122 0-.139.074h.07Q.176.884.205.884l.06.006Q.335.9.352.9.485.899.485.77z'/>\"""",""variables"":{""cell"":""cell""}},""2"":{""global"":false,""kind"":""SingleCell"",""logic"":""a from ([cell]), $b adjacent: a.value != b.value"",""name"":""Anti-king (cell)"",""preview"":null,""public"":true,""svgdefs"":null,""svg"":""\""<path transform='translate({cell.x}, {cell.y})' opacity='.2' d='M.614.325Q.672.265.77.265q.076 0 .124.043.049.045.049.113 0 .132-.155.216v.205q0 .042-.092.07Q.604.941.5.941.396.94.304.912.212.884.212.842V.637Q.057.553.057.421q0-.068.049-.113Q.154.264.23.264q.098 0 .156.061.03-.052.076-.07H.397V.059h.206v.196H.537q.048.018.077.07zM.547.089H.453L.5.136zm.025.12V.106l-.05.051zM.48.157L.428.107v.102zm.068.069L.5.179.453.226zm.044.12Q.578.316.552.296.526.273.5.273T.448.295Q.422.316.409.346q.022.022.05.075.029.053.041.1.012-.047.04-.1Q.568.368.59.346zm.173.271Q.834.582.873.531q.04-.052.04-.11T.872.329Q.833.294.77.294q-.174 0-.246.254.154.007.24.07zM.476.547Q.404.295.23.295.166.294.127.33q-.04.035-.04.092 0 .058.04.11.04.051.11.086.085-.062.239-.07zm.282.234V.758L.718.695.758.67V.655Q.732.62.66.599.589.578.5.578q-.089 0-.16.021-.072.02-.098.056V.67l.04.025-.04.063V.78Q.348.723.5.723q.152 0 .258.058zM.566.646L.5.693.434.646.5.604zM.5.91Q.578.91.668.885q.09-.024.09-.053 0-.026-.082-.053Q.594.753.5.753q-.094 0-.177.026Q.242.806.242.832q0 .029.09.053Q.422.91.5.91z'/>\"""",""variables"":{""cell"":""cell""}},""3"":{""global"":false,""kind"":""SingleCell"",""logic"":""a from ([cell]), $b ([(a.x-b.x).abs, (a.y-b.y).abs].contains([1, 2])): a.value != b.value"",""name"":""Anti-knight (cell)"",""preview"":null,""public"":true,""svgdefs"":null,""svg"":""\""<path transform='translate({cell.x}, {cell.y})' opacity='.2' d='M.946.946H.3q0-.097.025-.15Q.35.746.412.71q.08-.045.08-.115Q.491.58.48.562.453.585.333.614q-.041.011-.05.08Q.276.726.26.744.245.762.223.762.167.762.11.717.054.67.054.615q0-.044.06-.125Q.159.431.17.408.182.384.186.35.191.312.196.297T.217.255Q.242.221.248.196.253.172.253.13V.065q.049.02.094.1L.376.164q.02-.035.03-.11.036.016.066.063l.043.065Q.739.21.842.347q.104.137.104.465zM.892.916V.834q0-.33-.091-.463T.502.21Q.49.203.469.166.442.117.424.106L.41.18Q.41.192.398.214q-.01.02-.023.02-.01 0-.01-.015 0-.009.004-.021L.3.23.298.219Q.31.202.323.197q0-.036-.04-.073 0 .058-.007.083Q.27.232.244.272.228.298.224.31.218.323.215.358.214.383.203.408q-.011.024-.064.1Q.105.555.094.574q-.01.02-.01.037 0 .037.025.063.026.027.04.027.009 0 .022-.025Q.194.63.209.63q.017 0 .017.019 0 .015-.014.035Q.197.7.187.72q.015.011.034.011.024 0 .032-.042Q.266.603.318.588L.397.567Q.437.555.458.543.48.531.49.518.501.505.521.466L.53.473Q.52.505.52.53L.52.595q0 .087-.09.137Q.38.76.359.802.336.844.336.917zM.357.344Q.337.347.324.358l.002.02q0 .007-.018.017L.285.392.281.398v.023Q.257.4.257.389q0-.019.027-.037Q.31.333.329.333h.024zM.154.58Q.15.595.138.604l.015.018q0 .01-.006.015Q.14.643.133.643.117.643.117.618q0-.016.01-.026Q.137.58.154.58z'/>\"""",""variables"":{""cell"":""cell""}},""4"":{""global"":false,""kind"":""Path"",""logic"":""a, b ~: a.value < b.value"",""name"":""Thermometer"",""preview"":null,""public"":true,""svgdefs"":null,""svg"":""\""<g opacity='.2'>\r\n                        <path d='M{{c: \"" {c.cx} {c.cy}\""}}' stroke='black' stroke-width='.3' stroke-linecap='round' stroke-linejoin='round' fill='none' />\r\n                        <circle cx='{{c first: c.cx}}' cy='{{c first: c.cy}}' r='.4' fill='black' />\r\n                    </g>\"""",""variables"":{""cells"":""list(cell)""},""shortcut"":""t""},""5"":{""global"":false,""kind"":""Path"",""logic"":""s first: {c after}.sum = s.value"",""name"":""Arrow (1-cell sum)"",""preview"":null,""public"":true,""svgdefs"":null,""svg"":""{f first, s ~, +sl, l ~ last: let endAngle = (l.y-sl.y).atan2(l.x-sl.x); \r\n                    \""<g fill='none' stroke='black' stroke-width='.05' opacity='.2'>\r\n                        <circle cx='{f.cx}' cy='{f.cy}' r='.4' />\r\n                        <path d='M{let angle = (s.y-f.y).atan2(s.x-f.x); \""{f.cx + .4*angle.cos} {f.cy + .4*angle.sin}\""}\r\n                                    {{a (a.pos > 1 & a.pos < cells.count): \"" {a.cx} {a.cy}\""}}\r\n                                    {l.cx + .3*endAngle.cos} {l.cy + .3*endAngle.sin}' />\r\n                        <path d='M -.2 -.2 .3 0 -.2 .2' transform='translate({l.cx}, {l.cy}) rotate({endAngle})' />\r\n                    </g>\""}"",""variables"":{""cells"":""list(cell)""},""shortcut"":""a""},""6"":{""global"":false,""kind"":""Region"",""logic"":""cells.unique"",""name"":""Killer cage (no sum)"",""preview"":null,""public"":true,""svgdefs"":null,""svg"":""\""<path d='{cells.outline(.06, .06)}' {shaded ? \""fill='rgba(0, 0, 0, .2)'\"" : \""fill='none' stroke='black' stroke-width='.025' stroke-dasharray='.09,.07'\""} />\"""",""variables"":{""cells"":""list(cell)"",""shaded"":""bool""}},""7"":{""global"":false,""kind"":""Region"",""logic"":""cells.sum = sum & cells.unique"",""name"":""Killer cage (sum)"",""preview"":null,""public"":true,""svgdefs"":null,""svg"":""{f topleft: \""<path d='{cells.outline(.06, .06, .275, .25)}' fill='none' stroke='black' stroke-width='.025' stroke-dasharray='.09,.07' /><text x='{f.x + .04}' y='{f.y + .25}' text-anchor='start' font-size='.25'>{sum}</text>\""}"",""variables"":{""cells"":""list(cell)"",""sum"":""int""},""shortcut"":""k""},""9"":{""global"":false,""kind"":""MatchingRegions"",""logic"":""f first, oth: {a from f, b from oth (b.pos = a.pos): b.value - a.value}.same"",""name"":""Snowball regions"",""preview"":null,""public"":true,""svgdefs"":""[\""<filter id='snowball-filter' color-interpolation-filters='sRGB'><feGaussianBlur result='fbSourceGraphic' stdDeviation='.03' /></filter>\""] +\r\n                        {region: let p = region.outline(0, 0); \""<clipPath id='snowball-clip-{p.hash}' clipPathUnits='userSpaceOnUse'><path d='{p}' /></clipPath>\""}"",""svg"":""region: let p = region.outline(0, 0);\r\n                        \""<path d='{p}' fill='none' opacity='.5' stroke='black' stroke-width='.08' filter='url(#snowball-filter)' clip-path='url(#snowball-clip-{p.hash})' />\"""",""variables"":{""cells"":""list(list(cell))""}},""11"":{""global"":false,""kind"":""Region"",""logic"":""cells.unique & {c ({d: d.value = c.value + 1}.none)}.count = 1"",""name"":""Renban cage"",""preview"":null,""public"":true,""svgdefs"":""[\""<pattern id='renban-pattern' width='2' height='2' patternTransform='rotate(45) scale(.35355) translate(.5, .5)' patternUnits='userSpaceOnUse'><path d='M0 0h1v1H0zM1 1h1v1H1z' /></pattern>\""]"",""svg"":""\""<path d='{cells.outline(.25, .25)}' fill='url(#renban-pattern)' stroke='none' opacity='.2' />\"""",""variables"":{""cells"":""list(cell)""},""shortcut"":""r""}}", @"[{""type"":1,""values"":{""cell"":1}},{""type"":2,""values"":{""cell"":15}},{""type"":3,""values"":{""cell"":59}},{""type"":4,""values"":{""cells"":[2,12,21,20,30]}},{""type"":5,""values"":{""cells"":[42,52,61,60,69]}},{""type"":6,""values"":{""cells"":[47,46,45,54,63],""shaded"":true}},{""type"":7,""values"":{""cells"":[50,41,32,33,34],""sum"":27}},{""type"":9,""values"":{""cells"":[[27,36,37],[65,74,75]]}},{""type"":11,""values"":{""cells"":[39,40,49,58,57,67,68]}}]" }
;
            var result = Commands.CheckConstraints(json[0], json[1], json[2]);
            return HttpResponse.PlainText(result);
        }

        private HttpResponse PlayWithSuco2(HttpRequest req)
        {
            string results = null;
            if (req.Method == HttpMethod.Post)
                results = Commands.CompileSuco(req.Post["code"].Value, @"{""cells"":""list(cell)""}");

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE("Suco play thing"),
                    new STYLELiteral(@"
                        body, button, input {
                            font-family: 'Roboto';
                        }
                        body {
                            background: red;
                        }
                        body.ready {
                            background: white;
                        }
                        textarea {
                            width: 100%;
                            height: 15em;
                        }
                        pre.javascript {
                            white-space: pre-wrap;
                        }
                        #results {
                            border: 2px solid black;
                        }
                            #results.error {
                                background: #fee;
                            }
                            #results > .error {
                                background: #fdd;
                                border-bottom: 1px solid black;
                                padding: 5px 10px;
                                text-align: center;
                            }
                            #results > .code {
                                font-family: monospace;
                                padding: 5px 10px;
                            }
                                #results > .code .mark {
                                    background: #800;
                                    color: white;
                                    padding: 1px 4px;
                                    font-weight: bold;
                                }
                                #results > .code .marker {
                                    background: #800;
                                    padding: 1px 4px;
                                }
                    "),
                    new RawTag(@"<script src=/_framework/blazor.webassembly.js autostart=false></script>"),
                    new SCRIPTLiteral(@"
                        Blazor.start({ })
                            .then(() => {
                                document.body.classList.add('ready');

                                document.getElementById('btn-wasm').onclick = function(ev)
                                {
                                    let results = document.getElementById('results');
                                    let code = document.getElementById('code').value;
                                    results.innerHTML = `<pre class='code'></pre>`;
                                    results.querySelector('.code').innerText = code;
                                    DotNet.invokeMethodAsync('ZingaWasm', 'CompileSuco', code, JSON.stringify({""cells"":""list(cell)""}))
                                        .then(resultStr => {
                                            let result = JSON.parse(resultStr);
                                            if (result.status === 'ok')
                                                results.className = 'ok';
                                            else
                                            {
                                                results.className = 'error';
                                                results.innerHTML = `<div class='error'></div><pre class='code'></pre>`;
                                                results.querySelector('.error').innerText = result.message;
                                                results.querySelector('.code').innerHTML = result.html || result.type;
                                            }
                                        });
                                    ev.stopPropagation();
                                    ev.preventDefault();
                                    return false;
                                };
                            });
                    ")),
                new BODY(
                    new DIV { id = "results" }._(results),
                    new DIV(new FORM { method = method.post, action = "/tmp" }._(
                        new DIV(new TEXTAREA { accesskey = ",", name = "code", id = "code" }),
                        new DIV(
                            new BUTTON { type = btype.button, accesskey = "w", id = "btn-wasm" }._("Wasm"),
                            new BUTTON { type = btype.submit, accesskey = "r" }._("Remote")))))));
        }
    }
}
