> ***Note: this article is currently under construction...***


# INI File Parser
Represents a parser that uses regular expressions and doesn't use any collections. It can parse different formats of the configuration file (also called as the INI file) and keeps the original file formatting when you edit entries.  

## Introduction

Parsing INI files is a fairly common task in programming when working with configurations. INI files are simple and easy to read by both humans and machines. There are several main ways to implement this:
- Manual parsing using string manipulation functions. This approach allows for maximum flexibility in handling various INI file formats, but requires more effort to implement.
- Using modules of various APIs. They provide ready-made functions for reading, writing, and processing data in the INI format. This is a simpler and faster way, but it is limited by the capabilities of the libraries themselves, and it also makes the project platform-dependent.
- Parsing using common libraries for working with configuration files, such as configparser in Python or .NET's ConfigurationManager. This approach is universal, but may be less flexible than specialized solutions.
- Processing using regular expression.

In this article, I plan to talk about parsing INI files using regular expressions in C#. This is an interesting and powerful approach that allows you to customize the processing logic as much as possible for your needs. Regular expressions provide greater flexibility in parsing file structure, but require a deeper understanding of regular expression syntax. This article will certainly be useful to readers who need customized INI file processing. This approach allows you to preserve the original file formatting, modify existing entries, and add new ones without using collections.
Thus, using regular expressions to parse INI files provides high performance, flexibility, preservation of original formatting and ease of use, which makes this approach an effective solution for working with configuration data in the INI format.

## INI file format

```
# Here is an example of system.ini file
[drivers]
wave=mmdrv.dll
timer=timer.drv
```

This format is quite simple and has long been known to most developers. In general, it is a list of key-value pairs separated by an equal sign, called parameters. For convenience, parameters are grouped into sections, which are enclosed by square brackets. However, despite this, there are still a number of nuances and small differences, since a single standard is not strictly defined. If I create a new parser, my goal is to make it universal, so that it extracts information as efficiently as possible, so when writing a universal parser for working with INI files, these features must be taken into account.

For example, different symbols can be used to indicate comments, the most common options are a hash or a semicolon, as well as various separators between the key and value. In addition to the usual equal sign, a colon is sometimes used in such cases. There are also files in which there are no sections, only key-value pairs. Different systems may use different characters to terminate a line. It is not strongly defined whether the keys "Key" and "key" should be considered different or treated as the same, regardless of case. The file may contain undefined or erroneous data, which, however, should not prevent the correct parsing of valid content.

There is also no consensus on storing arrays of strings. Some standards allow multiple keys with the same name, others - the use of escaped characters to separate strings within the parameter value. Although most often the parser extracts the single value that found first. Our parser can handle all these tasks equally well.

```
;Here is an example of different configuration file standards
key=value ; The key without section

[Section1]
Number1=1
Number2=2

[Section2] ; Indented section
 NumberPI     = 3.14 
 SingleString = Hello, world! ; Comment after value or solid string?
 MultiString  = "ABCDE This is my family\r\nGHIJ I love them every day"
 ArrayString  = "ABCDE This is my family"
 ArrayString  = "GHIJ I love them every day"

[Section3] # Another formatting style
encoding: UTF-8
culture:  en-US
url:      https://www.site.com
```

## Regular expression

After much research, I came up with the following regular expression that allows you to determine the meaning of each character in an ini file. In its entirety it looks like this:  
>`(?=\S)(?<text>(?<comment>(?<open>[#;]+)(?:[^\S\r\n]*)(?<value>.+))|(?<section>(?<open>\[)(?:\s*)(?<value>[^\]]*\S+)(?:[^\S\r\n]*)(?<close>\]))|(?<entry>(?<key>[^=\r\n\ [\]]*\S)(?:[^\S\r\n]*)(?<delimiter>:|=)(?:[^\S\r\n]*)(?<value>[^#;\r\n]*))|(?<undefined>.+))(?<=\S)|(?<linebreaker>\r\n|\n)|(?<whitespace>[^\S\r\n]+)`

Before we move on to writing the code, I want to break down the parsing regular expression itself and explain what each piece is for.

1. `(?=\S)` is a positive lookahead condition that checks that the next character is not a whitespace. This is necessary to skip leading whitespace in the file.

2. `(?<text>....)` is a named group that captures the text block of the file.. This will allow us to get the entire content of the file for further analysis.

