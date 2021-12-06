using Bicep.Core.Navigation;
using Bicep.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bicep2Terraform.HclExtension
{
    public static partial class SyntaxBaseHclExtension
    {
        public static string ToHcl(this ParameterDeclarationSyntax syntax, string indent = "")
        {
            if (syntax.Type.ToText() == "string" && syntax.Modifier != null && syntax.Modifier is ParameterDefaultValueSyntax)
            {
                var defaultValue = ((ParameterDefaultValueSyntax)syntax.Modifier).ToHcl();
                if (defaultValue.StartsWith("azurerm_resource_group.test") || defaultValue.StartsWith("data.azurerm_client_config.current"))
                {
                    Console.WriteLine("[INFO] Variables not allowed as default input value. All usage of ${0} will be replaced with ${1}", syntax.Name.ToText(), defaultValue);
                    predefinedMap[syntax.Name.ToText()] = defaultValue;
                    return "";
                }
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0}variable \"{1}\" {{\n", indent, syntax.Name.ToText());
            sb.AppendFormat("{0}type = {1}\n", indent + INDENT_UNIT, syntax.Type.ToHcl());
            if (syntax.Modifier != null)
            {
                switch (syntax.Modifier)
                {
                    case ParameterDefaultValueSyntax:
                        var defaultValue = ((ParameterDefaultValueSyntax)syntax.Modifier).ToHcl();
                        if (defaultValue.Contains("var.")) {
                            Console.WriteLine("[WARN]  Variables not allowed as default input value. default value: {0}", defaultValue);
                        }
                        else
                        {
                            sb.AppendFormat("{0}default = {1}\n", indent + INDENT_UNIT, ((ParameterDefaultValueSyntax)syntax.Modifier).ToHcl());
                        }
                        break;
                    default:
                        syntax.Modifier.ToHcl();
                        break;
                }
            }

            var descriptionDecorator = syntax.Decorators.FirstOrDefault(x => (x.Expression is FunctionCallSyntax) && ((FunctionCallSyntax)x.Expression).Name.ToText() == "description");
            if (descriptionDecorator != null && descriptionDecorator.Arguments.Count() != 0)
            {
                sb.AppendFormat("{0}description = {1}\n", indent + INDENT_UNIT, descriptionDecorator.Arguments.First().ToHcl());
            }

            var allowedDecorator = syntax.Decorators.FirstOrDefault(x => (x.Expression is FunctionCallSyntax) && ((FunctionCallSyntax)x.Expression).Name.ToText() == "allowed");
            if (allowedDecorator != null && allowedDecorator.Arguments.Count() != 0)
            {
                sb.AppendFormat("{0}validation {{\n", indent + INDENT_UNIT);
                var allowedValues = allowedDecorator.Arguments.First().ToHcl(indent + INDENT_UNIT + INDENT_UNIT);
                sb.AppendFormat("{0}condition     = contains({1}, var.{2})\n", indent + INDENT_UNIT + INDENT_UNIT, allowedValues, syntax.Name.ToText());
                sb.AppendFormat("{0}error_message = \"Allowed values are {1}.\"\n", indent + INDENT_UNIT + INDENT_UNIT, allowedValues.Replace(" ", "").Replace("\n", "").Replace("\"", "'").Replace(",]", "]"));
                sb.AppendFormat("{0}}}\n", indent + INDENT_UNIT);
            }

            sb.Append("}\n");
            return sb.ToString();
        }

        public static string ToHcl(this VariableDeclarationSyntax syntax, string indent = "")
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}locals {{\n", indent);
            sb.AppendFormat("{0}{1} = {2}\n", indent + INDENT_UNIT, syntax.Name.ToText(), syntax.Value.ToHcl(indent + INDENT_UNIT));
            sb.AppendFormat("{0}}}\n", indent);
            return sb.ToString();
        }

        public static string ToHcl(this ResourceDeclarationSyntax syntax, string indent = "")
        {
            var sb = new StringBuilder();
            var label = syntax.Name.IdentifierName;
            var type = syntax.Type.ToHcl();
            var id = GetResourceId(syntax);
            if (syntax.IsExistingResource())
            {
                sb.AppendFormat("{0}data \"azurerm-restapi_resource\" \"{1}\" {{\n", indent, label);
                sb.AppendFormat("{0}resource_id = {1}\n", indent + INDENT_UNIT, id);
                sb.AppendFormat("{0}type        = {1}\n", indent + INDENT_UNIT, type);
                sb.AppendFormat("{0}response_export_values = []\n", indent + INDENT_UNIT);
                sb.AppendFormat("{0}}}\n", indent);
            }
            else
            {
                sb.AppendFormat("{0}resource \"azurerm-restapi_resource\" \"{1}\" {{\n", indent, label);
                sb.AppendFormat("{0}resource_id = {1}\n", indent + INDENT_UNIT, id);
                sb.AppendFormat("{0}type        = {1}\n", indent + INDENT_UNIT, type);
                sb.AppendFormat("{0}response_export_values = []\n", indent + INDENT_UNIT);
                sb.Append(Environment.NewLine);
                var body = syntax.Value;
                switch (body)
                {
                    case ObjectSyntax:
                        var objSyntax = (ObjectSyntax)body;
                        sb.Append(objSyntax.ToHcl(indent + INDENT_UNIT, true));
                        break;
                    case IfConditionSyntax:
                        var ifSyntax = (IfConditionSyntax)body;
                        sb.AppendFormat("{0}count       = {1} ? 1 : 0\n", indent + INDENT_UNIT, ifSyntax.ConditionExpression.ToHcl(indent));
                        switch (ifSyntax.Body)
                        {
                            case ObjectSyntax:
                                sb.Append(((ObjectSyntax)(ifSyntax.Body)).ToHcl(indent + INDENT_UNIT, true));
                                break;
                            default:
                                ifSyntax.Body.ToHcl();
                                break;
                        }
                        break;
                    case ForSyntax:
                        sb.AppendFormat("{0}body        = jsonencode({{}})\n", indent + INDENT_UNIT);                        
                        Console.WriteLine("[WARN] Bicep for-loop accepts an array but this is not supported in terraform. Details: {0}", body.ToText());
                        break;
                    default:
                        body.ToHcl(indent + INDENT_UNIT);
                        break;
                }

                sb.AppendFormat("{0}}}\n", indent);
            }
            return sb.ToString();
        }

        private static string GetResourceId(ResourceDeclarationSyntax syntax)
        {
            var type = syntax.Type.ToHcl();
            var resourceType = type.Substring(0, type.IndexOf("@")).Replace("\"", "");
            ObjectSyntax objSyntax = null;

            var body = syntax.Value;
            switch (body)
            {
                case ObjectSyntax:
                    objSyntax = (ObjectSyntax)body;
                    break;
                case IfConditionSyntax:
                    var ifSyntax = (IfConditionSyntax)body;
                    if (ifSyntax.Body is ObjectSyntax)
                    {
                        objSyntax = (ObjectSyntax)ifSyntax.Body;
                    }
                    break;
                case ForSyntax:
                    var forSyntax = (ForSyntax)body;
                    if (forSyntax.Body is ObjectSyntax)
                    {
                        localScopeVarMap.Clear();
                        localScopeVarMap.Add(forSyntax.ItemVariable.ToText(), "each");
                        objSyntax = (ObjectSyntax)forSyntax.Body;
                    }
                    break;
            }
            if (objSyntax == null)
            {
                return "";
            }

            var parentProp = objSyntax.Properties.FirstOrDefault(x => x.TryGetKeyText() == "parent");
            var nameProp = objSyntax.Properties.FirstOrDefault(x => x.TryGetKeyText() == "name");
            var name = nameProp != null ? nameProp.Value.ToHcl() : "default";
            if (parentProp != null)
            {
                var lastType = resourceType.Substring(resourceType.LastIndexOf("/") + 1);

                var parent = parentProp.Value.ToHcl();
                var parentId = parent + (parent.Contains("azurerm-restapi") ? ".resource_id" : ".id");
                return String.Format("\"${{{0}}}/{1}/{2}\"", parentId, lastType, name);
            }
            localScopeVarMap.Clear();
            return String.Format("\"${{azurerm_resource_group.test.id}}/providers/{0}/${{{1}}}\"", resourceType, name);
        }

        public static string ToHcl(this ModuleDeclarationSyntax syntax, string indent = "")
        {

            return "";
        }

        public static string ToHcl(this OutputDeclarationSyntax syntax, string indent = "")
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}output \"{1}\" {{\n", indent, syntax.Name.ToText());
            sb.AppendFormat("{0}value = {1}\n", indent + INDENT_UNIT, syntax.Value.ToHcl(indent));
            sb.AppendFormat("{0}}}\n", indent);
            return sb.ToString();
        }

        public static string ToHcl(this ImportDeclarationSyntax syntax, string indent = "")
        {

            return "";
        }

        public static string ToHcl(this TargetScopeSyntax syntax, string indent = "")
        {

            return "";
        }
    }
}
