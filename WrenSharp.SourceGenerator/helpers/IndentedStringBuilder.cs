using System.Text;

namespace WrenSharp.SourceGenerator;

    /// <summary>
    /// A StringBuilder wrapper that automatically applies indentation to the start of each new line.
    /// Useful for generating source code, config files, or any structured text output.
    /// </summary>
    public sealed class IndentedStringBuilder
    {
        private readonly StringBuilder sb = new();
        private readonly string indentUnit;
        private int indentLevel;
        private bool atLineStart = true;
 
        /// <param name="indentUnit">The string used for a single indent level. Defaults to 4 spaces.</param>
        public IndentedStringBuilder(string indentUnit = "    ")
        {
            this.indentUnit = indentUnit;
        }
 
        public int IndentLevel => indentLevel;
 
        /// <summary>Increases the indent level by one.</summary>
        public IndentedStringBuilder Indent(int level = 1)
        {
            indentLevel += level;
            return this;
        }
 
        /// <summary>Decreases the indent level by one (never below zero).</summary>
        public IndentedStringBuilder Unindent(int level = 1)
        { 
            indentLevel = Math.Max(indentLevel - level, 0);
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
        public IndentScope Indented() => new(this);
 
        public IndentedStringBuilder Append(string text)
        {
            WriteIndentIfNeeded();
            sb.Append(text);
            return this;
        }
 
        public IndentedStringBuilder Append(char c)
        {
            WriteIndentIfNeeded();
            sb.Append(c);
            if (c == '\n')
                atLineStart = true;
            return this;
        }
 
        /// <summary>Appends a blank line.</summary>
        public IndentedStringBuilder AppendLine()
        {
            sb.AppendLine();
            atLineStart = true;
            return this;
        }
 
        /// <summary>Appends text, indented if at the start of a line, followed by a newline.</summary>
        public IndentedStringBuilder AppendLine(string text)
        {
            WriteIndentIfNeeded();
            sb.Append(text);
            sb.AppendLine();
            atLineStart = true;
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
            sb.Clear();
            indentLevel = 0;
            atLineStart = true;
            return this;
        }
 
        public override string ToString() => sb.ToString();
 
        private void WriteIndentIfNeeded()
        {
            if (atLineStart && indentLevel > 0)
            {
                for (int i = 0; i < indentLevel; i++)
                    sb.Append(indentUnit);
            }
            atLineStart = false;
        }
 
        /// <summary>
        /// A disposable indent scope. Increases the owner's indent level when created,
        /// and decreases it when disposed.
        /// </summary>
        public readonly struct IndentScope : IDisposable
        {
            private readonly IndentedStringBuilder owner;
 
            internal IndentScope(IndentedStringBuilder owner)
            {
                this.owner = owner;
                this.owner.Indent();
            }
 
            public void Dispose() => owner.Unindent();
        }
    }
