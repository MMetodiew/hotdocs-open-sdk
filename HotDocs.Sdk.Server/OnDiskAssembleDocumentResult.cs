using System.Collections.Generic;
using System.Linq;

namespace HotDocs.Sdk.Server
{
    public class OnDiskAssembleDocumentResult : AssembleDocumentResult
    {
        /// <summary>
        /// <c>AssembleDocumentResult</c> constructor stores the result of calling IServices.AssembleDocument
        /// </summary>
        /// <param name="document">The assembled document.</param>
        /// <param name="answers">The answers after the assembly has finished. (Depending on template scripting, the answers may be different at the end of the assembly than at the start.)</param>
        /// <param name="pendingAssemblies">A list of assemblies to complete as a result of finishing this assembly.</param>
        /// <param name="unansweredVariables">A list of variables that were unanswered during the assembly.</param>
        internal OnDiskAssembleDocumentResult(Document document, string answers, IEnumerable<ITemplateOnDisk> pendingAssemblies, IEnumerable<string> unansweredVariables)
        {
            Document = document;
            if (answers != null)
                Answers = answers;
            else
                Answers = string.Empty;
            PendingAssemblies = pendingAssemblies;
            UnansweredVariables = unansweredVariables;
        }
        /// <summary>
        /// An collection of assemblies that need to be completed after this assembly is finished
        /// (results of ASSEMBLE instructions in the assembled template).
        /// </summary>
        public IEnumerable<ITemplateOnDisk> PendingAssemblies { get; private set; }

        /// <summary>
        /// Returns the number of pending assemblies (or 0 if it is null)
        /// </summary>
        public int PendingAssembliesCount
        {
            get
            {
                if (PendingAssemblies == null)
                    return 0;
                else
                    return PendingAssemblies.Count<ITemplate>();
            }
        }
    }
}