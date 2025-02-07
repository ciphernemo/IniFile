namespace Ini;

#region USING
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#endregion

public sealed class IniFile
{
	//░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░150 Column Max with tabs at 4 spaces░░░░░░░░░
	#region ABOUT: IniFile
	/*
		An ini configuration file parser designed for easy reading and writing of varying ini formats via a pre-set regular expression.
		It retains the original file formatting when updating values and supports easy class serialization and deserialization.
		To use the class, pass it an ini file name, text reader, or stream containing the ini file data, along with optional settings.
	*/
	#endregion

	#region VERSION, LICENSE
	/*
		IniFile-Mini

		Version:		2.1
		Author:			cipher_nemo
		License:		MIT License
		Creation Date:	08/28/2024
		Updated:		02/06/2025
		Version History:
			1.0: Fork from IniFile with 2024 Builds: 08/28, 08/31, 10/22, 11/07, 11/08, 12/22
			1.1: 01/29/2025: minor regex pattern change, GetKeysValues(), and customization & formatting to match PortalTime
			2.0: 02/06/2025: Shrunk ng256's IniFile from 2,781 to just 1,104 lines, updated comments, added regions, trimmed repeated 
				code, removed variable type conversions, simplified developer experience for reading and writing, migrated to string, List, and 
				Dictionary types, down to two Write functions, eight Read functions for keys/values, and one Read function for sections and also 
				comments. Added support for matching sections with or without square brackets, trimming/adding double quotes around values, handling 
				duplicate keys, and padding key/value delimiters. All options moved into a single class instead of handling them individually in 
				functions. I refactored all code, moved repeated code into two classes, and simplified serialization/de-serialization.
			2.1: 02/06/2025: Corrected bug with DuplicateExists skipping renamed duplicates, allowed passing null for options in constructor

		Original code for IniFile Copyright ©2024 Pavel Bashkardin, available at:
		https://github.com/ng256/IniFile
		https://www.codeproject.com/Articles/5387487/Csharp-INI-File-Parser

		Copyright ©2025 cipher_nemo

		Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the
		"Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
		distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
		following conditions:

		The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
		MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
		CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
		SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
	*/
	#endregion

	#region FIELDS, PROPERTIES

	//stores content of the ini file
	private string _content;
	private readonly Regex _regex;
	private readonly bool _trimValueQuotes;
	private readonly bool _requireSectionSquareBrackets;
	private readonly bool _allowEscapeChars;
	private readonly bool _allowDuplicateKeys;
	private readonly bool _padDelimiters;
	private readonly string _lineBreaker = Environment.NewLine;
	private readonly CultureInfo _culture;
	private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
	//determines how string comparisons are performed in the ini file, configured based on settings passed to the constructor
	private readonly StringComparison _comparison = StringComparison.InvariantCultureIgnoreCase;
	/// <summary>
	/// Returns a string representing the contents of the ini file.
	/// </summary>
	public string Content
	{
		get { return _content ??= string.Empty; }
		set { _content = value ?? (_content = string.Empty); }
	}

	#endregion

	#region CLASSES

	/// <summary>
	/// Options for IniFile methods.
	/// </summary>
	public class IniOptions
	{
		/// <summary>Specifies the rules for string comparison.</summary>
		public StringComparison Comparison { get; set; } = StringComparison.InvariantCultureIgnoreCase;
		/// <summary>Trim leading and trailing double quotes from values?</summary>
		public bool TrimValueQuotes { get; set; } = true;
		/// <summary>Require square brackets when matching sections? If false, square brackets are not required for section name matching.</summary>
		public bool RequireSectionSquareBrackets { get; set; } = false;
		/// <summary>Allow escape characters in Content for the ini file?</summary>
		public bool AllowEscChars { get; set; } = false;
		/// <summary>Allow duplicate keys in read results? Allowing duplicates will add hash suffixes to those keys' names.</summary>
		public bool AllowDuplicateKeys { get; set; } = true;
		/// <summary>Add spaces between the KeyValue pair equal sign delimiter?</summary>
		public bool PadDelimiters { get; set; } = true;
		/// <summary>The encoding used to read the stream.</summary>
		public Encoding? Encoding { get; set; } = Encoding.UTF8;
	}

	private class Search(IniFile MyParent, string? Section)
	{
		#region FIELDS, PROPERTIES

