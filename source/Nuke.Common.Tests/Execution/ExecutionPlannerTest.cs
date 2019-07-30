// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nuke.Common.Execution;
using Xunit;

namespace Nuke.Common.Tests.Execution
{
    public class ExecutionPlannerTest
    {
        private ExecutableTarget A = new ExecutableTarget { Name = nameof(A), IsDefault = true };
        private ExecutableTarget B = new ExecutableTarget { Name = nameof(B) };
        private ExecutableTarget C = new ExecutableTarget { Name = nameof(C) };
        private ExecutableTarget DEF = new ExecutableTarget { Name = nameof(DEF) };

        [Fact]
        public void TestDefault()
        {
            GetPlan().Should().BeEquivalentTo(A);
        }

        [Fact]
        public void TestInvoked()
        {
            GetPlan(invokedTargets: new[] { B }).Should().Equal(B);
            A.Invoked.Should().BeFalse();
            B.Invoked.Should().BeTrue();
            C.Invoked.Should().BeFalse();

            GetPlan(invokedTargets: new[] { A, B }).Should().BeEquivalentTo(A, B);
            A.Invoked.Should().BeTrue();
            B.Invoked.Should().BeTrue();
            C.Invoked.Should().BeFalse();
        }

        [Fact]
        public void TestExecutionDependencies()
        {
            A.ExecutionDependencies.Add(B);
            GetPlan().Should().Equal(B, A);

            B.ExecutionDependencies.Add(C);
            GetPlan().Should().Equal(C, B, A);
        }

        [Fact]
        public void TestTriggerDependencies()
        {
            AddTrigger(A, B);
            GetPlan().Should().Equal(A, B);

            AddTrigger(B, C);
            GetPlan().Should().Equal(A, B, C);
        }

        [Fact]
        public void TestOrderDependencies()
        {
            B.OrderDependencies.Add(A);
            C.OrderDependencies.Add(A);
            GetPlan(invokedTargets: new[] { B, A, C }).First().Should().Be(A);

            C.OrderDependencies.Add(B);
            GetPlan(invokedTargets: new[] { A, C, B }).Should().Equal(A, B, C);
        }

        [Fact]
        public void TestMixedDependencies()
        {
            A.ExecutionDependencies.Add(B);
            AddTrigger(B, C);
            C.OrderDependencies.Add(A);
            GetPlan().Should().Equal(B, A, C);
        }

        [Theory]
        [InlineData("def")]
        [InlineData("DEF")]
        [InlineData("dEf")]
        [InlineData("d-ef")]
        [InlineData("d-e-f")]
        public void TestTargetNameVariations(string invokedTargetName)
        {
            var plan = ExecutionPlanner.GetExecutionPlan(new[] { A, B, C, DEF }, new[] { invokedTargetName });

            plan.Should().Equal(DEF);
        }

        private IEnumerable<ExecutableTarget> GetPlan(ExecutableTarget[] invokedTargets = null)
        {
            string[] SelectNames(ExecutableTarget[] targets) => targets?.Select(x => x.Name).ToArray();

            return ExecutionPlanner.GetExecutionPlan(new[] { A, B, C }, SelectNames(invokedTargets));
        }

        private void AddTrigger(ExecutableTarget source, ExecutableTarget target)
        {
            source.Triggers.Add(target);
            target.TriggerDependencies.Add(source);
        }
    }
}
