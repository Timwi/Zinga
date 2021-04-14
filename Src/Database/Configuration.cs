using System.Data.Entity.Migrations;
using Zinga.Suco;

namespace Zinga.Database
{
    internal sealed class Configuration : DbMigrationsConfiguration<Db>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "Zinga.Db";
        }

        protected override void Seed(Db db)
        {
            db.Constraints.AddOrUpdate(
                p => p.Name,

                new Constraint
                {
                    Name = "Anti-bishop (cell)",
                    LogicSuco = "a, $b diagonal: a.value != b.value",
                    SvgSuco = @"c: ""<path transform='translate({c.x}, {c.y})' opacity='.2' d='M.943.932H.826q0-.018-.03-.018L.742.92Q.666.929.647.929q-.059 0-.1-.026Q.509.878.5.834.492.878.452.904q-.04.025-.1.025-.018 0-.095-.01L.205.915q-.03 0-.03.018H.056V.907q0-.025.02-.052Q.096.83.134.815.174.798.23.798q.025 0 .07.006Q.331.81.343.81.375.81.39.803.404.795.423.767.353.755.33.732V.596Q.257.545.257.465q0-.04.02-.074.02-.033.05-.058L.473.21Q.428.189.428.14q0-.03.02-.05Q.47.067.5.067t.051.021Q.572.11.572.14.572.19.527.21q.138.116.162.14.025.022.04.053.014.03.014.063 0 .08-.073.131v.136Q.646.755.577.767.596.795.61.803.625.81.657.81.669.81.7.805.746.8.77.8q.084 0 .127.033.045.033.045.075zM.5.181q.017 0 .029-.012T.54.14Q.54.124.529.112T.5.1Q.483.1.471.112T.46.14q0 .017.012.029T.5.181zm.14.397Q.674.566.694.534q.02-.032.02-.07Q.714.41.642.347L.5.221.393.315Q.336.365.321.383.307.4.296.423q-.01.022-.01.042 0 .037.02.07.021.03.053.043Q.447.554.5.554q.052 0 .14.024zM.58.44H.515v.064h-.03V.44H.42V.41h.064V.347h.03V.41H.58zm.06.263V.675L.615.646.64.63V.608Q.57.584.5.584T.36.608v.02l.026.018L.36.675v.028Q.408.686.5.686q.092 0 .14.017zM.544.633L.5.662.457.634.5.605zm.08.094Q.592.715.5.715T.377.727Q.417.744.5.744q.083 0 .122-.017zm.29.175Q.895.828.773.828q-.017 0-.061.007Q.67.84.655.84.615.84.592.823.569.805.547.77H.515Q.515.9.647.9.663.899.736.89l.06-.006q.028 0 .046.018zM.484.77H.453Q.431.805.408.823.385.84.345.84.33.84.288.835.243.828.226.828q-.122 0-.139.074h.07Q.176.884.205.884l.06.006Q.335.9.352.9.485.899.485.77z'/>""",
                    Variables = new[] { "cells".ListOfCellsVariable() },
                    Public = true
                },
                new Constraint
                {
                    Name = "Anti-king (cell)",
                    LogicSuco = "a, $b adjacent: a.value != b.value",
                    SvgSuco = @"c: ""<path transform='translate({c.x}, {c.y})' opacity='.2' d='M.614.325Q.672.265.77.265q.076 0 .124.043.049.045.049.113 0 .132-.155.216v.205q0 .042-.092.07Q.604.941.5.941.396.94.304.912.212.884.212.842V.637Q.057.553.057.421q0-.068.049-.113Q.154.264.23.264q.098 0 .156.061.03-.052.076-.07H.397V.059h.206v.196H.537q.048.018.077.07zM.547.089H.453L.5.136zm.025.12V.106l-.05.051zM.48.157L.428.107v.102zm.068.069L.5.179.453.226zm.044.12Q.578.316.552.296.526.273.5.273T.448.295Q.422.316.409.346q.022.022.05.075.029.053.041.1.012-.047.04-.1Q.568.368.59.346zm.173.271Q.834.582.873.531q.04-.052.04-.11T.872.329Q.833.294.77.294q-.174 0-.246.254.154.007.24.07zM.476.547Q.404.295.23.295.166.294.127.33q-.04.035-.04.092 0 .058.04.11.04.051.11.086.085-.062.239-.07zm.282.234V.758L.718.695.758.67V.655Q.732.62.66.599.589.578.5.578q-.089 0-.16.021-.072.02-.098.056V.67l.04.025-.04.063V.78Q.348.723.5.723q.152 0 .258.058zM.566.646L.5.693.434.646.5.604zM.5.91Q.578.91.668.885q.09-.024.09-.053 0-.026-.082-.053Q.594.753.5.753q-.094 0-.177.026Q.242.806.242.832q0 .029.09.053Q.422.91.5.91z'/>""",
                    Variables = new[] { "cells".ListOfCellsVariable() },
                    Public = true
                },
                new Constraint
                {
                    Name = "Anti-knight (cell)",
                    LogicSuco = "a, $b ([abs(a.x-b.x), abs(a.y-b.y)].contains([1, 2])): a.value != b.value",
                    SvgSuco = @"c: ""<path transform='translate({c.x}, {c.y})' opacity='.2' d='M.946.946H.3q0-.097.025-.15Q.35.746.412.71q.08-.045.08-.115Q.491.58.48.562.453.585.333.614q-.041.011-.05.08Q.276.726.26.744.245.762.223.762.167.762.11.717.054.67.054.615q0-.044.06-.125Q.159.431.17.408.182.384.186.35.191.312.196.297T.217.255Q.242.221.248.196.253.172.253.13V.065q.049.02.094.1L.376.164q.02-.035.03-.11.036.016.066.063l.043.065Q.739.21.842.347q.104.137.104.465zM.892.916V.834q0-.33-.091-.463T.502.21Q.49.203.469.166.442.117.424.106L.41.18Q.41.192.398.214q-.01.02-.023.02-.01 0-.01-.015 0-.009.004-.021L.3.23.298.219Q.31.202.323.197q0-.036-.04-.073 0 .058-.007.083Q.27.232.244.272.228.298.224.31.218.323.215.358.214.383.203.408q-.011.024-.064.1Q.105.555.094.574q-.01.02-.01.037 0 .037.025.063.026.027.04.027.009 0 .022-.025Q.194.63.209.63q.017 0 .017.019 0 .015-.014.035Q.197.7.187.72q.015.011.034.011.024 0 .032-.042Q.266.603.318.588L.397.567Q.437.555.458.543.48.531.49.518.501.505.521.466L.53.473Q.52.505.52.53L.52.595q0 .087-.09.137Q.38.76.359.802.336.844.336.917zM.357.344Q.337.347.324.358l.002.02q0 .007-.018.017L.285.392.281.398v.023Q.257.4.257.389q0-.019.027-.037Q.31.333.329.333h.024zM.154.58Q.15.595.138.604l.015.018q0 .01-.006.015Q.14.643.133.643.117.643.117.618q0-.016.01-.026Q.137.58.154.58z'/>""",
                    Variables = new[] { "cells".ListOfCellsVariable() },
                    Public = true
                },
                new Constraint
                {
                    Name = "Thermometer",
                    LogicSuco = "a, b ~: a.value < b.value",
                    SvgSuco = @"""<g opacity='.2'>
                        <path d='M{{c: "" {c.cx} {c.cy}""}}' stroke='black' stroke-width='.3' stroke-linecap='round' stroke-linejoin='round' fill='none' />
                        <circle cx='{{c first: c.cx}}' cy='{{c first: c.cy}}' r='.4' fill='black' />
                    </g>""",
                    Variables = new[] { "cells".ListOfCellsVariable() },
                    Public = true
                },
                new Constraint
                {
                    Name = "Arrow (1-cell sum)",
                    LogicSuco = "s first: {c after}.sum = s.value",
                    SvgSuco = @"{f first, s ~, +sl, l ~ last: let endAngle = (l.y-sl.y).atan2(l.x-sl.x); 
                    ""<g fill='none' stroke='black' stroke-width='.05' opacity='.2'>
                        <circle cx='{f.cx}' cy='{f.cy}' r='.4' />
                        <path d='M{let angle = (s.y-f.y).atan2(s.x-f.x); ""{f.cx + .4*angle.cos} {f.cy + .4*angle.sin}""}
                                    {{a (a.pos > 1 & a.pos < cells.count): "" {a.cx} {a.cy}""}}
                                    {l.cx + .3*endAngle.cos} {l.cy + .3*endAngle.sin}' />
                        <path d='M -.2 -.2 .3 0 -.2 .2' transform='translate({l.cx}, {l.cy}) rotate({endAngle})' />
                    </g>""}",
                    //SvgSuco = @"""<text x='0' y='4' font-size='.5' text-anchor='start'>{{f first, s ~, +sl, l ~ last: let endAngle = (l.y-sl.y).atan2(l.x-sl.x); ""{f.index},{s.index},{sl.index},{l.index} || {l.y-sl.y}.atan2({l.x-sl.x}) = {endAngle}""}}</text>""",
                    Variables = new[] { "cells".ListOfCellsVariable() },
                    Public = true
                });

            db.Puzzles.AddOrUpdate(
                p => p.UrlName,

                new Puzzle
                {
                    UrlName = "Test",
                    Title = "Test",
                    Author = "Timwi",
                    Givens = new (int cell, int value)[] { (0, 1), (20, 5) }
                });

            //            db.Puzzles.AddOrUpdate(
            //                p => p.UrlName,

            //                new Puzzle
            //                {
            //                    Title = "Y-Sums Sudoku",
            //                    Author = "cdwg2000",
            //                    UrlName = "Y-Sums-Sudoku-by-cdwg2000",
            //                    Links = Ut.NewArray(new Link { Text = "Y-Sums Sudoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0004A5" }),
            //                    Rules = "Place the digits from 1 to 9 in every row, column and 3×3 box.\r\n\r\nThe clues outside the grid indicate the sum of the first Y numbers seen from the clue, where Y is the Xth digit and X is the first digit.",
            //                    Constraints = Ut.NewArray(
            //                        new[] { 11, 23, 40, 13, 11, 2, 45, 12, 1 }.Select((clue, ix) => new YSum(true, ix, false, clue, SvgRowColNumberConstraint.RowColDisplay.TopLeft)),
            //                        new[] { 7, 45, 25, 10, 21, 8, 1, 36, 45 }.Select((clue, ix) => new YSum(true, ix, true, clue, SvgRowColNumberConstraint.RowColDisplay.BottomRight)),
            //                        new[] { 11, 27, 1, 14, 21, 32, 33, 28, 7 }.Select((clue, ix) => new YSum(false, ix, false, clue, SvgRowColNumberConstraint.RowColDisplay.TopLeft)),
            //                        new[] { 1, 11, 23, 40, 33, 15, 5, 14, 38 }.Select((clue, ix) => new YSum(false, ix, true, clue, SvgRowColNumberConstraint.RowColDisplay.BottomRight))
            //                    ).SelectMany(x => x).ToArray<SvgConstraint>()
            //                },

            //                new Puzzle
            //                {
            //                    Title = "Plethora Sudoku",
            //                    Author = "Timwi",
            //                    UrlName = "Plethora-Sudoku-by-Timwi",
            //                    Links = Ut.NewArray(new Link { Text = "Plethora Sudoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0005QA" }),
            //                    Rules = @"Normal Sudoku rules apply. Additionally, the gray 3×3 area must contain the digits 1–9 exactly once.

            //The clue above the grid shows the sum of the indicated diagonal.

            //Row 2 has an X-sum constraint, which specifies the sum of the first (rightmost) X digits, where X is the first (rightmost) cell in the row.

            //Row 8 has a Sandwich constraint, which specifies the sum of the digits sandwiched between the 1 and the 9.

            //The top-left, top-right and bottom-left cages are Binairo cages: (1) In each cage, each row and each column has equal counts of odd and even numbers. (2) In each cage, the rows have unique arrangements of odds/evens. The same is separately true of the columns.

            //The bottom-right cage specifies the sum of the digits contained in it. (The digits may repeat.)

            //Digits along the thermometer must increase from the bulb end.

            //Digits along the diagonal arrow must sum to the digit in the circle.

            //The cell with the down-pointing arrow must state how far in the indicated direction the digit 9 is. (For example, a 1 would mean the cell right below it is a 9.)",
            //                    Constraints = Ut.NewArray<SvgConstraint>(
            //                        new BinairoCage(0, 4),
            //                        new BinairoCage(5, 4),
            //                        new BinairoCage(5 * 9, 4),
            //                        new KillerCage(Enumerable.Range(0, 16).Select(i => i % 4 + 5 + 9 * (i / 4 + 5)).ToArray(), sum: 73, nonUnique: true),
            //                        new FindThe9(10, SvgConstraint.CellDirection.Down),
            //                        new LittleKiller(DiagonalDirection.NorthEast, 6, 15, opposite: true),
            //                        new XSum(false, 1, true, 25),
            //                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 2 + 9 * (i / 3 + 2)).ToArray(), shaded: true),
            //                        new Arrow(new[] { 23, 33, 43, 53 }),
            //                        new Thermometer(new[] { 58, 49, 50, 41, 42 }),
            //                        new Sandwich(false, 7, 1, 9, 23, omitCrust: true))
            //                },

            //                new Puzzle
            //                {
            //                    Title = "Little Sandwich Sudoku",
            //                    Author = "Brawlbox",
            //                    UrlName = "Little-Sandwich-Sudoku-by-Brawlbox",
            //                    Links = Ut.NewArray(new Link { Text = "Little Sandwich Sudoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0003EL" }),
            //                    Rules = "Normal Sudoku rules apply.\r\nEvery marked diagonal contains exactly one 1 and one 9. The digits between the 1 and 9 must add up to the number specified.\r\nThe numbers without arrows are the sums between the 1 and 9 in that row/column.",
            //                    Constraints = Ut.NewArray<SvgConstraint>(
            //                        new LittleSandwich(DiagonalDirection.SouthEast, 0, 2, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
            //                        new LittleSandwich(DiagonalDirection.SouthEast, 5, 10, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
            //                        new LittleSandwich(DiagonalDirection.NorthEast, 1, 16, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
            //                        new LittleSandwich(DiagonalDirection.SouthEast, 7, 0, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
            //                        new LittleSandwich(DiagonalDirection.SouthEast, 1, 14, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
            //                        new LittleSandwich(DiagonalDirection.SouthWest, 3, 9, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
            //                        new LittleSandwich(DiagonalDirection.SouthWest, 1, 47, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
            //                        new Sandwich(true, 0, 1, 9, 27, SvgRowColNumberConstraint.RowColDisplay.TopLeft),
            //                        new Sandwich(true, 2, 1, 9, 0, SvgRowColNumberConstraint.RowColDisplay.TopLeft),
            //                        new Sandwich(false, 2, 1, 9, 6, SvgRowColNumberConstraint.RowColDisplay.TopLeft),
            //                        new Sandwich(false, 4, 1, 9, 16, SvgRowColNumberConstraint.RowColDisplay.TopLeft),
            //                        new Sandwich(false, 8, 1, 9, 0, SvgRowColNumberConstraint.RowColDisplay.TopLeft)),
            //                    Givens = new (int cell, int value)[] { (73, 1), (79, 7) }
            //                },

            //                new Puzzle
            //                {
            //                    Title = "Rösselsprung Windoku",
            //                    Author = "surbier",
            //                    UrlName = "Rösselsprung-Windoku-by-surbier",
            //                    Links = new[] { new Link { Text = "Rösselsprung Windoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0002TF" } },
            //                    Rules = "Normal Sudoku, Windoku and anti-knight rules apply.",
            //                    Constraints = Ut.NewArray<SvgConstraint>(
            //                        new GlobalAntiKnight(),
            //                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 1 + 9 * (i / 3 + 1)).ToArray(), shaded: true),
            //                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 5 + 9 * (i / 3 + 1)).ToArray(), shaded: true),
            //                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 1 + 9 * (i / 3 + 5)).ToArray(), shaded: true),
            //                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 5 + 9 * (i / 3 + 5)).ToArray(), shaded: true)),
            //                    Givens = ".1.6............1...75....3....5........4........9....7....53...2................".Select((ch, ix) => (cell: ix, value: ch == '.' ? 0 : ch - '0')).Where(g => g.value != 0).ToArray(),
            //                    LastAccessed = DateTime.UtcNow
            //                },

            //                new Puzzle
            //                {
            //                    Title = "Thermo Killer Sudoku",
            //                    Author = "Cane_Puzzles",
            //                    UrlName = "Thermo-Killer-Sudoku-by-Cane_Puzzles",
            //                    Links = new[] { new Link { Text = "Thermo Killer Sudoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0005UO" } },
            //                    Rules = @"Standard Sudoku rules apply.

            //Standard Thermometer rules apply: Digits increase along thermometers, starting at the bulb ends.

            //Standard Killer Sudoku rules apply: The digits in a cage sum to the number in the corner of the cage.",
            //                    Constraints = Ut.NewArray<SvgConstraint>(
            //                        new KillerCage(new[] { 0, 1 }, sum: 10),
            //                        new KillerCage(new[] { 5, 14, 13 }, sum: 12),
            //                        new KillerCage(new[] { 25, 34 }, sum: 8),
            //                        new KillerCage(new[] { 52, 53 }, sum: 12),
            //                        new KillerCage(new[] { 58, 59 }, sum: 11),
            //                        new KillerCage(new[] { 76, 77 }, sum: 10),
            //                        new Thermometer(new[] { 22, 13, 4, 5 }),
            //                        new Thermometer(new[] { 35, 26, 17, 8, 7 }),
            //                        new Thermometer(new[] { 32, 41, 42, 43 }),
            //                        new Thermometer(new[] { 18, 19, 28, 29, 38, 47, 56 }),
            //                        new Thermometer(new[] { 36, 37 }),
            //                        new Thermometer(new[] { 49, 58 }),
            //                        new Thermometer(new[] { 55, 64 }),
            //                        new Thermometer(new[] { 69, 70, 71 }))
            //                },

            //                new Puzzle
            //                {
            //                    Title = "Consequences",
            //                    Author = "fuxia",
            //                    UrlName = "Consequences-by-fuxia",
            //                    Links = new[] { new Link { Text = "Consequences (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0005VS" } },
            //                    Rules = @"Normal Sudoku rules apply.

            //Orthogonally adjacent digits are not consecutive. For example a 2 cannot be placed directly next to, above or below a 1 or a 3.

            //Every digit in a quadruple clue must be placed in one of the four surrounding cells.",
            //                    Constraints = Ut.NewArray<SvgConstraint>(
            //                        new GlobalNoConsecutive(),
            //                        new Inclusion(4, new[] { 2, 6, 8, 9 }),
            //                        new Inclusion(10, new[] { 2, 3, 5, 8 }),
            //                        new Inclusion(15, new[] { 1, 2, 4, 9 }),
            //                        new Inclusion(27, new[] { 1, 3, 6, 7 }),
            //                        new Inclusion(43, new[] { 3, 4, 7, 8 }),
            //                        new Inclusion(55, new[] { 1, 5, 7, 9 }),
            //                        new Inclusion(60, new[] { 3, 4, 7, 8 }),
            //                        new Inclusion(66, new[] { 1, 2, 4, 9 }))
            //                },

            //                new Puzzle
            //                {
            //                    Title = "REDRUM",
            //                    Author = "grkles",
            //                    UrlName = "REDRUM-by-grkles",
            //                    Links = new[] { new Link { Text = "REDRUM (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0005W1" } },
            //                    Rules = @"Normal Sudoku rules apply.

            //Little killer: Digits along the indicated diagonals sum to the given total, and may repeat (subject to the rules of Sudoku).

            //Palindromes: Digits along grey lines form palindromes, and read the same in both directions.",
            //                    Constraints = Ut.NewArray<IEnumerable<SvgConstraint>>(
            //                        Ut.NewArray<SvgConstraint>(
            //                            new LittleKiller(DiagonalDirection.SouthEast, 0, 39),
            //                            new LittleKiller(DiagonalDirection.NorthEast, 7, 10, opposite: true),
            //                            new LittleKiller(DiagonalDirection.SouthEast, 7, 10),
            //                            new LittleKiller(DiagonalDirection.SouthWest, 3, 42),
            //                            new LittleKiller(DiagonalDirection.SouthWest, 4, 15),
            //                            new LittleKiller(DiagonalDirection.SouthWest, 5, 21),
            //                            new LittleKiller(DiagonalDirection.NorthEast, 3, 47),
            //                            new LittleKiller(DiagonalDirection.NorthEast, 4, 32),
            //                            new LittleKiller(DiagonalDirection.NorthEast, 5, 17)),
            //                        Ut.NewArray(
            //                            new Palindrome(new[] { 3, 12, 20, 28, 27 }),
            //                            new Palindrome(new[] { 14, 23, 33, 34 }),
            //                            new Palindrome(new[] { 22, 32, 42 }))
            //                            .SelectMany(p => new[] { p, new Palindrome(p.Cells.Select(c => 80 - c).ToArray()) })
            //                            .ToArray())
            //                        .SelectMany(x => x)
            //                        .ToArray()
            //                },

            //                new Puzzle
            //                {
            //                    Title = "Hubble Deep Field Sudoku",
            //                    Author = "Azireo",
            //                    UrlName = "Hubble-Deep-Field-Sudoku-by-Azireo",
            //                    Links = new[] { new Link { Text = "Hubble Deep Field Sudoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0005VR" } },
            //                    Rules = @"Normal sudoku rules apply. Digits must increase along thermo, from the bulb to the end. Along arrows, digits must sum to the digit in the corresponding circle.",
            //                    Constraints = Ut.NewArray(
            //                        Ut.NewArray(
            //                            new[] { 11, 2, 3, 4 },
            //                            new[] { 11, 20, 19, 18 },
            //                            new[] { 6, 7, 8 },
            //                            new[] { 34, 33, 42 },
            //                            new[] { 56, 57, 48, 47, 46 },
            //                            new[] { 56, 55, 64, 65, 66 },
            //                            new[] { 76, 67, 77 }
            //                        ).Select(arr => new Arrow(arr)).ToArray<SvgConstraint>(),
            //                        Ut.NewArray(
            //                            new[] { 10, 0, 9, 1 },
            //                            new[] { 16, 17, 26, 25, 24, 15 },
            //                            new[] { 22, 21, 30, 31 },
            //                            new[] { 60, 59, 50, 51, 52, 61, 70 },
            //                            new[] { 64, 65, 74, 73 }
            //                        ).Select(arr => new Thermometer(arr)).ToArray<SvgConstraint>()
            //                    ).SelectMany(x => x).ToArray()
            //                },

            //                new Puzzle
            //                {
            //                    Title = "Wheel of Arrows",
            //                    Author = "Aspartagcus",
            //                    UrlName = "Wheel-of-Arrows-by-Aspartagcus",
            //                    Links = new[] { new Link { Text = "Wheel of Arrows (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0005WD" } },
            //                    Rules = @"Standard Sudoku rules apply.
            //Digits on an arrow sum up to the digit in the circle of that arrow.
            //Clues outside the grid indicate the sum of the digits in the direction of the arrow.",
            //                    Constraints = Ut.NewArray<SvgConstraint>(
            //                        new LittleKiller(DiagonalDirection.SouthEast, 0, 55),
            //                        new LittleKiller(DiagonalDirection.SouthEast, 3, 26, opposite: true),
            //                        new LittleKiller(DiagonalDirection.SouthWest, 6, 15, opposite: true),
            //                        new LittleKiller(DiagonalDirection.NorthWest, 3, 31, opposite: true),
            //                        new LittleKiller(DiagonalDirection.NorthEast, 6, 8, opposite: true),
            //                        new LittleKiller(DiagonalDirection.NorthEast, 0, 53, opposite: true),
            //                        new Arrow(new[] { 20, 30 }),
            //                        new Arrow(new[] { 20, 28, 37, 46 }),
            //                        new Arrow(new[] { 24, 14, 13, 12 }),
            //                        new Arrow(new[] { 24, 32 }),
            //                        new Arrow(new[] { 56, 48 }),
            //                        new Arrow(new[] { 56, 66, 67, 68 }),
            //                        new Arrow(new[] { 60, 50 }),
            //                        new Arrow(new[] { 60, 52, 43, 34 }))
            //                });
        }
    }
}