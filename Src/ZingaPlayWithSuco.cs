using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
                .DeclareVariable("cells", SucoType.Cell.List().List());

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
                catch (SucoCompileException ce) { htmlBlocks.Add(compileExceptionBox(ce)); }
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
