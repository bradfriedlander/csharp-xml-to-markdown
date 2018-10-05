using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static System.String;

namespace XmlToMarkdown
{
    internal static class XmlToMarkdown
    {
        private const string ListTableHeader = "| {0} | {1} |\n|-----|------|\n";
        private const string MemberPattern = "^[T|M|P|F|E]:";
        private const string ParameterTableHeader = "| Parameter | Description |\n|-----|------|\n| {0} | {1} |\n";

        private static readonly Func<string, XElement, string[]> d = new Func<string, XElement, string[]>((att, node) =>
        {
            var attValue = node.Attribute(att).Value;
            attValue = Regex.Replace(attValue, MemberPattern, Empty);
            return new[]
            {
                    attValue,
                    node.Nodes().ToMarkDown()
                    };
        });

        private static readonly Func<string, XElement, string[]> dl = new Func<string, XElement, string[]>((att, node) =>
        {
            var attValue = node.Attribute(att).Value;
            attValue = Regex.Replace(attValue, MemberPattern, Empty);
            return new[]
            {
                    attValue.ToLower(),
                    node.Nodes().ToMarkDown()
                    };
        });

        private static readonly Dictionary<string, string> templates = BuildTemplates();
        private static string currentlistPrefix;
        private static bool isListTable;
        private static Dictionary<string, Func<XElement, IEnumerable<string>>> methods;
        private static bool previousWasParameter;
        private static string rootName;
        private static XElement namespaceElement;

        internal static string ToMarkDown(this XNode e)
        {
            if (methods == null)
            {
                BuildMethods();
            }
            string name;
            if (e.NodeType == XmlNodeType.Element)
            {
                var el = (XElement)e;
                name = el.Name.LocalName;
                if (name == "doc")
                {
                    rootName = el.Element("assembly").Element("name").Value;
                    namespaceElement = el.Element("members").Elements("member").Where(m => IsNamespaceDocElement(m)).FirstOrDefault();
                }
                var previousNode = e.PreviousNode;
                previousWasParameter =
                    previousNode != null
                    && previousNode.NodeType == XmlNodeType.Element
                    && (((XElement)previousNode).Name.LocalName == "param" || ((XElement)previousNode).Name.LocalName == "typeparam");
                if (previousWasParameter && (name == "param" || name == "typeparam"))
                {
                    name = "param2";
                }
                if (name == "member")
                {
                    switch (el.Attribute("name").Value[0])
                    {
                        case 'F':
                            name = "field";
                            break;
                        case 'P':
                            name = "property";
                            break;
                        case 'T':
                            name = "type";
                            if (IsNamespaceDocElement(el))
                            {
                                name = "none";
                            }
                            break;
                        case 'E':
                            name = "event";
                            break;
                        case 'M':
                            name = "method";
                            break;
                        default:
                            name = "none";
                            break;
                    }
                }
                if (name == "see" || name == "seealso")
                {
                    var attValue = Regex.Replace(el.Attribute("cref").Value, MemberPattern, Empty);
                    name = attValue.StartsWith("!:#") ? "seeAnchor" : attValue.StartsWith(rootName) ? "seeHeader" : "seePage";
                }
                if (name == "list")
                {
                    var attValue = el.Attribute("type").Value;
                    switch (attValue)
                    {
                        case "number":
                            currentlistPrefix = "1.";
                            isListTable = false;
                            break;
                        case "bullet":
                            currentlistPrefix = "*";
                            isListTable = false;
                            break;
                        case "table":
                            currentlistPrefix = "|";
                            isListTable = true;
                            break;
                        default:
                            currentlistPrefix = Empty;
                            isListTable = false;
                            break;
                    }
                }
                if (name == "item" && isListTable)
                {
                    name = "tableitem";
                }
                return Format(templates[name], methods[name](el).ToArray());
            }
            return e.NodeType == XmlNodeType.Text ? Regex.Replace(((XText)e).Value.Replace('\n', ' '), @"\s+", " ") : Empty;
        }

        internal static string ToMarkDown(this IEnumerable<XNode> es) => es.Aggregate("", (current, x) => current + x.ToMarkDown());

