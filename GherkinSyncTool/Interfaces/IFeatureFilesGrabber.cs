using System.Collections.Generic;

namespace GherkinSyncTool.Interfaces
{
    /// <summary>
    /// The IFeatureFilesGrabber interface implements a method that received feature files from a source.
    /// </summary>
    public interface IFeatureFilesGrabber
    {
        /// <summary>
        /// Takes feature files from a source.
        /// </summary>
        /// <returns>List of a feature files.</returns>
        List<IFeatureFile> TakeFiles();
    }
}