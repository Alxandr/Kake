using System;
using System.Collections.Immutable;
using System.Text;

namespace Kake
{
    /// <summary>
    /// Summary description for CodeBlock
    /// </summary>
    public class CodeBlock
    {
        readonly int _startLine;
        readonly string _code;

        public CodeBlock(int startLine, string code)
        {
            _startLine = startLine;
            _code = code;
        }

        public int StartLine
        {
            get { return _startLine; }
        }

        public string Code
        {
            get { return _code; }
        }

        public static implicit operator CodeBlock(ImmutableList<Line> code)
        {
            if (code.Count == 0)
                return null;

            var start = code[0].Index;
            var sb = new StringBuilder();
            for(var i = 0; i < code.Count; i++)
            {
                var line = code[i];
                if (line.Index != start + i)
                    throw new InvalidOperationException("Code block is not from consecutive lines");

                sb.AppendLine(line.Text);
            }

            return new CodeBlock(start, sb.ToString());
        }
    }
}