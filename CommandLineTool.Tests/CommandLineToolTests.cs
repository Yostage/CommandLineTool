﻿using System;
using Xunit;
using Xunit.Extensions;


namespace CommandLineTool.Tests
{
    public class CommandLineToolTests
    {
        public static class TestCommandsImplementation
        {
            public static string LastResult { get; set; }
            public static void CommandWithNoParameters()
            {
                LastResult = "CommandWithNoParametersExecuted";
            }

            public static void CommandWithOneArgument(string argument1)
            {
                LastResult = String.Format("CommandWithOneArgument argument1={0}", argument1);
            }

            public static void CommandWithOneOptionalArgument(string argument1 = "defaultValue")
            {
                LastResult = String.Format("CommandWithOneOptionalArgument argument1={0}", argument1);
            }

            [Abbreviation("cwa")]
            public static void CommandWithAbbreviation(string argument1 = "defaultValue")
            {
                LastResult = String.Format("CommandWithAbbreviation argument1={0}", argument1);
            }

        }

        CommandLineTool GetCommandLineTool()
        {
            // clear our results storage
            TestCommandsImplementation.LastResult = null;
            
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

            Assert.NotEqual<string>("CommandWithNoParametersExecuted", TestCommandsImplementation.LastResult);
            clt.InvokeCommandLine(StringToArgs("CommandWithNoParameters"));
            Assert.Equal<string>("CommandWithNoParametersExecuted", TestCommandsImplementation.LastResult);
        }

        [Fact]
        public void CanExecuteCommandsWithOneImplicitArgument()
        {
            var clt = GetCommandLineTool();

            clt.InvokeCommandLine(StringToArgs("CommandWithOneArgument HelloWorld"));
            Assert.Equal<string>("CommandWithOneArgument argument1=HelloWorld", TestCommandsImplementation.LastResult);
        }

        [Fact]
        public void CanExecuteCommandsWithOneExplicitArgument()
        {
            var clt = GetCommandLineTool();

            // BUGBUG: we don't let you explicitly specify required arguments
            Assert.Throws<InvalidOperationException>(() =>
            {
                clt.InvokeCommandLine(StringToArgs("CommandWithOneArgument -argument1 HelloWorld"));
                Assert.Equal<string>("CommandWithOneArgument argument1=HelloWorld", TestCommandsImplementation.LastResult);
            });

            // but we do let you list them manually
            clt.InvokeCommandLine(StringToArgs("CommandWithOneArgument HelloWorld"));
            Assert.Equal<string>("CommandWithOneArgument argument1=HelloWorld", TestCommandsImplementation.LastResult);

        }

        [Fact]
        public void CanOverrideOptionalParameters()
        {
            var clt = GetCommandLineTool();

            clt.InvokeCommandLine(StringToArgs("CommandWithOneOptionalArgument ActualValue"));
            Assert.Equal<string>("CommandWithOneOptionalArgument argument1=ActualValue", TestCommandsImplementation.LastResult);
        }

        [Fact]
        public void CanSkipOptionalParameters()
        {
            var clt = GetCommandLineTool();

            clt.InvokeCommandLine(StringToArgs("CommandWithOneOptionalArgument"));
            Assert.Equal<string>("CommandWithOneOptionalArgument argument1=defaultValue", TestCommandsImplementation.LastResult);
        }

        [Theory,
        InlineData("CommandWithAbbreviation", "CommandWithAbbreviation argument1=defaultValue"),
        // can drop in the abbreviation instead
        InlineData("cwa", "CommandWithAbbreviation argument1=defaultValue"),
        // parameters still work
        InlineData("CommandWithAbbreviation parameter1", "CommandWithAbbreviation argument1=parameter1"),
        InlineData("cwa parameter1", "CommandWithAbbreviation argument1=parameter1"),
        ]
        public void CanAbbreviateParameters(string input, string expectedOutput)
        {
            var clt = GetCommandLineTool();

            clt.InvokeCommandLine(StringToArgs(input));
            Assert.Equal<string>(expectedOutput, TestCommandsImplementation.LastResult);
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
