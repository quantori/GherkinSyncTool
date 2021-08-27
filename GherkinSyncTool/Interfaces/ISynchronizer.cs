using System.Collections.Generic;

namespace GherkinSyncTool.Interfaces
{
    /// <summary>
    /// The ISynchronizer interface implements a method that synchronizes feature files changes (creating\updating\deleting) with destinations like a test management system
    /// </summary>
    public interface ISynchronizer
    {
        /// <summary>
        /// Synchronize feature files changes (creating\updating\deleting).
        /// </summary>
        /// <param name="featureFiles">List of a feature files.</param>
        void Sync(List<IFeatureFile> featureFiles);
    }
}