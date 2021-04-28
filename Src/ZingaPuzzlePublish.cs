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
#warning This is temporary!
                            VariablesJson = kind switch
                            {
                                ConstraintKind.Diagonal => @"{""cells"":""list(cell)""}",
                                ConstraintKind.FourCells => @"{""topleftcell"":""cell""}",
                                ConstraintKind.Global => @"{}",
                                ConstraintKind.MatchingRegions => @"{""cells"":""list(list(cell))""}",
                                ConstraintKind.Path => @"{""cells"":""list(cell)""}",
                                ConstraintKind.Region => @"{""cells"":""list(cell)""}",
                                ConstraintKind.RowColumn => @"{""cells"":""list(cell)""}",
                                ConstraintKind.SingleCell => @"{""cell"":""cell""}",
                                ConstraintKind.TwoCells => @"{""topleftcell"":""cell""}",
                                _ => @"{""cells"":""list(cell)""}"
                            }
                        };
                        db.Constraints.Add(newConstraintType);
                        dbConstraintTypes[typeId] = newConstraintType;
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
