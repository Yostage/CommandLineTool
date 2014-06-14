using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScidCruncher
{
    /// <summary>
    /// Command line tool
    /// </summary>
    public class CommandLineTool
    {
        class ParameterAndValue
        {
            public ParameterInfo ParameterInfo { get; set; }
            public object Value { get; set; }
        }

        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// dump out usage information 
        /// </summary>
        public void Usage()
        {
            Console.WriteLine(Name);
            Console.WriteLine("General usage: {0} <command> [parameters]", Name);
            Console.WriteLine("You can either specify the full command name or the alias (shown in parentheses)");
            Console.WriteLine("Command listing:");
            PrintCommands();
            var enumTypes = EnumerateCommands().SelectMany(mi => mi.GetParameters()).Where(param => param.ParameterType.IsEnum).Select(e => e.ParameterType).Distinct().ToList();
            foreach (var et in enumTypes)
            {
                Console.WriteLine(GetValidEnumTypesString(et));
            }
        }

        internal string GetValidEnumTypesString(Type enumType)
        {
            return String.Format(CultureInfo.CurrentCulture, "Valid {0} are {1}", enumType.Name, String.Join(", ", Enum.GetNames(enumType)));
        }

        private string GetParametersString(MethodInfo mi)
        {
            var parameters = mi.GetParameters();
            List<string> parameterStrings = new List<string>();
            foreach (var param in parameters)
            {
                if (param.IsOptional)
                {
                    if (param.DefaultValue == null)
                    {
                        parameterStrings.Add(String.Format(CultureInfo.CurrentCulture, "[{0}:{1}]", param.ParameterType.Name, param.Name));
                    }
                    else
                    {
                        parameterStrings.Add(String.Format(CultureInfo.CurrentCulture, "[{0}:{1} = {2}]", param.ParameterType.Name, param.Name, param.DefaultValue));
                    }
                }
                else
                {
                    parameterStrings.Add(String.Format(CultureInfo.CurrentCulture, "<{0}:{1}>", param.ParameterType.Name, param.Name));
                }
            }
            return String.Join(" ", parameterStrings);
        }

        private string GetDocumentationString(MethodInfo mi)
        {
            object[] attrs = mi.GetCustomAttributes(typeof(DocumentationAttribute), false);
            string inlineDoc;

            if (attrs.Length == 1)
            {
                DocumentationAttribute da = attrs[0] as DocumentationAttribute;
                inlineDoc = da.Documentation;

            }
            else
            {
                inlineDoc = "[No inline documentation]";
            }

            return String.Format(CultureInfo.CurrentCulture, "{0} - {1}", GetParametersString(mi), inlineDoc);
        }

        private string GetAbbreviationString(MethodInfo mi)
        {
            object[] attrs = mi.GetCustomAttributes(typeof(AbbreviationAttribute), false);
            string inlineAbbreviation = null;

            if (attrs.Length == 1)
            {
                AbbreviationAttribute da = attrs[0] as AbbreviationAttribute;
                inlineAbbreviation = da.Abbreviation;
            }

            return inlineAbbreviation;
        }

        /// <summary>
        /// Reflect a method out of Commands and print out the attached
        /// documentation string from it
        /// </summary>
        /// <param name="methodName"></param>
        public void PrintDocumentationString(MethodInfo mi)
        {
            string abbreviation = GetAbbreviationString(mi);
            abbreviation = string.IsNullOrEmpty(abbreviation) ? "" : String.Format(CultureInfo.CurrentCulture, " ({0})", abbreviation);
            Console.WriteLine("    {0}{1}: {2}", mi.Name, abbreviation, GetDocumentationString(mi));
        }

        private void PrintCommands()
        {
            foreach (var command in EnumerateCommands())
            {
                PrintDocumentationString(command);
            }
        }

        public Type CommandClass { get; set; }

        private IEnumerable<MethodInfo> EnumerateCommands()
        {
            MethodInfo[] methods = CommandClass.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (MethodInfo mi in methods.OrderBy(m => m.Name))
            {
                yield return mi;
            }
        }

        private Object TranslateParameter(ParameterInfo pi, string parameterValue)
        {
            if (pi.ParameterType.IsEnum)
            {
                try
                {
                    return Enum.Parse(pi.ParameterType, parameterValue, true);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "{0} is not valid.  {1}", parameterValue, GetValidEnumTypesString(pi.ParameterType)));
                }
            }
            else if (pi.ParameterType == typeof(Boolean))
            {
                return Boolean.Parse(parameterValue);
            }
            else if (pi.ParameterType == typeof(Nullable<Boolean>))
            {
                Nullable<Boolean> ret = null;
                if (parameterValue != null)
                {
                    ret = Boolean.Parse(parameterValue);
                }
                return ret;
            }
            else if (pi.ParameterType == typeof(int))
            {
                return Int32.Parse(parameterValue);
            }

            return parameterValue;
        }

        private bool InvokeCommand(string commandName, List<string> args)
        {
            MethodInfo[] methods = CommandClass.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (MethodInfo mi in methods)
            {
                // if we find the method
                if (String.Equals(commandName, mi.Name, StringComparison.InvariantCultureIgnoreCase) ||
                    String.Equals(commandName, GetAbbreviationString(mi), StringComparison.InvariantCultureIgnoreCase))
                {
                    var parameters = new List<ParameterAndValue>();
                    var methodParameters = mi.GetParameters();
                    foreach (var pi in methodParameters)
                    {
                        parameters.Add(new ParameterAndValue { ParameterInfo = pi, Value = null });
                    }

                    // Make sure that every required parameter has a matching argument
                    for (int i = 0; i < methodParameters.Length; i++)
                    {
                        var pi = methodParameters[i];
                        if (!pi.IsOptional)
                        {
                            if (args.Count == 0)
                            {
                                Console.WriteLine("Missing required parameter \"{0}\"", pi.Name);
                                PrintDocumentationString(mi);
                                return false;
                            }
                            else
                            {
                                parameters[i].Value = TranslateParameter(pi, args[0]);
                                args.RemoveAt(0);
                            }
                        }
                    }

                    // For each remaining argument, match it to one of the remaining optional parameters
                    for (int i = 0; i < args.Count; i++)
                    {
                        // Argument is explicitly specified with parameter name
                        if (args[i].StartsWith("-", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var matchingParameters = methodParameters.Where(p => p.IsOptional).Where(p => p.Name.StartsWith(args[i].Substring(1), StringComparison.InvariantCultureIgnoreCase)).ToList();
                            if (matchingParameters.Count == 0)
                            {
                                Console.WriteLine("Argument specifier \"{0}\" doesn't match any parameters", args[i]);
                                PrintDocumentationString(mi);
                                return false;
                            }
                            if (matchingParameters.Count > 1)
                            {
                                Console.WriteLine("Argument specifier \"{0}\" is too ambiguous, it matches multiple parameters", args[i]);
                                PrintDocumentationString(mi);
                                return false;
                            }
                            else
                            {
                                var matchingParameter = matchingParameters.Single();

                                // If there's only one argument left
                                bool isLastArg = i == (args.Count - 1);

                                if (matchingParameter.ParameterType == typeof(Boolean))
                                {
                                    bool throwaway;

                                    // If this is the last argument, OR the next argument isn't a bool...
                                    if (isLastArg || !Boolean.TryParse(args[i + 1], out throwaway))
                                    {
                                        // The current parameter is a boolean, and there's no argument value specified, we treat the arg as a switch, and set the parameter to true (example: "-verbose")
                                        parameters.Where(pv => pv.ParameterInfo.Name == matchingParameter.Name).Single().Value = true;
                                        break;
                                    }
                                }

                                if (isLastArg)
                                {
                                    // If the current parameter is NOT a boolean, and there's no argument value specified, we have a bad command line (example: "-AppContainer")
                                    Console.WriteLine("Argument specifier \"{0}\" is missing a value", args[i]);
                                    PrintDocumentationString(mi);
                                    return false;
                                }
                                else
                                {
                                    // Otherwise, we have an argument and an argument value, assign argument value to parameter (example: "-verbose true")
                                    parameters.Where(pv => pv.ParameterInfo.Name == matchingParameter.Name).Single().Value = TranslateParameter(matchingParameter, args[++i]);
                                }
                            }
                        }
                        else
                        {
                            // Argument is positionally specified
                            var parameterAndValue = parameters.First(pv => pv.Value == null);
                            parameterAndValue.Value = TranslateParameter(parameterAndValue.ParameterInfo, args[i]);
                        }
                    }

                    // For any remaining optional parameters that haven't been specified, use their default value
                    foreach (var pv in parameters)
                    {
                        if (pv.Value == null)
                        {
                            pv.Value = pv.ParameterInfo.DefaultValue;
                        }
                    }

                    // Display all parameters and arguments
                    Console.WriteLine("Parameters: " + String.Join((", "), parameters.Select(p => String.Format("{0}: {1}", p.ParameterInfo.Name, p.Value))));

                    // call the method and quit
                    mi.Invoke(null, parameters.Select(pv => pv.Value).ToArray());
                    return true;
                }
            }

            Console.WriteLine("No method {0} found", commandName);
            Usage();
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public int Main(string[] args)
        {

            if (args.Length == 0)
            {
                Usage();
                return 1;
            }

            // Allow another level of quoting for args with spaces in them (sac foo.exe "/a:\"foo bar\" /b")
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Replace("\\\"", "\"");
            }

            string command = args[0];
            bool invokeResult = false;

            // open Command
            // reflect its methods
            // call the methods
            try
            {
                invokeResult = InvokeCommand(command, args.Skip(1).ToList());
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException;
                // Don't let exceptions get to the GUI.
                Console.WriteLine(inner.GetType() + ": " + inner.Message);
                Console.WriteLine(inner.StackTrace);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType() + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
            }


            Console.WriteLine("(done)");
            return invokeResult ? 0 : 1;
        }
    }

}
