using System;
using System.Data.Entity.Migrations;
using System.Linq;
using RT.Serialization;
using RT.Util;
using SvgPuzzleConstraints;

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
            db.Puzzles.AddOrUpdate(
                p => p.UrlName,

                new Puzzle
                {
                    Title = "Y-Sums Sudoku",
                    Author = "cdwg2000",
                    UrlName = "Y-Sums-Sudoku-by-cdwg2000",
                    Links = Ut.NewArray(new Link { Text = "Y-Sums Sudoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0004A5" }),
                    Rules = "Place the digits from 1 to 9 in every row, column and 3×3 box.\r\n\r\nThe clues outside the grid indicate the sum of the first Y numbers seen from the clue, where Y is the Xth digit and X is the first digit.",
                    Constraints = Ut.NewArray(
                        new[] { 11, 23, 40, 13, 11, 2, 45, 12, 1 }.Select((clue, ix) => new YSum(true, ix, false, clue, SvgRowColNumberConstraint.RowColDisplay.TopLeft)),
                        new[] { 7, 45, 25, 10, 21, 8, 1, 36, 45 }.Select((clue, ix) => new YSum(true, ix, true, clue, SvgRowColNumberConstraint.RowColDisplay.BottomRight)),
                        new[] { 11, 27, 1, 14, 21, 32, 33, 28, 7 }.Select((clue, ix) => new YSum(false, ix, false, clue, SvgRowColNumberConstraint.RowColDisplay.TopLeft)),
                        new[] { 1, 11, 23, 40, 33, 15, 5, 14, 38 }.Select((clue, ix) => new YSum(false, ix, true, clue, SvgRowColNumberConstraint.RowColDisplay.BottomRight))
                    ).SelectMany(x => x).ToArray<SvgConstraint>(),
                    LastAccessed = DateTime.UtcNow
                },

                new Puzzle
                {
                    Title = "Plethora Sudoku",
                    Author = "Timwi",
                    UrlName = "Plethora-Sudoku-by-Timwi",
                    Links = Ut.NewArray(new Link { Text = "Plethora Sudoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0005QA" }),
                    Rules = @"Normal Sudoku rules apply. Additionally, the gray 3×3 area must contain the digits 1–9 exactly once.

The clue above the grid shows the sum of the indicated diagonal.

Row 2 has an X-sum constraint, which specifies the sum of the first (rightmost) X digits, where X is the first (rightmost) cell in the row.

Row 8 has a Sandwich constraint, which specifies the sum of the digits sandwiched between the 1 and the 9.

The top-left, top-right and bottom-left cages are Binairo cages: (1) In each cage, each row and each column has equal counts of odd and even numbers. (2) In each cage, the rows have unique arrangements of odds/evens. The same is separately true of the columns.

The bottom-right cage specifies the sum of the digits contained in it. (The digits may repeat.)

Digits along the thermometer must increase from the bulb end.

Digits along the diagonal arrow must sum to the digit in the circle.

The cell with the down-pointing arrow must state how far in the indicated direction the digit 9 is. (For example, a 1 would mean the cell right below it is a 9.)",
                    Constraints = Ut.NewArray<SvgConstraint>(
                        new BinairoCage(0, 4),
                        new BinairoCage(5, 4),
                        new BinairoCage(5 * 9, 4),
                        new KillerCage(Enumerable.Range(0, 16).Select(i => i % 4 + 5 + 9 * (i / 4 + 5)).ToArray(), sum: 73, nonUnique: true),
                        new FindThe9(10, SvgConstraint.CellDirection.Down),
                        new LittleKiller(DiagonalDirection.NorthEast, 6, 15, opposite: true),
                        new XSum(false, 1, true, 25),
                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 2 + 9 * (i / 3 + 2)).ToArray(), shaded: true),
                        new Arrow(new[] { 23, 33, 43, 53 }),
                        new Thermometer(new[] { 58, 49, 50, 41, 42 }),
                        new Sandwich(false, 7, 1, 9, 23, omitCrust: true)),
                    LastAccessed = DateTime.UtcNow
                },

                new Puzzle
                {
                    Title = "Little Sandwich Sudoku",
                    Author = "Brawlbox",
                    UrlName = "Little-Sandwich-Sudoku-by-Brawlbox",
                    Links = Ut.NewArray(new Link { Text = "Little Sandwich Sudoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0003EL" }),
                    Rules = "Normal Sudoku rules apply.\r\nEvery marked diagonal contains exactly one 1 and one 9. The digits between the 1 and 9 must add up to the number specified.\r\nThe numbers without arrows are the sums between the 1 and 9 in that row/column.",
                    Constraints = Ut.NewArray<SvgConstraint>(
                        new LittleSandwich(DiagonalDirection.SouthEast, 0, 2, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
                        new LittleSandwich(DiagonalDirection.SouthEast, 5, 10, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
                        new LittleSandwich(DiagonalDirection.NorthEast, 1, 16, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
                        new LittleSandwich(DiagonalDirection.SouthEast, 7, 0, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
                        new LittleSandwich(DiagonalDirection.SouthEast, 1, 14, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
                        new LittleSandwich(DiagonalDirection.SouthWest, 3, 9, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
                        new LittleSandwich(DiagonalDirection.SouthWest, 1, 47, opposite: true, display: SvgDiagonalConstraint.DiagonalDisplay.NumberOnly),
                        new Sandwich(true, 0, 1, 9, 27, SvgRowColNumberConstraint.RowColDisplay.TopLeft),
                        new Sandwich(true, 2, 1, 9, 0, SvgRowColNumberConstraint.RowColDisplay.TopLeft),
                        new Sandwich(false, 2, 1, 9, 6, SvgRowColNumberConstraint.RowColDisplay.TopLeft),
                        new Sandwich(false, 4, 1, 9, 16, SvgRowColNumberConstraint.RowColDisplay.TopLeft),
                        new Sandwich(false, 8, 1, 9, 0, SvgRowColNumberConstraint.RowColDisplay.TopLeft)),
                    Givens = new (int cell, int value)[] { (73, 1), (79, 7) },
                    LastAccessed = DateTime.UtcNow
                },

                new Puzzle
                {
                    Title = "Rösselsprung Windoku",
                    Author = "surbier",
                    UrlName = "Rösselsprung-Windoku-by-surbier",
                    Links = Ut.NewArray(new Link { Text = "Rösselsprung Windoku (Logic Masters Germany)", Url = "https://logic-masters.de/Raetselportal/Raetsel/zeigen.php?id=0002TF" }),
                    Rules = "Normal Sudoku, Windoku and anti-knight rules apply.",
                    Constraints = Ut.NewArray<SvgConstraint>(
                        new GlobalAntiKnight(),
                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 1 + 9 * (i / 3 + 1)).ToArray(), shaded: true),
                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 5 + 9 * (i / 3 + 1)).ToArray(), shaded: true),
                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 1 + 9 * (i / 3 + 5)).ToArray(), shaded: true),
                        new KillerCage(Enumerable.Range(0, 9).Select(i => i % 3 + 5 + 9 * (i / 3 + 5)).ToArray(), shaded: true)),
                    Givens = ".1.6............1...75....3....5........4........9....7....53...2................".Select((ch, ix) => (cell: ix, value: ch == '.' ? 0 : ch - '0')).Where(g => g.value != 0).ToArray(),
                    LastAccessed = DateTime.UtcNow
                });
        }
    }
}
