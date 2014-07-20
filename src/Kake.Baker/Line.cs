using System;

namespace Kake
{
    /// <summary>
    /// Summary description for Line
    /// </summary>
    public class Line
    {
        readonly string _text;
        readonly int _index;

        public Line(string text, int index)
        {
            _text = text;
            _index = index;
        }

        public string Text
        {
            get { return _text; }
        }

        public int Index
        {
            get { return _index; }
        }
    }
}