using System.Collections.ObjectModel;
using System.CommandLine;
using System.Text.Json;
using Dacier.Core;
using Dacier.Scheduler;
using YamlDotNet.Serialization;

namespace JAMS2Dacier;

internal class ConvertCommand : Command
{
    public ConvertCommand() : base("convert", "Converts JAMS XML definitions into Dacier YAML definitions")
    {
        var inputDirectoryArgument = new Argument<string>("input")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The path to the XML input files."
        };
        Arguments.Add(inputDirectoryArgument);

        var outputDirectoryArgument = new Argument<string>("output")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The path to the output YAML files."
        };
        Arguments.Add(outputDirectoryArgument);

        this.SetAction(parseResult => ConvertCommandAction(
            parseResult,
            parseResult.GetValue(inputDirectoryArgument),
            parseResult.GetValue(outputDirectoryArgument)));
    }

    public async Task ConvertCommandAction(
        ParseResult parseResult,
        string? inputDirectory,
        string? outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(inputDirectory) || string.IsNullOrWhiteSpace(outputDirectory))
        {
            Console.WriteLine($"You must specify the input and output directories:");
            Console.WriteLine($"Usage: JAMS2Dacier convert <input directory> <output directory>");
            return;
        }

        //
        //  Read the method mapping configuration
        //
        string mappingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "method-mapping.json");
        if (!File.Exists(mappingPath))
        {
            Console.WriteLine($"Method mapping config not found: {mappingPath}");
            return;
        }

        var methodMappings = new Dictionary<string, MethodMap>(StringComparer.OrdinalIgnoreCase);
        using var methodMappingJson = System.Text.Json.JsonDocument.Parse(File.ReadAllText(mappingPath));
        foreach (var element in methodMappingJson.RootElement.EnumerateArray())
        {
            // Deserialize each element into a Person object
            var methodMap = JsonSerializer.Deserialize<MethodMap>(element.GetRawText());
            if ((methodMap != null) && (!string.IsNullOrWhiteSpace(methodMap.MethodName)))
            {
                methodMappings[methodMap.MethodName] = methodMap;
            }
        }

        //
        //  Grab all XML files and convert them
        //
        var fullInputPath = Path.GetFullPath(inputDirectory);
        var xmlFiles = Directory.GetFiles(fullInputPath, "*.xml", SearchOption.AllDirectories);
        foreach (var xmlFile in xmlFiles)
        {
            var relPath = Path.GetRelativePath(inputDirectory, xmlFile);
            var outPath = Path.Combine(outputDirectory, Path.ChangeExtension(relPath, ".dacier.yaml"));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            var yamlSerializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
                .Build();

            var doc = new System.Xml.XmlDocument();
            doc.Load(xmlFile);
            var jamsObjects = doc.DocumentElement;
            if (jamsObjects == null || jamsObjects.Name != "JAMSObjects")
            {
                Console.WriteLine($"Warning: The file {xmlFile} did not contain a <JAMSObjects>");
                continue;
            }
            var objNode = jamsObjects.FirstChild;
            if (objNode == null)
            {
                Console.WriteLine($"Warning: The file {xmlFile} did not contain an object");
                continue;
            }

            if (objNode.Name == "folder")
            {
                // Folder handling
                var folderWrapper = new YamlWrapper<Folder>();
                folderWrapper.Kind = "folder";
                folderWrapper.Metadata.Name = objNode.Attributes?["name"]?.Value ?? "(unknown)";
                folderWrapper.Metadata.Description = objNode.SelectSingleNode("description")?.Value ?? string.Empty;
                var tagList = objNode.Attributes?["tags"]?.Value?.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                ConvertParametersToVariables(objNode, folderWrapper.Spec);
                using var writer = new StreamWriter(outPath);
                yamlSerializer.Serialize(writer, folderWrapper);
            }
            else if (objNode.Name == "job")
            {
                var methodAttr = objNode.Attributes?["method"]?.Value;
                var jobName = objNode.Attributes?["name"]?.Value ?? "(unknown)";
                if (string.IsNullOrEmpty(methodAttr) || !methodMappings.TryGetValue(methodAttr, out var methodMap))
                {
                    Console.WriteLine($"Warning: Method '{methodAttr}' not found for job '{jobName}' in {relPath}");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(methodMap.TypeName))
                {
                    Console.WriteLine($"Warning: Method '{methodAttr}' is missing the TypeName for job '{jobName}' in {relPath}");
                    continue;
                }

                var type = Type.GetType(methodMap.TypeName);
                if (type == null)
                {
                    Console.WriteLine($"Warning: Dacier type '{methodMap.TypeName}' not found for method '{methodAttr}' in job '{jobName}'");
                    continue;
                }
                var wrapperType = typeof(YamlWrapper<>).MakeGenericType(type);
                var jobObj = Activator.CreateInstance(wrapperType);
                if (jobObj == null)
                {
                    Console.WriteLine($"Warning: Unable to create an instance of '{methodMap.TypeName}' for method '{methodAttr}' in job '{jobName}'");
                    continue;
                }
                if (type.GetCustomAttributes(typeof(YamlKindAttribute), false).FirstOrDefault() is YamlKindAttribute yamlKind)
                {
                    dynamic jobWrapper = jobObj;
                    jobWrapper.Kind = yamlKind.KindName;
                    jobWrapper.Metadata.Name = jobName;
                    jobWrapper.Metadata.Description = objNode.SelectSingleNode("description")?.Value ?? string.Empty;

                    switch (yamlKind.KindName)
                    {
                        case "cli":
                            ConvertCliJob(methodMap, objNode, jobWrapper.Spec);
                            break;
                        default:
                            Console.WriteLine($"Warning: The kind: '{yamlKind.KindName}' is not supported for method '{methodAttr}' in job '{jobName}'");
                            break;
                    }
                    ConvertParametersToVariables(objNode, jobObj);
                    using var writer = new StreamWriter(outPath);
                    yamlSerializer.Serialize(writer, jobObj);
                }
                else
                {
                    Console.WriteLine($"Warning: Type '{methodMap.TypeName}' does not have a YamlKindAttribute for method '{methodAttr}' in job '{jobName}'");
                }
            }
        }

    }

    private static void ConvertCliJob(MethodMap methodMap, System.Xml.XmlNode xmlNode, Dacier.CliJobs.CliJob jobObj)
    {
        jobObj.Shell = methodMap.Shell ?? string.Empty;
        jobObj.Script = "";
        jobObj.Script = xmlNode.SelectSingleNode("source")?.Value ?? string.Empty;
        jobObj.Result = "";
        foreach (var arg in methodMap.args ?? Array.Empty<string>())
        {
            jobObj.Arguments.Add(arg);
        }
    }


    /// <summary>
    /// Convert Parameters to Variables.
    /// </summary>
    /// <param name="xmlNode"></param>
    /// <param name="target"></param>
    private static void ConvertParametersToVariables(System.Xml.XmlNode xmlNode, object target)
    {
        var paramsNode = xmlNode.SelectSingleNode("parameters");
        if (paramsNode == null) return;

        var variablesProp = target as ISupportVariables;
        if (variablesProp == null) return;

        foreach (System.Xml.XmlNode param in paramsNode.ChildNodes)
        {
            if (param.NodeType != System.Xml.XmlNodeType.Element) continue;

            var inherited = bool.Parse(param.Attributes?["inherited"]?.Value ?? "False");
            if (inherited) continue;

            var varObj = new VariableDefinition();
            varObj.VariableName = param.Attributes?["name"]?.Value ?? "unknown";
            switch (param.Attributes?["dataType"]?.Value ?? "Unspecified")
            {
                case "Date":
                    varObj.DataType = DataType.DateTime;
                    break;
                case "int":
                case "integer":
                    varObj.DataType = DataType.Int;
                    break;
                case "bool":
                case "boolean":
                    varObj.DataType = DataType.Boolean;
                    break;
                default:
                    varObj.DataType = DataType.String;
                    break;
            }
            varObj.MaximumLength = int.Parse(param.Attributes?["length"]?.Value ?? "0");

            foreach(var property in param.SelectSingleNode("properties")?.ChildNodes.OfType<System.Xml.XmlElement>() ?? Enumerable.Empty<System.Xml.XmlElement>())
            {
                Console.WriteLine($"Found property for parameter '{varObj.VariableName}': {property.Name} = {property.Value}");
                var propName = property.Name;
                var propValue = property.Value;
            }   

            variablesProp.VariableDefinitions.Add(varObj);
        }
    }
}
