using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;

namespace XmlToMarkdown
{
	/// <summary>This class contains the main entry point for the application.</summary>
	internal class Program
	{
		private static readonly bool isDebugging = Debugger.IsAttached;

		/// <summary>Defines the entry point of the application.</summary>
		/// <param name="args">These are the arguments for the program.</param>
		/// <remarks>
		/// <para>Usage: XmlToMarkdown -i *documentationInputFile* [-o *markdownOutputFile*]</para>
		/// <list type="bullet">
		/// <item><term>-i</term><description>This is a required pathname for the C# XML documentation file.</description></item>
		/// <item><term>-o</term><description>This is the optional pathname for the resulting Markdown file.</description></item>
		/// </list>
		/// <para>If "-o" is not specified, the pathname used is the same as the "-i" file.</para>
		/// </remarks>
		private static void Main(string[] args)
		{
			var builder = new ConfigurationBuilder();
			builder.AddCommandLine(args, new Dictionary<string, string> { { "-i", "documentationInputFile" }, { "-o", "markdownOutputFile" } });
			var config = builder.Build();
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: XmlToMarkdown -i {documentationInputFile} -o {markdownOutputFile}");
				PauseIfDebugging();
				Environment.Exit(1);
			}
			var documentationInputFile = config["documentationInputFile"];
			if (!File.Exists(documentationInputFile))
			{
				Console.WriteLine($"File '{documentationInputFile} does not exist.");
				PauseIfDebugging();
				Environment.Exit(1);
			}
			string markdownOutputFile = config["markdownOutputFile"];
			markdownOutputFile ??= Path.ChangeExtension(documentationInputFile, ".md");
			var xml = File.ReadAllText(documentationInputFile);
			var doc = XDocument.Parse(xml);
			var md = doc.Root.ToMarkDown();
			md = Regex.Replace(md, "\n\n(\n)+", "\n\n");
			if (isDebugging)
			{
				Console.WriteLine(md);
			}
			File.WriteAllText(markdownOutputFile, md);
			Console.WriteLine($"File '{markdownOutputFile} has been created or replaced.");
			PauseIfDebugging();
		}

		/// <summary>Pauses if debugging is enabled.</summary>
		private static void PauseIfDebugging()
		{
			if (isDebugging)
			{
				Console.ReadKey();
			}
		}
	}
}
