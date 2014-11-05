using NUnit.Framework;
using System;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Pash.Implementation;
using System.Text;
using TestHost.TestHelpers;

namespace ReferenceTests.Language
{
    [TestFixture]
    public class ErrorTests : ReferenceTestBase
    {
        [Test]
        public void ParseErrorAppearsInStreamAndErrorVar()
        {
            Assert.Throws<ParseException>(delegate {
                ReferenceHost.Execute(@"""foo"" |; ");
            });
            var errorVarVal = ReferenceHost.GetVariableValue("error") as Collection<ErrorRecord>;
            Assert.NotNull(errorVarVal, "Error var not set or not a collection of error records");
            errorVarVal.ShouldNotBeEmpty();
            Assert.True(errorVarVal[0].Exception is ParseException);
         }

        [Test]
        public void ErrorVarCanBeCleared()
        {
            // incomplete pipe: parse error
            Assert.Throws<ParseException>(delegate {
                ReferenceHost.Execute(@"""foo"" |; ");
            });
            var errorVarVal = ReferenceHost.GetVariableValue("error") as Collection<ErrorRecord>;
            Assert.NotNull(errorVarVal, "Error var not set or not a collection of error records");
            errorVarVal.ShouldNotBeEmpty();

            ReferenceHost.RawExecuteInLastRunspace("$error.Clear()");
            errorVarVal = ReferenceHost.GetVariableValue("error") as Collection<ErrorRecord>;
            Assert.NotNull(errorVarVal, "Error var not set or not a collection of error records");
            errorVarVal.ShouldBeEmpty();
        }

        [Test]
        public void CreateErrorCmdletCanThrowTerminating()
        {
            var e = Assert.Throws<CmdletInvocationException>(delegate {
                ReferenceHost.Execute("Test-CreateError -Terminating");
            });
            Assert.That(e.InnerException, Is.InstanceOf<TestException>());
        }

        [Test]
        public void CreateErrorCmdletCanWriteError()
        {
            ReferenceHost.RawExecute("Test-CreateError", false);
            var errors = ReferenceHost.GetLastRawErrorRecords();
            Assert.That(errors.Length, Is.EqualTo(1));
            Assert.That(errors[0].Exception, Is.InstanceOf<TestException>());
        }

        [Test]
        public void CreateErrorCmdletCanWriteObject()
        {
            ExecuteAndCompareTypedResult("'test' | Test-CreateError -NoError", "test");
        }

        [TestCase("begin", new string[] {"begin"})]
        [TestCase("process", new string[] {"begin", "process"})]
        [TestCase("end", new string[] {"begin", "process", "foo", "end"})]
        public void ThrowTerminatingBreaksPipelineInCorrectPhase(string phase, string[] expectedParts)
        {
            var cmd = "'foo' | Test-CreateError -Terminating -Phase " + phase;
            var ex = Assert.Throws<CmdletInvocationException>(delegate {
                ReferenceHost.Execute(cmd);
            });
            Assert.That(ex.InnerException, Is.InstanceOf<TestException>());
            var expected = NewlineJoin(expectedParts);
            Assert.AreEqual(expected, ReferenceHost.LastResults);
        }

        [TestCase("begin")]
        [TestCase("process")]
        [TestCase("end")]
        public void WriteErrorDoesNotBreakPipeline(string phase)
        {
            var cmd = "'foo' | Test-CreateError -Phase " + phase + " | Test-CreateError -NoError";
            var output = ReferenceHost.Execute(cmd, false);
            var expected = NewlineJoin("begin", "process", "foo", "end");
            Assert.AreEqual(expected, output);
            var errors = ReferenceHost.GetLastRawErrorRecords();
            Assert.That(errors.Length, Is.EqualTo(1));
            Assert.That(errors[0].Exception, Is.InstanceOf<TestException>());
        }
    }
}