		public Dictionary<string, string> ResultsDict = new Dictionary<string, string>();
		public List<string> ResultsList = new List<string>();
		public string ThisKey = String.Empty;
		public string ThisValue = String.Empty;
		public string Result = String.Empty;
		public string ResultKey = String.Empty;
		public string ResultValue = String.Empty;
		public bool Global = String.IsNullOrEmpty(Section);
		public bool InSection = false;
		public bool EmptySection = String.IsNullOrEmpty(Section);
		public bool NewKeyValue = true;
		public Match LastMatch = null;
		public StringBuilder StrBuilder = new StringBuilder(MyParent._content);
		public int I = 0;
		/// <summary>Regex results group name to reference when searching by sections</summary>
		public string SectionGroup
		{
			get
			{
				//determine if matching with square brackets or not
				if (MyParent._requireSectionSquareBrackets) { return "section"; }
				else { return "value_section"; }
			}
		}
		private string Space
		{
			get
			{
				if (MyParent._padDelimiters) { return " "; }
				else { return String.Empty; }
			}
		}

		#endregion

		/// <summary>
		/// Replace existing Key's Value in the Content.
		/// </summary>
		/// <param name="MyGroup">Regex result group</param>
		public void ReplaceValue(Group MyGroup) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			int index = MyGroup.Index;
			int length = MyGroup.Length;
			StrBuilder.Remove(index, length);
			if (MyParent._trimValueQuotes) { AddQuotes(); }
			if (MyParent._allowEscapeChars) { EscapeChars(); }
			StrBuilder.Insert(index, ThisValue);
			NewKeyValue = false;
		}

