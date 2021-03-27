using System.Data.Entity;
using System.Linq;
using RT.PropellerApi;
using RT.Servers;
using RT.TagSoup;
using RT.Util.ExtensionMethods;
using Zinga.Database;

namespace Zinga
{
    public class ZingaPropellerModule : PropellerModuleBase<ZingaSettings>
    {
        public override string Name => "Zinga";

        public override void Init()
        {
            System.Data.Entity.Database.SetInitializer(new MigrateDatabaseToLatestVersion<Db, Configuration>());
            Db.ConnectionString = Settings.ConnectionString;

            // This also triggers any pending migrations. Without doing some DB stuff here, transactions that don’t commit mess up the migrations.
            using var db = new Db();
            Log.Info($"Zinga: Number of puzzles in the database: {db.Puzzles.Count()}");
        }

        public override HttpResponse Handle(HttpRequest req)
        {
            var url = req.Url.Path.SubstringSafe(1);
            if (url.Length == 0)
                return HttpResponse.Html("<h1>404 — Not Found</h1>", HttpStatusCode._404_NotFound);

            using var db = new Db();
            var puzzle = db.Puzzles.FirstOrDefault(p => p.PuzzleHash == url);
            if (puzzle == null)
                return HttpResponse.Html("<h1>404 — Not Found</h1>", HttpStatusCode._404_NotFound);

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE($"{puzzle.Title} by {puzzle.Author}"),
                    new SCRIPTLiteral(Resources.Js),
                    new STYLELiteral(Resources.Css),
                    new LINK { rel = "shortcut icon", type = "image/png", href = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAABAAAAAQAAQMAAABF07nAAAAABlBMVEUAAAD///+l2Z/dAAACFElEQVR42u3YsQ2AMBAEwZMIKINS3RplERm38ERvodn4gokvkSRJkiRJ2qHxFrqTXJXhk+SsDCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAJEmSJEmStslFAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8EPA8Q0gSZIkSZLUflD4iAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAANoBkiRJkiRJnS37yw5ZFqD7+QAAAABJRU5ErkJggg==" }),
                new BODY { class_ = "is-puzzle" }._(
                    new DIV { id = "topbar" }._(
                        new DIV { class_ = "title" }._(puzzle.Title),
                        puzzle.Author == null ? null : new DIV { class_ = "author" }._("by ", puzzle.Author),
                        new DIV { class_ = "main" }._(
                            new DIV { class_ = "puzzle", tabindex = 0 }._(
                                "(SVG)"
                            ))))));
        }
    }
}