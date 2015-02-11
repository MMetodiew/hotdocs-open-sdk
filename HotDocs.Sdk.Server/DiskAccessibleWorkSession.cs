using System.Collections.Generic;
using System.IO;
using HotDocs.Sdk;
using HotDocs.Sdk.Server;
using HotDocs.Sdk.Server.Contracts;

public class DiskAccessibleWorkSession : WorkSession
{
    public IServices Service
    {
        get { return _service; }
    }

    private IServicesUsingTemplatesOnDisk _service;

    /// <summary>
    /// <c>WorkSession</c> constructor
    /// </summary>
    /// <param name="service">An object implementing the IServices interface, encapsulating the instance of
    /// HotDocs Server with which the host app is communicating.</param>
    /// <param name="template">The template upon which this WorkSession is based. The initial interview and/or
    /// document work items in the WorkSession will be based on this template (including its Switches property).</param>
    public DiskAccessibleWorkSession(IServicesUsingTemplatesOnDisk service, IOnDiskTemplate template) : this(service, template, null, null) { }

    /// <summary>
    /// Creates a WorkSession object that a host application can use to step through the process of presenting
    /// all the interviews and assembling all the documents that may result from the given template.
    /// </summary>
    /// <param name="service">An object implementing the IServices interface, encapsulating the instance of
    /// HotDocs Server with which the host app is communicating.</param>
    /// <param name="template">The template upon which this WorkSession is based. The initial interview and/or
    /// document work items in the WorkSession will be based on this template (including its Switches property).</param>
    /// <param name="answers">A collection of XML answers to use as a starting point for the work session.
    /// The initial interview (if any) will be pre-populated with these answers, and the subsequent generation
    /// of documents will have access to these answers as well.</param>
    public DiskAccessibleWorkSession(IServicesUsingTemplatesOnDisk service, IOnDiskTemplate template, TextReader answers) : this(service, template, answers, null) { }
    /// <summary>
    /// Creates a WorkSession object that a host application can use to step through the process of presenting
    /// all the interviews and assembling all the documents that may result from the given template.
    /// 
    /// Allows the default interview settings to be specified instead of being read from config file
    /// </summary>
    /// <param name="service">An object implementing the IServices interface, encapsulating the instance of
    /// HotDocs Server with which the host app is communicating.</param>
    /// <param name="template">The template upon which this WorkSession is based. The initial interview and/or
    /// document work items in the WorkSession will be based on this template (including its Switches property).</param>
    /// <param name="answers">A collection of XML answers to use as a starting point for the work session.
    /// The initial interview (if any) will be pre-populated with these answers, and the subsequent generation
    /// of documents will have access to these answers as well.</param>
    /// <param name="defaultInterviewSettings">The default interview settings to be used throughout the session</param>
    public DiskAccessibleWorkSession(IServicesUsingTemplatesOnDisk service, IOnDiskTemplate template, TextReader answers, InterviewSettings defaultInterviewSettings)
    {
        _service = service;
        AnswerCollection = new AnswerCollection();
        if (answers != null)
            AnswerCollection.ReadXml(answers);
        DefaultAssemblySettings = new AssembleDocumentSettings();
        if (defaultInterviewSettings != null)
            DefaultInterviewSettings = defaultInterviewSettings;
        else
            DefaultInterviewSettings = new InterviewSettings();
        // add the work items
        _workItems = new List<DiskAccessibleWorkItem>();
        if (template.HasInterview)
            _workItems.Add(new DiskAccessibleInterviewWorkItem(template));
        if (template.GeneratesDocument)
            _workItems.Add(new DiskAccessibleDocumentWorkItem(template));
    }


