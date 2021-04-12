using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RT.PropellerApi;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using SvgPuzzleConstraints;
using Zinga.Database;
using Zinga.Suco;

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
                new UrlMapping(path: "/tmp", handler: PlayWithSuco),
                new UrlMapping(path: "/edit", handler: PuzzleEditPage),
                new UrlMapping(path: null, handler: PuzzlePage));
        }

        public override HttpResponse Handle(HttpRequest req) => _resolver.Handle(req);
    }
}