using System;
using System.Linq;
using Xunit;
using Xunit.Extensions;


namespace CommandLineTool.Tests
{
    public class CommandLineToolTests
    {
        public enum TestEnum 
        {
            FirstValue,
            SecondValue,
            LastValue
        }

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
            public static void CommandWithEnumParameters(TestEnum argument1)
            {
                LastResult = String.Format("CommandWithEnumParameters argument1={0}", argument1);
            }

            public static void CommandWithOtherDataTypes(bool boolParam = false, bool? nullableBoolParam=null, int intParam=0)
            {
                LastResult = String.Format("CommandWithOtherDataTypes boolParam={0} nullableBoolParam={1} intParam={2}",
                    boolParam,
                    nullableBoolParam, 
                    intParam);
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

            // TODO BUGBUG: we don't let you explicitly specify required arguments
            Assert.Throws<InvalidOperationException>(() =>
            {
                clt.InvokeCommandLine(StringToArgs("CommandWithOneArgument -argument1 HelloWorld"));
                Assert.Equal<string>("CommandWithOneArgument argument1=HelloWorld", TestCommandsImplementation.LastResult);
            });

            // but we do let you list them manually
            clt.InvokeCommandLine(StringToArgs("CommandWithOneArgument HelloWorld"));
            Assert.Equal<string>("CommandWithOneArgument argument1=HelloWorld", TestCommandsImplementation.LastResult);

            // TODO: extra parameters should throw
            clt.InvokeCommandLine(StringToArgs("CommandWithOneArgument HelloWorld -extraParams"));
            Assert.Equal<string>("CommandWithOneArgument argument1=HelloWorld", TestCommandsImplementation.LastResult);

            // todo: better error handling when we can't match a switch
            //clt.InvokeCommandLine(StringToArgs("CommandWithOneArgument HelloWorld extraParams"));
            //Assert.Equal<string>("CommandWithOneArgument argument1=HelloWorld", TestCommandsImplementation.LastResult);


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

        [Theory,
            // all possible values work
        InlineData("CommandWithEnumParameters FirstValue", "CommandWithEnumParameters argument1=FirstValue"),
            // case insensitive
        InlineData("CommandWithEnumParameters SECONDVALUE", "CommandWithEnumParameters argument1=SecondValue"),
        InlineData("CommandWithEnumParameters lastValue", "CommandWithEnumParameters argument1=LastValue"),
        ]
        public void CanUseEnums(string input, string expectedOutput)
        {
            var clt = GetCommandLineTool();

            clt.InvokeCommandLine(StringToArgs(input));
            Assert.Equal<string>(expectedOutput, TestCommandsImplementation.LastResult);
        }

        [Fact]
        public void EnumsAreParsed()
        {
            var clt = GetCommandLineTool();


            var ex = Assert.Throws<ArgumentException>(() =>
            {
                clt.InvokeCommandLine(StringToArgs("CommandWithEnumParameters BogusValue"));
            });

            // and we print out all the actual values
            var values = Enum.GetNames(typeof(TestEnum));
            
            foreach (var value in values)
            {
                Assert.Contains(value, ex.Message);
            }
        }

        [Theory,
        // each one can handle default values
        InlineData("CommandWithOtherDataTypes", "CommandWithOtherDataTypes boolParam=False nullableBoolParam= intParam=0"),
        // each type by itself
        InlineData("CommandWithOtherDataTypes -nullableBoolParam true", "CommandWithOtherDataTypes boolParam=False nullableBoolParam=True intParam=0"),
        InlineData("CommandWithOtherDataTypes -boolParam true", "CommandWithOtherDataTypes boolParam=True nullableBoolParam= intParam=0"),
        InlineData("CommandWithOtherDataTypes -intParam 999999", "CommandWithOtherDataTypes boolParam=False nullableBoolParam= intParam=999999"),
        // TODO: support hex
        //InlineData("CommandWithOtherDataTypes -intParam 0xFF", "CommandWithOtherDataTypes boolParam=False nullableBoolParam= intParam=255"),
        // Negative numbers
        InlineData("CommandWithOtherDataTypes -intParam -3", "CommandWithOtherDataTypes boolParam=False nullableBoolParam= intParam=-3"),
        ]
        public void CanUsePrimitiveTypes(string input, string expectedOutput)
        {
            var clt = GetCommandLineTool();
            clt.InvokeCommandLine(StringToArgs(input));
            Assert.Equal<string>(expectedOutput, TestCommandsImplementation.LastResult);
            // todo: error on bogus switch
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
