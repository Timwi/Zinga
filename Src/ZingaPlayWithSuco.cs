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
            var variables = req.Post["variables"].Value;
            var code = req.Post["code"].Value;

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

                object parseExceptionBox(SucoParseException exc) =>
                    exceptionBox(exc, (exc.Highlights?.OrderBy(h => h.StartIndex).Select(h => (start: h.StartIndex, end: h.EndIndex)).ToArray() ?? Enumerable.Empty<(int start, int? end)>())
                        .Concat((start: exc.StartIndex, end: exc.EndIndex)).ToArray());
                object compileExceptionBox(SucoCompileException exc) => exceptionBox(exc, new[] { (start: exc.StartIndex, end: exc.EndIndex.Nullable()) });

                var environment = SucoTypeEnvironment.Default;
                if (variables != null)
                    foreach (var (name, type) in variables.Split('\n').Where(v => v.Contains('=')).Select(v => v.Split('=')).Select(ar => (ar[0].Trim(), SucoType.Parse(ar[1].Trim()))))
                        environment = environment.DeclareVariable(name, type);

                // Parse tree
                try
                {
                    var parseTree = SucoParser.ParseCode(code, environment, req.Post["context"].Value == "svg" ? SucoContext.Svg : SucoContext.Constraint);

                    object span(SucoNode node)
                    {
                        var inner = visit(node).ToList();
                        var outer = new List<object>();
                        while (inner.Count > 0 && inner[0] is string firstText && (firstText.Length == 0 || char.IsWhiteSpace(firstText, 0)))
                        {
                            if (string.IsNullOrWhiteSpace(firstText))
                            {
                                if (firstText.Length > 0)
                                    outer.Add(firstText);
                                inner.RemoveAt(0);
                            }
                            else
                            {
                                var i = 0;
                                while (i < firstText.Length && char.IsWhiteSpace(firstText, i)) i++;
                                outer.Add(firstText.Substring(0, i));
                                inner[0] = firstText.Substring(i);
                            }
                        }
                        return new object[] { outer, new SPAN { class_ = "node" }.Data("type", $"{Regex.Replace(node.GetType().Name, @"^Suco|Expression$", "")}{(node is SucoExpression expr ? $" — {expr.Type}" : null)}")._(inner) };
                    }

                    IEnumerable<object> visit(SucoNode expr)
                    {
                        var ix = expr.StartIndex;
                        var properties = expr.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        var collection = properties.Where(p => typeof(SucoNode).IsAssignableFrom(p.PropertyType)).Select(p => (property: p, value: (SucoNode) p.GetValue(expr)))
                            .Concat(properties.Where(p => typeof(IEnumerable<SucoNode>).IsAssignableFrom(p.PropertyType)).SelectMany(p => ((IEnumerable<SucoNode>) p.GetValue(expr)).Select(val => (property: p, value: val))))
                            .Where(expr => expr.value != null)
                            .OrderBy(expr => expr.value.StartIndex);
                        foreach (var (innerProperty, inner) in collection)
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
                        .node {
                            border-color: black;
                            background: #def;
                        }
                        .node::after {
                            content: attr(data-type);
                            position: absolute;
                            left: 50%;
                            transform: translateX(-50%);
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
                    new DIV(new FORM { method = method.post, action = "/play-with-suco" }._(
                        new DIV(new TEXTAREA { accesskey = ",", name = "variables" }._(variables)),
                        new DIV(new TEXTAREA { accesskey = ".", name = "code" }._(code)),
                        new DIV("Context: ",
                            new INPUT { type = itype.checkbox, name = "context", value = "svg" }, " ", new LABEL { accesskey = "s", for_ = "context-svg" }._("SVG".Accel('S')),
                            new INPUT { type = itype.checkbox, name = "context", value = "logic" }, " ", new LABEL { accesskey = "l", for_ = "context-logic" }._("Logic".Accel('L'))),
                        new DIV(new BUTTON { type = btype.submit, accesskey = "p" }._("Parse")))))));
        }
    }
}
