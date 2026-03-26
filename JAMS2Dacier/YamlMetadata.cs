
using System.Collections.ObjectModel;

namespace JAMS2Dacier;

/// <summary>
/// Defines the metadata for a YAML file.
/// </summary>
public class YamlMetadata
{
    /// <summary>
    /// The name of the object defined by the YAML file.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// A description of the object defined by the YAML file.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The tags associated with the object defined by the YAML file.
    /// </summary>
    public Collection<string>? Tags { get; init; } = new();
}