		/// <summary>
		/// Append a new Key and its Value to the Content. If a Section is defined, append the Section first.
		/// </summary>
		public void AppendKeyValue() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			int index = 0;
			//If a match was found previously, append after the last match
			if (LastMatch != null) { index = LastMatch.Index + LastMatch.Length; }
			//if no match was found, append a new section and then insert the key-value pair
			else if (!EmptySection)
			{
				StrBuilder.Append(MyParent._lineBreaker);
				StrBuilder.Append($"[{Section}]{MyParent._lineBreaker}");
				index = StrBuilder.Length;
			}
			if (MyParent._trimValueQuotes) { AddQuotes(); }
			if (MyParent._allowEscapeChars) { EscapeChars(); }
			//insert the new key-value pair into the content
			string line = $"{MyParent._lineBreaker}{ThisKey}{Space}={Space}{ThisValue}{MyParent._lineBreaker}";
			//inserts a specified line at the specified index in the StringBuilder, followed by a specified new line and update the index
			//StrBuilder = MoveIndexToEndOfLinePosition(StrBuilder, ref index);
			StrBuilder = StrBuilder.Insert(index, line);
			index += line.Length;
		}

		/// <summary>
		/// Trim quotes and/or escape characters from Rresult, ResultKey, ResultValue, and ThisValue.
		/// </summary>
		/// <param name="TrimResults">Trim the results?</param>
		public void TrimAndEscape() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			if (MyParent._trimValueQuotes)
			{
				if (!String.IsNullOrEmpty(Result)) { Result = StripQuotes(Result); }
				if (!String.IsNullOrEmpty(ResultKey)) { ResultKey = StripQuotes(ResultKey); }
				if (!String.IsNullOrEmpty(ResultValue)) { ResultValue = StripQuotes(ResultValue); }
				if (!String.IsNullOrEmpty(ThisValue)) { ThisValue = StripQuotes(ThisValue); }
			}
			if (MyParent._allowEscapeChars)
			{
				if (!String.IsNullOrEmpty(Result)) { Result = UnEscapeChars(Result); }
				if (!String.IsNullOrEmpty(ResultKey)) { ResultKey = UnEscapeChars(ResultKey); }
				if (!String.IsNullOrEmpty(ResultValue)) { ResultValue = UnEscapeChars(ResultValue); }
				if (!String.IsNullOrEmpty(ThisValue)) { ThisValue = UnEscapeChars(ThisValue); }
			}
		}

		/// <summary>
		/// Handle duplicate keys in ResultsDict.
		/// </summary>
		/// <returns>True if duplicates found</returns>
		public bool DuplicateExists() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			if (ResultsDict.ContainsKey(ResultKey))
			{
				if (MyParent._allowDuplicateKeys)
				{
					ResultKey += "_" + Generate6CharHash();
					return false;
				}
				else { return true; }
			}
			return false;
		}

		/// <summary>
		/// Provide default values when and where appropriate in ResultValue.
		/// </summary>
		/// <param name="DefaultValues">String array of Default values</param>
		public void ProvideDefaults(string[]? DefaultValues) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			if (String.IsNullOrEmpty(ResultValue) && DefaultValues is not null)
			{
				if (DefaultValues.Length == 1) { ResultValue = DefaultValues[0]; }
				else
				{
					try { ResultValue = DefaultValues[I]; } catch { }
					I++;
				}
			}
		}

		/// <summary>
		/// Converts a nullable string to a nullable array of one string.
		/// </summary>
		/// <param name="Input">String to convert</param>
		/// <returns>Array of 1 string value</returns>
		public string[]? ToArray(string? Input) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			string[]? results = new string[1] { Input };
			return results;
		}

		private void EscapeChars() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			//escape characters in ThisValue with backslashes
			int i = 0;
			int length = ThisValue.Length;
			if (length == 0) { return; }
			StringBuilder sb = new StringBuilder(length * 2);
			do
			{
				char c = ThisValue[i++];
				switch (c)
				{
					case '\\': sb.Append(@"\\"); break;
					case '\0': sb.Append(@"\0"); break;
					case '\a': sb.Append(@"\a"); break;
					case '\b': sb.Append(@"\b"); break;
					case '\n': sb.Append(@"\n"); break;
					case '\r': sb.Append(@"\r"); break;
					case '\f': sb.Append(@"\f"); break;
					case '\t': sb.Append(@"\t"); break;
					case '\v': sb.Append(@"\v"); break;
					default: sb.Append(c); break;
				}
			} while (i < length);
			ThisValue = sb.ToString();
		}

		private string UnEscapeChars(string Input) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			//converts characters within Input to un-escaped values
			int i = -1;
			int length = Input.Length;
			if (length == 0) return Input;
			//find the first occurrence of backslash or return the original text
			for (int j = 0; j < length; ++j)
			{
				//if backslash not found, keep searching
				if (Input[j] != '\\') { continue; }
				//index was set to first backslash found
				i = j;
				break;
			}
			//no backslash found, so return unaltered string
			if (i < 0) { return Input; }

			StringBuilder sb = new StringBuilder(Input[..i]);
			do
			{
				char c = Input[i++];
				//if backslash not found
				if (c != '\\') { continue; }
				//if index of backslash is less than Input length, then get char at that index, otherwise treat as just a backslash
				c = i < length ? Input[i] : '\\';
				switch (c)
				{
					case '\\': c = '\\'; break;
					case '0': c = '\0'; break;
					case 'a': c = '\a'; break;
					case 'b': c = '\b'; break;
					case 'n': c = '\n'; break;
					case 'r': c = '\r'; break;
					case 'f': c = '\f'; break;
					case 't': c = '\t'; break;
					case 'v': c = '\v'; break;
					//3 digit unicode value
					case 'u' when i < length - 3:
						c = UnHex(Input.Substring(++i, 4));
						i += 3;
						break;
					//hex escape value
					case 'x' when i < length - 1:
						c = UnHex(Input.Substring(++i, 2));
						i++;
						break;
					//control characters
					case 'c' when i < length:
						c = Input[++i];
						if (c >= 'a' && c <= 'z') { c -= ' '; }
						if ((c = (char)(c - 0x40U)) >= ' ') { c = '?'; }
						break;
					//any other escaped character
					default:
						sb.Append("\\" + c);
						i++;
						continue;
				}
				i++;
				sb.Append(c);
			} while (i < length);
			return sb.ToString();
		}

		private static char UnHex(string Hex) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			//converts hex number to unicode character
			int c = 0;
			for (int i = 0; i < Hex.Length; i++)
			{
				//obtain next digit
				int r = Hex[i];
				if (r > 0x2F && r < 0x3A) r -= 0x30;
				else if (r > 0x40 && r < 0x47) r -= 0x37;
				else if (r > 0x60 && r < 0x67) r -= 0x57;
				else return '?';
				//insert next digit
				c = (c << 4) + r;
			}
			return (char)c;
		}

		private static string StripQuotes(string Input) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			//removes quotes surrounding the Input
			if (Input is null) { return Input; }
			if (Input.StartsWith("\"")) { Input = Input.Remove(0, 1); }
			if (Input.EndsWith("\"")) { Input = Input.Remove(Input.Length - 1, 1); }
			return Input;
		}

		private void AddQuotes() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			//adds quotes surrounding the Input
			if (ThisValue is null) { return; }
			ThisValue = "\"" + ThisValue + "\"";
		}

		private static string Generate6CharHash() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
		{
			using SHA256 sha256 = SHA256.Create();
			byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(DateTime.Now.Ticks.ToString()));
			byte[] shortBytes = new byte[6];
			Array.Copy(hashBytes, shortBytes, 6);
			string base64 = Convert.ToBase64String(shortBytes);
			return base64.Substring(0, 6);
		}
	}

	#endregion

	#region CONSTRUCTORS

	private IniFile() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		//private constructor to prevent direct instantiation
	}

	private IniFile(string content, IniOptions? options) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		//constructor accepting ini content as a string and the following settings:
		//	string comparison rule, regex pattern, escape character allowance, trim quotes, match square brackets, and delimiter

		//if options is null, load defaults
		options ??= new IniOptions();
		//if content is null, set it to an empty string
		content ??= String.Empty;
		_comparison = options.Comparison;
		_content = content;
		//regular expression reference
		//	@"(?=\S)" + //positive lookahead
		//	@"(?<text>" + //main text capture group
		//		@"(?<comment>" + //comment start with # or ;
		//			@"(?<open_comment>[#;]+)" + //actual COMMENTS
		//			@"(?:[^\S\r\n]*)" + //do not capture
		//			@"(?<value>.+)" + //after comments (any other characters)
		//		@")|" +
		//		@"(?<section>" + //section capture group
		//			@"(?<open_section>\[)" + //sections start with [
		//			@"(?:\s*)" + //any whitespace
		//			@"(?<value_section>[^\]]*\S+)" + //actual SECTION name
		//			@"(?:[^\S\r\n]*)" + //do not capture
		//			@"(?<close>\])" + //sections end with ]
		//		@")|" +
		//		@"(?<entry>" + //entry capture group
		//			@"(?<key>[^=\r\n\ [\]]*\S)" + //actual KEY
		//			@"(?:[^\S\r\n]*)" + //do not capture
		//			@"(?<delimiter>:|=)" + //delimiter between key and value
		//			@"(?:[^\S\r\n]*)" + //do not capture
		//			@"(?<value_entry>[^#;\r\n]*)" + //actual VALUE
		//		@")|" +
		//		@"(?<undefined>.+)" + //undefined capture group (any other characters)
		//	@")" +
		//	@"(?<=\S)|" + //positive lookbehind
		//	@"(?<linebreaker>\r\n|\n)|" + ///capture line breaks
		//	@"(?<whitespace>[^\S\r\n]+)" //ignore any whitespace
		_regex = new Regex(@"(?=\S)(?<text>(?<comment>(?<open_comment>[#;]+)(?:[^\S\r\n]*)(?<value>.+))|(?<section>(?<open_section>\[)(?:\s*)(?<value_section>[^\]]*\S+)(?:[^\S\r\n]*)(?<close>\]))|(?<entry>(?<key>[^=\r\n\ [\]]*\S)(?:[^\S\r\n]*)(?<delimiter>:|=)(?:[^\S\r\n]*)(?<value_entry>[^#;\r\n]*))|(?<undefined>.+))(?<=\S)|(?<linebreaker>\r\n|\n)|(?<whitespace>[^\S\r\n]+)",
			GetRegexOptions((options.Comparison), RegexOptions.Compiled));
		_culture = GetCultureInfo(_comparison);
		_trimValueQuotes = options.TrimValueQuotes;
		_requireSectionSquareBrackets = options.RequireSectionSquareBrackets;
		_allowEscapeChars = options.AllowEscChars;
		_allowDuplicateKeys = options.AllowDuplicateKeys;
		_padDelimiters = options.PadDelimiters;
		_lineBreaker = AutoDetectLineBreaker(_content);
	}

	#endregion

	#region DE/SERIALIZATION

	/// <summary>
	/// Reads or writes the Values, optionally matches to a specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read/write Key/Value pairs. Pass a null to read/write only globally, above Section.</param>
	/// <param name="DefaultValues">The Values to be returned if the specified entry is not found.</param>
	/// <returns>Values for the specified search.</returns>
	public Dictionary<string, string> this[string? Section, string[]? DefaultValues] //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		get => ReadKeysValues(Section, DefaultValues);
		set => WriteKeysValues(Section, value);
	}

	#endregion

	#region FUNCTIONS: CREATE, LOAD, SAVE

	/// <summary>
	/// Create a new instance of <see cref="IniFile"/> with empty content.
	/// </summary>
	/// <param name="Options">Ini options class. string comparison type and whether or not to allow escaped characters</param>
	/// <returns>An instance of IniFile with the specified settings</returns>
	public static IniFile Create(IniOptions? Options) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		return new IniFile(string.Empty, Options);
	}

	/// <summary>
	/// Loads an ini file using a TextReader and creates an instance of IniFile.
	/// </summary>
	/// <param name="Reader">TextReader containing the ini file data.</param>
	/// <param name="Options">Ini options class. string comparison type and whether or not to allow escaped characters</param>
	/// <returns>An instance of IniFile.</returns>
	public static IniFile Load(TextReader Reader, IniOptions? Options) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		return new IniFile(Reader.ReadToEnd(), Options);
	}

	/// <summary>
	/// Loads an ini file from a Stream and creates an instance of IniFile.
	/// </summary>
	/// <param name="Stream">Stream containing the ini file data.</param>
	/// <param name="Options">Ini options class. string comparison type and whether or not to allow escaped characters</param>
	/// <returns>An instance of IniFile.</returns>
	/// <exception cref="ArgumentNullException">Stream is null.</exception>
	public static IniFile Load(Stream Stream, IniOptions? Options) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		using StreamReader reader = new StreamReader(Stream ?? throw new ArgumentNullException(nameof(Stream)), Options.Encoding ?? Encoding.UTF8);
		return new IniFile(reader.ReadToEnd(), Options);
	}

	/// <summary>
	/// Loads an ini file from a file path and creates an instance of IniFile.
	/// </summary>
	/// <param name="FileName">Path to the file containing ini data.</param>
	/// <param name="Options">Ini options class. string comparison type and whether or not to allow escaped characters</param>
	/// <returns>An instance of IniFile.</returns>
	public static IniFile Load(string FileName, IniOptions? Options) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		string filePath = GetFullPath(FileName, true);
		return new IniFile(File.ReadAllText(filePath, Options.Encoding ?? AutoDetectEncoding(filePath, Encoding.UTF8)), Options);
	}

	/// <summary>
	/// Loads an ini file using a TextReader or creates create an empty instance of IniFile.
	/// </summary>
	/// <param name="FileName">Path to the file containing ini data.</param>
	/// <param name="Options">Ini options class. string comparison type and whether or not to allow escaped characters</param>
	/// <returns>An instance of IniFile.</returns>
	public static IniFile LoadOrCreate(string FileName, IniOptions? Options) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		string filePath = GetFullPath(FileName);
		Encoding enc = Options.Encoding ?? AutoDetectEncoding(filePath, Encoding.UTF8);
		return new IniFile(File.Exists(filePath) ? File.ReadAllText(filePath, enc) : String.Empty, Options);
	}

	///<summary>
	///Saves ini content to a TextWriter
	///</summary>
	///<param name="Writer">The TextWriter used to write the ini file.</param>
	public void Save(TextWriter Writer) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Writer.Write(Content);
	}

	/// <summary>
	/// Saves the ini file content to a Stream using the specified encoding.
	/// </summary>
	/// <param name="Stream">Stream where the ini file data will be written.</param>
	/// <param name="Encoding">Encoding used to write the data to the stream.</param>
	public void Save(Stream Stream, Encoding Encoding = null) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		using StreamWriter writer = new StreamWriter(Stream, Encoding ?? Encoding.UTF8);
		writer.Write(Content);
	}

	/// <summary>
	/// Saves the ini file content to a file specified by its path using the specified encoding.
	/// </summary>
	/// <param name="FileName">Path to the file where ini data will be saved.</param>
	/// <param name="Encoding">Encoding used to write the data to the stream.</param>
	public void Save(string FileName, Encoding Encoding = null) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		string fullPath = GetFullPath(FileName);
		File.WriteAllText(fullPath, Content, Encoding ?? Encoding.UTF8);
	}

	#endregion

	#region FUNCTIONS: READ

	/// <summary>
	/// Read a Key's Value from Content in the ini file that optionally matches the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read the Key's Value. Pass a null to read only global Values, above Sections.</param>
	/// <param name="Key">Key name in which to find the Value.</param>
	/// <param name="DefaultValue">For an empty Value, apply this default Value.</param>
	/// <returns>Matching Value found. If multiple found, only return the first one.</returns>
	public string ReadValue(string? Section, string Key, string? DefaultValue) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, Section);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if need only global entries and match is an entry not in a Section
			if (search.Global && !search.InSection && match.Groups["key"].Value.Equals(Key, _comparison))
			{
				search.ResultValue = match.Groups["value_entry"].Value;
				break;
			}
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				if (search.Global) { break; }
				continue;
			}
			//if in matched Section and the Key matches
			if (search.InSection && match.Groups["key"].Value.Equals(Key, _comparison))
			{
				search.ResultValue = match.Groups["value_entry"].Value;
				break;
			}
			search.ProvideDefaults(search.ToArray(DefaultValue));
		}
		search.TrimAndEscape();
		return search.ResultValue;
	}

	/// <summary>
	/// Read a Value's parent Key from Content in the ini file that optionally matches the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read the Value's parent Key. Pass a null to read only global Keys, above Sections.</param>
	/// <param name="Value">Value in which to find its parent Key.</param>
	/// <returns>Matching Key found. If multiple found, only return the first one.</returns>
	public string ReadKey(string? Section, string Value) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, Section);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//get the Value_entry and strip quotes if needed
			search.ThisValue = match.Groups["value_entry"].Value;
			search.TrimAndEscape();
			//if need only global entries and match is an entry not in a Section
			if (search.Global && !search.InSection && search.ThisValue.Equals(Value, _comparison))
			{
				search.Result = match.Groups["key"].Value;
				break;
			}
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				if (search.Global) { break; }
				continue;
			}
			//if in matched Section and matching Value
			if (search.InSection && search.ThisValue.Equals(Value, _comparison))
			{
				search.Result = match.Groups["key"].Value;
				break;
			}
		}
		search.TrimAndEscape();
		return search.Result;
	}

	/// <summary>
	/// Read all Values for Keys of the same name from Content in the ini file that optionally matches the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read the Values. Pass a null to read all Values, regardles of Sections.</param>
	/// <param name="Key">Key name in which to find the Values.</param>
	/// <param name="DefaultValues">For empty Values, apply these default Values in order. If only 1 default specified, then use it globally.</param>
	/// <returns>Matching Values found</returns>
	public List<string> ReadValuesByKey(string? Section, string Key, string[]? DefaultValues) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, Section);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if need only global entries and match is an entry not in a Section
			if (search.Global && !search.InSection && match.Groups["key"].Value.Equals(Key, _comparison))
			{
				search.ResultValue = match.Groups["value_entry"].Value;
				search.TrimAndEscape();
				search.ProvideDefaults(DefaultValues);
				search.ResultsList.Add(search.ResultValue);
				continue;
			}
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				continue;
			}
			//if in matched Section and matching Key
			if (search.InSection && match.Groups["key"].Value.Equals(Key, _comparison))
			{
				search.ResultValue = match.Groups["value_entry"].Value;
				search.TrimAndEscape();
				search.ProvideDefaults(DefaultValues);
				search.ResultsList.Add(search.ResultValue);
				continue;
			}
		}
		return search.ResultsList;
	}

	/// <summary>
	/// Read all Keys that have the same Value from Content in the ini file that optionally matches the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read the Value's parent Key. Pass a null to read all Keys, regardles of Sections.</param>
	/// <param name="Value">Value in which to find its parent Keys.</param>
	/// <returns>Matching Keys found</returns>
	public List<string> ReadKeysByValue(string? Section, string Value) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, Section);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//get the Value_entry and strip quotes if needed
			search.ThisValue = match.Groups["value_entry"].Value;
			search.TrimAndEscape();
			//if need only global entries and match is an entry not in a Section
			if (search.Global && !search.InSection && search.ThisValue.Equals(Value, _comparison))
			{
				search.Result = match.Groups["key"].Value;
				search.TrimAndEscape();
				search.ResultsList.Add(search.Result);
				continue;
			}
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				continue;
			}
			//if in matched Section and matching Value
			if (search.InSection && search.ThisValue.Equals(Value, _comparison))
			{
				search.Result = match.Groups["key"].Value;
				search.TrimAndEscape();
				search.ResultsList.Add(search.Result);
				continue;
			}
		}
		return search.ResultsList;
	}

	/// <summary>
	/// Reads all Values from Content in the ini file that optionally match the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read Values. Pass a null to read all Values, regardless of Section.</param>
	/// <param name="DefaultValues">For empty Values, apply these default Values in order. If only 1 default specified, then use it globally.</param>
	/// <returns>Matching Values found</returns>
	public List<string> ReadValues(string? Section, string[]? DefaultValues) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, Section);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if need only global entries and match is an entry not in a Section
			if (search.Global && !search.InSection && match.Groups["key"].Success)
			{
				search.ResultValue = match.Groups["value_entry"].Value;
				search.TrimAndEscape();
				search.ProvideDefaults(DefaultValues);
				search.ResultsList.Add(search.ResultValue);
				continue;
			}
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				if (search.Global) { break; }
				continue;
			}
			//if in matched Section and matching Key
			if (search.InSection && match.Groups["key"].Success)
			{
				search.ResultValue = match.Groups["value_entry"].Value;
				search.TrimAndEscape();
				search.ProvideDefaults(DefaultValues);
				search.ResultsList.Add(search.ResultValue);
				continue;
			}
		}
		return search.ResultsList;
	}

	/// <summary>
	/// Reads all Values' parent Keys from Content in the ini file that optionally match the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read Values. Pass a null to read all Values, regardless of Section.</param>
	/// <returns>Matching Keys found</returns>
	public List<string> ReadKeys(string? Section) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, Section);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if need only global entries and match is an entry not in a Section
			if (search.Global && !search.InSection && match.Groups["key"].Success)
			{
				search.Result = match.Groups["key"].Value;
				search.TrimAndEscape();
				search.ResultsList.Add(search.Result);
				continue;
			}
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				if (search.Global) { break; }
				continue;
			}
			//if in matched Section and matching key group
			if (search.InSection && match.Groups["key"].Success)
			{
				search.Result = match.Groups["key"].Value;
				search.TrimAndEscape();
				search.ResultsList.Add(search.Result);
				continue;
			}
		}
		return search.ResultsList;
	}

	/// <summary>
	/// Reads all Key/Value pairs from Content in the ini file that optionally match the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read Key/Value pairs. Pass a null to read only global Key/Value pairs, above Section.</param>
	/// <param name="DefaultValues">For empty Values, apply these default Values in order. If only 1 default specified, then use it globally.</param>
	/// <returns>Matching Key/Value pairs. Duplicates keys given a 6-char suffix if options allow it, otherwise only 1st pair returned.</returns>
	public Dictionary<string, string> ReadKeysValues(string? Section, string[]? DefaultValues) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, Section);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if need only global entries and match is an entry not in a Section
			if (search.Global && !search.InSection && match.Groups["key"].Success)
			{
				search.ResultKey = match.Groups["key"].Value;
				search.ResultValue = match.Groups["value_entry"].Value;
				search.TrimAndEscape();
				if (search.DuplicateExists()) { continue; }
				search.ProvideDefaults(DefaultValues);
				search.ResultsDict.Add(search.ResultKey, search.ResultValue);
				continue;
			}
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				if (search.Global) { break; }
				continue;
			}
			//if in matched Section and matching Key
			if (search.InSection && match.Groups["key"].Success)
			{
				search.ResultKey = match.Groups["key"].Value;
				search.ResultValue = match.Groups["value_entry"].Value;
				search.TrimAndEscape();
				if (search.DuplicateExists()) { continue; }
				search.ProvideDefaults(DefaultValues);
				search.ResultsDict.Add(search.ResultKey, search.ResultValue);
				continue;
			}
		}
		return search.ResultsDict;
	}

	/// <summary>
	/// Reads all Key/Value pairs from Content in the ini file.
	/// </summary>
	/// <param name="DefaultValues">For empty Values, apply these default Values in order. If only 1 default specified, then use it globally.</param>
	/// <returns>All Key/Value pairs. Duplicate keys given a 6-char suffix if options allow it, otherwise only 1st pair returned.</returns>
	public Dictionary<string, string> ReadAllKeysValues(string[]? DefaultValues) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, null);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if matching key group
			if (match.Groups["key"].Success)
			{
				search.ResultKey = match.Groups["key"].Value;
				search.ResultValue = match.Groups["value_entry"].Value;
				search.TrimAndEscape();
				if (search.DuplicateExists()) { continue; }
				search.ProvideDefaults(DefaultValues);
				search.ResultsDict.Add(search.ResultKey, search.ResultValue);
				continue;
			}
		}
		return search.ResultsDict;
	}

	/// <summary>
	/// Reads all sections from Content in the ini file.
	/// </summary>
	/// <returns>All sections found</returns>
	public List<string> ReadSections() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, null);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.Result = match.Groups[search.SectionGroup].Value;
				search.TrimAndEscape();
				search.ResultsList.Add(search.Result);
				continue;
			}
		}
		return search.ResultsList;
	}

	/// <summary>
	/// Reads all Comments from Content in the ini file that optionally match the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to read Comments. Pass a null to read all Comments, regardless of Section.</param>
	/// <returns>Matching Comments found</returns>
	public List<string> ReadComments(string? Section) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		Search search = new Search(this, null);
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if need only global entries and matching open_comment group
			if (search.Global && match.Groups["open_comment"].Success)
			{
				search.Result = match.Groups["value"].Value;
				search.TrimAndEscape();
				search.ResultsList.Add(search.Result);
				continue;
			}
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				continue;
			}
			//if in matched Section and matching open_comment group
			if (search.InSection && match.Groups["open_comment"].Success)
			{
				search.Result = match.Groups["value"].Value;
				search.TrimAndEscape();
				search.ResultsList.Add(search.Result);
				continue;
			}
		}
		return search.ResultsList;
	}

	#endregion

	#region FUNCTIONS: WRITE

	/// <summary>
	/// Writes a Key and its Value to Content in the ini file that optionally match the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to write the Key and Value. Pass a null to write above Sections.</param>
	/// <param name="Key">Key to write</param>
	/// <param name="Value">Value to write</param>
	public void WriteKeyValue(string? Section, string Key, string Value)
	{
		Search search = new Search(this, Section);
		search.ThisKey = Key;
		search.ThisValue = Value;
		//iterate through all regex matches of content
		for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
		{
			//if in matched Section
			if (match.Groups[search.SectionGroup].Success)
			{
				search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
				if (search.EmptySection) { break; }
				continue;
			}
			//if in matched Section and matching entry group
			if (search.InSection && match.Groups["entry"].Success)
			{
				search.LastMatch = match;
				if (!match.Groups["key"].Value.Equals(search.ThisKey, _comparison)) { continue; }
				search.ReplaceValue(match.Groups["value_entry"]);
				break;
			}
		}
		//if the KeyValue pair is new (doesn't exist), append it at the correct position
		if (search.NewKeyValue) { search.AppendKeyValue(); }
		Content = search.StrBuilder.ToString();
	}

	/// <summary>
	/// Writes KeyValue pairs to Content in the ini file that optionally match the specified Section.
	/// </summary>
	/// <param name="Section">Section name in which to write the KeyValue pairs. Pass a null to write above Sections.</param>
	/// <param name="KeysValues">Dictionary of Keys and their Values</param>
	public void WriteKeysValues(string? Section, Dictionary<string, string> KeysValues)
	{
		Search search = new Search(this, Section);
		search.ResultsDict = KeysValues;
		foreach (KeyValuePair<string, string> kvp in KeysValues)
		{
			search.ThisKey = kvp.Key;
			search.ThisValue = kvp.Value;
			//iterate through all regex matches of content
			for (Match match = _regex.Match(Content); match.Success; match = match.NextMatch())
			{
				//if need only global entries and match is an entry not in a Section
				if (search.Global && !search.InSection && match.Groups["entry"].Success)
				{
					search.LastMatch = match;
					if (!match.Groups["key"].Value.Equals(search.ThisKey, _comparison)) { continue; }
					search.ReplaceValue(match.Groups["value_entry"]);
					break;
				}
				//if in matched Section
				if (match.Groups[search.SectionGroup].Success)
				{
					search.InSection = match.Groups[search.SectionGroup].Value.Equals(Section, _comparison);
					if (search.EmptySection) { break; }
					continue;
				}
				//if in matched Section and matching entry group
				if (search.InSection && match.Groups["entry"].Success)
				{
					search.LastMatch = match;
					if (!match.Groups["key"].Value.Equals(kvp.Key, _comparison)) { continue; }
					search.ReplaceValue(match.Groups["value_entry"]);
					break;
				}
			}
			//if the KeyValue pair is new (doesn't exist), append it at the correct position
			if (search.NewKeyValue) { search.AppendKeyValue(); }
		}
		Content = search.StrBuilder.ToString();
	}

	#endregion

	#region FUNCTIONS: GENERAL

	public override string ToString() //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		return Content;
	}

	private static string AutoDetectLineBreaker(string text) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		//determine if either \r or \n chars are present, and breaks when both are found
		if (String.IsNullOrEmpty(text)) { return Environment.NewLine; }
		bool r = false, n = false;
		//search for cr and lf characters
		for (int index = 0; index < text.Length; index++)
		{
			char c = text[index];
			if (c == '\r') { r = true; }
			if (c == '\n') { n = true; }
			if (r && n) { break; }
		}
		//determine the line break type based on the flags set
		return n ? r ? "\r\n" : "\n" : r ? "\r" : Environment.NewLine;
	}

	private static Encoding AutoDetectEncoding(string fileName, Encoding defaultEncoding = null) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		//tries to determine the encoding, checking the presence of signature (BOM) for some popular encodings
		byte[] bom = new byte[4];
		using (FileStream fs = File.OpenRead(fileName))
		{
			int count = fs.Read(bom, 0, 4);
			//check for Byte Order Mark (BOM)
			if (count > 2)
			{
				if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
				if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
			}
			else if (count > 1)
			{
				if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
				if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
			}
		}
		return defaultEncoding ?? Encoding.Default;
	}

	private static bool IsInvalidPath(string fileName) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		return fileName.Any(InvalidPathChar);
	}

	private static bool InvalidPathChar(char c) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		return InvalidPathChars.Contains(c);
	}

	private static Exception ValidateFileName(string fileName, bool checkExists = false) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		//checks file name and if it exists, then return null if file name is valid or an exception
		if (fileName == null) { return new ArgumentNullException(nameof(fileName)); }
		if (String.IsNullOrEmpty(fileName) || fileName.All(char.IsWhiteSpace) || IsInvalidPath(fileName))
		{
			return new ArgumentException(null, nameof(fileName));
		}
		if (checkExists && !File.Exists(fileName)) { return new FileNotFoundException(null, fileName); }
		return null;
	}

	private static string GetFullPath(string fileName, bool checkExists = false) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		if (ValidateFileName(fileName, checkExists) is Exception exception) { throw exception; }
		return Path.GetFullPath(fileName);
	}

	private static CultureInfo GetCultureInfo(StringComparison comparison) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		//returns a CultureInfo object that defines the string comparison rules for the specified StringComparison
		return comparison < StringComparison.InvariantCulture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture;
	}

	private static RegexOptions GetRegexOptions(StringComparison comparison, RegexOptions options = RegexOptions.None) //░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
	{
		//sets or clears the RegexOptions flags based on the specified StringComparison, returning the modified value
		switch (comparison)
		{
			case StringComparison.CurrentCulture: options &= ~RegexOptions.CultureInvariant; break;
			case StringComparison.CurrentCultureIgnoreCase: options &= ~RegexOptions.CultureInvariant; options |= RegexOptions.IgnoreCase; break;
			case StringComparison.InvariantCulture: options |= RegexOptions.CultureInvariant; break;
			case StringComparison.InvariantCultureIgnoreCase: options |= RegexOptions.IgnoreCase | RegexOptions.CultureInvariant; break;
			case StringComparison.OrdinalIgnoreCase: options |= RegexOptions.IgnoreCase; break;
		}
		return options;
	}

	#endregion
}
