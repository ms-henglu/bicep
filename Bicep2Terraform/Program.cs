using System;
using System.Diagnostics;
using System.IO;
using Azure.Deployments.Core.Json;
using Bicep.Core.Navigation;
using Bicep.Core.Parsing;
using Bicep.Core.Syntax;

namespace Bicep2Terraform
{
    internal class Program
    {
        static String template = @"
terraform {
  required_providers {
    azurerm-restapi = {
      source  = ""Azure/azurerm-restapi""
    }
  }
}

provider ""azurerm"" {
  features { }
}

provider ""azurerm-restapi"" {
}

resource ""azurerm_resource_group"" ""test"" {
  name = ""example-resource-group""
  location = ""west europe""
}

data ""azurerm_client_config"" ""current"" {
}
";
        static void Main(string[] args)
        {
            var workingDir = Directory.GetCurrentDirectory();
            foreach (var i in Directory.GetFiles(workingDir, "*.bicep"))
            {
                var bicepContent = File.ReadAllText(i);
                var hclContent = Converter.ToHcl(bicepContent);
                File.WriteAllText(workingDir + "\\main.tf", hclContent);
                File.WriteAllText(workingDir + "\\base.tf", template);
            }

            var p = new Process();
            p.StartInfo.FileName = "terraform";
            p.StartInfo.Arguments = "fmt";
            p.Start();
            p.WaitForExit();
        }
    }
}
