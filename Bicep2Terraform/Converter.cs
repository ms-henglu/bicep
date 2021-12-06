using Bicep.Core.Parsing;
using Bicep.Core.Syntax;
using Bicep2Terraform.HclExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bicep2Terraform
{
    internal static class Converter
    {
        public static String  ToHcl(string bicep)
        {
            var parser = new Parser(bicep);
            var syntax = parser.Program();
            syntax.SetupContext();

            var sb = new StringBuilder();
            foreach (var item in syntax.Children)
            {
                if (item is ParameterDeclarationSyntax)
                {
                    sb.Append(((ParameterDeclarationSyntax)item).ToHcl());
                    sb.Append(Environment.NewLine);
                }
            }
            sb.Append(Environment.NewLine);

            foreach (var item in syntax.Children)
            {
                if (item is VariableDeclarationSyntax)
                {
                    sb.Append(((VariableDeclarationSyntax)item).ToHcl());
                    sb.Append(Environment.NewLine);
                }
            }
            sb.Append(Environment.NewLine);

            foreach (var item in syntax.Children)
            {
                if (item is ResourceDeclarationSyntax)
                {
                    sb.Append(((ResourceDeclarationSyntax)item).ToHcl());
                    sb.Append(Environment.NewLine);
                }
            }
            sb.Append(Environment.NewLine);

            foreach (var item in syntax.Children)
            {
                if (item is OutputDeclarationSyntax)
                {
                    sb.Append(((OutputDeclarationSyntax)item).ToHcl());
                    sb.Append(Environment.NewLine);
                }
            }
            sb.Append(Environment.NewLine);
            return syntax.UpdateOutputValues(sb.ToString());
        }
    }
}
