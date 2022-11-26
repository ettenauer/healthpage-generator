using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
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
            var sourceCode = @$"
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace AspNetCore.HealthChecks.Generator
{{
    public static class HealthChecksDefinitionFileExtensions
    {{
        public static IServiceCollection AddDefinitionFileHealthChecks(this IServiceCollection source)
        {{
            {healthPageDefinition.GenerateSourceCode()}
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

    public string GenerateSourceCode()
    {
        var sourceCode = new StringBuilder();
        sourceCode.AppendFormat("source.AddHealthChecks()");

        foreach (var dependency in Dependencies)
            sourceCode.Append(dependency.GenerateSourceCode());

        sourceCode.Append(";");

        return sourceCode.ToString();
    }
}

file record HealthDependency
{
    public string Name { get; set; }

    public string ConnectionString { get; set; }

    public List<string> Tags { get; set; } = new();

    public string Type { get; set; }

    public string GenerateSourceCode()
    {
        static string resolveTags(List<string> tags) => string.Join(",", tags.Select(t => $"\"{t}\""));

        return Type.ToLowerInvariant() switch {
            "uri" => $"\r\n\t\t\t.AddUrlGroup(new Uri(\"{ConnectionString}\"), name: \"{Name}\", tags: new string[] {{{resolveTags(Tags)}}})",
            "sqlserver" => $"\r\n\t\t\t.AddSqlServer(\"{ConnectionString}\", name: \"{Name}\", tags: new string[] {{{resolveTags(Tags)}}})",
            _ => throw new NotImplementedException($"SourceCode for {Type} is not implemented")
        };
    }
}