3. `(?<comment>(?<open>[#;]+)(?:[^\S\r\n]*)(?<value>.+))` is a named group that captures comments in the file. It consists of:
    - `(?<open>[#;]+)` is a group that captures one or more "#" or ";" characters, denoting the beginning of a comment.
    - `(?:[^\S\r\n]*)` - a group that captures zero or more non-whitespace characters, not including newline characters.
    - `(?<value>.+)` - a group that captures all characters up to the end of the line, i.e. the entire comment text.
    - 
4. `(?<section>(?<open>\[)(?:\s*)(?<value>[^\]]*\S+)(?:[^\S\r\n]*)(?<close>\]))` - is a named group that captures sections. It consists of:
    - `(?<open>\[)` - a group that captures the "[" character, which denotes the beginning of a section.
    - `(?:\s*)` - a group that captures zero or more whitespace characters.
    - `(?<value>[^\]]*\S+)` - a group that captures one or more non-whitespace characters, not including the "]" character.
    - `(?:[^\S\r\n]*)` is a group that captures zero or more non-whitespace characters, not including newline characters.
    - `(?<close>\])` is a group that captures the "]" character, which marks the end of a section.

5. `(?<entry>(?<key>[^=\r\n\[\]]*\S)(?:[^\S\r\n]*)(?<delimiter>:|=)(?:[^\S\r\n]*)(?<value>[^#;\r\n]*))` is a named group that captures entries (key-value). It consists of:
    - `(?<key>[^=\r\n\[\]]*\S)` is a group that captures one or more non-whitespace characters, not including the "=", newline, and "[" characters.
    - `(?:[^\S\r\n]*)` is a group that captures zero or more non-whitespace characters, not including newline characters.
    - `(?<delimiter>:|=)` is a group that captures the ":" or "=" character separating the key and value.
    - `(?:[^\S\r\n]*)` is a group that captures zero or more non-whitespace characters, not including newline characters.
    - `(?<value>[^#;\r\n]*)` is a group that captures zero or more characters, not including "#", ";", newline characters.
6. `(?<undefined>.+)` is a named group that captures any undefined parts of the text that did not match the previous groups.

7. `(?<=\S)` is a positive lookahead condition that checks that the preceding character is not a whitespace character. This is necessary to skip trailing whitespace in the file.

8. `(?<linebreaker>\r\n|\n)` is a named group that captures newline characters ("\r\n" or "\n").

9. `(?<whitespace>[^\S\r\n]+)` is a named group that captures one or more whitespace characters, not including newline characters.

  This is a very detailed and carefully designed regular expression designed to accurately parse the structure of an INI file and extract all the necessary components (sections, keys, values, comments, etc.) from it. It can handle various formatting variations of INI files and provides a robust and flexible way of parsing.

## C-Sharp coding

To solve the problem of parsing INI files using regular expressions, we will create the **IniFile** class. This class will be responsible for reading and parsing the contents of an INI file using regular expressions to extract keys, values, and sections. The class will have methods for loading the file, getting a list of sections, getting values ​​by keys, and writing changes back to the file. By using regular expressions, IniFileRegexParser will be able to handle various INI file formats, including files with comments, spaces, and other features. This will make the parser more flexible and versatile. To use the class, you must pass it a string or stream containing the INI file data and parsing settings.

### Key features of the class
1. Using regular expressions: Using regular expressions allows for flexible and efficient handling of various INI file formats, including support for comments, sections, and key-values. This makes the code more compact and easily extensible compared to using manual string processing.
2. No dependence on collections: The class does not use collections to store INI file data, which makes it more memory efficient and simplifies working with large files.
3. Preserving original formatting: When modifying existing entries or adding new ones, the class preserves the original INI file formatting, including the location of comments, spaces, and line breaks. This helps maintain the readability and structure of the file.
4. Support for escape characters: The class provides the ability to work with escape characters in key values. This allows for the correct handling of special characters such as tabs, line feeds, etc. 5. Automatic detection of line break characters: The class automatically detects the type of line break characters (CRLF, LF, or CR) in the INI file and uses them when saving changes.
6. Flexible customization of string comparison: The class allows you to customize the string comparison rules (case sensitivity, cultural specificity) according to the requirements of the application.
7. Support for various loading and saving methods: The class provides methods for loading INI files from a string, stream, or file, as well as saving them to a stream or file.
8. Convenient API for working with INI files: The class offers a simple and intuitive API for reading and writing values ​​to INI files, including support for various data types.
Thus, using the IniFileRegexParser class allows you to efficiently and flexibly work with INI files, preserving their structure and formatting, and also provides ample opportunities for customization and expansion of functionality.

