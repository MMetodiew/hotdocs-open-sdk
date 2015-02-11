/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace HotDocs.Sdk
{
	/// <summary>
	/// These flags enumerate which portions of a template manifest (or manifests) can be parsed (when calling <c>ParseManifest</c>).
	/// </summary>
	[Flags]
	public enum ManifestParseFlags
	{
		/// <summary>
		/// Find the basic metadata about a template: its version, title, description, effective component file, expiration date, etc.
		/// </summary>
		ParseTemplateInfo		= 0x0001,
		/// <summary>
		/// Find the variables that are used by the template or its interview.
		/// </summary>
		ParseVariables			= 0x0002,
		/// <summary>
		/// Find the dependencies this template has on other templates, component files, images, etc.
		/// </summary>
		ParseDependencies		= 0x0004,
		/// <summary>
		/// Find additional dependencies this template may have on other files that are not typically managed by HotDocs.
		/// </summary>
		ParseAdditionalFiles	= 0x0008,
		/// <summary>
		/// Find the answer sources used in this template.
		/// </summary>
		ParseDataSources		= 0x0010,
		/// <summary>
		/// When this flag is set, information will be compiled by recursively parsing the manifests for this template
		/// and any templates inserted by this one as well.
		/// </summary>
		ParseRecursively		= 0x0020,
		/// <summary>
		/// Reads and parses all available information in the manifest.
		/// </summary>
		ParseAll				= ParseVariables|ParseDependencies|ParseAdditionalFiles|ParseDataSources
	};


	/// <summary>
	/// <c>DataSourceFieldType</c> lists the possible types of fields in data sources (Text, Number, Date, or TrueFalse).
	/// </summary>
	public enum DataSourceFieldType
	{
		/// <summary>
		/// The type of the data source field is <c>Text</c>
		/// </summary>
		Text,

		/// <summary>
		/// The type of the data source field is <c>Number</c>
		/// </summary>
		Number,

		/// <summary>
		/// The type of the data source field is <c>Date</c>
		/// </summary>
		Date,

		/// <summary>
		/// The type of the data source field is <c>TrueFalse</c>
		/// </summary>
		TrueFalse
	};

	/// <summary>
	/// <c>DataSourceBackfillType</c> enumerates the ways in which modified answers
	/// may be written back to the original data source.
	/// </summary>
	public enum DataSourceBackfillType
	{
		/// <summary>
		/// Indicates never store the current field back to the data source.
		/// </summary>
		Never,

		/// <summary>
		/// Indicates always store the current field back to the data source.
		/// </summary>
		Always,

		/// <summary>
		/// Indicates to prompt whether to store the current field back to
		/// the data source.
		/// </summary>
		Prompt,

		/// <summary>
		/// Indicates the the current field should be specifically prevented from
		/// storing back to the data source.
		/// </summary>
		DoNotAllow
	};

	/// <summary>
	/// <c>DataSourceType</c> indicates from where the answers were supplied to the interview or assembly.
	/// </summary>
	public enum DataSourceType
	{
		/// <summary>
		/// Indicates the current answer file is being supplied to the interview or assembly.
		/// </summary>
		CurrentAnswerFile,

		/// <summary>
		/// Indicates a specified answer file is being supplied to the interview or assembly.
		/// </summary>
		AnswerFile,

		/// <summary>
		/// Indicates answers from a database component are being supplied to the interview or assembly.
		/// </summary>
		DatabaseComponent,

		/// <summary>
		/// Indicates answers from a customized source are being supplied to the interview or assembly.
		/// </summary>
		Custom
	};

	/// <summary>
	/// <c>VariableInfo</c> contains basic metadata (name and type) about a HotDocs variable. This type is used
	/// by <c>TemplateManifest</c> to list the variables that may potentially be gathered by an interview
	/// and/or used in a template.
	/// </summary>
	public class VariableInfo : IEquatable<VariableInfo>, IComparable<VariableInfo>
	{
		private string _key;

		internal VariableInfo(string name, ValueType valueType)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			Name = name;
			Type = valueType;

			_key = Name + Type.ToString();
		}

		/// <summary>
		/// <c>Name</c> and <c>Type</c> help to identify the current variable
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// <c>Name</c> and <c>Type</c> help to identify the current variable
		/// </summary>
		public ValueType Type { get; private set; }
		
		/// <summary>
		/// Overrides Object.Equals
		/// </summary>
		/// <param name="obj">The object to compare with to test for equality.</param>
		/// <returns>A value indicating whether or not the objects are equal.</returns>
		public override bool Equals(object obj)
		{
			return (obj != null) && (obj is VariableInfo) && Equals((VariableInfo)obj);
		}

		/// <summary>
		/// Overrides Object.GetHashCode()
		/// </summary>
		/// <returns>a number of type int</returns>
		public override int GetHashCode()
		{
			return _key.GetHashCode();
		}

		/// <summary>
		/// Overrides Object.ToString()
		/// </summary>
		/// <returns>a string instance similar in content to 'this'</returns>
		public override string ToString()
		{
			return String.Format("Name: {0}  AnswerType: {1}", Name, Type);
		}

		#region IEquatable<Variable> Members

		/// <summary>
		/// <c>Equals</c> determines whether of not 'other' is equivalent to 'this'
		/// </summary>
		/// <param name="other">The object being compared to 'this'</param>
		/// <returns>boolean (true or false)</returns>
		public bool Equals(VariableInfo other)
		{
			return CompareTo(other) == 0;
		}

		#endregion

		#region IComparable<Variable> Members

		/// <summary>
		/// <c>CompareTo</c> implements IComparable.CompareTo
		/// </summary>
		/// <param name="other">The object being compared to 'this'</param>
		/// <returns>-1, 0, or 1 depending on whether 'other' is less than,
		/// equal to, or greater than 'this'</returns>
		public int CompareTo(VariableInfo other)
		{
			return string.CompareOrdinal(other._key, _key);
		}

		#endregion
	}

	/// <summary>
	/// <c>AdditionalFile</c> is used by <c>TemplateManifest</c> to identify files that were listed in the manifest
	/// as miscellaneous dependencies. Such files are not typically referred to <i>directly</i> by the template
	/// or component file, but were explicitly declared by the template author as being necessary for the proper operation
	/// of the template. Examples may include image files that could potentially be referred to by a dynamic INSERT IMAGE
	/// instruction, or other files that may be required to be in the same location as the template in custom integration
	/// scenarios.
	/// </summary>
	public class AdditionalFile : IEquatable<AdditionalFile>, IComparable<AdditionalFile>
	{
		internal AdditionalFile(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("fileName");

			FileName = fileName;
		}

		/// <summary>
		/// <c>FileName</c> specifies the file name but its location or path.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// Overrides IEquatable.Equals
		/// </summary>
		/// <param name="obj">The object being compared to 'this'</param>
		/// <returns>boolean (true or false)</returns>
		public override bool Equals(object obj)
		{
			return (obj != null) && (obj is AdditionalFile) && Equals((AdditionalFile)obj);
		}

		/// <summary>
		/// Overrides Object.GetHashCode
		/// </summary>
		/// <returns>a number of type int</returns>
		public override int GetHashCode()
		{
			return FileName.ToLower().GetHashCode();
		}

		/// <summary>
		/// Overrides Object.ToString
		/// </summary>
		/// <returns>a string instance similar in content to 'this'</returns>
		public override string ToString()
		{
			return String.Format("FileName: {0}", FileName);
		}

		#region IEquatable<AdditionalFile> Members

		/// <summary>
		/// Implements IEquatable.Equals
		/// </summary>
		/// <param name="other">The object being compared to 'this'</param>
		/// <returns>boolean (true or false)</returns>
		public bool Equals(AdditionalFile other)
		{
			return CompareTo(other) == 0;
		}

		#endregion

		#region IComparable<AdditionalFile> Members

		/// <summary>
		/// Implements IComparable.CompareTo
		/// </summary>
		/// <param name="other">The object being compared to 'this'</param>
		/// <returns>-1, 0, or 1 depending on whether 'other' is less than,
		/// equal to, or greater than 'this'</returns>
		public int CompareTo(AdditionalFile other)
		{
			return string.Compare(other.FileName, FileName, true);
		}

		#endregion
	}

	/// <summary>
	/// <c>DataSourceField</c> provides information about the current field (such as source name, type, etc.)
	/// </summary>
	public class DataSourceField
	{
		internal DataSourceField(string sourceName, DataSourceFieldType fieldType, DataSourceBackfillType backfillType, bool isKey)
		{
			if (string.IsNullOrEmpty(sourceName))
				throw new ArgumentNullException("sourceName");

			SourceName = sourceName;
			FieldType = fieldType;
			BackfillType = backfillType;
			IsKey = isKey;
		}

		/// <summary>
		/// <c>SourceName</c> is a string that describes the source of the current data field.
		/// </summary>
		public string SourceName { get; private set; }

		/// <summary>
		/// <c>DataSourceFieldType</c> is an enumeration that describes the type (such as text, number, etc.) 
		/// of the current field.
		/// </summary>
		public DataSourceFieldType FieldType { get; private set; }

		/// <summary>
		/// <c>DataSourceBackfillType</c> is an enumeration that specifies 
		/// whether and under what conditions the current field will be stored back to 
		/// the data source.
		/// </summary>
		public DataSourceBackfillType BackfillType { get; private set; }

		/// <summary>
		/// <c>IsKey</c> is a boolean key that indicates whether or not the current data source field 
		/// is a key for unique identification.
		/// </summary>
		public bool IsKey { get; private set; }
	}

	/// <summary>
	/// <c>DataSource</c> has information about the current data source, such as id, name, type, and fields.
	/// </summary>
	public class DataSource : IEquatable<DataSource>, IComparable<DataSource>
	{
		internal DataSource(string id, string name, DataSourceType type, DataSourceField[] fields)
		{
			if (string.IsNullOrEmpty(id))
				throw new ArgumentNullException("id");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			Id = id;
			Name = name;
			Type = type;
			Fields = fields;
		}

		/// <summary>
		/// <c>Id</c> is a string the identifies the current data source.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// <c>Name</c> is a string that provides a name for the current data source.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// <c>Type</c> is an enumeration that indicates the type of the current data source.
		/// </summary>
		public DataSourceType Type { get; private set; }

		/// <summary>
		/// <c>Fields</c> a group of data fields associated with the current data source.
		/// </summary>
		public DataSourceField[] Fields { get; private set; }

		/// <summary>
		/// Overrides Object.Equals
		/// </summary>
		/// <param name="obj">The object being compared to 'this'</param>
		/// <returns>boolean (true or false)</returns>
		public override bool Equals(object obj)
		{
			return (obj != null) && (obj is DataSource) && Equals((DataSource)obj);
		}

		/// <summary>
		/// Overrides Object.GetHashCode
		/// </summary>
		/// <returns>a number of type int</returns>
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		/// <summary>
		/// Overrides Object.ToString
		/// </summary>
		/// <returns>a string instance similar in content to 'this'</returns>
		public override string ToString()
		{
			return String.Format("Id: {0}  Name: {1}  Type: {2}", Id, Name, Type);
		}

		#region IEquatable<DataSource> Members

		/// <summary>
		/// Overrides IEquatable<DataSource>.Equals</DataSource>
		/// </summary>
		/// <param name="other">The object being compared to 'this'</param>
		/// <returns>boolean (true or false)</returns>
		public bool Equals(DataSource other)
		{
			return CompareTo(other) == 0;
		}

		#endregion

		#region IComparable<DataSource> Members

		/// <summary>
		/// Overrides IComparable&lt;DataSource&gt;.CompareTo
		/// </summary>
		/// <param name="other">The object being compared to 'this'</param>
		/// <returns>-1, 0, or 1 depending on whether 'other' is less than,
		/// equal to, or greater than 'this'</returns>
		public int CompareTo(DataSource other)
		{
			return string.CompareOrdinal(other.Id, Id);
		}

		#endregion
	}

	/// <summary>
	/// <c>TemplateManifest</c> contains the information needed for document assembly creation with
	/// the specified template file, sometimes called the 'main template'
	/// </summary>
	public class TemplateManifest
	{
		private static readonly XNamespace s_namespace = "http://www.hotdocs.com/schemas/template_manifest/2012";

		private struct TemplateFileLocation : IEquatable<TemplateFileLocation>
		{
			private string _fileName;
			private ITemplateLocation _fileLocation;

            internal TemplateFileLocation(string fileName, ITemplateLocation location)
			{
				_fileName = fileName;
				_fileLocation = location;
			}

			internal TemplateFileLocation(string filePath)
			{
				_fileName = Path.GetFileName(filePath);
				_fileLocation = new PathTemplateLocation(Path.GetDirectoryName(filePath));
			}

			internal string FileName
			{
				get { return _fileName; }
			}

            internal ITemplateLocation FileLocation
			{
				get { return _fileLocation; }
			}

			public override bool Equals(object obj)
			{
				return (obj != null) && (obj is TemplateFileLocation) && Equals((TemplateFileLocation)obj);
			}

			/// <summary>
			/// Overrides Object.GetHashCode
			/// </summary>
			/// <returns>a number of type int</returns>
			public override int GetHashCode()
			{
				const int prime = 397;
				int result = FileLocation.GetHashCode(); // hash for the location, combined with
				result = (result * prime) ^ FileName.ToLower().GetHashCode(); // case-insensitive hash for the file name
				return result;
			}

			#region IEquatable<TemplateFileLocation> Members

			/// <summary>
			/// Implements IEquatable.Equals
			/// </summary>
			/// <param name="other">The object being compared to 'this'</param>
			/// <returns>boolean (true or false)</returns>
			public bool Equals(TemplateFileLocation other)
			{
				return string.Equals(other.FileName, FileName, StringComparison.OrdinalIgnoreCase)
					&& FileLocation.Equals(other.FileLocation);
			}

			#endregion
		}

		internal TemplateManifest() {}

		/// <summary>
		/// <c>HotDocsVersion</c> indicates the version of HotDocs that was used to create
		/// the current template manifest.
		/// </summary>
		public string HotDocsVersion { get; private set; }

		/// <summary>
		/// <c>TemplateId</c> identifies the template to distinguish it from other templates.
		/// </summary>
		public string TemplateId { get; private set; }

		/// <summary>
		/// <c>FileName</c> indicates the file name of the current template.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// <c>EffectiveCmpFileName</c> is the component file name that is used 
		/// for document assembly creation with the main template contained in 
		/// this template manifest.
		/// </summary>
		public string EffectiveCmpFileName { get; private set; }

		/// <summary>
		/// <c>ExpirationDate</c> indicates the current template cannot be used for 
		/// document assembly creations after the given date, except
		/// possibly for a given number of <c>ExtensionDays</c>
		/// </summary>
		public DateTime? ExpirationDate { get; private set; }

		/// <summary>
		/// <c>WarningDays</c> indicates for how many days prior to a document's expiration date
		/// users should be warned about using the current template for document assembly creations.
		/// </summary>
		public int? WarningDays { get; private set; }

		/// <summary>
		/// <c>ExtensionDays</c> indicates for how many days after the current templates 
		/// expiration date has passed users can still perform document assembly creations.
		/// </summary>
		public int? ExtensionDays { get; private set; }

		/// <summary>
		/// <c>Title</c> provides the title for the main template
		/// in the current template manifest.
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// <c>Description</c> provides a description for the main template
		/// in the current template manifest.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// <c>Variables</c> Contains the set of variables associated with the
		/// main template in the current template manifest.
		/// </summary>
		public VariableInfo[] Variables { get; private set; }

		/// <summary>
		/// <c>Dependencies</c> indicates a collection of files associated with
		/// the the main template of the template manifest.
		/// </summary>
		public Dependency[] Dependencies { get; private set; }

		/// <summary>
		/// <c>AdditionalFiles</c> lists additional files that may be required during interviews,
		/// assemblies or other processing associated with the template.
		/// </summary>
		public AdditionalFile[] AdditionalFiles { get; private set; }

		/// <summary>
		/// <c>DataSources</c> lists the data sources (<c>DataSource</c> objects) from which the template
		/// may (at user request) attempt to retrieve data during an interview.
		/// </summary>
		public DataSource[] DataSources { get; private set; }

		/// <summary>
		/// <c>ParseManifest</c> parses the XML template manifest indicated by <paramref name="templatePath"/>
		/// and creates a corresponding instance of <c>TemplateManifest</c>.
		/// </summary>
		/// <param name="templatePath">The full path of the template</param>
		/// <param name="parseFlags">Specifies settings related to template manifest creation</param>
		/// <returns>a reference to the newly created <c>TemplateManifest</c></returns>
		public static TemplateManifest ParseManifest(string templatePath, ManifestParseFlags parseFlags)
		{
			string fileName = Path.GetFileName(templatePath);
			TemplateLocation fileLoc = new PathTemplateLocation(Path.GetDirectoryName(templatePath));

			return ParseManifest(fileName, fileLoc, parseFlags);
		}

		/// <summary>
		/// <c>ParseManifest</c> parses the XML template manifest indicated by <paramref name="fileName"/>
		/// and <paramref name="location"/>, and creates a corresponding instance of <c>TemplateManifest</c>.
		/// </summary>
		/// <param name="fileName">The file name of the template</param>
		/// <param name="location">The location of the template, such as a file system folder, a package file, a database, etc.</param>
		/// <param name="parseFlags">Specifies settings related to template manifest creation</param>
		/// <returns>a reference to the newly created <c>TemplateManifest</c></returns>
        public static TemplateManifest ParseManifest(string fileName, ITemplateLocation location, ManifestParseFlags parseFlags)
		{
			TemplateManifest templateManifest = new TemplateManifest();
			TemplateFileLocation baseTemplateLoc = new TemplateFileLocation(fileName, location);

			HashSet<VariableInfo> variables = GetHashSet<VariableInfo>(parseFlags, ManifestParseFlags.ParseVariables);
			HashSet<Dependency> dependencies = GetHashSet<Dependency>(parseFlags, ManifestParseFlags.ParseDependencies);
			HashSet<AdditionalFile> additionalFiles = GetHashSet<AdditionalFile>(parseFlags, ManifestParseFlags.ParseAdditionalFiles);
			HashSet<DataSource> dataSources = GetHashSet<DataSource>(parseFlags, ManifestParseFlags.ParseDataSources);

			Queue<TemplateFileLocation> templateQueue = new Queue<TemplateFileLocation>();
			HashSet<TemplateFileLocation> processedTemplates = new HashSet<TemplateFileLocation>();

			// Add the base template to the queue
			templateQueue.Enqueue(baseTemplateLoc);

			while (templateQueue.Count > 0)
			{
				TemplateFileLocation templateFileLoc = templateQueue.Dequeue();
				if (!processedTemplates.Contains(templateFileLoc))
				{
					try
					{
						// TODO: Condider re-writing this using an XmlReader so that the entire document does not need to be allocated in a DOM. Doing so
						// should make processing template manifest files faster. For now using an XDocument to just get things done fast.
						XDocument manifest;
						using (Stream manifestStream = templateFileLoc.FileLocation.GetFile(GetManifestName(templateFileLoc.FileName)))
						{
							// Read the template manifest so that it can be parsed.
							manifest = XDocument.Load(manifestStream, LoadOptions.None);
						}

						if (templateFileLoc.Equals(baseTemplateLoc))
						{
							// Process the root templateManifest element.
							templateManifest.HotDocsVersion = manifest.Root.Attribute("hotdocsVersion").Value;
							templateManifest.TemplateId = manifest.Root.Attribute("templateId").Value;
							templateManifest.FileName = manifest.Root.Attribute("fileName").Value;
							templateManifest.EffectiveCmpFileName = manifest.Root.Attribute("effectiveCmpFile").Value;

							if (manifest.Root.Attribute("expirationDate") != null)
							{
								templateManifest.ExpirationDate = DateTime.Parse(manifest.Root.Attribute("expirationDate").Value);

								if (manifest.Root.Attribute("warningDays") != null)
								{
									templateManifest.WarningDays = int.Parse(manifest.Root.Attribute("warningDays").Value);
								}

								if (manifest.Root.Attribute("extensionDays") != null)
								{
									templateManifest.ExtensionDays = int.Parse(manifest.Root.Attribute("extensionDays").Value);
								}
							}

							var titleElem = manifest.Root.Element(s_namespace + "title");
							templateManifest.Title = (titleElem != null) ? titleElem.Value.Trim() : string.Empty;

							var descriptionElem = manifest.Root.Element(s_namespace + "description");
							templateManifest.Description = (descriptionElem != null) ? descriptionElem.Value.Trim() : string.Empty;
						}

						if (variables != null)
						{
							// Process the variables element.
							var variablesElem = manifest.Root.Element(s_namespace + "variables");
							if (variablesElem != null)
							{
								// Add any not yet encountered variable to the variables collection.
								foreach (var variableElem in variablesElem.Elements(s_namespace + "variable"))
								{
									ValueType valueType;
									switch (variableElem.Attribute("type").Value)
									{
										case "text":
											valueType = ValueType.Text;
											break;
										case "number":
											valueType = ValueType.Number;
											break;
										case "date":
											valueType = ValueType.Date;
											break;
										case "trueFalse":
											valueType = ValueType.TrueFalse;
											break;
										case "multipleChoice":
											valueType = ValueType.MultipleChoice;
											break;
										default:
											valueType = ValueType.Unknown;
											break;
									}
									variables.Add(new VariableInfo(variableElem.Attribute("name").Value, valueType));
								}
							}
						}

						if (dependencies != null)
						{
							// Process the dependencies element.
							var dependenciesElem = manifest.Root.Element(s_namespace + "dependencies");
							if (dependenciesElem != null)
							{
								// Add any not yet encountered dependency to the dependencies collection.
								foreach (var dependencyElem in dependenciesElem.Elements(s_namespace + "dependency"))
								{
									string hintPath = (dependencyElem.Attribute("hintPath") != null) ? dependencyElem.Attribute("hintPath").Value : null;

									DependencyType dependencyType;
									switch (dependencyElem.Attribute("type").Value)
									{
										case "baseCmpFile":
											dependencyType = DependencyType.BaseCmpFile;
											break;
										case "pointedToCmpFile":
											dependencyType = DependencyType.PointedToCmpFile;
											break;
										case "templateInsert":
											dependencyType = DependencyType.TemplateInsert;
											break;
										case "clauseInsert":
											dependencyType = DependencyType.ClauseInsert;
											break;
										case "clauseLibraryInsert":
											dependencyType = DependencyType.ClauseLibraryInsert;
											break;
										case "imageInsert":
											dependencyType = DependencyType.ImageInsert;
											break;
										case "interviewImage":
											dependencyType = DependencyType.InterviewImage;
											break;
										case "assemble":
											dependencyType = DependencyType.Assemble;
											break;
										case "publisherMapFile":
											dependencyType = DependencyType.PublisherMapFile;
											break;
										case "userMapFile":
											dependencyType = DependencyType.UserMapFile;
											break;
										case "additionalTemplate":
											dependencyType = DependencyType.AdditionalTemplate;
											break;
										default:
											throw new Exception(string.Format("Invalid dependency type '{0}'.", dependencyElem.Attribute("type").Value));
									}
									dependencies.Add(new Dependency(dependencyElem.Attribute("fileName").Value, hintPath, dependencyType));
								}
							}
						}

						if (additionalFiles != null)
						{
							// Process the additionalFiles element.
							var additionalFilesElem = manifest.Root.Element(s_namespace + "additionalFiles");
							if (additionalFilesElem != null)
							{
								// Add any not yet encountered additionalFile to the additionalFiles collection.
								foreach (var fileElem in additionalFilesElem.Elements(s_namespace + "file"))
								{
									additionalFiles.Add(new AdditionalFile(fileElem.Attribute("fileName").Value));
								}
							}
						}

						if (dataSources != null)
						{
							// Process the dataSources element.
							var dataSourcesElem = manifest.Root.Element(s_namespace + "dataSources");
							if (dataSourcesElem != null)
							{
								// Add any not yet encountered dataSource to the dataSources collection.
								foreach (var dataSourceElem in dataSourcesElem.Elements(s_namespace + "dataSource"))
								{
									DataSourceType dataSourceType;
									switch (dataSourceElem.Attribute("type").Value)
									{
										case "currentAnswerFile":
											dataSourceType = DataSourceType.CurrentAnswerFile;
											break;
										case "answerFile":
											dataSourceType = DataSourceType.AnswerFile;
											break;
										case "databaseComponent":
											dataSourceType = DataSourceType.DatabaseComponent;
											break;
										case "custom":
											dataSourceType = DataSourceType.Custom;
											break;
										default:
											throw new Exception(string.Format("Invalid data source type '{0}'.", dataSourceElem.Attribute("type").Value));
									}

									List<DataSourceField> dataSourceFields = new List<DataSourceField>();
									foreach (var dataSourceFieldElem in dataSourceElem.Elements(s_namespace + "dataSourceField"))
									{
										DataSourceFieldType fieldType;
										switch (dataSourceFieldElem.Attribute("type").Value)
										{
											case "text":
												fieldType = DataSourceFieldType.Text;
												break;
											case "number":
												fieldType = DataSourceFieldType.Number;
												break;
											case "date":
												fieldType = DataSourceFieldType.Date;
												break;
											case "trueFalse":
												fieldType = DataSourceFieldType.TrueFalse;
												break;
											default:
												throw new Exception(string.Format("Invalid data source field type '{0}'.", dataSourceFieldElem.Attribute("type").Value));
										}

										DataSourceBackfillType backfillType;
										switch (dataSourceFieldElem.Attribute("backfill").Value)
										{
											case "never":
												backfillType = DataSourceBackfillType.Never;
												break;
											case "always":
												backfillType = DataSourceBackfillType.Always;
												break;
											case "prompt":
												backfillType = DataSourceBackfillType.Prompt;
												break;
											case "doNotAllow":
												backfillType = DataSourceBackfillType.DoNotAllow;
												break;
											default:
												throw new Exception(string.Format("Invalid data source backfill type '{0}'.", dataSourceFieldElem.Attribute("backfill").Value));
										}

										bool isKey = (dataSourceFieldElem.Attribute("isKey") != null) ? Convert.ToBoolean(dataSourceFieldElem.Attribute("isKey").Value) : false;

										dataSourceFields.Add(new DataSourceField(dataSourceFieldElem.Attribute("sourceName").Value, fieldType, backfillType, isKey));
									}
									dataSources.Add(new DataSource(dataSourceElem.Attribute("id").Value, dataSourceElem.Attribute("name").Value,
										dataSourceType, dataSourceFields.ToArray()));
								}
							}
						}

						if ((parseFlags & ManifestParseFlags.ParseRecursively) == ManifestParseFlags.ParseRecursively)
						{
							// Add any referenced templates to the template queue.
							var dependenciesElem = manifest.Root.Element(s_namespace + "dependencies");
							if (dependenciesElem != null)
							{
								var templateDependencies = from d in dependenciesElem.Elements(s_namespace + "dependency")
														   let type = d.Attribute("type").Value
														   where type == "templateInsert" || type == "additionalTemplate"
														   select d;

								foreach (var templateDependency in templateDependencies)
								{
									templateQueue.Enqueue(GetDependencyFileLocation(baseTemplateLoc,
										(templateDependency.Attribute("hintPath") != null) ? templateDependency.Attribute("hintPath").Value : null,
										templateDependency.Attribute("fileName").Value));
								}
							}

							// Mark the template as processed so that its manifest will not get processed again.
							processedTemplates.Add(templateFileLoc);
						}
					}
					catch (Exception e)
					{
						string errorText = String.Format("Failed to read the manifest file for the template:\r\n\r\n     {0}     \r\n\r\n" +
							"The following error occurred:\r\n\r\n     {1}     ", templateFileLoc.FileName, e.Message);
						throw new Exception(errorText, e);
					}
				}
			}

			if (variables != null)
				templateManifest.Variables = variables.ToArray();

			if (dependencies != null)
				templateManifest.Dependencies = dependencies.ToArray();

			if (additionalFiles != null)
				templateManifest.AdditionalFiles = additionalFiles.ToArray();

			if (dataSources != null)
				templateManifest.DataSources = dataSources.ToArray();

			return templateManifest;
		}

		private static HashSet<T> GetHashSet<T>(ManifestParseFlags parseFlags, ManifestParseFlags flag) where T : class
		{
			return ((parseFlags & flag) == flag) ? new HashSet<T>() : null;
		}

		private static string GetManifestName(string itemName)
		{
			// If the passed file name appears to already be a manifest file name then just use it.
			if (itemName.EndsWith(".manifest.xml", StringComparison.OrdinalIgnoreCase))
				return itemName;

			return itemName + ".manifest.xml";
		}

		private static TemplateFileLocation GetDependencyFileLocation(TemplateFileLocation itemLoc, string hintPath, string fileName)
		{
			if (!string.IsNullOrEmpty(hintPath))
			{
				// A hint path was specified. Use it to locate the dependent file.
				return new TemplateFileLocation(Path.Combine(hintPath, fileName));
			}
			else
			{
				// No hint path was specified. Assume the dependent template is in the same folder as the main (item) template.
				return new TemplateFileLocation(fileName, itemLoc.FileLocation);
			}
		}

	}
}
