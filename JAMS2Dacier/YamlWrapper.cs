using System;
using System.Collections.Generic;
using System.Text;

namespace JAMS2Dacier;

public class YamlWrapper<T> where T : class, new()
{
    public YamlWrapper()
    {
        ApiVersion = "1.0";
        Metadata = new YamlMetadata();
        Spec = new();
    }
    public string? ApiVersion { get; set; }
    public string? Kind { get; set; }
    public YamlMetadata Metadata { get; }
    public T Spec { get; }
}
