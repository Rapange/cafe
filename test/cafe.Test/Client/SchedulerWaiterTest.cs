﻿using System;
using System.Net.Http;
using cafe.Client;
using cafe.Shared;
using cafe.Test.Server.Scheduling;
using FluentAssertions;
using Moq;
using Xunit;

namespace cafe.Test.Client
{
    public class SchedulerWaiterTest
    {
        [Fact]
        public void Exception_ShouldSetAutoresetEventAndFail()
        {
            var scheduler = new Mock<ISchedulerServer>();
            scheduler.Setup(s => s.GetTaskStatus(It.IsAny<Guid>())).Throws<HttpRequestException>();
            var autoResetEvent = new FakeAutoResetEvent();
            var fakeTimerFactory = new FakeTimerFactory();

            var waiter = new SchedulerWaiter(() => scheduler.Object, autoResetEvent, fakeTimerFactory);
            autoResetEvent.WhatToDoDuringWaitOne = () =>
            {
                fakeTimerFactory.FireTimerAction();
            };

            var result = waiter.WaitForTaskToComplete(ScheduledTaskStatus.Create("sample task"));

            result.Result.IsSuccess.Should().BeFalse("because an exception was thrown by the scheduler");
            result.Result.FailureDescription.Should()
                .NotBeEmpty("because it should contain a description of what happened");
        }
    }

    public class FakeAutoResetEvent : IAutoResetEvent
    {
        public FakeAutoResetEvent()
        {
            WhatToDoDuringWaitOne = () => { };
        }

        public void WaitOne(TimeSpan timeout)
        {
            WhatToDoDuringWaitOne();
        }

        public Action WhatToDoDuringWaitOne { get; set; }

        public void Set()
        {
        }
    }
}