### Structure of the class
1. **Storing the INI file contents.** The class has a private field *_content* to store the INI file contents.
2. **Regular expression for parsing.** The class uses a regular expression stored in the *_regex* field to parse the INI file.
3. **Support for escaped characters.** The *_allowEscapeChars* flag determines whether escaped characters are allowed in the INI file.
4. **Defining the type of line breaker.** Different operating systems use different methods to mark the end of a line. Before we can process a file, we must determine which method is used in the file.
6. **Culture information.** The *_culture* field contains information about the culture used for parsing. The _lineBreaker field contains the string used to represent line breaks in the INI file. 
7. **String comparison rules.** The *_comparison* field determines how string comparisons are performed in the INI file.
The class provides static Load methods for loading INI files from various sources (string, stream, file) and Save methods for saving the contents of the INI file to various output streams.
In addition, the class contains methods for getting sections, keys, and values ​​from the INI file, as well as for setting values. These methods use a regular expression stored in the _regex field to process the contents of the INI file.
The class also provides a number of helper methods for working with regular expressions, strings, and the file system.

Here is the code responsible for the structure initialization of its parameters.
```csharp
public partial class IniFile
{
	// Private field for storing the content of the INI file.
	private string _content;

	// Regular expression used for parsing the INI file.
	private readonly Regex _regex;

	// Indicates whether escape characters are allowed in the INI file.
	private readonly bool _allowEscapeChars;

	// String used to represent line breaks in the INI file.
	private readonly string _lineBreaker = Environment.NewLine;

	// Contains culture-specific information for parsing.
	private readonly CultureInfo _culture;

	// Determines how string comparisons are performed in the INI file.
	// Configured based on settings passed to the constructor.
	private readonly StringComparison _comparison = StringComparison.InvariantCultureIgnoreCase;
	
	// Constructor accepting ini content as a string and settings.
	// Initializes the parser settings, setting the comparison rules,
	// regular expression pattern, escape character allowance, and delimiter
	// based on the provided settings.
	private IniFile(string content, 
		StringComparison comparison = StringComparison.InvariantCultureIgnoreCase, 
		bool allowEscChars = false)
	{
		if (content == null) content = string.Empty;
		_comparison = comparison;
		_content = content;
		_regex = new Regex(@"(?=\S)(?<text>(?<comment>(?<open>[#;]+)(?:[^\S\r\n]*)(?<value>.+))|" +
						   @"(?<section>(?<open>\[)(?:\s*)(?<value>[^\]]*\S+)(?:[^\S\r\n]*)(?<close>\]))|" +
						   @"(?<entry>(?<key>[^=\r\n\[\]]*\S)(?:[^\S\r\n]*)(?<delimiter>:|=)(?:[^\S\r\n]*)(?<value>[^#;\r\n]*))|" +
						   @"(?<undefined>.+))(?<=\S)|" +
						   @"(?<linebreaker>\r\n|\n)|" +
						   @"(?<whitespace>[^\S\r\n]+)", 
			GetRegexOptions(comparison, RegexOptions.Compiled));
		_culture = GetCultureInfo(_comparison);
		_allowEscapeChars = allowEscChars;
		_lineBreaker = AutoDetectLineBreaker(_content); // Obtaining content-based line breaker.
	}
	
	// Sets or clears the RegexOptions flags based on the specified StringComparison, returning the modified value.
	private static RegexOptions GetRegexOptions(StringComparison comparison,
    RegexOptions options = RegexOptions.None)
	{
		switch (comparison)
		{
			case StringComparison.CurrentCulture:
				options &= ~RegexOptions.CultureInvariant;
				break;
			case StringComparison.CurrentCultureIgnoreCase:
				options &= ~RegexOptions.CultureInvariant;
				options |= RegexOptions.IgnoreCase;
				break;
			case StringComparison.InvariantCulture:
				options |= RegexOptions.CultureInvariant;
				break;
			case StringComparison.InvariantCultureIgnoreCase:
				options |= RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
				break;
			case StringComparison.OrdinalIgnoreCase:
				options |= RegexOptions.IgnoreCase;
				break;
		}

		return options;
	}
	
	// Returns a CultureInfo object that defines the string comparison rules for the specified StringComparison.
	private static CultureInfo GetCultureInfo(StringComparison comparison)
	{
		return comparison < StringComparison.InvariantCulture
			? CultureInfo.CurrentCulture
			: CultureInfo.InvariantCulture;
	}
	
	// This method uses boolean flags to determine if either \r (carriage return) or \n (line feed) characters are present.
	// It stops iterating as soon as it finds both characters.
	private static string AutoDetectLineBreaker(string text)
	{
		if (string.IsNullOrEmpty(text)) return Environment.NewLine;

		bool r = false, n = false;
		
		// Searching for cr and lf characters.
		for (int index = 0; index < text.Length; index++)
		{
			char c = text[index];
			if (c == '\r') r = true;
			if (c == '\n') n = true;

			// If both carriage return and line feed were found, exit the loop.
			if (r && n) break;
		}

		// Determine the line break type based on the flags set.
		return n ? r ? "\r\n" : "\n" : r ? "\r" : Environment.NewLine;
	}
}
```

