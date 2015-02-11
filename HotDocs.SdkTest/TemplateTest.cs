using System;
using System.Collections.Generic;
using System.IO;
using HotDocs.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HotDocs.SdkTest
{
	public class TemplateInfo
	{
		public string DocExtension { get; set; }
		public string TplExtension { get; set; }
		public string Title { get; set; }
		public TemplateType TmpType { get; set; }
		public DocumentType DocType { get; set; }
	}

	/// <summary>
	///This is a test class for the Template and TemplateLocation classes and is intended
	///to contain all Template-related Unit Tests
	///</summary>
	[TestClass]
	public class TemplateTest
	{
		public TemplateTest()
		{
			TemplateLocationSerializer.RegisterLocation(typeof(HotDocs.Sdk.PathTemplateLocation));
			TemplateLocationSerializer.RegisterLocation(typeof(HotDocs.Sdk.PackagePathTemplateLocation));
		}

		[TestMethod]
		public void TestPackagePathLocation()
		{
			string packageID = "d1f7cade-cb74-4457-a9a0-27d94f5c2d5b";
			string templateFileName = "Demo Employment Agreement.docx";
			HotDocs.Sdk.PackagePathTemplateLocation location = CreatePackagePathLocation(packageID);
			DiskAccessibleTemplate template;
			Assert.IsTrue(File.Exists(location.PackagePath));

			// Verify that null switches and key are resolved to empty strings.
            template = new DiskAccessibleTemplate(location, null, null);
			Assert.AreEqual("", template.Switches);
			Assert.AreEqual("", template.Key);
			Assert.IsTrue(template.HasInterview);
			template.Switches = "/nw /naw /ni";
			Assert.IsFalse(template.HasInterview);

			string switches = "/ni";
			string key = "Test file key";
            template = new DiskAccessibleTemplate(location, switches, key);

			Assert.AreEqual(templateFileName, template.FileName);
			Assert.AreEqual(key, template.Key);
			Assert.AreEqual(switches, template.Switches);

			Assert.AreEqual("Employment Agreement (Word RTF version)", template.Title.Trim());

			string filePath = template.GetFullPath();
			Assert.IsTrue(File.Exists(filePath));
			//Directory.Delete(Path.GetDirectoryName(filePath));

			//Check the second time since the folder has been deleted.
			//filePath = template.GetFullPath();//The folder should come into existence here.
			//Assert.IsTrue(File.Exists(filePath));
			//Directory.Delete(Path.GetDirectoryName(filePath));//Clean up.



			TestTemplate(template, new TemplateInfo()
			{
				Title = "Employment Agreement",
				DocExtension = ".docx",
				TmpType = TemplateType.WordDOCX,
				DocType = DocumentType.WordDOCX
			});
		}

		[TestMethod]
		public void TestPathTemplateLocation()
		{
			string templateDir = Path.Combine(GetSamplePortalTemplateDir(), "TestTemplates");
			Assert.IsTrue(Directory.Exists(templateDir));
			PathTemplateLocation location = new PathTemplateLocation(templateDir);
			ITemplateOnDisk template;

			// Make sure that null fileName causes exception.
			try
			{
                template = new DiskAccessibleTemplate(null, location, null, null);
				Assert.Fail();
			}
			catch (Exception ex)
			{
				Assert.IsTrue(ex.Message.Contains("fileName"));
			}

			// Make sure that null location causes exception.
			try
			{
                template = new DiskAccessibleTemplate("filename.docx", null, null, null);
				Assert.Fail();
			}
			catch (Exception ex)
			{
				Assert.IsTrue(ex.Message.Contains("location"));
			}

			// Make sure that a null location causes exception.
			try
			{
                template = new DiskAccessibleTemplate(null, null, null);
				Assert.Fail();
			}
			catch (Exception ex)
			{
				Assert.IsTrue(ex.Message.Contains("location"));
			}

			// Try to locate a template with a null locator.
			try
			{
                template = DiskAccessibleTemplate.Locate(null);
				Assert.Fail();
			}
			catch (ArgumentNullException ex)
			{
				Assert.IsTrue(ex.Message.Contains("locator"));
			}

			// Try to locate a template with an empty string locator.
			try
			{
                template = DiskAccessibleTemplate.Locate("");
				Assert.Fail();
			}
			catch (ArgumentNullException ex)
			{
				Assert.IsTrue(ex.Message.Contains("locator"));
			}

			// Try to locate a template with an invalid template locator.
			try
			{
                template = DiskAccessibleTemplate.Locate(Util.EncryptString("abcdefg")); 
				Assert.Fail();
			}
			catch (Exception ex)
			{
				Assert.IsTrue(ex.Message.Contains("Invalid template locator."));
			}

			try
			{
				TemplateLocationSerializer.RegisterLocation(typeof(string));
				Assert.Fail();
			}
			catch (Exception ex)
			{
				Assert.IsTrue(ex.Message.Contains("The registered location must be of type TemplateLocation."));
			}

			// Try to get the TemplateType for an unknown template type.
            template = new DiskAccessibleTemplate("filename.tfx", location, null, null);
			Assert.AreEqual(TemplateType.Unknown, template.TemplateType);
			Assert.AreEqual("", template.Title);

			// Verify that null switches and key resolve to empty string.
            template = new DiskAccessibleTemplate("Demo Employment Agreement.docx", location, null, null);
			Assert.AreEqual("", template.Switches);
			Assert.AreEqual("", template.Key);



			string switches = "/ni";
			string key = "Test file key";

			Dictionary<string, TemplateInfo> testTemplates = new Dictionary<string, TemplateInfo>();
			testTemplates.Add("Demo Employment Agreement.docx", new TemplateInfo()
			{
				Title = "Employment Agreement",
				DocExtension = ".docx",
				TmpType = TemplateType.WordDOCX,
				DocType = DocumentType.WordDOCX
			});
			testTemplates.Add("Sample rtf template.rtf", new TemplateInfo()
			{
				Title = "Sample rtf template",
				DocExtension = ".rtf",
				TmpType = TemplateType.WordRTF,
				DocType = DocumentType.WordRTF
			});
			testTemplates.Add("Sample wpt template.wpt", new TemplateInfo()
			{
				Title = "Sample wpt template",
				DocExtension = ".wpd",
				TmpType = TemplateType.WordPerfect,
				DocType = DocumentType.WordPerfect
			});
			testTemplates.Add("sample interview template.cmp", new TemplateInfo()
			{
				Title = "sample interview template",
				DocExtension = ".wpd",
				TmpType = TemplateType.InterviewOnly,
				DocType = DocumentType.Unknown
			});
			testTemplates.Add("sample hft template.hft", new TemplateInfo()
			{
				Title = "sample hft template",
				DocExtension = ".hfd",
				TmpType = TemplateType.HotDocsHFT,
				DocType = DocumentType.HFD
			});
			testTemplates.Add("sample pdf template.hpt", new TemplateInfo()
			{
				Title = "sample pdf template",
				DocExtension = ".pdf",
				TmpType = TemplateType.HotDocsPDF,
				DocType = DocumentType.PDF
			});
			testTemplates.Add("sample ttx template.ttx", new TemplateInfo()
			{
				Title = "sample ttx template",
				DocExtension = ".txt",
				TmpType = TemplateType.PlainText,
				DocType = DocumentType.PlainText
			});


			foreach (var t in testTemplates)
			{
                template = new DiskAccessibleTemplate(t.Key, location, switches, key);
				Assert.AreEqual(template.Title, t.Value.Title);
				string filePath = template.GetFullPath();
				Assert.IsTrue(File.Exists(filePath));
				TestTemplate(template, t.Value);
			}
		}

		private void TestTemplate(ITemplate template, TemplateInfo tInfo)
		{
			Assert.IsFalse(template.HasInterview);
			Assert.IsTrue(template.GeneratesDocument == (tInfo.TmpType != TemplateType.InterviewOnly));
			Assert.AreEqual(template.NativeDocumentType, tInfo.DocType);
			Assert.AreEqual(template.TemplateType, tInfo.TmpType);

			string locator = template.CreateLocator();
			ITemplate template2 = Template.Locate(locator);

			Assert.AreEqual(template.FileName, template2.FileName);
			Assert.AreEqual(template.Key, template2.Key);
			Assert.AreEqual(template.Switches, template2.Switches);
			Assert.AreEqual(template.Title, template2.Title);
			Assert.AreEqual(template.GeneratesDocument, template2.GeneratesDocument);
			Assert.AreEqual(template.HasInterview, template2.HasInterview);
			Assert.AreEqual(template.NativeDocumentType, template2.NativeDocumentType);
			Assert.AreEqual(template.TemplateType, template2.TemplateType);

			template.UpdateFileName();
			Assert.AreEqual(template.FileName, template2.FileName);

			Assert.AreEqual(".hfd", HotDocs.Sdk.Template.GetDocExtension(DocumentType.HFD, null));
			Assert.AreEqual(".hpd", HotDocs.Sdk.Template.GetDocExtension(DocumentType.HPD, null));
			Assert.AreEqual(".htm", HotDocs.Sdk.Template.GetDocExtension(DocumentType.HTML, null));
			Assert.AreEqual(".htm", HotDocs.Sdk.Template.GetDocExtension(DocumentType.HTMLwDataURIs, null));
			Assert.AreEqual(".htm", HotDocs.Sdk.Template.GetDocExtension(DocumentType.MHTML, null));
			Assert.AreEqual(".pdf", HotDocs.Sdk.Template.GetDocExtension(DocumentType.PDF, null));
			Assert.AreEqual(".txt", HotDocs.Sdk.Template.GetDocExtension(DocumentType.PlainText, null));
			Assert.AreEqual(".doc", HotDocs.Sdk.Template.GetDocExtension(DocumentType.WordDOC, null));
			Assert.AreEqual(".docx", HotDocs.Sdk.Template.GetDocExtension(DocumentType.WordDOCX, null));
			Assert.AreEqual(".wpd", HotDocs.Sdk.Template.GetDocExtension(DocumentType.WordPerfect, null));
			Assert.AreEqual(".rtf", HotDocs.Sdk.Template.GetDocExtension(DocumentType.WordRTF, null));
			//Assert.AreEqual(".xml", HotDocs.Sdk.Template.GetDocExtension(DocumentType.XML, null)); // TODO: Should we be testing XML?

			// Unknown doc type.
			try
			{
				Assert.AreEqual(".unk", HotDocs.Sdk.Template.GetDocExtension(DocumentType.Unknown, null));
				Assert.Fail(); // Should have thrown exception.
			}
			catch (Exception ex)
			{
				Assert.IsTrue(ex.Message.Contains("Unsupported document type."));
			}

			// Native doc type.
			try
			{
				string extension = HotDocs.Sdk.Template.GetDocExtension(DocumentType.Native, null);
				Assert.Fail(); // We should have had an exception.
			}
			catch (ArgumentNullException ex)
			{
				Assert.IsTrue(ex.Message.Contains("template")); // templae cannot be null if the type is Native.
			}

			// Native doc type.
			if (tInfo.DocType != DocumentType.Unknown)
			{
				Assert.AreEqual(tInfo.DocExtension, template.GetDocExtension());
				Assert.AreEqual(tInfo.DocExtension, HotDocs.Sdk.Template.GetDocExtension(DocumentType.Native, template));
			}


			// Set the template title.
			template.Title = "This has been done.";
			Assert.AreEqual("This has been done.", template.Title);

		}

		[TestMethod]
		public void TestTemplateLocationEquatability()
		{
			string templateDir = Path.Combine(GetSamplePortalTemplateDir(), "TestTemplates");
			string dir1 = Path.Combine(templateDir, "temp");
			string dir2 = Path.Combine(templateDir, "tĕmp");
			string dir3 = Path.Combine(templateDir, "tëmp");

			// ensure PathTemplateLocation is case insensitive and not diacritic insensitive
			var loc1a = new PathTemplateLocation(dir1);
			var loc1b = new PathTemplateLocation(dir1.ToUpper());
			var loc2a = new PathTemplateLocation(dir2);
			var loc2b = new PathTemplateLocation(dir2.ToUpper());
			var loc3 = new PathTemplateLocation(dir3);
			PathTemplateLocation loc4 = null;
			PathTemplateLocation loc5 = null;

			// Try to get a TemplateLocation from an invalid locator.
			ITemplateAnywhere loc = TemplateLocation.Locate(Util.EncryptString("abcdefg"));
			Assert.IsNull(loc);

			try
			{
				loc = ITemplateAnywhere.Locate(Util.EncryptString("abc|defg"));
				Assert.Fail();
			}
			catch (Exception ex)
			{
				Assert.IsTrue(ex.Message.Contains("The type abc is not registered"));
			}

			// ensure PathTemplateLocation is case insensitive
			Assert.AreNotEqual(loc1a, "");
			Assert.AreEqual(loc1a, loc1b);
			Assert.AreEqual(loc1a.GetHashCode(), loc1b.GetHashCode());
			Assert.AreEqual(loc2a, loc2b);
			Assert.AreEqual(loc2a.GetHashCode(), loc2b.GetHashCode());
			// ensure PathTemplateLocation is not unicode/diacritic insensitive
			Assert.AreNotEqual(loc1a, loc2a);
			Assert.AreNotEqual(loc1a.GetHashCode(), loc2a.GetHashCode());
			Assert.AreNotEqual(loc1a, loc3);
			Assert.AreNotEqual(loc1a.GetHashCode(), loc3.GetHashCode());
			Assert.AreNotEqual(loc2b, loc3);
			Assert.AreNotEqual(loc2b.GetHashCode(), loc3.GetHashCode());
			// ensure we can tell the difference between nulls & non
			Assert.AreNotEqual(loc1a, loc4);
			Assert.AreEqual(loc4, loc5); // null template locations are equal to each other, I suppose

			// ensure hashing is working pretty much like we expect it to
			var set = new HashSet<TemplateLocation>();
			set.Add(loc1a);
			Assert.IsTrue(set.Contains(loc1a));
			Assert.IsTrue(set.Contains(loc1b));
			Assert.IsFalse(set.Contains(loc2a));
			Assert.IsFalse(set.Contains(loc2b));
			Assert.IsFalse(set.Contains(loc3));
			Assert.IsFalse(set.Contains(loc4));
			Assert.IsFalse(set.Contains(loc5));
			set.Add(loc3);
			Assert.IsTrue(set.Contains(loc1a));
			Assert.IsTrue(set.Contains(loc1b));
			Assert.IsFalse(set.Contains(loc2a));
			Assert.IsFalse(set.Contains(loc2b));
			Assert.IsTrue(set.Contains(loc3));
			Assert.IsFalse(set.Contains(loc4));
			Assert.IsFalse(set.Contains(loc5));
			set.Add(loc2b);
			Assert.IsTrue(set.Contains(loc1a));
			Assert.IsTrue(set.Contains(loc1b));
			Assert.IsTrue(set.Contains(loc2a));
			Assert.IsTrue(set.Contains(loc2b));
			Assert.IsFalse(set.Contains(loc4));
			Assert.IsFalse(set.Contains(loc5));
			set.Add(loc4);
			Assert.IsTrue(set.Contains(loc4));
			Assert.IsTrue(set.Contains(loc5));

			// now test PackagePathTemplateLocation too (& in combination)
			var pkgloc1a = new PackagePathTemplateLocation(@"2f98a9f3-d106-44a8-ba16-e01d8cd65cbd", Path.Combine(dir1, "temp.pkg"));
			var pkgloc1b = new PackagePathTemplateLocation(@"2f98a9f3-d106-44a8-ba16-e01d8cd65cbd", Path.Combine(dir1, "TEMP.pkg"));
			var pkgloc2a = new PackagePathTemplateLocation(@"2f98a9f3-d106-44a8-ba16-e01d8cd65cbD", Path.Combine(dir1, "TEMP.pkg"));
			var pkgloc3a = new PackagePathTemplateLocation(@"l'identité unique", Path.Combine(dir3, "tëmp.pkg"));
			var pkgloc3b = new PackagePathTemplateLocation(@"l'identité unique", Path.Combine(dir3, "TËMP.pkg"));
			var pkgloc4a = new PackagePathTemplateLocation(@"l'identité unique", Path.Combine(dir2, "tĕmp.pkg"));
			var pkgloc5a = new PackagePathTemplateLocation(@"l'identite unique", Path.Combine(dir2, "TĔMP.pkg"));

			// ensure package ID is case sensitive & package path is case insensitive
			Assert.AreEqual(pkgloc1a, pkgloc1b);
			Assert.AreEqual(pkgloc1a.GetHashCode(), pkgloc1b.GetHashCode());
			Assert.AreNotEqual(pkgloc1a, pkgloc2a);
			Assert.AreNotEqual(pkgloc1a.GetHashCode(), pkgloc2a.GetHashCode());
			Assert.AreNotEqual(pkgloc1b, pkgloc2a);
			Assert.AreNotEqual(pkgloc1b.GetHashCode(), pkgloc2a.GetHashCode());
			// ensure package ID & path are diacritic sensitive
			Assert.AreEqual(pkgloc3a, pkgloc3b);
			Assert.AreEqual(pkgloc3a.GetHashCode(), pkgloc3b.GetHashCode());
			Assert.AreNotEqual(pkgloc3a, pkgloc4a);
			Assert.AreNotEqual(pkgloc3a.GetHashCode(), pkgloc4a.GetHashCode());
			Assert.AreNotEqual(pkgloc3b, pkgloc5a);
			Assert.AreNotEqual(pkgloc3b.GetHashCode(), pkgloc5a.GetHashCode());

			// TODO: check comparisons/equality of different types of TemplateLocations with each other.
			// (should always be not equal, hopefully even if null?) 
		}

		#region Utility methods

		private static PackagePathTemplateLocation CreatePackagePathLocation(string packageID)
		{
			//Note that in this sample portal, the package ID is used to construct the package file name, but this does not need to be the case.
			string templateDirectory = Path.Combine(GetSamplePortalTemplateDir(), "TestTemplates");
			string packagePath = Path.Combine(templateDirectory, packageID + ".pkg");

			if (!File.Exists(packagePath))
				throw new Exception("The template does not exist.");
			return new HotDocs.Sdk.PackagePathTemplateLocation(packageID, packagePath);
		}

		private static string GetSamplePortalTemplateDir()
		{
			string samplePortalRoot = GetSamplePortalRootedPath();
			string filesDir = Path.Combine(samplePortalRoot, "Files");
			return Path.Combine(filesDir, "Templates");
		}

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

		#endregion // Utility methods

	}
}
