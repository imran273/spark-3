/*
   Copyright 2008 Louis DeJardin - http://whereslou.com

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Spark.Compiler.ChunkVisitors;

namespace Spark.Compiler.ChunkVisitors
{
    public class GeneratedCodeVisitor : AbstractChunkVisitor
    {
        private readonly StringBuilder _source;

        public GeneratedCodeVisitor(StringBuilder output)
        {
            _source = output;
        }

        public int Indent { get; set; }

        private StringBuilder AppendIndent()
        {
            return _source.Append(' ', Indent);
        }

        protected override void Visit(SendLiteralChunk chunk)
        {
            if (string.IsNullOrEmpty(chunk.Text))
                return;

            AppendIndent().AppendLine("Output.Write(\"" + chunk.Text.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"") + "\");");
        }

        protected override void Visit(SendExpressionChunk chunk)
        {
            AppendIndent().AppendLine(string.Format("Output.Write({0});", chunk.Code));
        }


        protected override void Visit(MacroChunk chunk)
        {

        }

        protected override void Visit(CodeStatementChunk chunk)
        {
            AppendIndent().AppendLine(chunk.Code.Replace("\r", "").Replace("\n", "\r\n"));
        }

        protected override void Visit(LocalVariableChunk chunk)
        {
            AppendIndent().Append(chunk.Type).Append(' ').Append(chunk.Name);
            if (!string.IsNullOrEmpty(chunk.Value))
            {
                _source.Append(" = ").Append(chunk.Value);
            }
            _source.AppendLine(";");
        }

        protected override void Visit(ForEachChunk chunk)
        {
            var terms = chunk.Code.Split(' ', '\r', '\n', '\t').ToList();
            var inIndex = terms.IndexOf("in");
            string variableName = (inIndex < 2 ? null : terms[inIndex - 1]);

            if (variableName == null)
            {
                AppendIndent().AppendLine(string.Format("foreach({0})", chunk.Code));
                AppendIndent().AppendLine("{");
                Indent += 4;
                Accept(chunk.Body);
                Indent -= 4;
                AppendIndent().AppendLine(string.Format("}} //foreach {0}", chunk.Code.Replace("\r", "").Replace("\n", " ")));
            }
            else
            {
                AppendIndent().AppendLine("{");
                _source.Append(' ', Indent + 4).AppendFormat("int {0}Index = 0;\r\n", variableName);
                _source.Append(' ', Indent + 4).AppendFormat("foreach({0})\r\n", chunk.Code);
                _source.Append(' ', Indent + 4).AppendLine("{");
                Indent += 8;
                Accept(chunk.Body);
                Indent -= 8;
                _source.Append(' ', Indent + 8).AppendFormat("++{0}Index;\r\n", variableName);
                _source.Append(' ', Indent + 4).AppendLine("}");
                AppendIndent().AppendFormat("}} //foreach {0}\r\n", chunk.Code.Replace("\r", "").Replace("\n", " "));
            }
        }

        protected override void Visit(ScopeChunk chunk)
        {
            AppendIndent().AppendLine("{");
            Indent += 4;
            Accept(chunk.Body);
            Indent -= 4;
            AppendIndent().AppendLine("}");
        }

        protected override void Visit(GlobalVariableChunk chunk)
        {
        }

        protected override void Visit(AssignVariableChunk chunk)
        {
            AppendIndent().AppendLine(string.Format("{0} = {1};", chunk.Name, chunk.Value));
        }


        protected override void Visit(ContentChunk chunk)
        {
            AppendIndent().AppendLine(string.Format("using(OutputScope(\"{0}\"))", chunk.Name));
            AppendIndent().AppendLine("{");
            Indent += 4;
            Accept(chunk.Body);
            Indent -= 4;
            AppendIndent().AppendLine("}");
        }

        protected override void Visit(ContentSetChunk chunk)
        {
            AppendIndent().AppendLine("using(OutputScope(new System.IO.StringWriter()))");
            AppendIndent().AppendLine("{");
            Indent += 4;
            Accept(chunk.Body);

            string format;
            switch (chunk.AddType)
            {
                case ContentAddType.AppendAfter:
                    format = "{0} = {0} + Output.ToString();";
                    break;
                case ContentAddType.InsertBefore:
                    format = "{0} = Output.ToString() + {0};";
                    break;
                default:
                    format = "{0} = Output.ToString();";
                    break;
            }
            AppendIndent().AppendFormat(format, chunk.Variable).AppendLine();

            Indent -= 4;
            AppendIndent().AppendLine("}");
        }

        protected override void Visit(UseContentChunk chunk)
        {
            AppendIndent().AppendLine(string.Format("if (Content.ContainsKey(\"{0}\"))", chunk.Name));
            AppendIndent().AppendLine("{");
            _source.Append(' ', Indent + 4).AppendLine(string.Format("Output.Write(Content[\"{0}\"]);", chunk.Name));
            AppendIndent().AppendLine("}");
            if (chunk.Default.Count != 0)
            {
                AppendIndent().AppendLine("else");
                AppendIndent().AppendLine("{");
                Indent += 4;
                Accept(chunk.Default);
                Indent -= 4;
                AppendIndent().AppendLine("}");
            }
        }

        public RenderPartialChunk OuterPartial { get; set; }
        protected override void Visit(RenderPartialChunk chunk)
        {
            var priorOuterPartial = OuterPartial;
            OuterPartial = chunk;
            Accept(chunk.FileContext.Contents);
            OuterPartial = priorOuterPartial;
        }


        protected override void Visit(RenderSectionChunk chunk)
        {
            if (string.IsNullOrEmpty(chunk.Name))
                Accept(OuterPartial.Body);
        }

        protected override void Visit(ViewDataChunk chunk)
        {

        }

        protected override void Visit(UseNamespaceChunk chunk)
        {

        }

        protected override void Visit(UseAssemblyChunk chunk)
        {

        }

        protected override void Visit(ExtensionChunk chunk)
        {
            chunk.Extension.VisitChunk(this, OutputLocation.RenderMethod, chunk.Body, _source);
        }

        protected override void Visit(ConditionalChunk chunk)
        {
            switch (chunk.Type)
            {
                case ConditionalType.If:
                    {
                        AppendIndent().AppendLine(string.Format("if ({0})", chunk.Condition));
                        AppendIndent().AppendLine("{");
                        Indent += 4;
                        Accept(chunk.Body);
                        Indent -= 4;
                        AppendIndent().AppendLine(string.Format("}} // if ({0})",
                                                         chunk.Condition.Replace("\r", "").Replace("\n", " ")));
                    }
                    break;
                case ConditionalType.ElseIf:
                    {
                        AppendIndent().AppendLine(string.Format("else if ({0})", chunk.Condition));
                        AppendIndent().AppendLine("{");
                        Indent += 4;
                        Accept(chunk.Body);
                        Indent -= 4;
                        AppendIndent().AppendLine(string.Format("}} // else if ({0})",
                                                         chunk.Condition.Replace("\r", "").Replace("\n", " ")));
                    }
                    break;
                case ConditionalType.Else:
                    {
                        AppendIndent().AppendLine("else");
                        AppendIndent().AppendLine("{");
                        Indent += 4;
                        Accept(chunk.Body);
                        Indent -= 4;
                        AppendIndent().AppendLine("}");
                    }
                    break;
            }
        }

        protected override void Visit(ViewDataModelChunk chunk)
        {

        }

    }
}