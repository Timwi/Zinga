using System;
using System.Data;
using System.Linq;
using System.Transactions;
using RT.Json;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Database;
using Zinga.Lib;
using Zinga.Suco;
using DbConstraint = Zinga.Database.Constraint;

namespace Zinga
{
    public partial class ZingaPropellerModule
    {
        public HttpResponse PuzzlePublish(HttpRequest req)
        {
            if (req.Method != HttpMethod.Post)
                return HttpResponse.Empty(HttpStatusCode._405_MethodNotAllowed);

            var jsonRaw = req.Post["puzzle"].Value;
            if (jsonRaw == null || !JsonDict.TryParse(jsonRaw, out var json))
                return HttpResponse.PlainText("The data transmitted is not valid JSON.", HttpStatusCode._400_BadRequest);

            var constraintIx = -1;
            JsonList constraints = null;
            var errorLocation = 0;
            try
            {
                var puzzle = new Puzzle();
                puzzle.UrlName = MD5.ComputeUrlName(jsonRaw);

                errorLocation = 1;
                using var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable });
                using var db = new Db();

                errorLocation = 2;
                if (db.Puzzles.Any(p => p.UrlName == puzzle.UrlName))
                    return HttpResponse.PlainText(puzzle.UrlName);

                errorLocation = 3;
                puzzle.Title = json["title"].GetString();
                puzzle.Author = json["author"].GetString();
                puzzle.Rules = json["rules"].GetString();
                puzzle.Links = json["links"]?.GetList().Select(lnk => new Link { Text = lnk["text"].GetString(), Url = lnk["url"].GetString() }).ToArray();
                puzzle.Info = new PuzzleInfo(
                    width: json["width"].GetInt(),
                    height: json["height"].GetInt(),
                    regions: json["regions"].GetList().Select(inner => inner.GetList().Select(v => v.GetInt()).ToArray()).ToArray(),
                    rowsUnique: json["rowsUniq"].GetBool(),
                    columnsUnique: json["colsUniq"].GetBool(),
                    values: json["values"].GetList().Select(v => v.GetInt()).ToArray());

                errorLocation = 4;
                var givens = json["givens"].GetList().Select((v, ix) => (v, ix)).Where(tup => tup.v != null).Select(tup => (cell: tup.ix, value: tup.v.GetInt())).ToArray();
                var invalidGiven = givens.FirstOrNull(given => given.cell < 0 || given.cell >= puzzle.Info.Width * puzzle.Info.Height);
                if (invalidGiven != null)
                    return HttpResponse.PlainText($"At least one given is out of range (cell {invalidGiven.Value.cell}).", HttpStatusCode._400_BadRequest);
                puzzle.Givens = givens.Length > 0 ? givens : null;

                errorLocation = 5;
                constraints = json["constraints"].GetList();
                var customConstraintTypes = json["customConstraintTypes"].GetList();

                errorLocation = 6;
                var dbConstraintTypes = constraints.Select(c => c["type"].GetInt()).Where(c => c >= 0).Distinct().ToArray()
                    .Apply(cIds => db.Constraints.Where(c => cIds.Contains(c.ConstraintID))).AsEnumerable().ToDictionary(c => c.ConstraintID);
                var faultyId = constraints.Select(c => c["type"].GetInt()).Where(c => c >= 0).FirstOrNull(c => !dbConstraintTypes.ContainsKey(c));
                if (faultyId != null)
                    return HttpResponse.PlainText($"Unknown constraint type ID: {faultyId.Value}.", HttpStatusCode._400_BadRequest);

