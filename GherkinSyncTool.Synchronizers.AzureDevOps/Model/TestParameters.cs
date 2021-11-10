using System.Collections.Generic;
using System.Xml.Serialization;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Model
{
    [XmlRoot(ElementName = "param")]
    public class Param
    {
        [XmlAttribute(AttributeName = "name")] public string Name { get; set; }
        [XmlAttribute(AttributeName = "bind")] public string Bind { get; set; } = "default";
    }

    [XmlRoot(ElementName = "parameters")]
    public class TestParameters
    {
        [XmlElement(ElementName = "param")] public List<Param> Param { get; set; }
    }
}