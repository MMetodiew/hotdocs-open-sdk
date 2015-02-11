using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using HotDocs.Sdk;

public class OnDiskTemplateLocationSerializer
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