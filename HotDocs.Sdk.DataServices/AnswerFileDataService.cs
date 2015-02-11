/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HotDocs.Sdk.Server;

namespace HotDocs.Sdk.DataServices
{
	/// <summary>
	/// <c>AnswerFileDataService</c> 
	/// </summary>
	public class AnswerFileDataService : DSPDataService<DSPContext>
	{
		private const string DefineAnswerFileDataSourceOperationName = "DefineAnswerFileDataSource";
		private const int AnswerFileDataSourceExpirationPeriod = 30;
		private const int AnswerFileDataSourceExpirationTimerPeriod = 15;

		private static ReaderWriterLockSlim s_readerWriterLock;
		private static Timer s_timer;
		private static Dictionary<string, AnswerFileDataSource> s_answerFileDataSourceDict;
		private static DSPMetadata s_metadata;
		private static DSPResourceQueryProvider s_queryProvider;
		private static DSPContext s_context;

		private class AnswerFileDataSource
		{
			FileSystemWatcher _fileSystemWatcher;
			DateTime _lastWriteTime;

			internal AnswerFileDataSource(string dataSourceId, string dataSourceName, string answerFilePath)
			{
				DataSourceId = dataSourceId;
				DataSourceName = dataSourceName;
				AnswerFilePath = answerFilePath;
				ResourceId = Guid.NewGuid().ToString("N");
				PropertyNameToSourceNameMap = new Dictionary<string, string>();
				LastAccess = DateTime.Now;

				// Watch if the underlying answer file changes on disk. If so, then refresh the cached answers in the data service 
				// for the data source.
				_fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(answerFilePath), Path.GetFileName(answerFilePath))
				{
					NotifyFilter = NotifyFilters.LastWrite,
					EnableRaisingEvents = true
				};
				_fileSystemWatcher.Changed += new FileSystemEventHandler(_fileSystemWatcher_Changed);
				_lastWriteTime = File.GetLastWriteTime(answerFilePath);
			}

			internal string DataSourceId { get; private set; }
			internal string DataSourceName { get; private set; }
			internal string AnswerFilePath { get; private set; }
			internal string ResourceId { get; private set; }
			internal Dictionary<string, string> PropertyNameToSourceNameMap { get; private set; }
			internal DateTime LastAccess { get; set; }

			void _fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
			{
				if (File.GetLastWriteTime(e.FullPath) > _lastWriteTime)
				{
					_lastWriteTime = File.GetLastWriteTime(e.FullPath);
					s_readerWriterLock.EnterWriteLock();
					try
					{
						LoadAnswerFileDataSource(this);
					}
					finally
					{
						s_readerWriterLock.ExitWriteLock();
					}
				}
			}
		}

		static AnswerFileDataService()
		{
			s_readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			s_timer = new Timer(AnswerFileExpirationTimerCallback, null,
				TimeSpan.FromMinutes(AnswerFileDataSourceExpirationTimerPeriod), TimeSpan.FromMinutes(AnswerFileDataSourceExpirationTimerPeriod));

			s_answerFileDataSourceDict = new Dictionary<string, AnswerFileDataSource>();

			s_metadata = new DSPMetadata("AnswerFileDataService", "AnswerFileDataService", s_readerWriterLock);

			// TODO: Pass the browser state string for the templatePath so that it will be encrypted over the wire.
			ServiceOperationParameter[] parameters = new[]
					{
						new ServiceOperationParameter("templateLocator", ResourceType.GetPrimitiveResourceType(typeof(string))),
						new ServiceOperationParameter("templateName", ResourceType.GetPrimitiveResourceType(typeof(string))),
						new ServiceOperationParameter("dataSourceId", ResourceType.GetPrimitiveResourceType(typeof(string)))
					};

			s_metadata.AddServiceOperation(DefineAnswerFileDataSourceOperationName, ServiceOperationResultKind.DirectValue,
				ResourceType.GetPrimitiveResourceType(typeof(string)), null, "GET", parameters);

			s_queryProvider = new DSPResourceQueryProvider();

			s_queryProvider.RegisterServiceOperationHandler(HandleServiceOperation);

			s_context = new DSPContext(s_readerWriterLock);
		}

