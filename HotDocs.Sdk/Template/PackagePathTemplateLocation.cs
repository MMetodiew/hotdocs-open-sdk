/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.IO;

namespace HotDocs.Sdk
{
	/// <summary>
	/// <c>PackagePathTemplateLocation</c> is a <c>PackageTemplateLocation</c> that expects a package
	/// to exist on disk. Furthermore, the package content is extracted to a subfolder of the package folder.
	/// The subfolder's name consists of the package ID followed by a ".dir" extension. To extract package
	/// content elsewhere, derive a different PackageTemplateLocation class.
	/// </summary>
    public class PackagePathTemplateLocation : PackageTemplateLocation
	{
		/// <summary>
		/// Construct a template location for a specific package in the file system.
		/// </summary>
		/// <param name="packageID">The ID of the package.</param>
		/// <param name="packagePath">The full path of the package file.</param>
		public PackagePathTemplateLocation(string packageID, string packagePath) : base(packageID)
		{
			if (!File.Exists(packagePath))
				throw new Exception("The package does not exist.");

			_templateDir = Path.Combine(Path.GetDirectoryName(packagePath), Path.GetFileNameWithoutExtension(packagePath) + ".dir");
			PackagePath = packagePath;
		}

		#region PackageTemplateLocation Implementation

		/// <summary>
		/// Returns a new duplicate instance of this object. Overrides <see cref="TemplateLocation.Duplicate"/>.
		/// </summary>
		/// <returns></returns>
		public override ITemplateLocation Duplicate()
		{
			PackagePathTemplateLocation location = new PackagePathTemplateLocation(PackageID, PackagePath);
			location._templateDir = _templateDir;
			return location;
		}
		/// <summary>
		/// Returns a stream for a file at this template location. Overrides TemplateLocation.GetFile.
		/// </summary>
		/// <param name="fileName">The file name for the file requested.</param>
		/// <returns></returns>
		public override Stream GetFile(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException();

			string filePath = Path.Combine(GetTemplateDirectory(), fileName);
			return new FileStream(filePath, FileMode.Open, FileAccess.Read);
		}
		/// <summary>
		/// Returns the folder that the package content was extracted to. Overrides <see cref="TemplateLocation.GetTemplateDirectory"/>.
		/// </summary>
		/// <returns></returns>
		public string GetTemplateDirectory()
		{
			ExtractPackageFiles();
			return _templateDir;
		}
		/// <summary>
		/// Returns a read-only stream for the package file.
		/// </summary>
		/// <returns></returns>
		public override Stream GetPackageStream()
		{
			return new FileStream(PackagePath, FileMode.Open, FileAccess.Read);
		}
		/// <summary>
		/// Returns a <see cref="TemplatePackageManifest"/> for the package.
		/// </summary>
		/// <returns></returns>
		public override TemplatePackageManifest GetPackageManifest()
		{
			HotDocs.Sdk.TemplatePackage pkg = new HotDocs.Sdk.TemplatePackage();
			pkg.Open(PackagePath);
			return pkg.Manifest;
		}
		/// <summary>
		/// Overrides <see cref="TemplateLocation.SerializeContent"/>.
		/// </summary>
		/// <returns></returns>
		protected override string SerializeContent()
		{
			return PackageID + "|" + PackagePath + "|" + GetTemplateDirectory();
		}
		/// <summary>
		/// Overrides <see cref="TemplateLocation.DeserializeContent"/>.
		/// </summary>
		/// <returns></returns>
		public override void DeserializeContent(string content)
		{
			if (content == null)
				throw new ArgumentNullException();

			string[] tokens = content.Split(new char[] {'|'});
			if (tokens.Length != 3)
				throw new Exception("Invalid template location.");

			PackageID = tokens[0];
			PackagePath = tokens[1];
			_templateDir = tokens[2];
		}

		#endregion

		/// <summary>
		/// Overrides Object.GetHashCode.
		/// </summary>
		/// <returns>A suitable hash code for this PackagePathTemplateLocation.</returns>
		public override int GetHashCode()
		{
			const int prime = 397;
			int result = PackagePath.ToLower().GetHashCode(); // package path must be case-insensitive
			result = (result * prime) ^ PackageID.GetHashCode(); // combine the hashes
			return result;

			// NOTE: Object.Equals is overridden in the base class, and therefore
			// does not need not be overridden here.
		}

		/// <summary>
		/// Overrides Object.Equals. This should be present wherever GetHashCode is overridden 
		/// and vice versa.
		/// </summary>
		/// <param name="obj">The object to compare to 'this'</param>
		/// <returns>boolean true or false, indicating whether or not the two objects are equal</returns>
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		#region IEquatable<TemplateLocation> Members

		/// <summary>
		/// Implements IEquatable&lt;TemplateLocation&gt;.Equals().
		/// </summary>
		/// <param name="other">Other location to compare to test equality.</param>
		/// <returns>A value indicating whether the two locations are equal or not.</returns>
		public override bool Equals(TemplateLocation other)
		{
			var otherPackagePathLoc = other as PackagePathTemplateLocation;
			return (otherPackagePathLoc != null)
				&& string.Equals(PackageID, otherPackagePathLoc.PackageID, StringComparison.Ordinal)
				&& string.Equals(_templateDir, otherPackagePathLoc._templateDir, StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		/// <summary>
		/// Extract the create the package content folder and extract the package content to it.
		/// </summary>
		/// <param name="force">If true, the directory is overwritten. Otherwise, the directory
		/// is created and files extracted only if the directory does not exist. If force is true
		/// all previously existing directory content is destroyed.</param>
		public virtual void ExtractPackageFiles(bool force=false)
		{
			if (force)
				CleanPackageFiles();
			if (!Directory.Exists(_templateDir))
			{
				Directory.CreateDirectory(_templateDir);
				TemplatePackage.ExtractAllFiles(PackagePath, "", _templateDir);
			}
		}
		/// <summary>
		/// Destroy the package template folder and all of its contents.
		/// </summary>
		public virtual void CleanPackageFiles()
		{
			if (Directory.Exists(_templateDir))
				Directory.Delete(_templateDir, true);
		}
		/// <summary>
		/// Returns the path to the package.
		/// </summary>
		public string PackagePath { get; protected set; }
		private string _templateDir = null;
	}
}
