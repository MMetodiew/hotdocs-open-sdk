/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Generic;

namespace HotDocs.Sdk.Server
{
    /// <summary>
    /// This delegate type allows a host application to request notification immediately prior to the SDK assembling
    /// a specific document (during the execution of the WorkSession.AssembleDocuments method).
    /// </summary>
    /// <param name="template">The Template from which a new document is about to be assembled.</param>
    /// <param name="answers">The AnswerCollection which will be used for the assembly.  This represents the current
    /// state of the answer session for the entire WorkSession, and if modified, will modify the answers from here
    /// through to the end of the answer session.</param>
    /// <param name="settings">The AssembleDocumentSettings that will be used for this specific document assembly. This is
    /// a copy of the WorkSession's DefaultAssemblySettings; if modified it affects only the current assembly.</param>
    /// <param name="userState">Whatever state object is passed by the host application into
    /// WorkSession.AssembleDocuments will be passed back out to the host application in this userState parameter.</param>
    public delegate void PreAssembleDocumentDelegate(ITemplate template, AnswerCollection answers, AssembleDocumentSettings settings, object userState);

    /// <summary>
    /// This delegate type allows a host application to request notification immediately following the SDK assembling
    /// a document during the execution of the WorkSession.AssembleDocuments method.
    /// </summary>
    /// <param name="template">The Template from which a new document was just assembled.</param>
    /// <param name="result">The AssemblyResult object associated with the assembled document.  The SDK has not yet
    /// processed this AssemblyResult.  The Document inside the result will be added to the Document array
    /// that will eventually be returned by AssembleDocuments.  The Answers will become the new answers for subsequent
    /// work in this WorkSession.</param>
    /// <param name="userState">Whatever state object is passed by the host application into
    /// WorkSession.AssembleDocuments will be passed back out to the host application in this userState parameter.</param>
    public delegate void PostAssembleDocumentDelegate(ITemplate template, AssembleDocumentResult result, object userState);

    /// <summary>
    /// WorkSession is a state machine enabling a host application to easily navigate an assembly queue.
    /// It maintains an answer collection and a list of completed (and pending) interviews and documents.
    /// A host application uses this class not only to process through the assembly queue, but also to
    /// inspect the assembly queue for purposes of providing user feedback ("disposition pages").
    /// </summary>
    /// <remarks>
    /// Note: The implementation of WorkSession is the same whether interfacing with HDS or Core Services.
    /// </remarks>
    [Serializable]
    public abstract class WorkSession
    {


        /* properties/state */

        /// <summary>
        /// Returns the collection of answers pertaining to the current work item.
        /// </summary>
        public AnswerCollection AnswerCollection { get; protected set; }

        /// <summary>
        /// When you create a WorkSession, a copy of the application-wide default assembly settings (as specified
        /// in web.config) is made and assigned to this property of the AnswerCollection.  If the host app customizes
        /// these DefaultAssemblyOptions, each subsequent document generated in the WorkSession will inherit those
        /// customized settings.
        /// </summary>
        public AssembleDocumentSettings DefaultAssemblySettings { get; protected set; }

        /// <summary>
        /// The intent with DefaultInterviewOptions was to work much like DefaultAssemblyOptions. When you create
        /// a WorkSession, a copy of the application-wide default interview settings (as specified in web.config) is
        /// made and assigned to this property of the AnswerCollection.  If the host app customizes these
        /// DefaultInterviewOptions, each subsequent interview in the WorkSession will inherit those customized
        /// settings.  HOWEVER, this doesn't work currently because InterviewWorkItems do not carry a reference
        /// to the WorkSession, and therefore don't have access to this property. TODO: figure out how this should work.
        /// One possibility would be for the WorkSession class to expose the GetInterview method (and maybe also
        /// FinishInterview) rather than having those on the InterviewWorkItem class.  However, this would mean
        /// WorkSession would expose a GetInterview method all the time, but it is only callable some of the time
        /// (i.e. when the CurrentWorkItem is an interview).
        /// </summary>
        public InterviewSettings DefaultInterviewSettings { get; protected set; }

        /// <summary>
        /// Returns the IServices object for the current work session.
        /// </summary>

        protected List<DiskAccessibleWorkItem> _workItems;

        /// <summary>
        /// Exposees a list of interview and document work items, both already completed and pending, as suitable for
        /// presentation in a host application (for example, to show progress through the work session).
        /// </summary>
        public IEnumerable<DiskAccessibleWorkItem> WorkItems
        {
            get
            {
                return _workItems;
            }
        }

        /* convenience accessors */

        /// <summary>
        /// This is the one that's next in line to be completed.  In the case of interview work items,
        /// an interview is current both before and after it has been presented to the user, all the way
        /// up until FinishInterview is called, at which time whatever work item that follows becomes current.
        /// If the current work item is a document, AssembleDocuments() should be called, which will
        /// complete that document (and any that follow it), advancing CurrentWorkItem as it goes.
        /// </summary>
        public DiskAccessibleWorkItem CurrentWorkItem
        {
            get
            {
                foreach (var item in _workItems)
                {
                    if (!item.IsCompleted)
                        return item;
                }
                // else
                return null;
            }
        }

        /// <summary>
        /// returns true when all work items in the session have been completed, i.e. CurrentWorkItem == null.
        /// </summary>
        public bool IsCompleted
        {
            get { return CurrentWorkItem == null; }
        }

       
        protected void InsertNewWorkItems(IEnumerable<IOnDiskTemplate> templates, int parentPosition)
        {
            int insertPosition = parentPosition + 1;
            foreach (var template in templates)
            {
                if (template.HasInterview)
                    _workItems.Insert(insertPosition++, new DiskAccessibleInterviewWorkItem(template));
                if (template.GeneratesDocument)
                    _workItems.Insert(insertPosition++, new DiskAccessibleDocumentWorkItem(template));
            }
        }
    }
}