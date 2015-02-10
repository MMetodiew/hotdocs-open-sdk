/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Reflection;

namespace HotDocs.Sdk.ServerTest
{
	static class Util
	{
		//private static string GetLocalPackagePath(string packageId)
		//{
		//	Assembly _asm = Assembly.GetExecutingAssembly();

		//	string templatePath = Path.GetDirectoryName(_asm.Location);
		//	return Path.Combine(templatePath, packageId + ".pkg");
		//}

		public static StreamReader GetTestFile(string fileName)
		{
			Assembly _assembly = Assembly.GetExecutingAssembly();
			return new StreamReader(_assembly.GetManifestResourceStream("HotDocs.Sdk.ServerTest.TestFiles." + fileName));
		}

		public static string TestFilesPath
		{
			get
			{
				Assembly _asm = Assembly.GetExecutingAssembly();
				string testFilesPath = Path.Combine(Path.GetDirectoryName(_asm.Location), "TestFiles");
				return testFilesPath;
			}
		}

		public static string GetFileContentAsString(string filePath)
		{
			string content = "";
			using (FileStream fs = File.OpenRead(filePath))
			{
				TextReader rdr = new StreamReader(fs);
				content = rdr.ReadToEnd();
			}
			return content;
		}

		public static PackagePathTemplateLocation CreatePackagePathLocation(string packageID)
		{
			//Note that in this sample portal, the package ID is used to construct the package file name, but this does not need to be the case.
			string templateDirectory = Path.Combine(GetSamplePortalTemplateDir(), "TestTemplates");
			string packagePath = Path.Combine(templateDirectory, packageID + ".pkg");

			if (!File.Exists(packagePath))
				throw new Exception("The template does not exist.");
			return new HotDocs.Sdk.PackagePathTemplateLocation(packageID, packagePath);
		}

		public static HotDocs.Sdk.ITemplate OpenTemplate(string packageID)
		{
			return new HotDocs.Sdk.Template(CreatePackagePathLocation(packageID));
		}

		public static HotDocs.Sdk.Server.IServices GetLocalServicesInterface()
		{
			string tempPath = ConfigurationManager.AppSettings["TempPath"];
			if (!Directory.Exists(tempPath))
				Directory.CreateDirectory(tempPath);
			return new HotDocs.Sdk.Server.Local.Services(tempPath);
		}

		public static HotDocs.Sdk.Server.IServices GetWebServiceServicesInterface()
		{
			string endPointName = ConfigurationManager.AppSettings["WebServiceEndPoint"];

			Assembly _asm = Assembly.GetExecutingAssembly();
			string templatePath = Path.Combine(GetSamplePortalTemplateDir());

			return new HotDocs.Sdk.Server.WebService.Services(endPointName, templatePath);
		}

		public static HotDocs.Sdk.Server.IServices GetCloudServicesInterface()
		{
			string cloudSigningKey = ConfigurationManager.AppSettings["SigningKey"];
			string cloudSubscriberID = ConfigurationManager.AppSettings["SubscriberID"];
			return new HotDocs.Sdk.Server.Cloud.Services(cloudSubscriberID, cloudSigningKey);
		}

		public static string GetTestProjectPath()
		{
			string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			string root = "";
			if (Path.IsPathRooted(assemblyPath))
				root = Path.GetPathRoot(assemblyPath);
			string[] tokens = assemblyPath.Substring(root.Length).Split(new char[] { Path.DirectorySeparatorChar, Path.DirectorySeparatorChar });
			if (tokens.Length <= 3)
				throw new Exception("Invalid path.");
			string[] subTokens = new string[tokens.Length - 3];//Get the path minus two folders and the file name, hence three.
			Array.Copy(tokens, subTokens, tokens.Length - 3);
			string testProjectPath = Path.Combine(subTokens);
			if (root.Length > 0)
				testProjectPath = Path.Combine(root, testProjectPath);
			return testProjectPath;
		}

		public static string GetSamplePortalAnswersDir()
		{
			string samplePortalRoot = GetSamplePortalRootedPath();
			string filesDir = Path.Combine(samplePortalRoot, "Files");
			return Path.Combine(filesDir, "Answers");
		}

		public static string GetTestAnswersDir()
		{
			return Path.Combine(GetSamplePortalAnswersDir(), "TestAnswers");
		}

		public static string GetSamplePortalTemplateDir()
		{
			string samplePortalRoot = GetSamplePortalRootedPath();
			string filesDir = Path.Combine(samplePortalRoot, "Files");
			return Path.Combine(filesDir, "Templates");
		}

		#region Private methods

		private static string GetSamplePortalRootedPath()
		{
			string sRet;
			// assemblyBinariesFolder should be bin\Debug or bin\Release, within the current solution folder:
			string assemblyBinariesFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			string serverTestRoot = Directory.GetParent(Directory.GetParent(assemblyBinariesFolder).ToString()).ToString();
			string sdkRoot = Directory.GetParent(serverTestRoot).ToString();
			sRet = Path.Combine(sdkRoot, "SamplePortal");
			return sRet;
		}

		#endregion // Private methods
	}
}
