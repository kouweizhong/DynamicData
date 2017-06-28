using System;
using DynamicData.Tests.Domain;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using FluentAssertions;
using System.Reactive.Linq;

namespace DynamicData.Tests.ListFixtures
{
    [TestFixture]
    public class BufferFixture
    {
        private ISourceList<Person> _source;
        private ChangeSetAggregator<Person> _results;
        private TestScheduler _scheduler;

        [SetUp]
        public void MyTestInitialize()
        {
            _scheduler = new TestScheduler();
            _source = new SourceList<Person>();
            _results = _source.Connect().Buffer(TimeSpan.FromMinutes(1), _scheduler).FlattenBufferResult().AsAggregator();
        }

        [TearDown]
        public void Cleanup()
        {
            _results.Dispose();
            _source.Dispose();
        }

        [Test]
        public void NoResultsWillBeReceivedBeforeClosingBuffer()
        {
            _source.Add(new Person("A", 1));
            _results.Messages.Count.Should().Be(0, "There should be no messages");
        }

        [Test]
        public void ResultsWillBeReceivedAfterClosingBuffer()
        {
            _source.Add(new Person("A", 1));

            //go forward an arbitary amount of time
            _scheduler.AdvanceBy(TimeSpan.FromSeconds(61).Ticks);
            _results.Messages.Count.Should().Be(1, "Should be 1 update");
        }
    }
}
