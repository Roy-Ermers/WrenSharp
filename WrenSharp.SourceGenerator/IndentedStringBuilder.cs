using System.Text;

namespace WrenSharp.SourceGenerator;

    /// <summary>
    /// A StringBuilder wrapper that automatically applies indentation to the start of each new line.
    /// Useful for generating source code, config files, or any structured text output.
    /// </summary>
    public sealed class IndentedStringBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly string _indentUnit;
        private int _indentLevel;
        private bool _atLineStart = true;
 
        /// <param name="indentUnit">The string used for a single indent level. Defaults to 4 spaces.</param>
        public IndentedStringBuilder(string indentUnit = "    ")
        {
            _indentUnit = indentUnit;
        }
 
        public int IndentLevel => _indentLevel;
 
        /// <summary>Increases the indent level by one.</summary>
        public IndentedStringBuilder Indent()
        {
            _indentLevel++;
            return this;
        }
 
        /// <summary>Decreases the indent level by one (never below zero).</summary>
        public IndentedStringBuilder Unindent()
        {
            if (_indentLevel > 0)
                _indentLevel--;
            return this;
        }
 
        /// <summary>
        /// Returns a disposable scope that increases the indent level on entry and restores it
        /// automatically on disposal. Intended for use in a <c>using</c> block:
        /// <code>
        /// sb.AppendLine("class Foo {");
        /// using (sb.Indented())
        /// {
        ///     sb.AppendLine("void Bar() {}");
        /// }
        /// sb.AppendLine("}");
        /// </code>
        /// </summary>
        public IndentScope Indented() => new IndentScope(this);
 
        public IndentedStringBuilder Append(string text)
        {
            WriteIndentIfNeeded();
            _sb.Append(text);
            return this;
        }
 
        public IndentedStringBuilder Append(char c)
        {
            WriteIndentIfNeeded();
            _sb.Append(c);
            if (c == '\n')
                _atLineStart = true;
            return this;
        }
 
        /// <summary>Appends a blank line.</summary>
        public IndentedStringBuilder AppendLine()
        {
            _sb.AppendLine();
            _atLineStart = true;
            return this;
        }
 
        /// <summary>Appends text, indented if at the start of a line, followed by a newline.</summary>
        public IndentedStringBuilder AppendLine(string text)
        {
            WriteIndentIfNeeded();
            _sb.Append(text);
            _sb.AppendLine();
            _atLineStart = true;
            return this;
        }
 
        /// <summary>
        /// Convenience helper for the common "open line, indented body, close line" pattern
        /// (e.g. braces, XML elements).
        /// </summary>
        public IndentedStringBuilder AppendBlock(string openLine, Action<IndentedStringBuilder> body, string closeLine)
        {
            AppendLine(openLine);
            using (Indented())
            {
                body(this);
            }
            AppendLine(closeLine);
            return this;
        }
 
        public IndentedStringBuilder Clear()
        {
            _sb.Clear();
            _indentLevel = 0;
            _atLineStart = true;
            return this;
        }
 
        public override string ToString() => _sb.ToString();
 
        private void WriteIndentIfNeeded()
        {
            if (_atLineStart && _indentLevel > 0)
            {
                for (int i = 0; i < _indentLevel; i++)
                    _sb.Append(_indentUnit);
            }
            _atLineStart = false;
        }
 
        /// <summary>
        /// A disposable indent scope. Increases the owner's indent level when created,
        /// and decreases it when disposed.
        /// </summary>
        public readonly struct IndentScope : IDisposable
        {
            private readonly IndentedStringBuilder _owner;
 
            internal IndentScope(IndentedStringBuilder owner)
            {
                _owner = owner;
                _owner.Indent();
            }
 
            public void Dispose() => _owner.Unindent();
        }
    }
