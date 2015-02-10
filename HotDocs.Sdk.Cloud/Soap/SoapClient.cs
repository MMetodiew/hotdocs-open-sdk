/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net;
using System.IO;
using HotDocs.Sdk.Server;
using HotDocs.Sdk.Server.Contracts;

namespace HotDocs.Sdk.Cloud
{
	/// <summary>
	/// The default Core Services client.
	/// This implementation uses the Core Services SOAP web service.
	/// </summary>
	public class SoapClient : ClientBase, IDisposable
	{
		#region Private fields
		private Proxy _proxy;
		#endregion

		#region Constructors
		/// <summary>
		/// Constructs a Client object using 
		/// </summary>
		/// <param name="subscriberId">Subscriber ID provided by HotDocs</param>
		/// <param name="signingKey">Signing key provided by HotDocs</param>
		/// <param name="hostAddress">Optional service host address -- overrides the config file</param>
		/// <param name="proxyServerAddress">Optional proxy server address -- overrides the config file</param>
		public SoapClient(
			string subscriberId,
			string signingKey,
			string hostAddress = null,
			string proxyServerAddress = null)
			: base(subscriberId, signingKey, hostAddress, "Core.svc", proxyServerAddress)
		{
			try
			{
				// The consumer has the option of configuring this client in their config file.
				// If there is no appropriate endpoint element in the config file, we catch the
				// resulting exception and create the proxy using hard-coded settings.
				_proxy = new Proxy();

				if (!string.IsNullOrEmpty(hostAddress))
				{
					_proxy.Endpoint.Address = new EndpointAddress(EndpointAddress);
				}
				else
				{
					EndpointAddress = _proxy.Endpoint.Address.Uri.AbsolutePath;
				}
			}
			catch (InvalidOperationException) // No configuration provided by the consumer
			{
				_proxy = new Proxy(GetDefaultBinding(), new EndpointAddress(EndpointAddress));
			}

			if (!string.IsNullOrEmpty(proxyServerAddress))
			{
				// Find the HttpTransportBindingElement binding element and set the proxy properties
				var binding = (CustomBinding)_proxy.Endpoint.Binding;
				var transport = (HttpTransportBindingElement)binding.Elements.Single(e => e is HttpTransportBindingElement);
				transport.UseDefaultWebProxy = false;
				transport.ProxyAddress = new Uri(proxyServerAddress);
				transport.BypassProxyOnLocal = false;
			}

			SetTcpKeepAlive();
#if DEBUG
			// For debug builds, allow invalid server certificates.
			ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif
		}
		#endregion

		#region Protected internal implementations of abstract methods from the base class

