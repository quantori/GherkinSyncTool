﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Configuration;
using GherkinSyncTool.Exceptions;
using GherkinSyncTool.Interfaces;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer.Client;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer.Model;
using NLog;

namespace GherkinSyncTool.Synchronizers.TestRailSynchronizer.Content
{
    public class SectionSynchronizer
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly TestRailClientWrapper _testRailClientWrapper;
        private readonly GherkynSyncToolConfig _config;
        private List<TestRailSection> _testRailSections;

        public SectionSynchronizer(TestRailClientWrapper testRailClientWrapper)
        {
            _config = ConfigurationManager.GetConfiguration();
            _testRailClientWrapper = testRailClientWrapper;
            _testRailSections = GetSectionsTree(_config.TestRailSettings.ProjectId, _config.TestRailSettings.SuiteId)
                .ToList();
        }

        /// <summary>
        /// Gets or creates TestRail section Id for selected .feature file
        /// </summary>
        /// <param name="relativePath">Relative path to .feature file</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ulong GetOrCreateSectionIdFromPath(string relativePath)
        {
            var suiteId = _config.TestRailSettings.SuiteId;
            var projectId = _config.TestRailSettings.ProjectId;
            Log.Info($"Input file: {relativePath}");
            //Path includes name of the feature file - hence SkipLast(1)
            var sourceSections = new Queue<string>(relativePath.Split(Path.DirectorySeparatorChar).SkipLast(1));
            return GetOrCreateSectionIdRecursively(_testRailSections, sourceSections, suiteId, projectId);
        }

        public void MoveNotExistingSectionsToArchive(List<IFeatureFile> featureFiles)
        {
            var featureFileFolders = GetFeatureFileFolderTree(featureFiles);

            //Define Roots
            foreach (var testRailSection in _testRailSections)
            {
                foreach (var featureFileFolder in featureFileFolders)
                {
                    if (featureFileFolder.Name == testRailSection.Name)
                    {
                        MoveSectionToArchiveRecursively(featureFileFolder.ChildFolders, testRailSection.ChildSections);
                    }
                }
            }
        }

        /// <summary>
        /// Builds a tree structure for TestRail sections
        /// </summary>
        /// <param name="projectId">TestRail project Id</param>
        /// <param name="suiteId">TestRail suite Id</param>
        /// <returns></returns>
        private IEnumerable<TestRailSection> GetSectionsTree(ulong projectId, ulong? suiteId)
        {
            if (suiteId is null)
                throw new ArgumentException($"SuiteId must be specified. Check the TestRail project #{projectId}");

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
            List<TestRailSection> testRailSections)
        {
            var archiveSection = _testRailSections.Find(section => section.Name.Equals(_config.TestRailSettings.ArchiveSectionName, StringComparison.InvariantCultureIgnoreCase))?.Id;
                
            if (archiveSection is null)
            {
                archiveSection = _testRailClientWrapper.CreateSection(new CreateSectionRequest(_config.TestRailSettings.ProjectId,null, _config.TestRailSettings.SuiteId, _config.TestRailSettings.ArchiveSectionName));
            }
            
            foreach (var testRailSection in testRailSections)
            {
                var featureFileFolder = featureFileFolders.Find(fileFolder => fileFolder.Name.Equals(testRailSection.Name, StringComparison.InvariantCultureIgnoreCase));

                if (featureFileFolder is not null)
                {
                    MoveSectionToArchiveRecursively(featureFileFolder.ChildFolders, testRailSection.ChildSections);
                    continue;
                }

                Log.Warn($"Section [{testRailSection.Id.Value}] {testRailSection.Name} is moving to {_config.TestRailSettings.ArchiveSectionName}.");
                try
                {
                    _testRailClientWrapper.MoveSection(testRailSection.Id.Value, archiveSection);
                }
                catch (TestRailException e)
                {
                    Log.Error(e, $"The section has not been moved: {testRailSection.Name}");
                }
            }
        }

        private List<FeatureFileFolder> GetFeatureFileFolderTree(List<IFeatureFile> featureFiles)
        {
            var result = new List<FeatureFileFolder>();
            
            var featureFilesPaths = featureFiles.Select(file => Path.GetDirectoryName(file.RelativePath)).Distinct().ToArray();
            
            var featureFileFoldersDictionary = new Dictionary<string, FeatureFileFolder>();

            foreach (var folderPath in featureFilesPaths)
            {
                var pathSeparated = folderPath.Split(Path.DirectorySeparatorChar);
                var folderName = pathSeparated.Last();
                var parentPath = Path.Combine(pathSeparated.SkipLast(1).ToArray());
                
                var featureFileFolder = new FeatureFileFolder
                {
                    Name = folderName,
                    ParentFolderPath = parentPath
                };

                featureFileFoldersDictionary.Add(folderPath, featureFileFolder);
            }

            foreach (var featureFileFolder in featureFileFoldersDictionary.Values)
            {
                if (!string.IsNullOrEmpty(featureFileFolder.ParentFolderPath))
                {
                    featureFileFoldersDictionary[featureFileFolder.ParentFolderPath].ChildFolders.Add(featureFileFolder);
                }
                else result.Add(featureFileFolder);
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