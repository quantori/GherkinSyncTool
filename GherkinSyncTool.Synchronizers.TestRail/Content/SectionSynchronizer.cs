using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.TestRail.Client;
using GherkinSyncTool.Synchronizers.TestRail.Exceptions;
using GherkinSyncTool.Synchronizers.TestRail.Model;
using NLog;

namespace GherkinSyncTool.Synchronizers.TestRail.Content
{
    public class SectionSynchronizer
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly TestRailClientWrapper _testRailClientWrapper;
        private readonly TestRailSettings _testRailSettings;
        private readonly GherkinSyncToolConfig _gherkinSyncToolConfig;
        private List<TestRailSection> _testRailSections;
        private readonly Context _context;
        private ulong? _archiveSection;

        public SectionSynchronizer(TestRailClientWrapper testRailClientWrapper, Context context)
        {
            _testRailSettings = ConfigurationManager.GetConfiguration<TestRailConfigs>().TestRailSettings;
            _gherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();
            _testRailClientWrapper = testRailClientWrapper;
            _context = context;
            _testRailSections = GetSectionsTree(_testRailSettings.ProjectId).ToList();
        }

        /// <summary>
        /// Gets or creates TestRail section Id for selected .feature file
        /// </summary>
        /// <param name="relativePath">Relative path to .feature file</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ulong GetOrCreateSectionIdFromPath(string relativePath)
        {
            var suiteId = _testRailSettings.SuiteId;
            var projectId = _testRailSettings.ProjectId;
            Log.Info($"Input file: {relativePath}");
            //Path includes name of the feature file - hence SkipLast(1)
            var sourceSections = new Queue<string>(relativePath.Split(Path.DirectorySeparatorChar).SkipLast(1));
            return GetOrCreateSectionIdRecursively(_testRailSections, sourceSections, suiteId, projectId);
        }

