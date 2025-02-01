# IniFile Mini

Quick and easy ini file reading and writing that's also quite powerful. This is based on the original IniFile parser by 
Represents a parser designed for easy reading, writing, and managing INI files that uses regular expressions. It can parse different formats of the configuration file (also called as the INI file) and keeps the original file formatting when you edit entries. It supports sections, keys, class serialization and deserialization with attributes, and flexible data handling using type converters.

## Introduction
There is an opinion that the INI file is outdated and is not suitable for storing parameters. I will not argue with this statement, but I will give several examples when using an INI file is justified and is the optimal solution:
1. If your software works with the command line, it is more convenient to group and transfer a large number of parameters in bulk via an INI file.
2. If your software uses third-party utilities that accept parameters via INI.
3. If your software has a small number of parameters and does not have a "settings" window or any graphical interface at all.
4. If you need the ability to export and import parameters to a file in a format understandable to the user.
5. Just as a backup option or as a nod to the past when the grass was greener and we were younger, when Doom was installed from a floppy disk, and parameters were transferred via INI.

### Key features of this parser

- **Read values from INI files with type conversion**:
```csharp
int userId = iniFile.Read("settings", "user_id", -1); // Default = -1
```
 - **Write values to sections**:
```csharp
iniFile.Write("settings", "theme", "dark");
```
- **Load a file**:
```csharp
IniFile iniFile = IniFile.Load("config.ini");
```
- **Save changes**:
```csharp
iniFile.Save("updated_config.ini");
```
- **Support for encoding via the `Encoding` parameter**:
```csharp
IniFile iniFile = IniFile.Load("config.ini", Encoding.UTF8);
iniFile.Save("updated_config.ini, Encoding.UTF8");
```
- **Object Serialization and Deserialization**:  
Using attributes, you can automatically map INI file data to classes and their properties.
```csharp
[IniSection("settings")]
public class Settings
{
    [IniEntry("lang")]
    public string Language { get; set; }

    // You don't need to specify it if the name of the property matches the parameter.
    // [IniEntry("theme")] 
    public string Theme { get; set; }

    [IniEntry("vol")]
    public int Volume { get; set; }
}

class Program
{
    static void Main()
    {
        IniFile iniFile = IniFile.Load("config.ini");

        // Read object
        Settings settings = new Settings();
        iniFile.ReadSettings(settings);

        // Modify data
        settings.Theme = "light";
        settings.Volume = 50;

        // Save back to file
        iiniFile.WriteSettings(settings);
        iniFile.Save("updated_config.ini");
    }
}
```

If the INI file contains:
```
[settings]
lang=en
theme=dark
vol=80
```

The code creates the object:
```csharp
settings.Language = "en";
settings.Theme = "dark";
settings.Volume = 80;
```

## Installation
Simply add `IniFile.cs` to your project and start using it. It is ideal for small solutions without connecting third-party libraries.

## License
This project is distributed under the MIT license. You are free to use and modify it as needed.

---

# Background
 
Parsing INI files is a fairly common task in programming when working with configurations. INI files are simple and easy to read by both humans and machines. There are several main ways to implement this:
- Manual parsing using string manipulation functions. This approach allows for maximum flexibility in handling various INI file formats, but requires more effort to implement.
- Using modules of various APIs. They provide ready-made functions for reading, writing, and processing data in the INI format. This is a simpler and faster way, but it is limited by the capabilities of the libraries themselves, and it also makes the project platform-dependent.
- Parsing using common libraries for working with configuration files, such as configparser in Python or .NET's ConfigurationManager. This approach is universal, but may be less flexible than specialized solutions.
- Processing using regular expression.

In this article, I plan to talk about parsing INI files using regular expressions in C#. This is an interesting and powerful approach that allows you to customize the processing logic as much as possible for your needs. Regular expressions provide greater flexibility in parsing file structure, but require a deeper understanding of regular expression syntax. This article will certainly be useful to readers who need customized INI file processing. This approach allows you to preserve the original file formatting, modify existing entries, and add new ones without using collections.
Thus, using regular expressions to parse INI files provides high performance, flexibility, preservation of original formatting and ease of use, which makes this approach an effective solution for working with configuration data in the INI format.

