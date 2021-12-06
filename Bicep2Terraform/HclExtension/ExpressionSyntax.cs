using Azure.Deployments.Core.Extensions;
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
        public static string ToHcl(this ArrayItemSyntax syntax, string indent = "")
        {
            return syntax.Value.ToHcl(indent);
        }

        public static string ToHcl(this ArraySyntax syntax, string indent = "")
        {
            var sb = new StringBuilder();
            sb.Append("[\n");
            foreach (var item in syntax.Items)
            {
                sb.AppendFormat("{0}{1},\n", indent + INDENT_UNIT, item.ToHcl(indent + INDENT_UNIT));
            }
            sb.Append(indent + "]");
            return sb.ToString();
        }

        public static string ToHcl(this BooleanLiteralSyntax syntax, string indent = "")
        {
            return syntax.ToText();
        }

        public static string ToHcl(this FunctionArgumentSyntax syntax, string indent = "")
        {
            return syntax.Expression.ToHcl(indent);
        }

        public static string ToHcl(this FunctionCallSyntax syntax, string indent = "")
        {
            switch (syntax.Name.ToText())
            {
                case "toLower":
                    if (AssertFunctionArgumentsLength(syntax, 1))
                    {
                        return String.Format("lower({0})", syntax.Arguments[0].ToHcl(indent));
                    }
                    break;
                case "any":
                    Console.WriteLine("[WARN] Bicep any func is used to convert parameter to correct type, ignore it for now.");
                    if (AssertFunctionArgumentsLength(syntax, 1))
                    {
                        return syntax.Arguments[0].ToHcl(indent);
                    }
                    break;
                case "empty":
                    if (AssertFunctionArgumentsLength(syntax, 1))
                    {
                        return String.Format("length({0}) == 0", syntax.Arguments[0].ToHcl(indent));
                    }
                    break;
                case "resourceGroup":
                    return "azurerm_resource_group.test";
                case "take":
                    if (AssertFunctionArgumentsLength(syntax, 2))
                    {
                        // TODO: it may be slice if first argument is array
                        return String.Format("substr({0}, 0, {1})", syntax.Arguments[0].ToHcl(indent), syntax.Arguments[1].ToHcl(indent));
                    }
                    break;
                case "replace":
                    if (AssertFunctionArgumentsLength(syntax, 3))
                    {
                        return String.Format("replace({0}, {1}, {2})", syntax.Arguments[0].ToHcl(indent), syntax.Arguments[1].ToHcl(indent), syntax.Arguments[2].ToHcl(indent));
                    }
                    break;
                case "guid":
                    return "uuid()";
                case "subscription":
                    return "data.azurerm_client_config.current";
                case "resourceId":
                    string sub = "", rg = "", type = "";
                    if (syntax.Arguments.Length > 0 && syntax.Arguments[0].Expression is StringSyntax && syntax.Arguments[0].ToHcl().Contains("."))
                    {
                        sub = "data.azurerm_client_config.current.subscription_id";
                        rg = "azurerm_resource_group.test.name";
                        type = syntax.Arguments[1].ToHcl().Replace("\"", "");
                    }
                    else if (syntax.Arguments.Length > 1 && syntax.Arguments[1].Expression is StringSyntax && syntax.Arguments[1].ToHcl().Contains("."))
                    {
                        sub = "data.azurerm_client_config.current.subscription_id";
                        rg = syntax.Arguments[0].ToHcl();
                        type = syntax.Arguments[1].ToHcl().Replace("\"", "");
                    }
                    else if (syntax.Arguments.Length > 2 && syntax.Arguments[2].Expression is StringSyntax && syntax.Arguments[2].ToHcl().Contains("."))
                    {
                        sub = syntax.Arguments[0].ToHcl();
                        rg = syntax.Arguments[1].ToHcl();
                        type = syntax.Arguments[2].ToHcl().Replace("\"", "");
                    }

                    if (type == "")
                    {
                        return syntax.ToText();
                    }

                    var parts = type.Split("/");
                    var id = String.Format("/subscriptions/${{{0}}}/resourceGroups/${{{1}}}/providers/{2}", sub, rg, parts[0]);
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var value = "";
                        if (i + 2 < syntax.Arguments.Length)
                        {
                            value = syntax.Arguments[i + 2].ToHcl();
                        } else
                        {
                            break;
                        }
                        id += String.Format("/{0}/${{{1}}}", parts[i], value);

                    }
                    return String.Format("\"{0}\"", id);
                case "uniqueString":
                    return String.Format("\"{0}\"", Guid.NewGuid().ToString("n").Substring(0, 13));
                case "last":
                    if (AssertFunctionArgumentsLength(syntax, 1))
                    {
                        var array = syntax.Arguments[0].ToHcl();
                        return String.Format("element({0}, length({0})-1)", array, array);
                    }
                    break;
                case "split":
                    if (AssertFunctionArgumentsLength(syntax, 2))
                    {
                        return String.Format("split({0}, {1})", syntax.Arguments[0].ToHcl(), syntax.Arguments[1].ToHcl());
                    }
                    break;
            }
            return "";
        }

        public static string ToHcl(this IntegerLiteralSyntax syntax, string indent = "")
        {
            return syntax.ToText();
        }

        public static string ToHcl(this NullLiteralSyntax syntax, string indent = "")
        {
            return "null";
        }


        public static String ToHclIdentity(SyntaxBase syntaxBase, string indent = "")
        {
            if (syntaxBase is ObjectSyntax)
            {
                var syntax = (ObjectSyntax)syntaxBase;
                var typeProp = syntax.Properties.FirstOrDefault(x => x.TryGetKeyText() == "type");
                var identitiesProp = syntax.Properties.FirstOrDefault(x => x.TryGetKeyText() == "userAssignedIdentities");

                var sb = new StringBuilder();
                if (typeProp != null)
                {
                    sb.Append("{\n");
                    sb.AppendFormat("{0}type = {1}\n", indent + INDENT_UNIT, typeProp.Value.ToHcl(indent));
                    if (identitiesProp != null)
                    {
                        var value = "";
                        if (identitiesProp.Value is ObjectSyntax)
                        {
                            var identityMap = (ObjectSyntax)(identitiesProp.Value);
                            var ids = new List<string>();
                            foreach (var prop in identityMap.Properties)
                            {
                                ids.Add(prop.Key.ToHcl());
                            }
                            value = String.Format("[{0}]", String.Join(",", ids));

                        }
                        else
                        {
                            value = identitiesProp.Value.ToHcl(indent);
                        }
                        sb.AppendFormat("{0}identity_ids = {1}\n", indent + INDENT_UNIT, value);
                    }
                    sb.AppendFormat("{0}}}", indent);
                }
                return sb.ToString();
            }
            return syntaxBase.ToHcl();
        }

        public static String ToHclTags(SyntaxBase syntax, string indent = "")
        {
            return syntax.ToHcl();
        }

        public static string ToHcl(this ObjectPropertySyntax syntax, string indent = "")
        {
            return String.Format("{0}{1} = {2}", indent, syntax.TryGetKeyText(), syntax.Value.ToHcl(indent));
        }

        public static string ToHcl(this ObjectSyntax syntax, string indent = "", bool isTopLevel = false)
        {
            var sb = new StringBuilder();
            if (isTopLevel)
            {
                var nameProp = syntax.Properties.FirstOrDefault(x => x.TryGetKeyText() == "name");
                var locationProp = syntax.Properties.FirstOrDefault(x => x.TryGetKeyText() == "location");
                var identityProp = syntax.Properties.FirstOrDefault(x => x.TryGetKeyText() == "identity");
                var tagsProp = syntax.Properties.FirstOrDefault(x => x.TryGetKeyText() == "tags");

                if (locationProp != null)
                {
                    sb.AppendFormat("{0}location = {1}\n", indent, locationProp.Value.ToHcl());
                }

                if (identityProp != null)
                {
                    sb.AppendFormat("{0}identity {1}\n", indent, ToHclIdentity(identityProp.Value, indent));
                    sb.Append(Environment.NewLine);
                }

                var props = syntax.Properties.ToList<ObjectPropertySyntax>();
                props.Remove(nameProp);
                props.Remove(locationProp);
                props.Remove(identityProp);
                props.Remove(tagsProp);

                if (!props.IsNullOrEmpty())
                {
                    sb.AppendFormat("{0}body = jsonencode({{\n", indent);
                    foreach (var prop in props)
                    {
                        sb.Append(prop.ToHcl(indent + INDENT_UNIT));
                        sb.Append(Environment.NewLine);
                    }
                    sb.AppendFormat("{0}}})\n", indent);
                }

                if (tagsProp != null)
                {
                    sb.AppendFormat("{0}tags = {1}\n", indent, ToHclTags(tagsProp.Value, indent));
                }

            }
            else
            {
                sb.Append("{\n");
                foreach (var prop in syntax.Properties)
                {
                    sb.Append(prop.ToHcl(indent + INDENT_UNIT));
                    sb.Append(Environment.NewLine);
                }
                sb.AppendFormat("{0}}}", indent);
            }

            return sb.ToString();
        }

        public static string ToHcl(this ParenthesizedExpressionSyntax syntax, string indent = "")
        {
            return String.Format("({0})", syntax.Expression.ToHcl(indent));
        }

        public static string ToHcl(this PropertyAccessSyntax syntax, string indent = "")
        {
            var target = syntax.BaseExpression.ToHcl(indent);
            var propName = syntax.PropertyName.ToText();
            if (target == "data.azurerm_client_config.current")
            {
                switch (propName)
                {
                    case "subscriptionId":
                        propName = "subscription_id";
                        break;
                    case "tenantId":
                        propName = "tenant_id";
                        break;
                    case "clientId":
                        propName = "client_id";
                        break;
                    default:
                        break;
                }
            }
            else if (target.StartsWith("azurerm-restapi_resource"))
            {
                var index = -1;
                for (int i=0; i<resourceOutputList.Count; i++)
                {
                    if (resourceOutputList[i].Item1 == target)
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1)
                {
                    resourceOutputList[index].Item2.Add(propName);
                }

                target = String.Format("jsondecode({0}.output)", target);
            }
            return String.Format("{0}.{1}", target, propName);
        }

        public static string ToHcl(this StringSyntax syntax, string indent = "")
        {
            if (syntax.SegmentValues.Length == 0)
            {
                return "";
            }
            if (syntax.Expressions.Length == 1 && syntax.SegmentValues[0] == "" && syntax.SegmentValues[1] == "")
            {
                return syntax.Expressions[0].ToHcl();
            }
            var sb = new StringBuilder("\"");
            sb.Append(syntax.SegmentValues[0]);
            for (int i = 0; i < syntax.Expressions.Length; i++)
            {
                sb.AppendFormat("${{{0}}}{1}", syntax.Expressions[0].ToHcl(), syntax.SegmentValues[i + 1]);
            }
            sb.Append("\"");
            return sb.ToString();
        }

        public static string ToHcl(this TernaryOperationSyntax syntax, string indent = "")
        {
            return String.Format("{0} ? {1} : {2}", syntax.ConditionExpression.ToHcl(indent), syntax.TrueExpression.ToHcl(indent), syntax.FalseExpression.ToHcl(indent));
        }

        public static string ToHcl(this VariableAccessSyntax syntax, string indent = "")
        {
            var key = syntax.Name.ToText();
            if (predefinedMap.ContainsKey(key))
            {
                return predefinedMap[key];
            }
            var type = "var";
            if (resourceMap.ContainsKey(key))
            {
                type = resourceMap[key];
            }
            else if (localsMap.Contains(key))
            {
                type = "local";
            }
            return String.Format("{0}.{1}", type, key);
        }

        public static bool AssertFunctionArgumentsLength(FunctionCallSyntax syntax, int count)
        {
            if (syntax.Arguments.Length != count)
            {
                Console.WriteLine("[WARN] Invalid arguments length, expect {0}, got {1}, func: {2}", count, syntax.Arguments.Length, syntax.ToText());
                return false;
            }
            return true;
        }
    }
}
