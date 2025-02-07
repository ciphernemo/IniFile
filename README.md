# IniFile-Mini
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

![INI file icon](https://upload.wikimedia.org/wikipedia/en/thumb/2/2f/INI_file_icon.png/64px-INI_file_icon.png)

Quick and easy INI file reading and writing that's also quite powerful. This is a fork of the original IniFile parser by ng256.

IniFile-Mini is an INI configuration file parser designed for easy reading and writing of varying ini formats via a pre-set regular expression. It retains the original file formatting when updating values. To use the class, pass it an ini file name, text reader, or stream containing the ini file data, along with optional settings.

## Introduction
Long live the simple yet effective INI configuration file! Some may consider ini files a relic of the past, but it is still used today because it is a simple and versatile format that has survived for decades.
* Simplicity. The INI file format is a plain text file that excels at brevity due to its simple structure and loose rules. It's very easy for even non-technical users to read and write due to its visual simplicity and forgiving nature.
* Compatibility. JSON might be king across the Internet, but older software may be incompatible with it. There are many third party utilities and libraries that work well with ini files.
* Longevity. From 16-bit to 64-bit, both old and modern software should be able to work with plain text files like the ini file.

### Key Features of IniFile-Mini

* **Quickly read data from ini files**
  * Read individual Values, Keys, Sections, Comments
  * Read multiple Key/Value pairs by Section or globally
  * Read from memory over and over again without reloading the file
* **Easily write data to ini files**
  * Write individual Values, Keys, and their Sections
  * Write multiple Key/Value pairs at once
  * Updates Values of existing Keys or adds new Key/Value pairs automatically
  * Quickly update data in memory before saving it to file
* **Edit anything in an ini file manually**
* **Useful support various options/aspects**
  * File encoding
  * Escape/unescape characters: unicode, hex, and control chars
  * StringComparison support
  * Automatic adoption of ini file line breaks
* **Easy to navigate code**
  * Code is broken into regions
  * Field and method naming is consistent
  * Logical progression

## Usage
Add the `IniFile.cs` file to your project and reference it by the Ini namespace and IniFile constructor to start using it. No need to mess with dependencies, NuGet extensions, etc. INI files are simple, so your ini tool should be simple as well!

Create an empty IniFile object:
```csharp
Ini.IniFile MyFile = IniFile.Create(null);
```
All public objects have summary information for IntelliSense.

## Examples
You don't need to create an empty object, as you can open an INI file immediately, which loads it into memory:
```csharp
Ini.IniFile MyFile = IniFile.Load("C:\\MyConfig.ini", null);
```

Read all of the key/value pairs, read all pairs in a section by direct reference, and read a single value:
```csharp
Dictionary<string, string> MyContent1 = MyFile.ReadAllKeysValues(null);
Dictionary<string, string> MyContent2 = MyFile["MySection", null];
string MyValue = MyFile.ReadValue("MySection", "MyKey", null);
```

Write all of the key/value pairs globally, write all pairs in a section by direct reference, and update/add a single key/value pair:
```csharp
MyFile.WriteKeysValues(null, MyContent1);
MyFile["MySection", null] = MyContent2;
MyFile.WriteKeyValue("MySection", "MyKey", "MyValue");
```

Save your changes back to the INI file, or save them to a Stream:
```csharp
MyFile.Save("C:\\MyConfig.ini");

Stream stream = new MemoryStream();
MyFile.Save(stream, Encoding.UTF8);
```

Open an INI file and use all options:
```csharp
using Ini;

IniFile.IniOptions opt = new();
opt.Comparison = StringComparison.OrdinalIgnoreCase;
opt.Encoding = Encoding.UTF8;
opt.AllowEscChars = false;
opt.TrimValueQuotes = false;
opt.PadDelimiters = false;
opt.AllowDuplicateKeys = false;
IniFile ini = IniFile.Load("C:\\config.ini", opt);
```

Open an INI file. Then read it and write back to it while replacing empty values with default ones, all on the same line. Finally save it back to the same file:
```csharp
using Ini;

IniFile ini = IniFile.Load("D:\\config.ini", null);
ini.WriteKeysValues(null, ini["MySection", new[] { "val1", "2", "C3" }]);
ini.Save("D:\\config.ini");
```

# History of IniFile-Mini

I am developing another project that works best with an INI file to load and save a long list of internal settings. These settings need to be easily accessible and editable by its users to prevent me from having to continually update my project. Non-technical users would get lost with JSON or XML, so it's back to basics for my app.

I found Pavel Bashkardin's IniFile project on both the old CodeProject and GitHub. Something about it clicked with me... I loved it! However, I quickly discovered that it did not support reading all key/value pairs by section. So I updated that. Then I discovered other little features I needed that it didn't support. Once I started poking around to update my copy, I realized Pavel's coding style isn't really utilizing the latest C#/.NET practices, and it was a bit unwieldy to navigate to make changes. So I revamped almost everything. I learned how Pavel approached this, took his good practices and ideas, dropped things I didn't like, and transformed it into a leaner, more easily navigated tool. Isn't that what programmers like to do? While my version is much easier to tweak, Pavel's approach is still more robust than mine for serialization/deserialization. They're now different tools that still function in a familiar way.

# Reference

**The single, long regular expression used by IniFile-Mini, which is almost identical to IniFile**

Example: https://regex101.com/r/LOXFOG/2
```regex
(?=\S)(?<text>(?<comment>(?<open_comment>[#;]+)(?:[^\S\r\n]*)(?<value>.+))|(?<section>(?<open_section>\[)(?:\s*)(?<value_section>[^\]]*\S+)(?:[^\S\r\n]*)(?<close>\]))|(?<entry>(?<key>[^=\r\n\ [\]]*\S)(?:[^\S\r\n]*)(?<delimiter>:|=)(?:[^\S\r\n]*)(?<value_entry>[^#;\r\n]*))|(?<undefined>.+))(?<=\S)|(?<linebreaker>\r\n|\n)|(?<whitespace>[^\S\r\n]+)
```

