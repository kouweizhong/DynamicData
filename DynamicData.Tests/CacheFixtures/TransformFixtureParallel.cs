using System;
using System.Linq;
using DynamicData.Tests.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace DynamicData.Tests.CacheFixtures
{
    [TestFixture]
    public class TransformFixtureParallel
    {
        private ISourceCache<Person, string> _source;
        private ChangeSetAggregator<PersonWithGender, string> _results;

        private readonly Func<Person, PersonWithGender> _transformFactory = p =>
        {
            string gender = p.Age % 2 == 0 ? "M" : "F";
            return new PersonWithGender(p, gender);
        };

        [SetUp]
        public void Initialise()
        {
            _source = new SourceCache<Person, string>(p => p.Name);

            var pTransform = _source.Connect().Transform(_transformFactory);
            _results = new ChangeSetAggregator<PersonWithGender, string>(pTransform);
        }

        [TearDown]
        public void Cleanup()
        {
            _source.Dispose();
            _results.Dispose();
        }

        [Test]
        public void Add()
        {
            var person = new Person("Adult1", 50);
            _source.AddOrUpdate(person);

            _results.Messages.Count.Should().Be(1, "Should be 1 updates");
            _results.Data.Count.Should().Be(1, "Should be 1 item in the cache");
            _results.Data.Items.First().Should().Be(_transformFactory(person), "Should be same person");
        }

        [Test]
        public void Remove()
        {
            const string key = "Adult1";
            var person = new Person(key, 50);

            _source.AddOrUpdate(person);
            _source.Remove(key);

            _results.Messages.Count.Should().Be(2, "Should be 2 updates");
            _results.Messages.Count.Should().Be(2, "Should be 2 updates");
            _results.Messages[0].Adds.Should().Be(1, "Should be 80 addes");
            _results.Messages[1].Removes.Should().Be(1, "Should be 80 removes");
            _results.Data.Count.Should().Be(0, "Should be nothing cached");
        }

        [Test]
        public void Update()
        {
            const string key = "Adult1";
            var newperson = new Person(key, 50);
            var updated = new Person(key, 51);

            _source.AddOrUpdate(newperson);
            _source.AddOrUpdate(updated);

            _results.Messages.Count.Should().Be(2, "Should be 2 updates");
            _results.Messages[0].Adds.Should().Be(1, "Should be 1 adds");
            _results.Messages[1].Updates.Should().Be(1, "Should be 1 update");
        }

        [Test]
        public void BatchOfUniqueUpdates()
        {
            var people = Enumerable.Range(1, 100).Select(i => new Person("Name" + i, i)).ToArray();
            _source.AddOrUpdate(people);

            _results.Messages.Count.Should().Be(1, "Should be 1 updates");
            _results.Messages[0].Adds.Should().Be(100, "Should return 100 adds");

            var transformed = people.Select(_transformFactory).ToArray();

            _results.Data.Items.OrderBy(p => p.Age).ShouldAllBeEquivalentTo(_results.Data.Items.OrderBy(p => p.Age), "Incorrect transform result");
        }

        [Test]
        public void SameKeyChanges()
        {
            var people = Enumerable.Range(1, 10).Select(i => new Person("Name", i)).ToArray();
            _source.AddOrUpdate(people);

            _results.Messages.Count.Should().Be(1, "Should be 1 updates");
            _results.Messages[0].Adds.Should().Be(1, "Should return 1 adds");
            _results.Messages[0].Updates.Should().Be(9, "Should return 9 adds");
            _results.Data.Count.Should().Be(1, "Should result in 1 record");

            var lastTransformed = _transformFactory(people.Last());
            var onlyItemInCache = _results.Data.Items.First();

            onlyItemInCache.Should().Be(lastTransformed, "Incorrect transform result");
        }

        [Test]
        public void Clear()
        {
            var people = Enumerable.Range(1, 100).Select(l => new Person("Name" + l, l)).ToArray();

            _source.AddOrUpdate(people);

            _source.Clear();

            _results.Messages.Count.Should().Be(2, "Should be 2 updates");
            _results.Messages[0].Adds.Should().Be(100, "Should be 80 addes");
            _results.Messages[1].Removes.Should().Be(100, "Should be 80 removes");
            _results.Data.Count.Should().Be(0, "Should be nothing cached");
        }
    }
}