        public void MoveNotExistingSectionsToArchive()
        {
            _archiveSection = _testRailSections.Find(section => section.Name.Equals(_testRailSettings.ArchiveSectionName, StringComparison.InvariantCultureIgnoreCase))?.Id;
                
            if (_archiveSection is null)
            {
                _archiveSection = _testRailClientWrapper.CreateSection(new CreateSectionRequest(_testRailSettings.ProjectId,null, _testRailSettings.SuiteId, _testRailSettings.ArchiveSectionName));
            }
            
            var featureFileFolders = GetFeatureFileFolderTree();

            //Define Roots
            foreach (var testRailSection in _testRailSections)
            {
                foreach (var featureFileFolder in featureFileFolders)
                {
                    if (featureFileFolder.Name.Equals(testRailSection.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        MoveSectionToArchiveRecursively(featureFileFolder.ChildFolders, testRailSection.ChildSections, _archiveSection.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Builds a tree structure for TestRail sections
        /// </summary>
        /// <param name="projectId">TestRail project Id</param>
        /// <returns></returns>
        private IEnumerable<TestRailSection> GetSectionsTree(ulong projectId)
        {
            var testRailSectionsDictionary = _testRailClientWrapper
                .GetSections(projectId)
                .Select(s => new TestRailSection(s))
                .ToDictionary(k => k.Id);

            var result = new List<TestRailSection>();
            foreach (var section in testRailSectionsDictionary.Values)
            {
                if (section.ParentId != null)
                    testRailSectionsDictionary[section.ParentId].ChildSections.Add(section);
                else result.Add(section);
            }

            return result;
        }

        private void MoveSectionToArchiveRecursively(List<FeatureFileFolder> featureFileFolders,
            List<TestRailSection> testRailSections, ulong archiveSection)
        {
            foreach (var testRailSection in testRailSections)
            {
                var featureFileFolder = featureFileFolders.Find(fileFolder => fileFolder.Name.Equals(testRailSection.Name, StringComparison.InvariantCultureIgnoreCase));

                if (featureFileFolder is not null)
                {
                    MoveSectionToArchiveRecursively(featureFileFolder.ChildFolders, testRailSection.ChildSections, archiveSection);
                    continue;
                }

                Log.Warn($"Section [{testRailSection.Id.Value}] {testRailSection.Name} is moving to {_testRailSettings.ArchiveSectionName}.");
                try
                {
                    _testRailClientWrapper.MoveSection(testRailSection.Id.Value, archiveSection);
                }
                catch (TestRailException e)
                {
                    Log.Error(e, $"The section has not been moved: {testRailSection.Name}");
                    _context.IsRunSuccessful = false;
                }
            }
        }

        private List<FeatureFileFolder> GetFeatureFileFolderTree()
        {
            var result = new List<FeatureFileFolder>();
            var baseDirectory = new DirectoryInfo(_gherkinSyncToolConfig.BaseDirectory);
            var children = Directory.GetDirectories(_gherkinSyncToolConfig.BaseDirectory);

            var root = new FeatureFileFolder {Name = baseDirectory.Name, ChildFolders = TraverseFolders(children)};
            result.Add(root);
            return result;
        }
        
        private static List<FeatureFileFolder> TraverseFolders(string[] children)
        {
            var result = new List<FeatureFileFolder>();
            foreach (var folder in children)
            {
                var directoryInfo = new DirectoryInfo(folder);
                var subFolders = directoryInfo.GetDirectories();
                var featureFileFolder = new FeatureFileFolder{Name = directoryInfo.Name};
                if (subFolders.Length > 0)
                {
                    featureFileFolder.ChildFolders.AddRange(TraverseFolders(subFolders.Select(s => s.FullName).ToArray()));
                }
                result.Add(featureFileFolder);
            }

            return result;
        }

        /// <summary>
        /// Compares section structures in TestRail and local storage
        /// and returns or creates (if not existed) section Id for the selected .feature file 
        /// </summary>
        /// <param name="targetSections">Collection that represents section structure in TestRail</param>
        /// <param name="sourceSections">Queue of local folder names from test files root to target file folder</param>
        /// <param name="suiteId">TestRail suite Id</param>
        /// <param name="projectId">TestRail project Id</param>
        /// <param name="parentSectionId">TestRail section Id, null for the tests root</param>
        /// <returns>Section Id for the selected .feature file</returns>
        private ulong GetOrCreateSectionIdRecursively(List<TestRailSection> targetSections,
            Queue<string> sourceSections,
            ulong suiteId, ulong projectId, ulong? parentSectionId = null)
        {
            var targetSectionsChecked = false;
            if (!sourceSections.Any() && parentSectionId is null)
                throw new InvalidOperationException(
                    "Attempt to create file without setting parent folder. Please check configuration file.");

            while (sourceSections.Count != 0)
            {
                var folderName = sourceSections.Dequeue();
                if (!targetSectionsChecked)
                {
                    foreach (var section in targetSections)
                    {
                        if (section.Name != folderName) continue;
                        return GetOrCreateSectionIdRecursively(section.ChildSections, sourceSections, suiteId,
                            projectId, section.Id);
                    }

                    targetSectionsChecked = true;
                }

                var parentId = parentSectionId;
                parentSectionId =
                    _testRailClientWrapper.CreateSection(new CreateSectionRequest(projectId, parentSectionId, suiteId, folderName));
                targetSections = CreateChildSection(parentSectionId, suiteId, parentId, folderName, targetSections);
            }

            return parentSectionId.Value;
        }

        /// <summary>
        /// Creates new section (only in local structure) without need of sending request to TestRail API
        /// </summary>
        /// <param name="suiteId">TestRail suite Id</param>
        /// <param name="sectionId">TestRail section Id, null for the tests root</param>
        /// <param name="parentId">id of parent Section</param>
        /// <param name="folderName">name of the folder that represents </param>
        /// <param name="targetSections">collection of sections for the new section to add</param>
        /// <returns></returns>
        private List<TestRailSection> CreateChildSection(ulong? sectionId, ulong suiteId, ulong? parentId,
            string folderName, List<TestRailSection> targetSections)
        {
            var newSection =
                new TestRailSection
                {
                    Id = sectionId,
                    SuiteId = suiteId,
                    ParentId = parentId,
                    Name = folderName
                };
            targetSections.Add(newSection);
            return newSection.ChildSections;
        }
    }
}