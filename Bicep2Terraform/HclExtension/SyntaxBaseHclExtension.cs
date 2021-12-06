using Azure.Deployments.Core.Extensions;
using Azure.Deployments.Core.Helpers;
using Bicep.Core.Navigation;
using Bicep.Core.PrettyPrint;
using Bicep.Core.PrettyPrint.Documents;
using Bicep.Core.Syntax;
using Bicep.Core.Syntax.Visitors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bicep2Terraform.HclExtension
{
    public static partial class SyntaxBaseHclExtension
    {
        public static string INDENT_UNIT = "  ";

        private static HashSet<String> localsMap = new HashSet<string>();
        private static Dictionary<String, String> resourceMap = new Dictionary<string, string>();
        private static Dictionary<String, String> predefinedMap = new Dictionary<string, string>();
        private static List<Tuple<String, HashSet<String>>> resourceOutputList = new List<Tuple<string, HashSet<string>>>();

        public static void SetupContext(this ProgramSyntax syntax)
        {
            localsMap = new HashSet<string>();
            resourceMap = new Dictionary<string, string>();
            predefinedMap = new Dictionary<string, string>();
            resourceOutputList = new List<Tuple<string, HashSet<string>>>();
            // TODO: check nested resource definition
            foreach (var item in syntax.Children)
            {
                if (item is ResourceDeclarationSyntax)
                {
                    var resourceSyntax = (ResourceDeclarationSyntax)item;
                    var label = resourceSyntax.Name.IdentifierName;
                    var type = (resourceSyntax.IsExistingResource() ? "data." : "") + "azurerm-restapi_resource";
                    resourceMap[label] = type;
                    resourceOutputList.Add(new Tuple<string, HashSet<string>>(String.Format("{0}.{1}", type, label), new HashSet<string>()));
                }
                else if (item is VariableDeclarationSyntax)
                {
                    var varSyntax = (VariableDeclarationSyntax)item;
                    localsMap.Add(varSyntax.Name.ToText());
                }
            }
        }

        public static string UpdateOutputValues(this ProgramSyntax syntax, string hcl)
        {
            for (int i=0; i<resourceOutputList.Count; i++)
            {
                var regex = new Regex(Regex.Escape("  response_export_values = []\n"));
                var value = "";
                if (resourceOutputList[i].Item2.Count != 0)
                {
                    var list = resourceOutputList[i].Item2.ToList();
                    value = String.Format("\"{0}\"", list[0]);
                    for (int index = 1; index < list.Count; index++)
                    {
                        value += String.Format(", \"{0}\"", list[index]);
                    }
                    value = String.Format("  response_export_values = [{0}]\n", value);
                }
                hcl = regex.Replace(hcl, value, 1);
            }
            return hcl;
        }

        public static string ToHcl(this SyntaxBase syntax, string indent = "")
        {
            switch (syntax)
            {
                // Top Level Declaration Syntax
                case ParameterDeclarationSyntax:
                    return ((ParameterDeclarationSyntax)syntax).ToHcl(indent);

                case VariableDeclarationSyntax:
                    return ((VariableDeclarationSyntax)syntax).ToHcl(indent);

                case ResourceDeclarationSyntax:
                    return ((ResourceDeclarationSyntax)syntax).ToHcl(indent);

                case ModuleDeclarationSyntax:
                    return ((ModuleDeclarationSyntax)syntax).ToHcl(indent);

                case OutputDeclarationSyntax:
                    return ((OutputDeclarationSyntax)syntax).ToHcl(indent);

                case ImportDeclarationSyntax:
                    return ((ImportDeclarationSyntax)syntax).ToHcl(indent);

                case TargetScopeSyntax:
                    return ((TargetScopeSyntax)syntax).ToHcl(indent);

                // Expression Syntax

                case ArrayItemSyntax:
                    return ((ArrayItemSyntax)syntax).ToHcl(indent);

                case ArraySyntax:
                    return ((ArraySyntax)syntax).ToHcl(indent);

                case BooleanLiteralSyntax:
                    return ((BooleanLiteralSyntax)syntax).ToHcl(indent);

                case FunctionArgumentSyntax:
                    return ((FunctionArgumentSyntax)syntax).ToHcl(indent);

                case FunctionCallSyntax:
                    return ((FunctionCallSyntax)syntax).ToHcl(indent);

                case IntegerLiteralSyntax:
                    return ((IntegerLiteralSyntax)syntax).ToHcl(indent);

                case NullLiteralSyntax:
                    return ((NullLiteralSyntax)syntax).ToHcl(indent);

                case ObjectPropertySyntax:
                    return ((ObjectPropertySyntax)syntax).ToHcl(indent);

                case ObjectSyntax:
                    return ((ObjectSyntax)syntax).ToHcl(indent);

                case ParenthesizedExpressionSyntax:
                    return ((ParenthesizedExpressionSyntax)syntax).ToHcl(indent);

                case PropertyAccessSyntax:
                    return ((PropertyAccessSyntax)syntax).ToHcl(indent);

                case StringSyntax:
                    return ((StringSyntax)syntax).ToHcl(indent);

                case TernaryOperationSyntax:
                    return ((TernaryOperationSyntax)syntax).ToHcl(indent);

                case VariableAccessSyntax:
                    return ((VariableAccessSyntax)syntax).ToHcl(indent);

                // Other

                case ParameterDefaultValueSyntax:
                    return ((ParameterDefaultValueSyntax)syntax).ToHcl(indent);

                case TypeSyntax:
                    return ((TypeSyntax)syntax).ToHcl(indent);
            }
            return "";
        }
    }
}
