using Gherkin.Ast;

namespace GherkinSyncTool.Interfaces
{
    /// <summary>
    /// Represent a Gherkin syntax feature file.
    /// </summary>
    public interface IFeatureFile
    {
        /// <summary>
        /// Gherkin document data model.
        /// </summary>
        GherkinDocument Document { get; }
        
        /// <summary>
        /// Absolute folder path that contains *.feature file.
        /// </summary>
        string AbsolutePath { get; }
        
        /// <summary>
        /// Relative to application folder path that contains *.feature file.
        /// </summary>
        string RelativePath { get; }
    }
}