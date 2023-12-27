using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static System.String;

namespace XmlToMarkdown
{
	/// <summary>This class supports the conversion of C# generated document to Markdown.</summary>
	internal static class XmlToMarkdown
	{
		private const string ListTableHeader = "|{0}|{1}|\n|:--|:--|\n";
		private const string MemberPattern = "^[T|M|P|F|E]:";
		private const string ParameterTableHeader = "|Parameter|Description|\n|:--|:--|\n|{0}|{1}|\n";

		/// <summary>This method gets node data.</summary>
		private static readonly Func<string, XElement, string[]> getNodeData = new((att, node) =>
		{
			var attValue = node.Attribute(att).Value;
			attValue = Regex.Replace(attValue, MemberPattern, Empty);
			return new[]
			{
				attValue,
				node.Nodes().ToMarkDown()
				};
		});

		/// <summary>
		/// <para>This method gets node data for an internal page anchor.</para>
		/// </summary>
		private static readonly Func<string, XElement, string[]> getCrefData = new((att, node) =>
		{
			var attValue = node.Attribute(att).Value;
			attValue = Regex.Replace(attValue, MemberPattern, Empty);
			return new[]
			{
				attValue,
				attValue.Replace(".",Empty).ToLower(),
				node.Nodes().ToMarkDown()
				};
		});

