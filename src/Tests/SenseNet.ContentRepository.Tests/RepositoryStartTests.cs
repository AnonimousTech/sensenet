﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security.Data;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class RepositoryStartTests : TestBase
    {
        public class TestNodeObserver1 : NodeObserver { }
        public class TestNodeObserver2 : NodeObserver { }

        public class TestEventLogger : IEventLogger
        {
            public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
                IDictionary<string, object> properties)
            {
                //do nothing
            }
        }
        public class TestSnTracer : ISnTracer
        {
            public void Write(string line)
            {
                //do nothing
            }

            public void Flush()
            {
                //do nothing
            }
        }

        [TestMethod]
        public void RepositoryStart_NamedProviders()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(dbProvider, DataProvider.Current);
                Assert.AreEqual(searchEngine, SearchManager.SearchEngine);
                Assert.AreEqual(accessProvider, AccessProvider.Current);
                Assert.AreEqual(emvrProvider, Providers.Instance.ElevatedModificationVisibilityRuleProvider);

                // Currently this does not work, because the property below re-creates the security 
                // db provider from the prototype, so it cannot be ref equal with the original.
                // Assert.AreEqual(securityDbProvider, SecurityHandler.SecurityContext.DataProvider);
                Assert.AreEqual(securityDbProvider, Providers.Instance.SecurityDataProvider);

                // Check a few trace categories that were switched ON above.
                Assert.IsTrue(SnTrace.Custom.Enabled);
                Assert.IsTrue(SnTrace.Test.Enabled);
                Assert.IsTrue(SnTrace.Web.Enabled);
                Assert.IsTrue(SnTrace.System.Enabled);
                Assert.IsFalse(SnTrace.TaskManagement.Enabled);
                Assert.IsFalse(SnTrace.Workflow.Enabled);
            }
        }

        [TestMethod]
        public void RepositoryStart_NodeObservers_DisableAll()
        {
            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(new InMemoryDataProvider())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.IsFalse(Providers.Instance.NodeObservers.Any());
            }
        }
        [TestMethod]
        public void RepositoryStart_NodeObservers_EnableOne()
        {
            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(new InMemoryDataProvider())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(1, Providers.Instance.NodeObservers.Length);
                Assert.AreEqual(typeof(TestNodeObserver1), Providers.Instance.NodeObservers[0].GetType());
            }
        }
        [TestMethod]
        public void RepositoryStart_NodeObservers_EnableMore()
        {
            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(new InMemoryDataProvider())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(TestNodeObserver1), typeof(TestNodeObserver2))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.AreEqual(2, Providers.Instance.NodeObservers.Length);
                Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver1)));
                Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver2)));
            }
        }
        [TestMethod]
        public void RepositoryStart_NodeObservers_DisableOne()
        {
            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(new InMemoryDataProvider())
                .UseSecurityDataProvider(new MemoryDataProvider(DatabaseStorage.CreateEmpty()))
                .UseSearchEngine(new InMemorySearchEngine())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .UseCacheProvider(new EmptyCache())
                .DisableNodeObservers(typeof(TestNodeObserver1))
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            using (Repository.Start(repoBuilder))
            {
                Assert.IsFalse(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver1)));

                //TODO: currently this does not work, because observers are enabled/disabled globally.
                // Itt will, when we move to a per-thread environment in tests.
                //Assert.IsTrue(Providers.Instance.NodeObservers.Any(no => no.GetType() == typeof(TestNodeObserver2)));
            }
        }

        [TestMethod]
        public void RepositoryStart_NullPopulator()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Web", "System");

            var originalIsOuterSearchEngineEnabled = Indexing.IsOuterSearchEngineEnabled;
            Indexing.IsOuterSearchEngineEnabled = false;
            try
            {
                using (Repository.Start(repoBuilder))
                {
                    Assert.IsFalse(SearchManager.IsOuterEngineEnabled);
                    Assert.AreEqual(typeof(InternalSearchEngine), SearchManager.SearchEngine.GetType());
                    var populator = SearchManager.GetIndexPopulator();
                    Assert.AreEqual(typeof(NullPopulator), populator.GetType());
                }
            }
            finally
            {
                Indexing.IsOuterSearchEngineEnabled = originalIsOuterSearchEngineEnabled;
            }
        }

        [TestMethod]
        public void RepositoryStart_Loggers()
        {
            var originalLogger = SnLog.Instance;
            var originalTracers = SnTrace.SnTracers;

            try
            {
                Test(repoBuilder =>
                {
                    repoBuilder
                        .UseLogger(new TestEventLogger())
                        .UseTracer(new TestSnTracer());
                }, () =>
                {
                    //test that the loggers were set correctly
                    Assert.AreEqual(1, SnTrace.SnTracers.Count);
                    Assert.IsTrue(SnTrace.SnTracers.First() is TestSnTracer);
                    Assert.IsTrue(SnLog.Instance is TestEventLogger);
                });
            }
            finally
            {
                SnLog.Instance = originalLogger;
                SnTrace.SnTracers.Clear();
                SnTrace.SnTracers.AddRange(originalTracers);
            }
        }
    }
}
