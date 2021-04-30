﻿using System.Data.Entity;
using System.Linq;
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
            System.Data.Entity.Database.SetInitializer(new MigrateDatabaseToLatestVersion<Db, Configuration>());
            Db.ConnectionString = Settings.ConnectionString;

            // This also triggers any pending migrations. Without doing some DB stuff here, transactions that don’t commit mess up the migrations.
            using var db = new Db();
            Log.Info($"Zinga: Number of puzzles in the database: {db.Puzzles.Count()}");

            _resolver = new UrlResolver(
#if DEBUG
                new UrlMapping(path: "/css-websocket", specificPath: true, handler: req => new HttpResponseWebSocket(new AutoUpdatingCssWebsocket(Settings))),
#endif

                new UrlMapping(path: "/tmp", handler: DebugSucoStuff),
                new UrlMapping(path: "/font", handler: req => HttpResponse.Css(Resources.FontCss)),
                new UrlMapping(path: "/edit", handler: PuzzleEditPage),
                new UrlMapping(path: "/publish", handler: PuzzlePublish),
                new UrlMapping(path: "/_framework", handler: new FileSystemHandler(Settings.FrameworkDir).Handle),
                new UrlMapping(path: "/edit/_framework", handler: new FileSystemHandler(Settings.FrameworkDir).Handle),
                new UrlMapping(path: null, handler: PuzzlePage));
        }

        public override HttpResponse Handle(HttpRequest req) => _resolver.Handle(req);
    }
}