                errorLocation = 7;
                // Add the custom constraint types into the database
                for (constraintIx = 0; constraintIx < constraints.Count; constraintIx++)
                {
                    var constraint = constraints[constraintIx];
                    if (constraint["type"].GetInt() is int typeId && typeId < 0 && !dbConstraintTypes.ContainsKey(typeId))
                    {
                        if (~typeId >= customConstraintTypes.Count || customConstraintTypes[~typeId] == null)
                            return HttpResponse.PlainText($"Undefined custom constraint type: {typeId}. List has {customConstraintTypes.Count} entries.", HttpStatusCode._400_BadRequest);
                        var cType = customConstraintTypes[~typeId];
                        var kind = EnumStrong.Parse<ConstraintKind>(cType["kind"].GetString());

                        // Some verifications:
                        // Make sure the variable types parse as valid Suco types
                        var env = new SucoTypeEnvironment();
                        foreach (var (varName, varType) in cType["variables"].GetDict())
                        {
                            if (!SucoType.TryParse(varType.GetString(), out var type))
                                return HttpResponse.PlainText($"Unrecognized Suco type: {varType.GetString()}.", HttpStatusCode._400_BadRequest);
                            env = env.DeclareVariable(varName, type);
                        }
                        // Make sure all the Suco code compiles
                        static string nullIfEmpty(string str) => string.IsNullOrWhiteSpace(str) ? null : str;
                        var logicCode = nullIfEmpty(cType["logic"]?.GetString());
                        var svgCode = nullIfEmpty(cType["svg"]?.GetString());
                        var svgDefsCode = nullIfEmpty(cType["svgdefs"]?.GetString());

                        if (!SucoParser.IsValidCode(logicCode, env, SucoContext.Constraint, SucoType.Boolean, out string error))
                            return HttpResponse.PlainText($"The Suco code for the constraint logic in “{cType["name"].GetString()}” doesn’t compile: {error}", HttpStatusCode._400_BadRequest);
                        if (svgCode != null && !SucoParser.IsValidCode(svgCode, env, SucoContext.Svg, SucoType.String, out error))
                            return HttpResponse.PlainText($"The Suco code for generating SVG code in “{cType["name"].GetString()}” doesn’t compile: {error}", HttpStatusCode._400_BadRequest);
                        if (svgDefsCode != null && !SucoParser.IsValidCode(svgDefsCode, env, SucoContext.Svg, SucoType.String.List(), out error))
                            return HttpResponse.PlainText($"The Suco code for generating SVG definitions in “{cType["name"].GetString()}” doesn’t compile: {error}", HttpStatusCode._400_BadRequest);

                        var newConstraintType = new DbConstraint
                        {
                            Kind = kind,
                            LogicSuco = logicCode,
                            Name = cType["name"].GetString(),
                            Public = false,
                            Shortcut = null,
                            SvgDefsSuco = svgDefsCode,
                            SvgSuco = svgCode,
                            VariablesJson = cType["variables"].ToString()
                        };
                        var already = db.Constraints.FirstOrDefault(c => c.Kind == newConstraintType.Kind && c.LogicSuco == newConstraintType.LogicSuco &&
                            c.Name == newConstraintType.Name && c.SvgDefsSuco == newConstraintType.SvgDefsSuco &&
                            c.SvgSuco == newConstraintType.SvgSuco && c.VariablesJson == newConstraintType.VariablesJson);
                        if (already != null)
                            dbConstraintTypes[typeId] = already;
                        else
                        {
                            db.Constraints.Add(newConstraintType);
                            dbConstraintTypes[typeId] = newConstraintType;
                        }
                    }
                }

                errorLocation = 8;
                db.Puzzles.Add(puzzle);
                db.SaveChanges();   // This assigns new IDs to all the new constraint types and the puzzle itself

                errorLocation = 9;
                foreach (var constraint in constraints)
                    db.PuzzleConstraints.Add(new PuzzleConstraint
                    {
                        PuzzleID = puzzle.PuzzleID,
                        ConstraintID = dbConstraintTypes[constraint["type"].GetInt()].ConstraintID,
                        ValuesJson = constraint["values"].ToString()
                    });

                errorLocation = 10;
                db.SaveChanges();
                tr.Complete();
                return HttpResponse.PlainText(puzzle.UrlName);
            }
            catch (Exception e)
            {
                return HttpResponse.PlainText($"{e.Message}{(constraintIx > -1 && constraints != null ? $"\nPossible culprit: constraint #{constraintIx + 1}" : "")} ({errorLocation})\n\nJSON:\n{json.ToStringIndented()}", HttpStatusCode._400_BadRequest);
            }
        }
    }
}
