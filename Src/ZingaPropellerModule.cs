using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RT.PropellerApi;
using RT.Servers;
using Zinga.Database;

namespace Zinga
{
    public partial class ZingaPropellerModule : PropellerModuleBase<ZingaSettings>
    {
        public override string Name => "Zinga";

        private UrlResolver _resolver;

        public override void Init()
        {
            Db.ConnectionString = Settings.ConnectionString;

            using (var db = new Db())
            {
                db.Database.Migrate();
                Log.Info($"Zinga: Number of puzzles in the database: {db.Puzzles.Count()}");
            }

            var frameworkPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "_framework");

            _resolver = new UrlResolver(
#if DEBUG
                new UrlMapping(path: "/css-websocket", specificPath: true, handler: req => new HttpResponseWebSocket(new AutoUpdatingCssWebsocket(Settings))),
                new UrlMapping(path: "/play-with-suco", specificPath: true, handler: PlayWithSuco),
#endif

                new UrlMapping(path: "/", specificPath: true, handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/edit"))),
                //new UrlMapping(path: "/tmp", handler: PlayWithSuco2),
                new UrlMapping(path: "/font", handler: req => HttpResponse.Css(Resources.FontCss)),
                new UrlMapping(path: "/edit", handler: PuzzleEditPage),
                new UrlMapping(path: "/publish", handler: PuzzlePublish),
                new UrlMapping(path: "/_framework", handler: new FileSystemHandler(frameworkPath).Handle),
                new UrlMapping(path: "/constraint-search", handler: ConstraintSearch),
                new UrlMapping(path: null, handler: PuzzlePage));
        }

        public override HttpResponse Handle(HttpRequest req) => _resolver.Handle(req);
    }
}