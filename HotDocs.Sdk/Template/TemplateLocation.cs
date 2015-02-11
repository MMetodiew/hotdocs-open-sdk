/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using HotDocs.Sdk;

namespace HotDocs.Sdk
{
    /// <summary>
    /// <para><c>TemplateLocation</c> is an abstract class serves as the base class for all template location objects.
    /// Template location objects are the means for designating the location of a file for
    /// a Template object. For example, a template may reside in a file system folder, a document
    /// management system (DMS), another type of database, a template package, etc. The host
    /// application must either choose one of the concrete <c>TemplateLocation</c> classes implemented
    /// in the SDK (e.g. <c>PathTemplateLocation</c> or <c>PackagePathTemplateLocation</c>) or implement a 
    /// custom template location.</para>
    /// 
    /// <para>The <c>TemplateLocation</c> class also serves as a dependency injection container for any
    /// <c>TemplateLocation</c> classes used by the host application. <c>TemplateLocation</c> class registration
    /// is done at application startup time. To register a <c>TemplateLocation</c> class, call
    /// <c>TemplateLocation.RegisterLocation</c>.</para>
    /// </summary>
    public abstract class TemplateLocation : IEquatable<TemplateLocation>, ITemplateLocation
    {

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns>A new <c>TemplateLocation</c> object, which is a copy of the original.</returns>
        public abstract ITemplateLocation Duplicate();

        /// <summary>
        /// Overrides Object.Equals. Calls into IEquatable&lt;TemplateLocation&gt;.Equals() to
        /// determine if instances of derived types are equal.
        /// </summary>
        /// <param name="obj">An object to use in the equality comparison.</param>
        /// <returns>A value indicating whether or not the two locations are equal.</returns>
        public override bool Equals(object obj)
        {
            // this override is effective for all derived classes, because they must all
            // implement IEquatable<TemplateLocation>, which this calls.
            return (obj != null) && (obj is TemplateLocation) && Equals((TemplateLocation)obj);

        }

        /// <summary>
        /// <c>GetHashCode</c> is needed wherever Equals(object) is defined.
        /// </summary>
        /// <returns>An integer hash code for the object.</returns>
        public abstract override int GetHashCode();

        #region IEquatable<TemplateLocation> Members

        /// <summary>
        /// Implements IEquatable&lt;TemplateLocation&gt;. Used to determine equality/equivalency
        /// between TemplateLocations.
        /// </summary>
        /// <param name="other">The other location to compare with when testing equality.</param>
        /// <returns>A value indicating whether or not the two locations are equal.</returns>
        public abstract bool Equals(TemplateLocation other);

        #endregion
        /// <summary>
        /// Returns a Stream for a file living at the same location as the template.
        /// </summary>
        /// <param name="fileName">The name of the file (without any path information).</param>
        /// <returns>A Stream containing the file.</returns>
        public abstract System.IO.Stream GetFile(string fileName);

        /// <summary>
        /// Return a string that can be used to initialize a new, uninitialized <c>TemplateLocation</c>
        /// object of the same type as this one. Deserialize the content with DeserializeContent.
        /// </summary>
        /// <returns></returns>
        protected abstract string SerializeContent();
        /// <summary>
        /// Initialize a <c>TemplateLocation</c> object from a string created by SerializeContent.
        /// </summary>
        /// <param name="content">A string representing a serialized <c>TemplateLocation</c>.</param>
        public abstract void DeserializeContent(string content);
        /// <summary>
        /// Return an encrypted locator string.
        /// </summary>
        /// <returns></returns>
        public string CreateLocator()
        {
            string stamp = GetType().ToString();
            return Util.EncryptString(stamp + "|" + SerializeContent());
        }
        /// <summary>
        /// Get an updated file name for a template. Return true if the file name needed updating.
        /// If this method returns true, then fileName contains the updated file name. This method
        /// should be overridden for file storage systems where the template is stored in a database
        /// such that a file name is created on demand.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="fileName">The updated file name for the specified template.</param>
        /// <returns>True if the file name was updated, or false otherwise.</returns>
        public virtual bool GetUpdatedFileName(ITemplate template, out string fileName)
        {
            fileName = "";
            return false;
        }
    }
}

public class TemplateLocationSerializer
{
    /// <summary>
    /// Create a <c>TemplateLocation</c> object from an encrypted locator string returned by <c>TemplateLocation.CreateLocator</c>.
    /// </summary>
    /// <param name="encodedLocator">An encoded template locator.</param>
    /// <returns>A <c>TemplateLocation</c> object.</returns>
    public static TemplateLocation Locate(string encodedLocator)
    {
        string locator = Util.DecryptString(encodedLocator);
        int stampLen = locator.IndexOf('|');
        if (stampLen == -1)
            return null;
        string stamp = locator.Substring(0, stampLen);
        string content = locator.Substring(stampLen + 1, locator.Length - (stampLen + 1));

        foreach (Type type in _registeredTypes)
        {
            if (stamp == type.ToString())
            {
                object obj = FormatterServices.GetSafeUninitializedObject(type);
                TemplateLocation templateLocation = obj as TemplateLocation;
                if (templateLocation == null)
                    throw new Exception("Invalid template location.");

                try
                {
                    templateLocation.DeserializeContent(content);
                }
                catch (Exception)
                {
                    throw new Exception("Invalid template location.");
                }
                return templateLocation;
            }
        }
        throw new Exception("The type " + stamp + " is not registered as a TemplateLocation. Call TemplateLocation.RegisterLocation at application start-up.");
    }

    /// <summary>
    /// Call this method to register a type derived from TemplateLocation. All concrete TemplateLocation derivatives must
    /// be registered before use in order for Template.Locate and TemplateLocation.Locate reconstitute
    /// template location information. This method should only be called at application start-up.
    /// </summary>
    /// <param name="type">The type derived from <c>TemplateLocation</c> to register.</param>
    public static void RegisterLocation(Type type)
    {
        //Validate the type.
        Type baseType = type.BaseType;
        while (baseType != null && baseType != typeof(TemplateLocation))
            baseType = baseType.BaseType;
        if (baseType != typeof(TemplateLocation))
            throw new Exception("The registered location must be of type TemplateLocation.");

        _registeredTypes.Enqueue(type);
    }

    //We use the ConcurrentQueue type here because multiple threads may be accessing the queue at
    // once. However, since this is a read-only queue (we only write to it at application startup),
    // no further thread synchronization is necessary. A queue is used because the type is available.
    // If a ConcurrentList<> type existed, that would suffice since we don't need FIFO functionality.
    private static ConcurrentQueue<Type> _registeredTypes = new ConcurrentQueue<Type>();
}