﻿namespace WorkoutWotch.UnitTests.Models
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;
    using Builders;
    using Kent.Boogaart.PCLMock;
    using ReactiveUI;
    using WorkoutWotch.Models;
    using WorkoutWotch.Models.Events;
    using WorkoutWotch.UnitTests.Models.Mocks;
    using WorkoutWotch.UnitTests.Services.Logger.Mocks;
    using WorkoutWotch.UnitTests.Services.Speech.Mocks;
    using Xunit;

    public class ExerciseFixture
    {
        [Fact]
        public void ctor_throws_if_logger_service_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Exercise(null, new SpeechServiceMock(), "name", 3, 10, Enumerable.Empty<MatcherWithAction>()));
        }

        [Fact]
        public void ctor_throws_if_speech_service_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Exercise(new LoggerServiceMock(), null, "name", 3, 10, Enumerable.Empty<MatcherWithAction>()));
        }

        [Fact]
        public void ctor_throws_if_name_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Exercise(new LoggerServiceMock(MockBehavior.Loose), new SpeechServiceMock(), null, 3, 10, Enumerable.Empty<MatcherWithAction>()));
        }

        [Fact]
        public void ctor_throws_if_set_count_is_less_than_zero()
        {
            Assert.Throws<ArgumentException>(() => new Exercise(new LoggerServiceMock(MockBehavior.Loose), new SpeechServiceMock(), "name", -3, 10, Enumerable.Empty<MatcherWithAction>()));
        }

        [Fact]
        public void ctor_throws_if_repetition_count_is_less_than_zero()
        {
            Assert.Throws<ArgumentException>(() => new Exercise(new LoggerServiceMock(MockBehavior.Loose), new SpeechServiceMock(), "name", 3, -10, Enumerable.Empty<MatcherWithAction>()));
        }

        [Fact]
        public void ctor_throws_if_matchers_with_actions_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Exercise(new LoggerServiceMock(MockBehavior.Loose), new SpeechServiceMock(), "name", 3, 10, null));
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("Some longer name")]
        [InlineData("An exercise name with !@*&(*$#&^$).,/.<?][:[]; weird characters")]
        public void name_yields_the_name_passed_into_ctor(string name)
        {
            var sut = new ExerciseBuilder()
                .WithName(name)
                .Build();
            Assert.Equal(name, sut.Name);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void set_count_yields_the_set_count_passed_into_ctor(int setCount)
        {
            var sut = new ExerciseBuilder()
                .WithSetCount(setCount)
                .Build();
            Assert.Equal(setCount, sut.SetCount);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void repetition_count_yields_the_repetition_count_passed_into_ctor(int repetitionCount)
        {
            var sut = new ExerciseBuilder()
                .WithRepetitionCount(repetitionCount)
                .Build();
            Assert.Equal(repetitionCount, sut.RepetitionCount);
        }

        [Fact]
        public void duration_returns_zero_if_there_are_no_actions()
        {
            var sut = new ExerciseBuilder().Build();
            Assert.Equal(TimeSpan.Zero, sut.Duration);
        }

        [Fact]
        public void duration_returns_sum_of_action_durations()
        {
            var action1 = new ActionMock();
            var action2 = new ActionMock();
            var action3 = new ActionMock();

            action1
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(10));

            action2
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(3));

            action3
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(1));

            var eventMatcher1 = new EventMatcherMock();
            var eventMatcher2 = new EventMatcherMock();
            var eventMatcher3 = new EventMatcherMock();

            eventMatcher1
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            eventMatcher2
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is DuringRepetitionEvent);

            eventMatcher3
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is AfterSetEvent);

            var sut = new ExerciseBuilder()
                .WithSetCount(2)
                .WithRepetitionCount(3)
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher1, action1))
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher2, action2))
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher3, action3))
                .Build();

            Assert.Equal(TimeSpan.FromSeconds(30), sut.Duration);
        }

        [Fact]
        public async Task execute_async_throws_if_execution_context_is_null()
        {
            var sut = new ExerciseBuilder().Build();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.ExecuteAsync(null));
        }

        [Fact]
        public async Task execute_async_executes_all_appropriate_actions()
        {
            var action1 = new ActionMock(MockBehavior.Loose);
            var action2 = new ActionMock(MockBehavior.Loose);
            var action3 = new ActionMock(MockBehavior.Loose);
            var eventMatcher1 = new EventMatcherMock();
            var eventMatcher2 = new EventMatcherMock();
            var eventMatcher3 = new EventMatcherMock();

            eventMatcher1
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            eventMatcher2
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is DuringRepetitionEvent);

            eventMatcher3
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is AfterSetEvent);

            var sut = new ExerciseBuilder()
                .WithSetCount(2)
                .WithRepetitionCount(3)
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher1, action1))
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher2, action2))
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher3, action3))
                .Build();

            using (var executionContext = new ExecutionContext())
            {
                await sut.ExecuteAsync(executionContext);

                action1
                    .Verify(x => x.ExecuteAsync(executionContext))
                    .WasCalledExactlyOnce();

                action2
                    .Verify(x => x.ExecuteAsync(executionContext))
                    .WasCalledExactly(times: 6);

                action3
                    .Verify(x => x.ExecuteAsync(executionContext))
                    .WasCalledExactly(times: 2);
            }
        }

        [Fact]
        public async Task execute_async_does_not_skip_zero_duration_actions()
        {
            var action = new ActionMock(MockBehavior.Loose);
            var eventMatcher = new EventMatcherMock();

            eventMatcher
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            var sut = new ExerciseBuilder()
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher, action))
                .Build();

            using (var executionContext = new ExecutionContext())
            {
                await sut.ExecuteAsync(executionContext);

                action
                    .Verify(x => x.ExecuteAsync(executionContext))
                    .WasCalledExactlyOnce();
            }
        }

        [Fact]
        public async Task execute_async_skips_actions_that_are_shorter_than_the_skip_ahead()
        {
            var action1 = new ActionMock(MockBehavior.Loose);
            var action2 = new ActionMock(MockBehavior.Loose);
            var action3 = new ActionMock(MockBehavior.Loose);
            var eventMatcher1 = new EventMatcherMock();
            var eventMatcher2 = new EventMatcherMock();
            var eventMatcher3 = new EventMatcherMock();

            action1
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(10));

            action2
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(3));

            action3
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(1));

            action1
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Throw();

            action2
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Throw();

            eventMatcher1
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            eventMatcher2
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            eventMatcher3
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            var sut = new ExerciseBuilder()
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher1, action1))
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher2, action2))
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher3, action3))
                .Build();

            using (var executionContext = new ExecutionContext(TimeSpan.FromSeconds(13)))
            {
                await sut.ExecuteAsync(executionContext);

                action3
                    .Verify(x => x.ExecuteAsync(executionContext))
                    .WasCalledExactlyOnce();
            }
        }

        [Fact]
        public async Task execute_async_skips_actions_that_are_shorter_than_the_skip_ahead_even_if_the_context_is_paused()
        {
            var action1 = new ActionMock(MockBehavior.Loose);
            var action2 = new ActionMock(MockBehavior.Loose);
            var action3 = new ActionMock(MockBehavior.Loose);
            var eventMatcher1 = new EventMatcherMock();
            var eventMatcher2 = new EventMatcherMock();
            var eventMatcher3 = new EventMatcherMock();

            action1
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(10));

            action2
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(3));

            action3
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(1));

            action1
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Throw();

            action2
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Throw();

            eventMatcher1
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            eventMatcher2
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            eventMatcher3
                .When(x => x.Matches(It.IsAny<IEvent>()))
                .Return((IEvent @event) => @event is BeforeExerciseEvent);

            var sut = new ExerciseBuilder()
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher1, action1))
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher2, action2))
                .AddMatcherWithAction(new MatcherWithAction(eventMatcher3, action3))
                .Build();

            using (var executionContext = new ExecutionContext(TimeSpan.FromSeconds(13)))
            {
                executionContext.IsPaused = true;
                await sut.ExecuteAsync(executionContext);

                action3
                    .Verify(x => x.ExecuteAsync(executionContext))
                    .WasCalledExactlyOnce();
            }
        }

        [Fact]
        public async Task execute_async_updates_the_current_exercise_in_the_context()
        {
            var sut = new ExerciseBuilder().Build();
            var context = new ExecutionContext();

            await sut.ExecuteAsync(context);

            Assert.Same(sut, context.CurrentExercise);
        }

        [Fact]
        public async Task execute_async_updates_the_current_set_in_the_context()
        {
            var sut = new ExerciseBuilder()
                .WithSetCount(3)
                .Build();
            var context = new ExecutionContext();
            var currentSetsTask = context
                .ObservableForProperty(x => x.CurrentSet)
                .Select(x => x.Value)
                .Take(3)
                .ToListAsync()
                .ToTask();

            await sut.ExecuteAsync(context);
            var currentSets = await currentSetsTask;

            Assert.Equal(3, currentSets.Count);
            Assert.Equal(1, currentSets[0]);
            Assert.Equal(2, currentSets[1]);
            Assert.Equal(3, currentSets[2]);
        }

        [Fact]
        public async Task execute_async_updates_the_current_repetitions_in_the_context()
        {
            var sut = new ExerciseBuilder()
                .WithRepetitionCount(5)
                .Build();
            var context = new ExecutionContext();
            var currentRepetitionsTask = context
                .ObservableForProperty(x => x.CurrentRepetition)
                .Select(x => x.Value)
                .Take(5)
                .ToListAsync()
                .ToTask();

            await sut.ExecuteAsync(context);
            var currentRepetitions = await currentRepetitionsTask;

            Assert.Equal(5, currentRepetitions.Count);
            Assert.Equal(1, currentRepetitions[0]);
            Assert.Equal(2, currentRepetitions[1]);
            Assert.Equal(3, currentRepetitions[2]);
            Assert.Equal(4, currentRepetitions[3]);
            Assert.Equal(5, currentRepetitions[4]);
        }

        [Fact]
        public async Task execute_async_says_exercise_name_first()
        {
            var speechService = new SpeechServiceMock(MockBehavior.Loose);
            var sut = new ExerciseBuilder()
                .WithName("some name")
                .WithSpeechService(speechService)
                .Build();

            await sut.ExecuteAsync(new ExecutionContext());

            speechService
                .Verify(x => x.SpeakAsync("some name", It.IsAny<CancellationToken>()))
                .WasCalledExactlyOnce();
        }
    }
}