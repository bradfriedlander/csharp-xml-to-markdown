# XmlToMarkdown

## XmlToMarkdown.Program

This calss contains the main entry point for the application.

---

### XmlToMarkdown.Program.Main(System.String[])

Defines the entry point of the application.

|Parameter|Description|
|:--|:--|
|args|These are the arguments for the program.|

Usage: XmlToMarkdown {inputPathname} {outputPathname}

---

## XmlToMarkdown.XmlToMarkdown

This class supports the conversion of C# generated document to Markdown.

---

### XmlToMarkdown.XmlToMarkdown.getNodeData

This method gets node data.

---

### XmlToMarkdown.XmlToMarkdown.getCrefData

This method gets node data for an internal page anchor.

---

### XmlToMarkdown.XmlToMarkdown.getLowercaseRefData

This method gets node data for and external reference.

---

### XmlToMarkdown.XmlToMarkdown.ToMarkDown(System.Xml.Linq.XNode)

This method converts the current C# documentation node to markdown.

|Parameter|Description|
|:--|:--|
|thisNode|This is a C# documentation node.|

**Returns:** This is a string containing the Markdown content.

---

### XmlToMarkdown.XmlToMarkdown.ToMarkDown(System.Collections.Generic.IEnumerable{System.Xml.Linq.XNode})

This method converts a set of C# documentation nodes to markdown content.

|Parameter|Description|
|:--|:--|
|es|This is the set of C# documentation nodes.|

**Returns:** This is the Markdown content.

---

### XmlToMarkdown.XmlToMarkdown.BuildMethods

This method builds a dictionary of markdown generation methods.

**Returns:** This is the dictionary of markdown generation methods.

---

### XmlToMarkdown.XmlToMarkdown.BuildTemplates

This method builds the template dictionary.

**Returns:** This is the template dictionary.

---

### XmlToMarkdown.XmlToMarkdown.IsNamespaceDocElement(System.Xml.Linq.XElement)

This method determines if the specified element is a NamespaceDoc class.

|Parameter|Description|
|:--|:--|
|elementToCheck|This is the element to check.|

**Returns:** `true` if the specified element is a NamespaceDoc class; otherwise, `false`.

---

### XmlToMarkdown.XmlToMarkdown.ToCodeBlock(System.String)

This method converts an "example" documentation block into a Markdown code block.

|Parameter|Description|
|:--|:--|
|exampleContent|This is the string containing the example code.|

**C# Example**
```c#
  var codeBlockText = exampleContent.ToCodeBlock();
```

**Returns:** This is formatted code block.

---

