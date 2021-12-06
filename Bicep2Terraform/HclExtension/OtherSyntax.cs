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
        public static string ToHcl(this ParameterDefaultValueSyntax syntax, string indent = "")
        {
            return syntax.DefaultValue.ToHcl();
        }

        public static string ToHcl(this TypeSyntax syntax, string indent = "")
        {
            var type = syntax.TypeName;
            switch (type)
            {
                case "array":
                    return "list";
                case "int":
                    return "number";
                case "object":
                    return "map";
                default:
                    return type;
            }
        }

    }
}