    /// <summary>
    /// AssembleDocuments causes all contiguous pending document work items (from CurrentWorkItem onwards)
    /// to be assembled, and returns the assembled documents.
    /// </summary>
    /// <include file="../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
    /// <returns></returns>
    /// <remarks>
    /// <para>If AssembleDocuments is called when the current work item is not a document (i.e. when there are
    /// currently no documents to assemble), it will return an empty array of results without performing any work.</para>
    /// <para>If you need to take any actions before or after each assembly, use the alternate constructor that
    /// accepts delegates.</para>
    /// TODO: include a table that shows the relationship between members of Document, AssemblyResult, WorkSession and DocumentWorkItem.
    /// </remarks>
    public Document[] AssembleDocuments(string logRef)
    {
        return AssembleDocuments(null, null, null, logRef);
    }

    /// <summary>
    /// AssembleDocuments causes all contiguous pending document work items (from CurrentWorkItem onwards)
    /// to be assembled, and returns the assembled documents.
    /// </summary>
    /// <param name="preAssembleDocument">This delegate will be called immediately before each document is assembled.</param>
    /// <param name="postAssembleDocument">This delegate will be called immediately following assembly of each document.</param>
    /// <param name="userState">This object will be passed to the above delegates.</param>
    /// <include file="../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
    /// <returns>An array of Document, one item for each document that was assembled.  Note that these items
    /// are of type Document, not AssemblyResult (see below).</returns>
    /// <remarks>
    /// <para>If AssembleDocuments is called when the current work item is not a document (i.e. when there are
    /// currently no documents to assemble), it will return an empty array of results without performing any work.</para>
    /// TODO: include a table that shows the relationship between members of Document, AssemblyResult, WorkSession and DocumentWorkItem.
    /// </remarks>
    public Document[] AssembleDocuments(PreAssembleDocumentDelegate preAssembleDocument,
        PostAssembleDocumentDelegate postAssembleDocument, object userState, string logRef)
    {
        var result = new List<Document>();
        // skip past completed work items to get the current workItem
        DiskAccessibleWorkItem workItem = null;
        int itemIndex = 0;
        for (; itemIndex < _workItems.Count; itemIndex++)
        {
            workItem = _workItems[itemIndex];
            if (!workItem.IsCompleted)
                break;
            workItem = null;
        }
        // while the current workItem != null && is a document (i.e. is not an interview)
        while (workItem != null && workItem is DiskAccessibleDocumentWorkItem)
        {
            var docWorkItem = workItem as DiskAccessibleDocumentWorkItem;
            // make a copy of the default assembly settings and pass it to the BeforeAssembleDocumentDelegate (if provided)
            AssembleDocumentSettings asmOpts = new AssembleDocumentSettings(DefaultAssemblySettings);
            asmOpts.Format = workItem.Template.NativeDocumentType;
            // if this is not the last work item in the queue, force retention of transient answers
            asmOpts.RetainTransientAnswers |= (workItem != _workItems[_workItems.Count - 1]);

            if (preAssembleDocument != null)
                preAssembleDocument(docWorkItem.Template, AnswerCollection, asmOpts, userState);

            // assemble the item
            using (var asmResult = _service.AssembleDocument(docWorkItem.Template, new StringReader(AnswerCollection.XmlAnswers), asmOpts, logRef))
            {
                if (postAssembleDocument != null)
                    postAssembleDocument(docWorkItem.Template, asmResult, userState);

                // replace the session answers with the post-assembly answers
                AnswerCollection.ReadXml(asmResult.Answers);
                // add pendingAssemblies to the queue as necessary
                InsertNewWorkItems(asmResult.PendingAssemblies, itemIndex);
                // store UnansweredVariables in the DocumentWorkItem
                docWorkItem.UnansweredVariables = asmResult.UnansweredVariables;
                // add an appropriate Document to a list being compiled for the return value of this method
                result.Add(asmResult.ExtractDocument());
            }
            // mark the current workitem as complete
            docWorkItem.IsCompleted = true;
            // advance to the next workitem
            workItem = (++itemIndex >= _workItems.Count) ? null : _workItems[itemIndex];
        }
        return result.ToArray();
    }

