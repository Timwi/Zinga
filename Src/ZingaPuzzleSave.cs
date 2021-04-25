using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using RT.Json;
using RT.Servers;
using RT.Util.ExtensionMethods;
using Zinga.Database;
using Zinga.Lib;

namespace Zinga
{
    public partial class ZingaPropellerModule
    {
        public HttpResponse PuzzleSave(HttpRequest req)
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

                var constraintIds = json["constraints"].GetList().Select(v => v["type"].GetInt()).Distinct().ToArray();
                using var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable });
                using var db = new Db();
                if (db.Puzzles.Any(p => p.UrlName == puzzle.UrlName))
                    return HttpResponse.PlainText(puzzle.UrlName);
                db.Puzzles.Add(puzzle);
                db.SaveChanges();
                var constraintTypes = db.Constraints.Where(c => constraintIds.Contains(c.ConstraintID)).AsEnumerable().ToDictionary(c => c.ConstraintID);
                if (constraintIds.Any(c => !constraintTypes.ContainsKey(c)))
                    return HttpResponse.PlainText($"Constraint ID {constraintIds.First(c => !constraintTypes.ContainsKey(c))} does not exist.", HttpStatusCode._400_BadRequest);

                var constraints = new List<PuzzleConstraint>();
                foreach (var constraint in json["constraints"].GetList())
                    constraints.Add(new PuzzleConstraint { PuzzleID = puzzle.PuzzleID, ConstraintID = constraint["type"].GetInt(), ValuesJson = constraint["values"].ToString() });
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
