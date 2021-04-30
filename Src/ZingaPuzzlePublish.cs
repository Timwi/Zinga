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

            try
            {
                var puzzle = new Puzzle();

                puzzle.Title = json["title"].GetString();
                puzzle.Author = json["author"].GetString();
                puzzle.Rules = json["rules"].GetString();
                puzzle.UrlName = MD5.ComputeUrlName(jsonRaw);

                var givens = json["givens"].GetList().Select((v, ix) => (v, ix)).Where(tup => tup.v != null).Select(tup => (cell: tup.ix, value: tup.v.GetInt())).ToArray();
                if (givens.Any(given => given.cell < 0 || given.cell >= 81))
                    return HttpResponse.PlainText($"At least one given is out of range (cell {puzzle.Givens.First(given => given.cell < 0 || given.cell >= 81).cell}).", HttpStatusCode._400_BadRequest);
                puzzle.Givens = givens.Length > 0 ? givens : null;

                var constraints = json["constraints"].GetList();
                var customConstraintTypes = json["customConstraintTypes"].GetList();

                using var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable });
                using var db = new Db();

                if (db.Puzzles.Any(p => p.UrlName == puzzle.UrlName))
                    return HttpResponse.PlainText(puzzle.UrlName);

                var dbConstraintTypes = constraints.Select(c => c["type"].GetInt()).Where(c => c >= 0).Distinct().ToArray()
                    .Apply(cIds => db.Constraints.Where(c => cIds.Contains(c.ConstraintID))).AsEnumerable().ToDictionary(c => c.ConstraintID);
                var faultyId = constraints.Select(c => c["type"].GetInt()).Where(c => c >= 0).FirstOrNull(c => !dbConstraintTypes.ContainsKey(c));
                if (faultyId != null)
                    return HttpResponse.PlainText($"Unknown constraint type ID: {faultyId.Value}.", HttpStatusCode._400_BadRequest);

                // Add the custom constraint types into the database
                foreach (var constraint in constraints)
                    if (constraint["type"].GetInt() is int typeId && typeId < 0 && !dbConstraintTypes.ContainsKey(typeId))
                    {
                        if (~typeId >= customConstraintTypes.Count || customConstraintTypes[~typeId] == null)
                            return HttpResponse.PlainText($"Undefined custom constraint type: {typeId}. List has {customConstraintTypes.Count} entries.", HttpStatusCode._400_BadRequest);
                        var cType = customConstraintTypes[~typeId];
                        var kind = EnumStrong.Parse<ConstraintKind>(cType["kind"].GetString());

                        // Some verifications:
                        // Make sure the variable types parse as valid Suco types
                        var env = new SucoTypeEnvironment();
                        foreach (var (varName, varType) in cType["variables"].GetDict().ToTuples())
                        {
                            if (!SucoType.TryParse(varType.GetString(), out var type))
                                return HttpResponse.PlainText($"Unrecognized Suco type: {varType.GetString()}.");
                            env = env.DeclareVariable(varName, type);
                        }
                        // Make sure all the Suco code compiles
                        if (!SucoParser.IsValidCode(cType["logic"].GetString(), env, SucoContext.Constraint, SucoBooleanType.Instance, out string error))
                            return HttpResponse.PlainText($"The Suco code for the constraint logic in “{cType["name"].GetString()}” doesn’t compile: {error}.");
                        if (cType["svg"] != null && !SucoParser.IsValidCode(cType["svg"].GetString(), env, SucoContext.Svg, SucoStringType.Instance, out error))
                            return HttpResponse.PlainText($"The Suco code for generating SVG code in “{cType["name"].GetString()}” doesn’t compile: {error}.");
                        if (cType["svgdefs"] != null && !SucoParser.IsValidCode(cType["svgdefs"].GetString(), env, SucoContext.Svg, new SucoListType(SucoStringType.Instance), out error))
                            return HttpResponse.PlainText($"The Suco code for generating SVG definitions in “{cType["name"].GetString()}” doesn’t compile: {error}.");

                        var newConstraintType = new DbConstraint
                        {
                            Kind = kind,
                            LogicSuco = cType["logic"].GetString(),
                            Name = cType["name"].GetString(),
                            PreviewSvg = cType["preview"]?.GetString(),
                            Public = false,
                            Shortcut = null,
                            SvgDefsSuco = cType["svgdefs"]?.GetString(),
                            SvgSuco = cType["svg"]?.GetString(),
                            VariablesJson = cType["variables"].ToString()
                        };
                        var already = db.Constraints.FirstOrDefault(c => c.Kind == newConstraintType.Kind && c.LogicSuco == newConstraintType.LogicSuco &&
                            c.Name == newConstraintType.Name && c.PreviewSvg == newConstraintType.PreviewSvg && !c.Public && c.Shortcut == null &&
                            c.SvgDefsSuco == newConstraintType.SvgDefsSuco && c.SvgSuco == newConstraintType.SvgSuco && c.VariablesJson == newConstraintType.VariablesJson);
                        if (already != null)
                            dbConstraintTypes[typeId] = already;
                        else
                        {
                            db.Constraints.Add(newConstraintType);
                            dbConstraintTypes[typeId] = newConstraintType;
                        }
                    }
                db.Puzzles.Add(puzzle);
                db.SaveChanges();   // This assigns new IDs to all the new constraint types and the puzzle itself

                foreach (var constraint in constraints)
                    db.PuzzleConstraints.Add(new PuzzleConstraint
                    {
                        PuzzleID = puzzle.PuzzleID,
                        ConstraintID = dbConstraintTypes[constraint["type"].GetInt()].ConstraintID,
                        ValuesJson = constraint["values"].ToString()
                    });

                db.SaveChanges();
                tr.Complete();
                return HttpResponse.PlainText(puzzle.UrlName);
            }
            catch (Exception e)
            {
                return HttpResponse.PlainText(e.Message, HttpStatusCode._400_BadRequest);
            }
        }
    }
}
