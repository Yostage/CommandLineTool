using System;
using Xunit;


namespace CommandLineTool.Tests
{
    public class CommandLineToolTests
    {
        public static class TestCommandsImplementation
        {
            public static bool CommandWithNoParametersExecuted { get; set; }
            public static void CommandWithNoParameters()
            {
                CommandWithNoParametersExecuted = true;
            }
        }

        CommandLineTool GetCommandLineTool()
        {
            var clt = new CommandLineTool();
            clt.CommandClass = typeof(TestCommandsImplementation);
            return clt;
        }

        string[] StringToArgs(string commandLine)
        {
            return commandLine.Split(' ');
        }

        [Fact]
        public void CanExecuteCommandsWithNoArguments()
        {
            var clt = GetCommandLineTool();

            Assert.False(TestCommandsImplementation.CommandWithNoParametersExecuted);
            clt.InvokeCommandLine(StringToArgs("CommandWithNoParameters"));
            Assert.True(TestCommandsImplementation.CommandWithNoParametersExecuted);
        }

        [Fact]
        public void InvokeThrowsWithNoArguments()
        {
            var clt = GetCommandLineTool();

            Assert.Throws<NullReferenceException>(() =>
            {
                clt.InvokeCommandLine(null);
            });

            // empty args is also no good
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                clt.InvokeCommandLine(new string[0]);
            });
        }

    }
}