		/// <summary>
		/// Initializes an answer file data service.
		/// </summary>
		/// <param name="config">The configuration of the answer file data service.</param>
		public static void InitializeService(DataServiceConfiguration config)
		{
			config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
			config.SetServiceOperationAccessRule(DefineAnswerFileDataSourceOperationName, ServiceOperationRights.ReadSingle);
			config.SetEntitySetPageSize("*", 100);
			config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;
			//config.DataServiceBehavior.AcceptProjectionRequests = true;
		}

		/// <summary>
		/// Returns the service meta data.
		/// </summary>
		/// <returns>Service meta data.</returns>
		protected override DSPMetadata CreateDSPMetadata()
		{
			return s_metadata;
		}

		/// <summary>
		/// Returns the service query provider.
		/// </summary>
		/// <returns>Service query provider.</returns>
		protected override DSPResourceQueryProvider CreateDSPQueryProvider()
		{
			return s_queryProvider;
		}

		/// <summary>
		/// Returns the service data source.
		/// </summary>
		/// <returns>Service data source.</returns>
		protected override DSPContext CreateDataSource()
		{
			return s_context;
		}

		private static object HandleServiceOperation(ServiceOperation serviceOperation, object[] parameters)
		{
			if (string.CompareOrdinal(serviceOperation.Name, DefineAnswerFileDataSourceOperationName) == 0)
			{
				string templateLocator = parameters[0].ToString();
				string templateName = parameters[1].ToString();
				string dataSourceId = parameters[2].ToString();

				s_readerWriterLock.EnterWriteLock();
				try
				{
					AnswerFileDataSource answerFileDataSource;
					if (!s_answerFileDataSourceDict.TryGetValue(dataSourceId, out answerFileDataSource))
					{
						answerFileDataSource = DefineAnswerFileDataSource(dataSourceId, templateLocator, templateName);
						s_answerFileDataSourceDict.Add(dataSourceId, answerFileDataSource);
						LoadAnswerFileDataSource(answerFileDataSource);

						// The below can be used to simulate a long data download for testing purposes. For example, to test that the 
						// BusyIndicator control properly displays in a Silverlight interview.
						//Thread.Sleep(TimeSpan.FromSeconds(10));
					}
					else
					{
						answerFileDataSource.LastAccess = DateTime.Now;
					}

					return answerFileDataSource.ResourceId;
				}
				finally
				{
					s_readerWriterLock.ExitWriteLock();
				}

				throw new DataServiceException(string.Format("The data source for the template '{0}' with the ID '{1}' cannot be defined.", 
					templateName, dataSourceId));
			}

			throw new DataServiceException(string.Format("The service operation '{0}' is not supported.", serviceOperation.Name));
		}

		private static string MakeValidIdent(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException();

			StringBuilder identString = new StringBuilder();

			foreach (char ch in name)
			{
				if (!char.IsLetterOrDigit(ch) || (ch > 0x007F))
					identString.AppendFormat("_{0:X4}", Convert.ToInt32(ch));
				else
					identString.Append(ch);
			}
			return identString.ToString();
		}