### Class methods
The IniFileRegexParser class provides convenient methods for reading and writing values ​​of various types to INI files:
1. Reading values:
    - ReadString, ReadStrings - for reading string values, including string arrays.
    - ReadObject, Read<T> - for reading values ​​of arbitrary types using TypeConverter.
    - ReadArray - for reading arrays of values ​​of arbitrary types.
    - Methods for reading primitive data types: ReadBoolean, ReadChar, ReadSByte, ReadByte, ReadInt16, ReadUInt16, ReadInt32, ReadUInt32, ReadInt64, ReadUInt64, ReadSingle, ReadDouble, ReadDecimal, ReadDateTime.
2. Writing values:
    - WriteString, WriteStrings - for writing string values, including string arrays.
    - WriteObject, Write<T> - for writing values ​​of arbitrary types using TypeConverter.
    - WriteArray - for writing arrays of values ​​of arbitrary types.
    - Methods for writing primitive data types: WriteBoolean, WriteChar, WriteSByte, WriteByte, WriteInt16, WriteUInt16, WriteInt32, WriteUInt32, WriteInt64, WriteUInt64, WriteSingle, WriteDouble, WriteDecimal, WriteDateTime. These methods allow you to easily read and write values ​​to INI files, automatically performing type conversion using TypeConverter. This simplifies working with INI files and makes the code more readable and reliable.
3. In addition, the IniFileRegexParser class provides convenient methods for automatically initializing object properties based on data stored in an INI file. This significantly simplifies and speeds up the process of reading and writing settings to INI files.

The ReadSettings and WriteSettings methods allow you to automatically read and write all static properties of a given type, including nested types. This is very useful when an application has many settings distributed across different classes.
The ReadProperty and WriteProperty methods allow you to read and write the values ​​of individual properties of objects. In doing so, they automatically determine the section and key for the property based on its name and type, which eliminates the need for the developer to manually specify this information.
Thus, using these methods greatly simplifies working with INI files, making it more efficient and less prone to errors compared to manually managing reading and writing settings. They also support various data types, including arrays, and provide the ability to use custom type converters.
The IniFileRegexParser class contains a number of additional helper methods for working with parser settings, regular expressions, strings, and the file system:
1. GetCultureInfo(StringComparison): Returns a CultureInfo object that defines the string comparison rules for the specified StringComparison.
2. GetRegexOptions(StringComparison, RegexOptions): Sets or clears the RegexOptions flags based on the specified StringComparison, returning the modified value.
3. GetComparer(StringComparison): Returns a StringComparer object based on the specified StringComparison.
4. ToEscape(string): Escapes special characters in the input string using backslashes.
5. UnHex(string): Converts a hexadecimal number to a Unicode character.
6. UnEscape(string): Converts any escaped characters in the input string.
7. MoveIndexToEndOfLinePosition(StringBuilder, ref int): Moves the index to the end of the current line in the StringBuilder.
8. InsertLine(StringBuilder, ref int, string, string): Inserts the specified string at the specified index in the StringBuilder, followed by the specified new line separator, and updates the index.
9. AutoDetectLineBreaker(string): Determines the type of line separator (\r\n, \n, or \r) in the specified string.
10. MayBeToLower(string, StringComparison): Converts a string to lowercase based on the specified StringComparison.
11. IsInvalidPath(string): Checks if a file name string contains invalid characters for a path.
12. ValidateFileName(string, bool): Checks if a file name is valid and, optionally, whether the file exists.
13. GetFullPath(string, bool): Returns the full path to the file with the given name, checking its validity.
14. GetDeclaringPath(Type, char): Returns the declaration path of the specified type using the specified separator.
These helper methods are used inside the IniFileRegexParser class to ensure correct work with regular expressions, strings, file system and parser settings.

## Conclusion

Using regular expressions to parse INI files in C# provides an efficient and flexible way to handle configuration data. This approach allows not only to parse the contents correctly, but also to preserve the original formatting, which can be critical in some applications.

I hope this article helps you better understand and use regular expressions to work with INI files!
> To be continued...