        private static void BuildMethods()
        {
            methods = new Dictionary<string, Func<XElement, IEnumerable<string>>>
            {
                {"doc", x=> new[]
                {
                    rootName,
                    namespaceElement?.Nodes().ToMarkDown() ?? Empty,
                    x.Element("members").Elements("member").ToMarkDown()
                }},
                {"type", x=>d("name", x)},
                {"field", x=> d("name", x)},
                {"property", x=> d("name", x)},
                {"method", x=>d("name", x)},
                {"event", x=>d("name", x)},
                {"summary", x=> new[]{x.Nodes().ToMarkDown()}},
                {"remarks", x => new[]{x.Nodes().ToMarkDown()}},
                {"example", x => new[]{x.Value.ToCodeBlock()}},
                {"seePage", x=> d("cref", x) },
                {"seeAnchor", x=> dl("cref",x)},
                {"seeHeader", x=> d("cref", x) },
                {"param", x => d("name", x) },
                {"typeparam", x => d("name", x) },
                {"param2", x => d("name", x) },
                {"paramref",x=> d("name", x) },
                {"typeparamref",x=> d("name", x) },
                {"exception", x => d("cref", x) },
                {"returns", x => new[]{x.Nodes().ToMarkDown()}},
                {"para", x => new[]{x.Nodes().ToMarkDown()}},
                {"value", x=> new[]{x.Nodes().ToMarkDown()}},
                {"c", x=> new[]{x.Nodes().ToMarkDown()}},
                {"list", x=> new[]{x.Nodes().ToMarkDown()}},
                {"listheader", x=> new[]
                {
                    x.Element("term").ToMarkDown(),
                    x.Element("description").ToMarkDown()
                }},
                {"tableitem", x=> new[]
                {
                    x.Element("term").ToMarkDown(),
                    x.Element("description").ToMarkDown()
                }},
                {"item", x=> new[]
                {
                    currentlistPrefix,
                    x.Nodes().ToMarkDown()
                }},
                {"term", x => new[]{x.Nodes().ToMarkDown()} },
                {"description", x => new[]{x.Nodes().ToMarkDown()}},
                {"none", x => new string[0]}
            };
        }

        private static Dictionary<string, string> BuildTemplates()
        {
            return new Dictionary<string, string>
            {
                {"doc", "# {0}\n\n{1}\n\n{2}\n\n"},
                {"type", "## {0}\n\n{1}\n\n---\n"},
                {"field", "### {0}\n\n{1}\n\n---\n"},
                {"property", "### {0}\n\n{1}\n\n---\n"},
                {"method", "### {0}\n\n{1}\n\n---\n"},
                {"event", "### {0}\n\n{1}\n\n---\n"},
                {"summary", "{0}\n\n"},
                {"remarks", "\n\n{0}\n\n"},
                {"example", "_C# code_\n\n```c#\n{0}\n```\n\n"},
                {"seePage", "`{0}`"},
                {"seeAnchor", "[{1}]({0})"},
                {"seeHeader", "[{0}](#{0})"},
                {"param", ParameterTableHeader },
                {"typeparam", ParameterTableHeader },
                {"param2", "| {0} | {1} |\n" },
                {"paramref", "`{0}`" },
                {"typeparamref", "`{0}`"},
                {"exception", "\n**Exception:** [{0}({0})]: {1}\n\n" },
                {"returns", "\n**Returns:** {0}\n\n"},
                {"para", "\n\n{0}\n\n"},
                {"value","**Value:** {0}\n\n" },
                {"c", "`{0}`" },
                {"list", "{0}\n\n"},
                {"listheader", ListTableHeader },
                {"tableitem", "| {0} | {1} |\n" },
                {"item", "{0} {1}\n"},
                {"term", "{0}" },
                {"description", "{0}" },
                {"none", Empty}
            };
        }

        /// <summary>
        ///     This method determines if the specified element is a NamespaceDoc class.
        /// </summary>
        /// <param name="elementToCheck">This is the element to check.</param>
        /// <returns><c>true</c> if the specified element is a NamespaceDoc class; otherwise, <c>false</c>.</returns>
        private static bool IsNamespaceDocElement(XElement elementToCheck)
        {
            if (elementToCheck.Name.LocalName != "member")
            {
                return false;
            }
            var memberName = elementToCheck.Attribute("name").Value;
            return memberName.StartsWith("T:") && memberName.EndsWith(".NamespaceDoc");
        }

        private static string ToCodeBlock(this string s)
        {
            var lines = s.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var blank = lines[0].TakeWhile(x => x == ' ').Count() - 4;
            return Join("\n", lines.Select(x => new string(x.SkipWhile((y, i) => i < blank).ToArray())));
        }
    }
}
