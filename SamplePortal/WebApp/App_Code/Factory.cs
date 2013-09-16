﻿/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.IO;

namespace SamplePortal
{
	/// <summary>
	/// The <c>Factory</c> class manages the allocation of objects whose lifetime is that of the user session.
	/// </summary>
	public class Factory
	{
		/// <summary>
		/// Returns the WorkSession for the user session.
		/// </summary>
		/// <param name="session">The user session.</param>
		/// <returns></returns>
		public static HotDocs.Sdk.Server.WorkSession GetWorkSession(System.Web.SessionState.HttpSessionState session)
		{
			return (HotDocs.Sdk.Server.WorkSession)session["HdSession"];
		}
		/// <summary>
		/// Creates and returns a new work session for a specific package.
		/// </summary>
		/// <param name="session">The user session.</param>
		/// <param name="packageID">The ID of the package to create a work session for.</param>
		/// <returns></returns>
		public static HotDocs.Sdk.Server.WorkSession CreateWorkSession(System.Web.SessionState.HttpSessionState session, string packageID)
		{
			HotDocs.Sdk.Template template = OpenTemplate(packageID);
			HotDocs.Sdk.Server.IServices service = GetServices();
			HotDocs.Sdk.Server.WorkSession workSession = new HotDocs.Sdk.Server.WorkSession(service, template);
			session["HdSession"] = workSession;
			return workSession;
		}
		/// <summary>
		/// Removes the current work session, if there is one.
		/// </summary>
		/// <param name="session"></param>
		public static void RetireWorkSession(System.Web.SessionState.HttpSessionState session)
		{
			HotDocs.Sdk.Server.WorkSession workSession = (HotDocs.Sdk.Server.WorkSession)session["HdSession"];
			if (workSession != null)
			{
				session["HdSession"] = null;
			}
		}
		/// <summary>
		/// Returns the <c>AssembledDocsCache</c> for the user session.
		/// </summary>
		/// <param name="session">The user session</param>
		/// <returns></returns>
		public static AssembledDocsCache GetAssembledDocsCache(System.Web.SessionState.HttpSessionState session)
		{
			AssembledDocsCache cache = (AssembledDocsCache)session["AssembledDocsCache"];
			if (cache == null)
			{
				cache = new AssembledDocsCache(Settings.DocPath);
				session["AssembledDocsCache"] = cache;
			}
			return cache;
		}
		/// <summary>
		/// Removes and disposes the current <c>AssembledDocsCache</c>, if there is one.
		/// </summary>
		/// <param name="session"></param>
		public static void RetireAssembledDocsCache(System.Web.SessionState.HttpSessionState session)
		{
			AssembledDocsCache cache = (AssembledDocsCache)session["AssembledDocsCache"];
			if (cache != null)
			{
				session["AssembledDocsCache"] = null;
				cache.Dispose();
			}
		}
		/// <summary>
		/// Returns a new IServices object for the HdsRoute specified in the web.config file.
		/// </summary>
		/// <returns></returns>
		static public HotDocs.Sdk.Server.IServices GetServices()
		{
			switch (Settings.HdsRoute)
			{
				case HotDocs.Sdk.Server.HdProtocol.Cloud:
					return new HotDocs.Sdk.Server.Cloud.Services(Settings.SubscriberID, Settings.SigningKey);
				case HotDocs.Sdk.Server.HdProtocol.Local:
					return new HotDocs.Sdk.Server.Local.Services(Settings.TempPath);
				case HotDocs.Sdk.Server.HdProtocol.WebService:
					return new HotDocs.Sdk.Server.WebService.Services(Settings.WebServiceEndPoint, Settings.TemplatePath);
			}
			throw new Exception("Unsupported protocol.");
		}

		private static HotDocs.Sdk.Template OpenTemplate(string packageId)
		{
			//Note that in this sample portal, the package ID is used to construct the package file name, but this does not need to be the case.
			string packagePath = PackageCache.GetLocalPackagePath(packageId);
			if (!File.Exists(packagePath))
				throw new Exception("The template does not exist.");
			HotDocs.Sdk.PackagePathTemplateLocation location = new HotDocs.Sdk.PackagePathTemplateLocation(packageId, packagePath);

			return new HotDocs.Sdk.Template(location);
		}
	}
}
