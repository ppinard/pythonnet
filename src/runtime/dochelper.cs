using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Python.Runtime
{
    internal static class dochelper
    {
        internal static string GetMethodSignatures(MethodBase[] methods)
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
                    var parameters = method.GetParameters().Where(x => !x.IsOut).OrderBy(x => x.Position);

                    strBuilder.Append($"{(method.IsConstructor ? method.DeclaringType.Name : method.Name)}(");
                    strBuilder.Append(string.Join(", ", parameters.Select(p => CreateParameterInfoSignature(p))));
                    strBuilder.Append(")");

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
                else
                {
                    var attr = (DocStringAttribute)attrs[0];
                    strBuilder.Append(attr.DocString);
                }
            }

            return strBuilder.ToString();
        }

        private static string CreateParameterInfoSignature(ParameterInfo parameterInfo)
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
