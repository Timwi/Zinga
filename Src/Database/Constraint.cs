using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using RT.Json;
using RT.Serialization;
using RT.Util;
using Zinga.Suco;

namespace Zinga.Database
{
    public sealed class Constraint
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ConstraintID { get; set; }

        public bool Public { get; set; }
        public string Name { get; set; }
        public ConstraintKind Kind { get; set; }
        public string VariablesJson { get; set; }
        public string LogicSuco { get; set; }
        public string SvgDefsSuco { get; set; }
        public string SvgSuco { get; set; }
        public string PreviewSvg { get; set; }
        public string Shortcut { get; set; }

        [ClassifyIgnore]
        private SucoVariable[] _variablesCache;
        public SucoVariable[] Variables
        {
            get => _variablesCache ??= VariablesJson.NullOr(v => JsonDict.Parse(v).Select(kvp => new SucoVariable(kvp.Key, SucoType.Parse(kvp.Value.GetString()))).ToArray());
            set { VariablesJson = value.ToJsonDict(v => v.Name, v => v.Type.ToString()).ToString(); _variablesCache = value; }
        }

        [ClassifyIgnore]
        private SucoExpression _svgDefsCache;
        public IEnumerable<string> GetSvgDefs(SucoEnvironment env)
        {
            if (SvgDefsSuco == null)
                return Enumerable.Empty<string>();
            _svgDefsCache ??= SucoParser.ParseCode(SvgDefsSuco, Variables, SucoContext.Svg, new SucoListType(SucoStringType.Instance));
            return ((IEnumerable<object>) _svgDefsCache.Interpret(env)).Cast<string>();
        }

        [ClassifyIgnore]
        private SucoExpression _svgCache;
        public string GetSvg(SucoEnvironment env)
        {
            if (SvgSuco == null)
                return null;
            _svgCache ??= SucoParser.ParseCode(SvgSuco, Variables, SucoContext.Svg, SucoStringType.Instance);
            return (string) _svgCache.Interpret(env);
        }
    }
}