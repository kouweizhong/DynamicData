﻿using DynamicData.Tests.Domain;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace DynamicData.Tests.ListFixtures
{
    [TestFixture]
    internal class ToObservableChangeSetFixture : ReactiveTest
    {        
        private IObservable<Person> _observable;
        private TestScheduler _scheduler;
        private IDisposable _disposable;
        private List<Person> _target;

        private readonly Person person1 = new Person("One", 1);
        private readonly Person person2 = new Person("Two", 2);
        private readonly Person person3 = new Person("Three", 3);

        [SetUp]
        public void Initialise()
        {
            _scheduler = new TestScheduler();
            _observable = _scheduler.CreateColdObservable(
                OnNext(1, person1),
                OnNext(2, person2),
                OnNext(3, person3));

            _target = new List<Person>();

            _disposable = _observable                
                .ToObservableChangeSet(2, _scheduler)                                                                          
                .Clone(_target)
                .Subscribe();            
        }

        [TearDown]
        public void Cleanup()
        {
            _disposable.Dispose();            
        }

        [Test]
        public void ShouldLimitSizeOfBoundCollection()
        {
            _scheduler.AdvanceTo(2);
            _target.Count.Should().Be(2, "Should be 2 item in target collection");

            _scheduler.AdvanceTo(3);
            _target.Count.Should().Be(2, "Should be 2 item in target collection because of size limit");
            
            var expected = new[] { person2, person3 };

            _target.ShouldAllBeEquivalentTo(expected);
        }
    }
}
