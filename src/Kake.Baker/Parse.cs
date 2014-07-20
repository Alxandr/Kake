using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kake
{
    /// <summary>
    /// Summary description for Parse
    /// </summary>
    public static class Parser
    {
        private static readonly Regex _ws = new Regex(@"\s+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex _meta = new Regex(@"^@(?<name>[a-zA-Z0-9-]+)(?<args>(\s+([a-zA-Z0-9-\.]+))*)\s*(\/\/.*)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex _target = new Regex(@"^(?<name>[a-zA-Z0-9-]+):\s*(\/\/.*)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static async Task<KakeUnit> Parse(TextReader textReader)
        {
            var source = new Source(textReader);

            var metas = ImmutableList.CreateBuilder<Meta>();
            Meta meta;
            while (await source.Advance() && ParseMeta(source.Current, out meta, indented: false))
                if (meta != null)
                    metas.Add(meta);

            var code = ImmutableList.CreateBuilder<Line>();
            if (!IsTarget(source.Current))
            {
                code.Add(source.Current);
                while (await source.Advance() && !IsTarget(source.Current))
                    code.Add(source.Current);
            }

            var targets = await ParseTargets(source, ImmutableList.Create<Target>());

            return new KakeUnit(metas.ToImmutable(), code.ToImmutable(), targets);
        }

        private static bool ParseMeta(Line line, out Meta meta, bool indented = false)
        {
            var text = line.Text;

            if (indented)
                text = text.TrimStart();

            if (string.IsNullOrWhiteSpace(text) || _ws.Match(text).Length == text.Length)
            {
                meta = null;
                return true;
            }

            var match = _meta.Match(text);
            if (!match.Success)
            {
                meta = null;
                return false;
            }

            var name = match.Groups["name"].Value;
            var args = _ws.Split(match.Groups["args"].Value).Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
            meta = new Meta(name, args, line.Index);
            return true;
        }

        private static async Task<ImmutableList<Target>> ParseTargets(Source source, ImmutableList<Target> acc)
        {
            while (true)
            {
                // Ensure first line is target
                var match = _target.Match(source.Current.Text);
                if (!match.Success)
                    throw new InvalidOperationException(string.Format("Line {0}: Not a target", source.Current.Index.ToString()));

                var name = match.Groups["name"].Value;

                var metas = ImmutableList.CreateBuilder<Meta>();
                Meta meta;
                while (await source.Advance() && ParseMeta(source.Current, out meta, indented: true))
                    if (meta != null)
                        metas.Add(meta);

                var code = ImmutableList.CreateBuilder<Line>();
                if (!IsTarget(source.Current))
                {
                    code.Add(source.Current);
                    while (await source.Advance() && !IsTarget(source.Current))
                        code.Add(source.Current);
                }

                acc = acc.Add(new Target(name, metas.ToImmutable(), code.ToImmutable()));
                if (source.Current == null)
                    return acc;
            }
        }

        private static bool IsTarget(Line line)
        {
            return _target.IsMatch(line.Text);
        }
    }
}