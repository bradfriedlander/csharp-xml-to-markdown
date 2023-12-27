using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace XmlToMarkdown
{
	/// <summary>This class contains the main entry point for the application.</summary>
	internal class Program
	{
		private static readonly bool isDebugging = Debugger.IsAttached;

		/// <summary>Defines the entry point of the application.</summary>
		/// <param name="args">These are the arguments for the program.</param>
		/// <remarks>
		/// <para>Usage: XmlToMarkdown {inputPathname} {outputPathname}</para>
		/// </remarks>
		private static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				Console.WriteLine("Usage: XmlToMarkdown {inputPathname} {outputPathname}");
				PauseIfDebugging();
				Environment.Exit(1);
			}
			if (!File.Exists(args[0]))
			{
				Console.WriteLine($"File '{args[0]} does not exist.");
				PauseIfDebugging();
				Environment.Exit(1);
			}
			var xml = File.ReadAllText(args[0]);
			var doc = XDocument.Parse(xml);
			var md = doc.Root.ToMarkDown();
			md = Regex.Replace(md, "\n\n(\n)+", "\n\n");
			if (isDebugging)
			{
				Console.WriteLine(md);
			}
			File.WriteAllText(args[1], md);
			Console.WriteLine($"File '{args[1]} has been created or replaced.");
			PauseIfDebugging();
		}

		private static void PauseIfDebugging()
		{
			if (isDebugging)
			{
				Console.ReadKey();
			}
		}
	}
}
