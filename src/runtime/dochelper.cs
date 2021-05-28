using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Python.Runtime
{
    internal static class dochelper
    {
        /// <summary>
        /// Create a docstring for the <paramref name="methods"/>.
        /// If a DocString attribute is present, it is returned.
        /// Otherwise, a signature is constructed in a similar syntax as Python type hints.
        /// </summary>
        internal static string GetDocString(MethodBase[] methods)
        {
            var strBuilder = new StringBuilder();
            Type marker = typeof(DocStringAttribute);
            foreach (MethodBase method in methods)
            {
                if (strBuilder.Length > 0)
                {
                    strBuilder.AppendLine();
                }

                var attrs = (Attribute[])method.GetCustomAttributes(marker, false);
                if (attrs.Length == 0)
                {
                    AppendMethodSignature(strBuilder, method);
                }
                else
                {
                    var attr = (DocStringAttribute)attrs[0];
                    strBuilder.Append(attr.DocString);
                }
            }

            return strBuilder.ToString();
        }

        /// <summary>
        /// Create a method signature for the <paramref name="method"/> in a similar syntax as Python type hints.
        /// </summary>
        /// <remarks>
        /// Examples:
        ///
        /// C#: public static double Add(double a, double b)
        /// Python: Add(a: Double, b: Double) -> Double
        ///
        /// C#: public static int Add(int a, int b)
        /// Python:Add(a: Int32, b: Int32) -> Int32
        ///
        /// C#: public static int Add(int a, int b, out int c)
        /// Python: Add(a: Int32, b: Int32) -> (Int32, Int32)
        /// </remarks>
        private static void AppendMethodSignature(StringBuilder strBuilder, MethodBase method)
        {
            strBuilder.Append($"{(method.IsConstructor ? method.DeclaringType.Name : method.Name)}(");

            // Add parameters. Only consider non-"out" parameters.
            var parameters = method.GetParameters().Where(x => !x.IsOut).OrderBy(x => x.Position);
            strBuilder.Append(string.Join(", ", parameters.Select(p => CreateParameterSignature(p))));
            strBuilder.Append(")");

            // Add return types.
            if (method is MethodInfo methodInfo)
            {
                strBuilder.Append($" -> ");

                var outParameters = method.GetParameters().Where(x => x.IsOut).OrderBy(x => x.Position);
                if (outParameters.Any())
                {
                    strBuilder.Append("(");

                    if (methodInfo.ReturnType != Type.GetType("System.Void"))
                    {
                        strBuilder.Append(methodInfo.ReturnType.Name + ", ");
                    }

                    strBuilder.Append(string.Join(", ", outParameters.Select(p => p.ParameterType.Name)));

                    strBuilder.Append(")");
                }
                else
                {
                    strBuilder.Append(methodInfo.ReturnType.Name);
                }
            }
        }

        private static string CreateParameterSignature(ParameterInfo parameterInfo)
        {
            string signature = $"{parameterInfo.Name}: {parameterInfo.ParameterType.Name}";

            if (parameterInfo.HasDefaultValue)
            {
                signature += $" = {parameterInfo.DefaultValue}";
            }

            return signature;
        }
    }
}