    /// <summary>
    /// Returns the current interview with the given settings
    /// </summary>
    /// <param name="settings">Settings to use with the interview.</param>
    /// <param name="markedVariables">A list of variable names whose prompts should be "marked" in the interview.</param>
    /// <include file="../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
    /// <returns>An <c>InterviewResult</c> containing the HTML fragment and other supporting files for the interview.</returns>
    public InterviewResult GetCurrentInterview(InterviewSettings settings, IEnumerable<string> markedVariables, string logRef = "")
    {
        DiskAccessibleWorkItem currentWorkItem = CurrentWorkItem;
        TextReader answers = new StringReader(AnswerCollection.XmlAnswers);


        settings.Title = settings.Title ?? CurrentWorkItem.Template.Title;

        return _service.GetInterview(currentWorkItem.Template, answers, settings, markedVariables, logRef);
    }

    /// <summary>
    /// Called by the host application when answers have been posted back from a browser interview.
    /// </summary>
    /// <param name="interviewAnswers">The answers that were posted back from the interview.</param>
    public void FinishInterview(TextReader interviewAnswers)
    {
        // overlay interviewAnswers over the session answer set,
        AnswerCollection.OverlayXml(interviewAnswers);

        // skip past completed work items to get the current workItem
        DiskAccessibleWorkItem workItem = null;
        int itemIndex = 0;
        for (; itemIndex < _workItems.Count; itemIndex++)
        {
            workItem = _workItems[itemIndex];
            if (!workItem.IsCompleted)
                break;
            workItem = null;
        }
        if (workItem != null && workItem is DiskAccessibleInterviewWorkItem)
        {
            // if the current template is an interview template
            if (workItem.Template.TemplateType == TemplateType.InterviewOnly)
            {
                //     "assemble" it...
                AssembleDocumentSettings asmOpts = new AssembleDocumentSettings(DefaultAssemblySettings);
                asmOpts.Format = DocumentType.Native;
                // if this is not the last work item in the queue, force retention of transient answers
                asmOpts.RetainTransientAnswers |= (itemIndex < _workItems.Count - 1);

                // assemble the item
                using (var asmResult = _service.AssembleDocument(workItem.Template, new StringReader(AnswerCollection.XmlAnswers), asmOpts, ""))
                {
                    // replace the session answers with the post-assembly answers
                    AnswerCollection.ReadXml(asmResult.Answers);
                    // add pendingAssemblies to the queue as necessary
                    InsertNewWorkItems(asmResult.PendingAssemblies, itemIndex);
                }
            }
            // mark this interview workitem as complete.  (This will cause the WorkSession to advance to the next workItem.)
            CurrentWorkItem.IsCompleted = true;
        }
    }
    /// <summary>
    /// This constructor accepts a value for the interview format in case the host application wants to have more
    /// control over which format to use other than the one format specified in web.config. For example, the host
    /// application can detect whether or not the user's browser has Silverlight installed, and if not, it can choose
    /// to fall back to JavaScript interviews even if its normal preference is Silverlight.
    /// </summary>
    /// <param name="format">The format (Silverlight or JavaScript) of interview being requested.</param>
    /// <returns>An <c>InterviewResult</c>, containing the HTML fragment and any other supporting files required by the interview.</returns>
    public InterviewResult GetCurrentInterview(InterviewFormat format)
    {
        InterviewSettings s = DefaultInterviewSettings;

        // If a format was specified (e.g., it is not "Unspecified") then use the format provided.
        if (format != InterviewFormat.Unspecified)
            s.Format = format;

        return GetCurrentInterview(s, null);
    }

    /// <summary>
    /// Returns the current interview using default interview settings
    /// </summary>
    /// <returns></returns>
    public InterviewResult GetCurrentInterview()
    {
        return GetCurrentInterview(DefaultInterviewSettings, null);
    }

}