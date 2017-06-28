﻿using System.Linq;
using System.Reactive.Disposables;
using FluentAssertions;
using NUnit.Framework;

namespace DynamicData.Tests.CacheFixtures
{
    [TestFixture]
    public class SubscribeManyFixture
    {
        private class SubscribeableObject
        {
            public bool IsSubscribed { get; private set; }
            public int Id { get; private set; }

            public void Subscribe()
            {
                IsSubscribed = true;
            }

            public void UnSubscribe()
            {
                IsSubscribed = false;
            }

            public SubscribeableObject(int id)
            {
                Id = id;
            }
        }

        private ISourceCache<SubscribeableObject, int> _source;
        private ChangeSetAggregator<SubscribeableObject, int> _results;

        [SetUp]
        public void Initialise()
        {
            _source = new SourceCache<SubscribeableObject, int>(p => p.Id);
            _results = new ChangeSetAggregator<SubscribeableObject, int>(
                _source.Connect().SubscribeMany(subscribeable =>
                {
                    subscribeable.Subscribe();
                    return Disposable.Create(subscribeable.UnSubscribe);
                }));
        }

        [TearDown]
        public void Cleanup()
        {
            _source.Dispose();
            _results.Dispose();
        }

        [Test]
        public void AddedItemWillbeSubscribed()
        {
            _source.AddOrUpdate(new SubscribeableObject(1));

            _results.Messages.Count.Should().Be(1, "Should be 1 updates");
            _results.Data.Count.Should().Be(1, "Should be 1 item in the cache");
            _results.Data.Items.First().IsSubscribed.Should().Be(true, "Should be subscribed");
        }

        [Test]
        public void RemoveIsUnsubscribed()
        {
            _source.AddOrUpdate(new SubscribeableObject(1));
            _source.Remove(1);

            _results.Messages.Count.Should().Be(2, "Should be 2 updates");
            _results.Data.Count.Should().Be(0, "Should be 0 items in the cache");
            _results.Messages[1].First().Current.IsSubscribed.Should().Be(false, "Should be be unsubscribed");
        }

        [Test]
        public void UpdateUnsubscribesPrevious()
        {
            _source.AddOrUpdate(new SubscribeableObject(1));
            _source.AddOrUpdate(new SubscribeableObject(1));

            _results.Messages.Count.Should().Be(2, "Should be 2 updates");
            _results.Data.Count.Should().Be(1, "Should be 1 items in the cache");
            _results.Messages[1].First().Current.IsSubscribed.Should().Be(true, "Current should be subscribed");
            _results.Messages[1].First().Previous.Value.IsSubscribed.Should().Be(false, "Previous should not be subscribed");
        }

        [Test]
        public void EverythingIsUnsubscribedWhenStreamIsDisposed()
        {
            _source.AddOrUpdate(Enumerable.Range(1, 10).Select(i => new SubscribeableObject(i)));
            _source.Clear();

            _results.Messages.Count.Should().Be(2, "Should be 2 updates");
            _results.Messages[1].All(d => !d.Current.IsSubscribed).Should().BeTrue();
        }
    }
}