		private static AnswerFileDataSource DefineAnswerFileDataSource(string dataSourceId, string templateLocator, string templateName)
		{
			Debug.Assert(s_readerWriterLock.IsWriteLockHeld);

			try
			{
				ITemplateOnDisk template = TemplateOnDisk.Locate(templateLocator);
				TemplateManifest templateManifest = template.GetManifest(ManifestParseFlags.ParseDataSources);

				DataSource dataSource = templateManifest.DataSources.Single(ds => string.CompareOrdinal(ds.Id, dataSourceId) == 0);
				if (dataSource.Type != DataSourceType.AnswerFile)
				{
					throw new Exception(string.Format("Attempt to use a data source with the name '{0}' as an answer file data source but its " +
						"data source type is '{1}'", dataSource.Name, dataSource.Type));
				}

				string dataSourcePath = Path.Combine(template.Location.GetTemplateDirectory(), dataSource.Name);
				AnswerFileDataSource answerFileDataSource = new AnswerFileDataSource(dataSourceId, dataSource.Name, dataSourcePath);

				// Create the metadata for the answer file data source.
				ResourceType resourceType = s_metadata.AddEntityType(answerFileDataSource.ResourceId);

				// All resources need a key property.
				s_metadata.AddKeyProperty(resourceType, "ID", typeof(int));
				answerFileDataSource.PropertyNameToSourceNameMap["ID"] = "ID";

				Type propType;
				foreach (var dataSourceField in dataSource.Fields)
				{
					switch (dataSourceField.FieldType)
					{
						case DataSourceFieldType.Text:
							propType = typeof(string);
							break;
						case DataSourceFieldType.Number:
							propType = typeof(double?);
							break;
						case DataSourceFieldType.Date:
							propType = typeof(DateTime?);
							break;
						case DataSourceFieldType.TrueFalse:
							propType = typeof(bool?);
							break;
						default:
							propType = null;
							break;
					}

					if (propType != null)
					{
						string sourceFieldName = dataSourceField.SourceName;
						string propertyName = "Prop" + MakeValidIdent(sourceFieldName);
						s_metadata.AddPrimitiveProperty(resourceType, propertyName, propType);
						answerFileDataSource.PropertyNameToSourceNameMap[propertyName] = sourceFieldName;
					}
				}
				resourceType.SetReadOnly();

				// Create a resource set for the resource type.
				ResourceSet resourceSet = s_metadata.AddResourceSet(answerFileDataSource.ResourceId, resourceType);
				resourceSet.SetReadOnly();

				return answerFileDataSource;
			}
			catch (Exception e)
			{
				throw new DataServiceException(string.Format("Failed to read the metadata for the data source Id '{0}' from the template manifest file for the " +
					"template '{1}'.", dataSourceId, templateName), e);
			}
		}