		/// <summary>
		/// <para>This method gets node data for and external reference.</para>
		/// </summary>
		private static readonly Func<string, XElement, string[]> getLowercaseRefData = new((att, node) =>
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
		private static readonly char[] lineSeparator = new char[] { '\n' };

		/// <summary>This method converts the current C# documentation node to markdown.</summary>
		/// <param name="thisNode">This is a C# documentation node.</param>
		/// <returns>This is a string containing the Markdown content.</returns>
		internal static string ToMarkDown(this XNode thisNode)
		{
			methods ??= BuildMethods();
			string elementName;
			if (thisNode.NodeType == XmlNodeType.Element)
			{
				var thisElement = (XElement)thisNode;
				elementName = thisElement.Name.LocalName;
				if (elementName == "doc")
				{
					rootName = thisElement.Element("assembly").Element("name").Value;
					namespaceElement = thisElement.Element("members").Elements("member").Where(m => IsNamespaceDocElement(m)).FirstOrDefault();
				}
				var previousNode = thisNode.PreviousNode;
				previousWasParameter =
					previousNode != null
					&& previousNode.NodeType == XmlNodeType.Element
					&& (((XElement)previousNode).Name.LocalName == "param" || ((XElement)previousNode).Name.LocalName == "typeparam");
				if (previousWasParameter && (elementName == "param" || elementName == "typeparam"))
				{
					elementName = "param2";
				}
				if (elementName == "member")
				{
					switch (thisElement.Attribute("name").Value[0])
					{
						case 'F':
							elementName = "field";
							break;

						case 'P':
							elementName = "property";
							break;

						case 'T':
							elementName = "type";
							if (IsNamespaceDocElement(thisElement))
							{
								elementName = "none";
							}
							break;

						case 'E':
							elementName = "event";
							break;

						case 'M':
							elementName = "method";
							break;

						default:
							elementName = "none";
							break;
					}
				}
				if (elementName == "see" || elementName == "seealso")
				{
					var attValue = Regex.Replace(thisElement.Attribute("cref").Value, MemberPattern, Empty);
					elementName = attValue.StartsWith("!:#") ? "seeAnchor" : attValue.StartsWith(rootName) ? "seeHeader" : "seePage";
				}
				if (elementName == "list")
				{
					var attValue = thisElement.Attribute("type").Value;
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
				if (elementName == "item" && isListTable)
				{
					elementName = "tableitem";
				}
				return Format(templates[elementName], methods[elementName](thisElement).ToArray());
			}
			return thisNode.NodeType == XmlNodeType.Text ? Regex.Replace(((XText)thisNode).Value.Replace("\n ", " ").Replace('\n', ' '), @"\s+", " ") : Empty;
		}

		/// <summary>This method converts a set of C# documentation nodes to markdown content.</summary>
		/// <param name="es">This is the set of C# documentation nodes.</param>
		/// <returns>This is the Markdown content.</returns>
		internal static string ToMarkDown(this IEnumerable<XNode> es)
		{
			return es.Aggregate("", (current, x) => current + x.ToMarkDown());
		}

		/// <summary>This method builds a dictionary of markdown generation methods.</summary>
		/// <returns>This is the dictionary of markdown generation methods.</returns>
		private static Dictionary<string, Func<XElement, IEnumerable<string>>> BuildMethods()
		{
			return new Dictionary<string, Func<XElement, IEnumerable<string>>>
			{
				{"doc", x=> new[]
				{
					rootName,
					namespaceElement?.Nodes().ToMarkDown() ?? Empty,
					x.Element("members").Elements("member").ToMarkDown()
				}},
				{"type", x=>getNodeData("name", x)},
				{"field", x=> getNodeData("name", x)},
				{"property", x=> getNodeData("name", x)},
				{"method", x=>getNodeData("name", x)},
				{"event", x=>getNodeData("name", x)},
				{"summary", x=> new[]{x.Nodes().ToMarkDown().Trim()}},
				{"remarks", x => new[]{x.Nodes().ToMarkDown().Trim()}},
				{"example", x => new[]{x.Value.ToCodeBlock()}},
				{"seePage", x=> getNodeData("cref", x) },
				{"seeAnchor", x=> getLowercaseRefData("cref",x)},
				{"seeHeader", x=> getCrefData("cref", x) },
				{"param", x => getNodeData("name", x) },
				{"typeparam", x => getNodeData("name", x) },
				{"param2", x => getNodeData("name", x) },
				{"paramref",x=> getNodeData("name", x) },
				{"typeparamref",x=> getNodeData("name", x) },
				{"exception", x => getNodeData("cref", x) },
				{"returns", x => new[]{x.Nodes().ToMarkDown().Trim()}},
				{"para", x => new[]{x.Nodes().ToMarkDown().Trim()}},
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
				{"term", x => new[]{x.Nodes().ToMarkDown()}},
				{"description", x => new[]{x.Nodes().ToMarkDown()}},
				{"none", x => Array.Empty<string>()}
			};
		}

		/// <summary>This method builds the template dictionary.</summary>
		/// <returns>This is the template dictionary.</returns>
		private static Dictionary<string, string> BuildTemplates()
		{
			return new Dictionary<string, string>
			{
				{"doc", "# {0}\n\n{1}\n\n{2}\n\n"},
				{"type", "## {0}\n\n{1}\n\n---\n\n"},
				{"field", "### {0}\n\n{1}\n\n---\n\n"},
				{"property", "### {0}\n\n{1}\n\n---\n\n"},
				{"method", "### {0}\n\n{1}\n\n---\n\n"},
				{"event", "### {0}\n\n{1}\n\n---\n\n"},
				{"summary", "{0}\n\n"},
				{"remarks", "\n\n{0}\n\n"},
				{"example", "\n**C# Example**\n```c#\n{0}\n```\n\n"},
				{"seePage", "`{0}`"},
				{"seeAnchor", "[{1}]({0})"},
				{"seeHeader", "[{0}](#{1})"},
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
				{"tableitem", "|{0}|{1}|\n" },
				{"item", "{0} {1}\n"},
				{"term", "{0}: " },
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

		/// <summary>This method converts an "example" documentation block into a Markdown code block.</summary>
		/// <param name="exampleContent">This is the string containing the example code.</param>
		/// <example>
		/// var codeBlockText = exampleContent.ToCodeBlock();
		/// </example>
		/// <returns>This is formatted code block.</returns>
		private static string ToCodeBlock(this string exampleContent)
		{
			var lines = exampleContent.TrimEnd().Split(lineSeparator, StringSplitOptions.RemoveEmptyEntries);
			var blank = lines[0].TakeWhile(x => x == ' ').Count() - 2;
			return Join("\n", lines.Select(x => new string(x.SkipWhile((y, i) => i < blank).ToArray())));
		}
	}
}
