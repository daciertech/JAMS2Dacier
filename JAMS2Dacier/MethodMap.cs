using System;
using System.Collections.Generic;
using System.Text;

namespace JAMS2Dacier;

/// <summary>
/// MethodMap is a class used to store the mapping between JAMS method names and Dacier kinds.
/// The mapping is defined in a JSON file called method-mapping.json.
/// </summary>
public class MethodMap
{
    public string? MethodName { get; set; }
    public string? TypeName { get; set; }
    public string? Shell { get; set; }
    public string[]? args { get; set; }
}
