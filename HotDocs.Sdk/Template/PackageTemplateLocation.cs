/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.IO;

namespace HotDocs.Sdk
{
	/// <summary>
	/// <c>PackageTemplateLocation</c> is the base class representing all template locations where
	/// the template resides in a package. This class provides access to the package ID, the package
	/// manifest, and the package itself.
	/// </summary>
	public abstract class PackageTemplateLocation : TemplateLocation
	{
		/// <summary>
		/// Construct a <c>PackageTemplateLocation</c> object representing a package with the specified ID.
		/// </summary>
		/// <param name="packageID">The ID of the package.</param>
		public PackageTemplateLocation(string packageID)
		{
			if (packageID == null)
				throw new ArgumentNullException();

			if (packageID == String.Empty)
				throw new ArgumentException();

			PackageID = packageID;
		}
		/// <summary>
		/// Returns a stream for the package.
		/// </summary>
		/// <returns></returns>
		public abstract Stream GetPackageStream();
		/// <summary>
		/// Returns a manifest for the package.
		/// </summary>
		/// <returns></returns>
		public abstract TemplatePackageManifest GetPackageManifest();
		/// <summary>
		/// Returns the package ID.
		/// </summary>
		public string PackageID { get; protected set; }
	}
}