		/// <summary>
		/// 
		/// </summary>
		/// <param name="template">The template to assemble.</param>
		/// <param name="answers">The answers to use when assembling the document.</param>
		/// <param name="settings">The settings to use when assembling the document.</param>
		/// <param name="billingRef">This parameter lets you specify information that will be included in usage logs for this call. For example, you can use a string to uniquely identify the end user that initiated the request and/or the context in which the call was made. When you review usage logs, you can then see which end users initiated each request. That information could then be used to pass costs on to those end users if desired.</param>
		/// <param name="uploadPackage">Indicates if the package should be uploaded (forcefully) or not. This should only be true if the package does not already exist in the Cloud Services cache.</param>
		/// <returns></returns>
		protected internal override AssemblyResult AssembleDocumentImpl(
			ITemplate template,
			string answers,
			AssembleDocumentSettings settings,
			string billingRef,
			bool uploadPackage)
		{
			var timestamp = DateTime.UtcNow;
			if (!(template.Location is PackageTemplateLocation))
				throw new Exception("HotDocs Cloud Services requires the use of template packages. Please use a PackageTemplateLocation derivative.");
			PackageTemplateLocation packageTemplateLocation = (PackageTemplateLocation)template.Location;

			// Determine the output format to use (e.g., translate settings.Type to Contracts.InterviewFormat)
			OutputFormat outputFormat;
			switch (settings.Format)
			{
				case DocumentType.HFD:
					outputFormat = OutputFormat.HFD;
					break;
				case DocumentType.HPD:
					outputFormat = OutputFormat.HPD;
					break;
				case DocumentType.HTML:
					outputFormat = OutputFormat.HTML;
					break;
				case DocumentType.HTMLwDataURIs:
					outputFormat = OutputFormat.HTMLwDataURIs;
					break;
				case DocumentType.MHTML:
					outputFormat = OutputFormat.MHTML;
					break;
				case DocumentType.Native:
					outputFormat = OutputFormat.Native;
					break;
				case DocumentType.PDF:
					outputFormat = OutputFormat.PDF;
					break;
				case DocumentType.PlainText:
					outputFormat = OutputFormat.PlainText;
					break;
				case DocumentType.WordDOC:
					outputFormat = OutputFormat.DOCX;
					break;
				case DocumentType.WordDOCX:
					outputFormat = OutputFormat.DOCX;
					break;
				case DocumentType.WordPerfect:
					outputFormat = OutputFormat.WPD;
					break;
				case DocumentType.WordRTF:
					outputFormat = OutputFormat.RTF;
					break;
				default:
					outputFormat = OutputFormat.None;
					break;
			}

			// Always include the Answers output
			outputFormat |= OutputFormat.Answers;

			string hmac = HMAC.CalculateHMAC(
				SigningKey,
				timestamp,
				SubscriberId,
				packageTemplateLocation.PackageID,
				template.FileName,
				uploadPackage,
				billingRef,
				outputFormat,
				settings.Settings);

			return _proxy.AssembleDocument(
				SubscriberId,
				packageTemplateLocation.PackageID,
				template.FileName,
				GetBinaryObjectArrayFromString(answers),
				outputFormat,
				settings.OutputOptions,
				settings.Settings,
				billingRef,
				timestamp,
				GetPackageIfNeeded(packageTemplateLocation, uploadPackage),
				hmac);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="template">The template for which the interview is being requested.</param>
		/// <param name="answers">The answers to use with the interview.</param>
		/// <param name="settings">The settings to use when requesting the interview.</param>
		/// <param name="billingRef">This parameter lets you specify information that will be included in usage logs for this call. For example, you can use a string to uniquely identify the end user that initiated the request and/or the context in which the call was made. When you review usage logs, you can then see which end users initiated each request. That information could then be used to pass costs on to those end users if desired.</param>
		/// <param name="uploadPackage">Indicates if the package should be uploaded (forcefully) or not. This should only be true if the package does not already exist in the Cloud Services cache.</param>
		/// <returns></returns>
		protected internal override BinaryObject[] GetInterviewImpl(
			ITemplate template,
			string answers,
			InterviewSettings settings,
			string billingRef,
			bool uploadPackage)
		{
			var timestamp = DateTime.UtcNow;
			if (!(template.Location is PackageTemplateLocation))
				throw new Exception("HotDocs Cloud Services requires the use of template packages. Please use a PackageTemplateLocation derivative.");
			PackageTemplateLocation packageTemplateLocation = (PackageTemplateLocation)template.Location;

			string interviewImageUrl = string.Empty;
			if (settings != null)
			{
				settings.Settings.TryGetValue("TempInterviewUrl", out interviewImageUrl);
			}
			else
			{
				settings = new InterviewSettings();
			}

			Dictionary<string, string> settingsDict = new Dictionary<string, string>(settings.Settings);

			// Workaround for bug in server that does not honor the Disable settings, so we have to just clear the url instead.
			// To do this, we make a copy of the settings that were given to us, modify them, and then use the modified version
			// in the call to Cloud Services.
			// TODO: After TFS #5598 is fixed, we can remove this workaround.
			// Note: TFS #5598 has been fixed, but the cloud implememntation does not pass those values through
			// to the cloud implmementation of HotDocs Server, so for now we leave these next 4 lines here that provide
			// equivalent functionality:
			if (settings.DisableDocumentPreview)
				settingsDict.Remove("DocPreviewUrl");
			if (settings.DisableSaveAnswers)
				settingsDict.Remove("SaveAnswersPageUrl");

			string hmac = HMAC.CalculateHMAC(
				SigningKey,
				timestamp,
				SubscriberId,
				packageTemplateLocation.PackageID,
				template.FileName,
				uploadPackage,
				billingRef,
				settings.Format,
				interviewImageUrl,
				settingsDict);

			return _proxy.GetInterview(
				SubscriberId,
				packageTemplateLocation.PackageID,
				template.FileName,
				GetBinaryObjectArrayFromString(answers),
				settings.Format,
				settings.MarkedVariables.ToArray<string>(),
				interviewImageUrl,
				settingsDict,
				billingRef,
				timestamp,
				GetPackageIfNeeded(packageTemplateLocation, uploadPackage),
				hmac);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="template">The template for which component information is being requested.</param>
		/// <param name="includeDialogs">Indicates whether or not information about dialogs should be included.</param>
		/// <param name="billingRef">This parameter lets you specify information that will be included in usage logs for this call. For example, you can use a string to uniquely identify the end user that initiated the request and/or the context in which the call was made. When you review usage logs, you can then see which end users initiated each request. That information could then be used to pass costs on to those end users if desired.</param>
		/// <param name="uploadPackage">Indicates if the package should be uploaded (forcefully) or not. This should only be true if the package does not already exist in the Cloud Services cache.</param>
		/// <returns></returns>
		protected internal override ComponentInfo GetComponentInfoImpl(
			ITemplate template,
			bool includeDialogs,
			string billingRef,
			bool uploadPackage)
		{
			var timestamp = DateTime.UtcNow;
			if (!(template.Location is PackageTemplateLocation))
				throw new Exception("HotDocs Cloud Services requires the use of template packages. Please use a PackageTemplateLocation derivative.");
			PackageTemplateLocation packageTemplateLocation = (PackageTemplateLocation)template.Location;

			string hmac = HMAC.CalculateHMAC(
				SigningKey,
				timestamp,
				SubscriberId,
				packageTemplateLocation.PackageID,
				template.FileName,
				uploadPackage,
				billingRef,
				includeDialogs);

			return _proxy.GetComponentInfo(
				SubscriberId,
				packageTemplateLocation.PackageID,
				template.FileName,
				includeDialogs,
				billingRef,
				timestamp,
				GetPackageIfNeeded(packageTemplateLocation, uploadPackage),
				hmac);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="answers">An array of answers as binary objects, which will be overlayed successively to produce a single <c>BinaryObject</c>.</param>
		/// <param name="billingRef">This parameter lets you specify information that will be included in usage logs for this call. For example, you can use a string to uniquely identify the end user that initiated the request and/or the context in which the call was made. When you review usage logs, you can then see which end users initiated each request. That information could then be used to pass costs on to those end users if desired.</param>
		/// <returns></returns>
		protected internal override BinaryObject GetAnswersImpl(BinaryObject[] answers, string billingRef)
		{
			DateTime timestamp = DateTime.UtcNow;

			string hmac = HMAC.CalculateHMAC(
				SigningKey,
				timestamp,
				SubscriberId,
				billingRef);

			return _proxy.GetAnswers(
				SubscriberId,
				answers.ToArray(),
				billingRef,
				timestamp,
				hmac);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="func">The delegate to call with and without the package (as needed).</param>
		/// <returns></returns>
		protected internal override object TryWithoutAndWithPackage(Func<bool, object> func)
		{
			try
			{
				// Try without the package first.
				// The "false" may be overridden by the NewPackage property of the Template object.
				return func(false);
			}
			catch (FaultException<ExceptionDetail> ex)
			{
				if (ex.Detail.Type == "HotDocs.Cloud.Storage.PackageNotFoundException")
				{
					return func(true);
				}
				throw;
			}
		}
		#endregion

		#region Private methods

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private Binding GetDefaultBinding()
		{
			var mtom = new MtomMessageEncodingBindingElement();
			mtom.MaxBufferSize = 1073741824;
			mtom.ReaderQuotas.MaxDepth = 256;
			mtom.ReaderQuotas.MaxStringContentLength = 1073741824;
			mtom.ReaderQuotas.MaxArrayLength = 1073741824;
			mtom.ReaderQuotas.MaxBytesPerRead = 1073741824;
			mtom.ReaderQuotas.MaxNameTableCharCount = 1073741824;

			var transport = new HttpsTransportBindingElement();
			transport.MaxReceivedMessageSize = 1073741824;
			transport.MaxBufferSize = 1073741824;
			transport.MaxBufferPoolSize = 1073741824;
			transport.KeepAliveEnabled = false;

			var binding = new CustomBinding(mtom, transport);
			binding.CloseTimeout = TimeSpan.FromMinutes(5);
			binding.OpenTimeout = TimeSpan.FromMinutes(5);
			binding.ReceiveTimeout = TimeSpan.FromMinutes(20);
			binding.SendTimeout = TimeSpan.FromMinutes(20);

			return binding;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str">The string from which to return a binary object array.</param>
		/// <returns></returns>
		private BinaryObject[] GetBinaryObjectArrayFromString(string str)
		{
			if (str != null)
			{
				return new BinaryObject[]
				{
					new BinaryObject
					{
						Data = Encoding.UTF8.GetBytes(str),
						DataEncoding = "UTF-8"
					}
				};
			}
			return null;
		}
		#endregion

		#region Dispose methods
		// Here we implement the dispose pattern to override the default disposal behavior.
		// We do this because the default disposal behavior leaves the proxy open if
		// Close() throws an exception.  We want to make sure the proxy always closes or aborts.

		/// <summary>
		/// Disposes the client.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes the client.
		/// </summary>
		/// <param name="disposing">Indicates whether or not managed resources should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (_proxy != null)
			{
				if (disposing)
				{
					if (_proxy.State != CommunicationState.Closed && _proxy.State != CommunicationState.Faulted)
					{
						try
						{
							_proxy.Close();
						}
						catch (Exception)
						{
							_proxy.Abort();
							throw;
						}
					}
					else
					{
						_proxy.Abort();
					}
				}
				_proxy = null;
			}
		}

		/// <summary>
		/// Finalizer for the client.
		/// </summary>
		~SoapClient()
		{
			Dispose(false);
		}
		#endregion
	}
}
