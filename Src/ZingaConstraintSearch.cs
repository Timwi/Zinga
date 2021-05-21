using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Json;
using RT.Servers;
using RT.Util.ExtensionMethods;
using Zinga.Database;

namespace Zinga
{
    public partial class ZingaPropellerModule
    {
        private HttpResponse ConstraintSearch(HttpRequest req)
        {
            if (req.Method != HttpMethod.Post)
                return HttpResponse.PlainText("Only POST requests allowed.", HttpStatusCode._405_MethodNotAllowed);

            var query = req.Post["msg"].Value;
            if (string.IsNullOrWhiteSpace(query))
                return HttpResponse.PlainText("No search query specified.", HttpStatusCode._400_BadRequest);

            var alreadyRaw = req.Post["already"].Value;
            if (alreadyRaw != null && !alreadyRaw.All(ch => ch == ',' || (ch >= '0' && ch <= '9')))
                return HttpResponse.PlainText("Invalid “already” field.", HttpStatusCode._400_BadRequest);
            var already = alreadyRaw?.Split(',').Select(int.Parse).ToArray();

            using var db = new Db();
            var results = db.Constraints.OrderBy(c => c.Name).Where(c => c.Public);
            if (already != null)
                results = results.Where(c => !already.Contains(c.ConstraintID));
            foreach (var piece in Regex.Split(query, @"\s"))
                if (!string.IsNullOrWhiteSpace(piece))
                    results = results.Where(c => c.Name.ToLower().Contains(piece.ToLower()) || c.Description.ToLower().Contains(piece.ToLower()) || c.AkasJson.ToLower().Contains(piece.ToLower()));

            var resultsArr = results.ToArray();
            return HttpResponse.Json(new JsonDict { ["status"] = "ok", ["results"] = resultsArr.ToJsonDict(c => c.ConstraintID.ToString(), c => c.ToJson()), ["order"] = resultsArr.Select(c => c.ConstraintID).ToJsonList() });
        }
    }
}
