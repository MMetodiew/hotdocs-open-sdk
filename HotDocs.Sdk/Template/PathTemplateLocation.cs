/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.IO;

namespace HotDocs.Sdk
{
	/// <summary>
	/// <c>PathTemplateLocation</c> is a <c>TemplateLocation</c> class that represents the directory path
	/// for a template that simply resides as a file in the file system. The template does not reside in
	/// a package or database, for example.
	/// </summary>
    public class PathTemplateLocation : TemplateLocation
	{
		private string _templateDir;//The directory where the template resides.

		/// <summary>
		/// Construct a <c>PathTemplateLocation</c> representing the path of the directory containing the
		/// template file.
		/// </summary>
		/// <param name="templateDir">The path of the directory containing the template file.</param>
		public PathTemplateLocation(string templateDir)
		{
			if (templateDir == null)
				throw new ArgumentNullException();

			if (!Directory.Exists(templateDir))
				throw new Exception("The folder \"" + templateDir + "\" does not exist.");
			_templateDir = templateDir;
		}
		/// <summary>
		/// Returns a new duplicate instance of this object. Overrides <see cref="TemplateLocation.Duplicate"/>.
		/// </summary>
		/// <returns></returns>
		public override ITemplateLocation Duplicate()
		{
			return new PathTemplateLocation(_templateDir);
		}

		/// <summary>
		/// Overrides Object.GetHashCode().
		/// </summary>
		/// <returns>A suitable hash code for this PathTemplateLocation.</returns>
		public override int GetHashCode()
		{
			return _templateDir.ToLower().GetHashCode();

			// NOTE: Object.Equals is overridden in the base class, and therefore
			// does not need not be overridden here.
		}

		#region IEquatable<TemplateLocation> Members

		/// <summary>
		/// Implements IEquatable&lt;TemplateLocation&gt;.Equals().
		/// </summary>
		/// <param name="other">The other template location to compare equality.</param>
		/// <returns>A value indicating whether or not the two locations are the same.</returns>
		public override bool Equals(TemplateLocation other)
		{
			var otherPathLoc = other as PathTemplateLocation;
			return (otherPathLoc != null)
				&& string.Equals(_templateDir, otherPathLoc._templateDir, StringComparison.OrdinalIgnoreCase);
		}

		#endregion
		/// <summary>
		/// Returns a stream for a file in the template's directory. Overrides <see cref="TemplateLocation.GetFile"/>.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <returns></returns>
		public override Stream GetFile(string fileName)
		{
			string filePath = Path.Combine(GetTemplateDirectory(), fileName);
			//Note that if the file path does not exist, the FileStream constructor will throw an exception.
			return new FileStream(filePath, FileMode.Open, FileAccess.Read);
		}
		/// <summary>
		/// Returns the template's directory. Overrides <see cref="TemplateLocation.GetTemplateDirectory"/>.
		/// </summary>
		/// <returns></returns>
		public string GetTemplateDirectory()
		{
			return _templateDir;
		}
		/// <summary>
		/// Serialize the content of this object. Overrides <see cref="TemplateLocation.SerializeContent"/>.
		/// </summary>
		/// <returns></returns>
		protected override string SerializeContent()
		{
			return _templateDir;
		}
		/// <summary>
		/// Set the fields and properties of this object by deserializing a string returned from SerializeContent.
		/// Overrides <see cref="TemplateLocation.DeserializeContent"/>.
		/// </summary>
		/// <param name="content">A content string returned from SerializeContent.</param>
		public override void DeserializeContent(string content)
		{
			if (content == null || content == "")
				throw new Exception("Invalid content parameter.");
			if (!Directory.Exists(content))
				throw new Exception("The folder \"" + content + "\" does not exist.");

			_templateDir = content;
		}
	}
}