		private static void LoadAnswerFileDataSource(AnswerFileDataSource answerFileDataSource)
		{
			Debug.Assert(s_readerWriterLock.IsWriteLockHeld);

			Stream answerFileStream = null;
			try
			{
				answerFileStream = new FileStream(answerFileDataSource.AnswerFilePath, FileMode.Open, FileAccess.Read);

				AnswerCollection answerSet = new AnswerCollection();
				answerSet.ReadXml(answerFileStream);

				ResourceSet resourceSet = s_metadata.ResourceSets.Single(rs => rs.Name == answerFileDataSource.ResourceId);
				ResourceType resourceType = resourceSet.ResourceType;
				List<Answer> answers = new List<Answer>(resourceType.Properties.Count);
				int repeatCount = 0;
				Answer answer;
				foreach (var property in resourceType.Properties)
				{
					if ((property.Kind & ResourcePropertyKind.Key) != ResourcePropertyKind.Key)
					{
						ValueType valueType = ValueType.Unknown;

						// Infer from the CLR type what the ValueType is of the answer to be fetched.
						if (property.ResourceType.InstanceType == typeof(string))
						{
							valueType = ValueType.Text;
						}
						else if (property.ResourceType.InstanceType == typeof(double?))
						{
							valueType = ValueType.Number;
						}
						else if (property.ResourceType.InstanceType == typeof(DateTime?))
						{
							valueType = ValueType.Date;
						}
						else if (property.ResourceType.InstanceType == typeof(bool?))
						{
							valueType = ValueType.TrueFalse;
						}

						if (valueType == ValueType.Unknown)
						{
							throw new Exception(string.Format("The metadata property '{0}' with a CLR type of '{1}' does not correspond to a supported " +
								"HotDocs ValueType.", answerFileDataSource.PropertyNameToSourceNameMap[property.Name],
								property.ResourceType.InstanceType.FullName));
						}

						if (answerSet.TryGetAnswer(answerFileDataSource.PropertyNameToSourceNameMap[property.Name], valueType, out answer) ||
							((valueType == ValueType.Text) && 
							answerSet.TryGetAnswer(answerFileDataSource.PropertyNameToSourceNameMap[property.Name], ValueType.MultipleChoice, out answer)))
						{
							repeatCount = Math.Max(repeatCount, answer.GetChildCount());
							answers.Add(answer);
						}
						else
							answers.Add(null);
					}
				}

				// Populate the data source with data.
				IList<DSPResource> resourceList = s_context.GetResourceSetEntities(resourceSet.Name);
				resourceList.Clear();

				for (int repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
				{
					var resource = new DSPResource(resourceSet.ResourceType, s_readerWriterLock);
					int answerIndex = 0;
					for (int propertyIndex = 0; propertyIndex < resourceType.Properties.Count; propertyIndex++)
					{
						ResourceProperty property = resourceType.Properties[propertyIndex];
						object value;
						if ((property.Kind & ResourcePropertyKind.Key) == ResourcePropertyKind.Key)
						{
							value = repeatIndex + 1;
						}
						else
						{
							IValue iValue = null;
							answer = answers[answerIndex++];
							if ((answer != null) && (repeatIndex <= answer.GetChildCount()))
							{
								iValue = answer.GetValue(repeatIndex);
							}

							if (property.ResourceType.InstanceType == typeof(string))
							{
								value = ((iValue != null) && iValue.IsAnswered) ? iValue.ToString(null) : string.Empty;
							}
							else if (property.ResourceType.InstanceType == typeof(double?))
							{
								if ((iValue != null) && iValue.IsAnswered)
									value = iValue.ToDouble(null);
								else
									value = null;
							}
							else if (property.ResourceType.InstanceType == typeof(DateTime?))
							{
								if ((iValue != null) && iValue.IsAnswered)
									value = iValue.ToDateTime(null);
								else
									value = null;
							}
							else
							{
								Debug.Assert(property.ResourceType.InstanceType == typeof(bool?));
								if ((iValue != null) && iValue.IsAnswered)
									value = iValue.ToBoolean(null);
								else
									value = null;
							}
						}

						resource.SetValue(property.Name, value);
					}

					resourceList.Add(resource);
				}
			}
			catch (Exception e)
			{
				throw new DataServiceException(string.Format("Failed to read the answers for the data source key '{0}' from the answer file '{1}'.",
					answerFileDataSource.DataSourceId, answerFileDataSource.DataSourceName), e);
			}
			finally
			{
				if (answerFileStream != null)
					answerFileStream.Close();
			}
		}

		private static void AnswerFileExpirationTimerCallback(object state)
		{
			s_readerWriterLock.EnterUpgradeableReadLock();
			try
			{
				AnswerFileDataSource[] expiredAnswerFileDataSources = s_answerFileDataSourceDict.Values.
					Where(a => (DateTime.Now - a.LastAccess).TotalMinutes >= AnswerFileDataSourceExpirationPeriod).ToArray();

				if (expiredAnswerFileDataSources.Any())
				{
					s_readerWriterLock.EnterWriteLock();
					try
					{
						foreach (var answerFileDataSource in expiredAnswerFileDataSources)
						{
							// Remove all the metadata for the answer source.
							s_metadata.RemoveResourceType(answerFileDataSource.ResourceId);
							s_metadata.RemoveResourceSet(answerFileDataSource.ResourceId);

							// Remove the cached data for the answer source.
							s_context.RemoveResourceSetEntities(answerFileDataSource.ResourceId);

							// Remove the answer source definition.
							s_answerFileDataSourceDict.Remove(answerFileDataSource.DataSourceId);
						}
					}
					finally
					{
						s_readerWriterLock.ExitWriteLock();
					}
				}
			}
			finally
			{
				s_readerWriterLock.ExitUpgradeableReadLock();
			}
		}

		/// <summary>
		/// Handles exceptions thrown by the data service.
		/// </summary>
		/// <param name="args">The exception arguments.</param>
		protected override void HandleException(HandleExceptionArgs args)
		{
			base.HandleException(args);
		}
	}
}