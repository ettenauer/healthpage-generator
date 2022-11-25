using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AspNetCore.HealthChecks.Generator;

[Generator]
public class HealthChecksExtensionsGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var fileText = context.AdditionalFiles.FirstOrDefault(at => at.Path.EndsWith("health.yml"))?.GetText()?.ToString();
        var healthPageDefinition = deserializer.Deserialize<HealthDefinition>(fileText);
        if (healthPageDefinition != null)
        {
            var sourceCode =
            @$"
            using Microsoft.AspNetCore.Diagnostics.HealthChecks;

            namespace AspNetCore.HealthChecks.Generator
            {{
                public static class HealthChecksDefinitionFileExtensions
                {{
                    public static IServiceCollection AddDefinitionFileHealthChecks(this IServiceCollection source)
                    {{
                        {healthPageDefinition.GenerateUrlGroupCheck()}
                        return source;
                    }}
                }}
            }}";

            context.AddSource($"HealthChecksGeneratorExtensions.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }
    }
    public void Initialize(GeneratorInitializationContext context) { }
}

file class HealthDefinition
{
    public string Name { get; set; }

    public List<HealthDependency> Dependencies { get; set; } = new();

    public string GenerateUrlGroupCheck()
    {
        var sourceCode = new StringBuilder();
        sourceCode.AppendFormat("source.AddHealthChecks()");
        foreach (var dependency in Dependencies)
            sourceCode.AppendFormat("\r\n.AddUrlGroup({0}, name: \"{1}\", tags: new string[] {{{2}}})",
                $"new Uri(\"{dependency.Url}\")", 
                dependency.Name,
                string.Join(",", dependency.Tags.Select(t => $"\"{t}\"")));
        sourceCode.Append(";");

        return sourceCode.ToString();
    }
}

file record HealthDependency
{
    public string Name { get; set; }

    public string Url { get; set; }

    public List<string> Tags { get; set; } = new();
}