## INI file format

This format is quite simple and has long been known to most developers. In general, it is a list of key-value pairs separated by an equal sign, called parameters. For convenience, parameters are grouped into sections, which are enclosed by square brackets. However, despite this, there are still a number of nuances and small differences, since a single standard is not strictly defined. If I create a new parser, my goal is to make it universal, so that it extracts information as efficiently as possible, so when writing a universal parser for working with INI files, these features must be taken into account.

![image](https://github.com/user-attachments/assets/517e69ff-1a5a-44ce-912b-d1a21d43ad65)

For example, different symbols can be used to indicate comments, the most common options are a hash or a semicolon, as well as various separators between the key and value. In addition to the usual equal sign, a colon is sometimes used in such cases. There are also files in which there are no sections, only key-value pairs. Different systems may use different characters to terminate a line. It is not strongly defined whether the keys "Key" and "key" should be considered different or treated as the same, regardless of case. The file may contain syntax errors or undefined data, which, however, should not prevent the correct parsing of valid content.

There is also no consensus on storing arrays of strings. Some standards allow multiple keys with the same name, others - the use of escaped characters to separate strings within the parameter value. Although most often the parser extracts the single value that found first. Our parser can handle all these tasks equally well.

Here is an example of syntax highlighting using a popular text editor. As you can see, its format does not provide for a comment after the section name or entry value.

![image](https://github.com/user-attachments/assets/f0d7bfc9-fa28-4d3a-98f4-619e16a8a572)

## Regular expression

After much research, I came up with the following regular expression that allows you to determine the meaning of each character in an ini file. In its entirety it looks like this:  
```regex
(?=\S)(?<text>(?<comment>(?<open>[#;]+)(?:[^\S\r\n]*)(?<value>.+))
|(?<section>(?<open>\[)(?:\s*)(?<value>[^\]]*\S+)(?:[^\S\r\n]*)(?<close>\]))
|(?<entry>(?<key>[^=\r\n\ [\]]*\S)(?:[^\S\r\n]*)(?<delimiter>:|=)(?:[^\S\r\n]*)(?<value>[^#;\r\n]*))
|(?<undefined>.+))(?<=\S)|(?<linebreaker>\r\n|\n)|(?<whitespace>[^\S\r\n]+)
```

Before we move on to writing the code, I want to break down the parsing regular expression itself and explain what each piece is for.

1. **`(?=\S)`** is a positive lookahead condition that checks that the next character is not a whitespace. This is necessary to skip leading whitespace in the file.

2. **`(?<text>....)`** is a named group that captures the text block of the file.. This will allow us to get the entire content of the file for further analysis.

3. **`(?<comment>(?<open>[#;]+)(?:[^\S\r\n]*)(?<value>.+))`** is a named group that captures comments in the file. It consists of:
    - **`(?<open>[#;]+)`** is a group that captures one or more "#" or ";" characters, denoting the beginning of a comment.
    - **`(?:[^\S\r\n]*)`** - a group that captures zero or more non-whitespace characters, not including newline characters.
    - **`(?<value>.+)`** - a group that captures all characters up to the end of the line, i.e. the entire comment text.
    - 
4. **`(?<section>(?<open>\[)(?:\s*)(?<value>[^\]]*\S+)(?:[^\S\r\n]*)(?<close>\]))`** - is a named group that captures sections. It consists of:
    - **`(?<open>\[)`** - a group that captures the "[" character, which denotes the beginning of a section.
    - **`(?:\s*)`** - a group that captures zero or more whitespace characters.
    - **`(?<value>[^\]]*\S+)`** - a group that captures one or more non-whitespace characters, not including the "]" character.
    - **`(?:[^\S\r\n]*)`** is a group that captures zero or more non-whitespace characters, not including newline characters.
    - **`(?<close>\])`** is a group that captures the "]" character, which marks the end of a section.

5. **`(?<entry>(?<key>[^=\r\n\[\]]*\S)(?:[^\S\r\n]*)(?<delimiter>:|=)(?:[^\S\r\n]*)(?<value>[^#;\r\n]*))`** is a named group that captures entries (key-value). It consists of:
    - **`(?<key>[^=\r\n\[\]]*\S)`** is a group that captures one or more non-whitespace characters, not including the "=", newline, and "[" characters.
    - **`(?:[^\S\r\n]*)`** is a group that captures zero or more non-whitespace characters, not including newline characters.
    - **`(?<delimiter>:|=)`** is a group that captures the ":" or "=" character separating the key and value.
    - **`(?:[^\S\r\n]*)`** is a group that captures zero or more non-whitespace characters, not including newline characters.
    - **`(?<value>[^#;\r\n]*)`** is a group that captures zero or more characters, not including "#", ";", newline characters.
6. **`(?<undefined>.+)`** is a named group that captures any undefined parts of the text that did not match the previous groups.

7. **`(?<=\S)`** is a positive lookahead condition that checks that the preceding character is not a whitespace character. This is necessary to skip trailing whitespace in the file.

8. **`(?<linebreaker>\r\n|\n)`** is a named group that captures newline characters ("\r\n" or "\n").

9. **`(?<whitespace>[^\S\r\n]+)`** is a named group that captures one or more whitespace characters, not including newline characters.

  This is a very detailed and carefully designed regular expression designed to accurately parse the structure of an INI file and extract all the necessary components (sections, keys, values, comments, etc.) from it. It can handle various formatting variations of INI files and provides a robust and flexible way of parsing.

Take a look at the parsing of the above sample using this regular expression:

![image](https://github.com/user-attachments/assets/fa2929cf-93bd-43b9-b11a-2c0039c93fff)

You can experiment with this regular expression using this [link](https://regex101.com/r/mul0C2).

## C-Sharp coding

To solve the problem of parsing INI files using regular expressions, I created the **IniFile** class. This class will be responsible for reading and parsing the contents of an INI file using regular expressions to extract keys, values, and sections. The class has methods for loading a file, getting a list of sections, getting values ​​by keys, and writing changes back to the file. Using regular expressions, IniFile will be able to handle various configuration file formats, including files with comments, indents, spaces, syntax errors, and other features. This will make the parser more flexible and universal. To use the class, you need to pass it a string or stream containing the INI file data and parsing settings.
### Key features of the class
1. Support for various loading and saving methods: The class provides methods for loading INI files from a string, stream, or file, as well as saving them to a stream or file.
2. Using regular expressions: Using regular expressions allows for flexible and efficient handling of various INI file formats, including support for comments, sections, and key-values. This makes the code more compact and easily extensible compared to using manual string processing.
3. No dependence on collections: The class does not use collections to store INI file data, which makes it more memory efficient and simplifies working with large files.
4. Preserving original formatting: When modifying existing entries or adding new ones, the class preserves the original INI file formatting, including the location of comments, spaces, and line breaks. This helps maintain the readability and structure of the file.
5. Support for escape characters: The class provides the ability to work with escape characters in key values. This allows for the correct handling of special characters such as tabs, line feeds, etc.
6. Automatic detection of line break characters: The class automatically detects the type of line break characters (CRLF, LF, or CR) in the INI file and uses them when saving changes.
7. Flexible customization of string comparison: The class allows you to customize the string comparison rules (case sensitivity, cultural specificity) according to the requirements of the application.
8. Support for various loading and saving methods: The class provides methods for loading INI files from a string, stream, or file, as well as saving them to a stream or file.
9. Convenient API for working with INI files: The class offers a simple and intuitive API for reading and writing values ​​to INI files, including support for various data types.
Thus, using the IniFileRegexParser class allows you to efficiently and flexibly work with INI files, preserving their structure and formatting, and also provides ample opportunities for customization and expansion of functionality.

### Usage

Here are some examples of using the IniFile class:

#### Opening a file
```csharp
// Here is an example of loading a file with parsing options that were explicitly specified:
IniFile ini = IniFile.Load("config.ini", Encoding.UTF8, StringComparison.InvariantCultureIgnoreCase, true);

// All the above parameter values ​​after the file name
// are passed in the form they are implied by default,
// So you can write the same in a shorter way:
ini = IniFile.Load("config.ini");
```
#### Reading a parameter into a string variable
```csharp
string value = ini.ReadString("Section1", "Key1", "default value");

// If you want to receive a key without a section, pass in a null or empty string as the section name:
value = ini.ReadString("", "Key0", "default value");
// - or -
value = ini.ReadString(null, "Key0", "default value");
```
#### Reading a parameter into an integer variable
```csharp
int intValue = ini.ReadInt32("Section1", "IntKey", 42);
```
#### Reading an array of strings into a new variable:
```csharp
string[] values ​​= ini.ReadStrings("Section1", "ArrayKey", "default1", "default2");
```
#### Reading using an indexer
```csharp
string value = ini["Section1", "Key1", "default"];
```
#### Reading various types of objects
```csharp
// In this example, we use the ReadObject method of the IniFile class to read the value of the
// CultureInfo parameter from the Settings section. We pass the CultureInfo type as the desired type,
// the default value CultureInfo.InvariantCulture, and an instance of CultureInfoTypeConverter as the type converter.
CultureInfo culture1 = (CultureInfo)ini.ReadObject("Settings", "Culture", typeof(CultureInfo),
                        CultureInfo.InvariantCulture, new CultureInfoTypeConverter());

// In this example, we use a generic method Read without using an optional parameter.
Uri uri = ini.Read<Uri>("Settings", "Culture");
```
#### Writing using the indexer
```csharp
ini["Section1", "Key1"] = "new value";
```
#### Writing a string
```csharp
ini.WriteString("Section1", "Key1", "new value");
```
#### Writing an array of strings
```csharp
ini.WriteStrings("Section1", "ArrayKey", "value1", "value2", "value3");
```
#### Writing using an indexer
```csharp
ini["Section1", "Key1"] = "new value";
```
#### Saving a file
```csharp
ini.Save("config.ini");
```

### Initializing custom classes

First, let's create the Person class:
```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public DateTime Birthday { get; set; }
}
```

Now, let's look at an example of using the ReadSettings method for the class we created:
```csharp
// Create an instance of IniFile.
IniFile ini = new IniFile("person.ini");

// Reading the settings for the Person class.
Person person = new Person();
ini.ReadSettings(typeof(Person), person);

// We display data about a person.
Console.WriteLine($"Name: {person.Name}");
Console.WriteLine($"Age: {person.Age}");
Console.WriteLine($"Birthday: {person.Birthday.ToString("yyyy-MM-dd")}");
```
Contents of person.ini generated by this code:
```
[Person]
Name=Joe
Age=35
Birthdyay=1989-04-25
```


At the same time, to read a Person object from the INI file parameters, you can use the following approach using type converter.
It is necessary to create a TypeConverter for the Person class:
```csharp
public class PersonTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string str)
        {
            string[] parts = str.Split(',');
            if (parts.Length == 3)
            {
                return new Person
                {
                    Name = parts[0].Trim(),
                    Age = int.Parse(parts[1].Trim()),
                    Birthday = DateTime.Parse(parts[2].Trim())
                };
            }
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is Person person)
        {
            return $"{person.Name},{person.Age},{person.Birthday.ToString("yyyy-MM-dd")}";
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
```

This way we can store and read Person objects in an INI file using a custom TypeConverter:
```csharp
var iniFile = new IniFile();
DateTime birthDay = DateTime.ParseExact(
    "25-04-1989", 
    "dd-MM-yyyy", 
    CultureInfo.InvariantCulture
);
Person person = new Person { Name = "John Doe", Age = 35, Birthday = birthDay };
iniFile.WriteObject("Section1", "Person", person, new PersonTypeConverter());
var person = iniFile.Read<Person>("Section1", "Person", null, new PersonTypeConverter());
iniFile.Save("persons.ini:);
```
Contents of persons.ini generated by this code:
```
[Section1]
Person=Joe,35,1989-04-25
```
As you can see from the examples, IniFile provides a number of advantages when working with various data types.

First, by using regular expressions and the absence of collections, IniFile provides efficient and fast reading and writing of data to an INI file, while preserving the original formatting. This allows you to work with large amounts of data without degrading performance.

Second, IniFile supports automatic initialization of object properties based on data from an INI file. This simplifies the process of setting up an application, since the developer does not need to manually extract and assign values ​​from the INI file.

Third, IniFile provides convenient methods for reading and writing data of various types, including standard .NET types, as well as the ability to use custom types using TypeConverter. This makes working with INI files more intuitive and reduces the likelihood of errors when converting types.

Thus, IniFile is a powerful and flexible tool for working with INI files, providing high performance, ease of use and extensibility when working with arbitrary data types.

### How it works

This class does not use collections to store data. Instead, it uses regular expressions to parse the contents of the INI file. This allows you to do without collections and preserve the original file format when editing.

The general algorithm for traversing the contents of an INI file, used in the GetSections(), GetKeys(), GetValue(), GetValues(), SetValue(), and SetValues() methods, is as follows:

- A regular expression is initialized that splits the file contents into sections, keys, and values.
- Iterates over all matches of the regular expression in the file contents.
- For each match, it is checked whether it is a section, key, or value.
- Depending on the match type, information about it is saved and used in the corresponding methods.
- For the GetValue(), GetValues(), SetValue(), and SetValues() methods, it is additionally monitored which section the current match is in in order to return or set the value in the correct section.
- The results of processing all matches are returned or used to modify the file contents. This approach allows you to efficiently work with the contents of an INI file without the need to use collections, while preserving the original file format.

The general view code of all this methods looks like this:
```csharp
Regex regex;    // A regular expression object.
string content; // A string containing INI file data.
// ...

// Iterate over the content to find the section and key
for (Match match = regex.Match(content); match.Success; match = match.NextMatch())
{
	if (match.Groups["section"].Success)
	{
		// Handling action for sections.
	}
	
	if (match.Groups["entry"].Success)
	{
		// Handling action for entries.
	}
	
	// Updating content if necessary.
}
```

The general algorithm for writing data in the SetValue() and SetValues() methods is as follows:

- A StringBuilder instance is created, which will be used to modify the contents of the INI file.
- The contents of the INI file are iterated over using a regular expression.
- If section an entry is found that matches the searched key, then:
- A group representing the value is obtained.
- The index and length of the group representing the value are calculated.
- The old value is removed from the StringBuilder.
- If the new value is not empty, it is inserted into the StringBuilder.
- If after the iteration the flag indicating that the value is not set is still set, then:
- The index is calculated where the new entry should be inserted.
- If this is not a global section and the section has not yet been encountered, a new section is inserted into the StringBuilder.
- A new entry with the key and value is inserted into the StringBuilder.
- The contents of the StringBuilder are written back to the Content field of the IniFile class.

Updating an existing key value is done by replacing the found value by its index and length. The new value is inserted in the same place, instead of the previous one, preserving the indent. The implementation is quite simple:

```csharp
string value; // String contains a new value.
string key;   // String contains a key name. 
//...

// Create a content editor.
StringBuilder sb = new StringBuilder(content);

// The match was found on the current iteration.
if (!match.Groups["key"].Value.Equals(key)) 
	continue;

// Value and it's start and stop position to replace.
Group group = match.Groups["value"];
int index = group.Index;
int length = group.Length;

// Remove the old value.
sb.Remove(index, length);

// Insert the new value in its place.
sb.Insert(index, value);

// Updating the content.
content = sb.ToString();
```

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

