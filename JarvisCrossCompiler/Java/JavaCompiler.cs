using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JCC.Java
{
    internal class JavaCompiler : Compiler
    {
        private string package;
        private readonly List<JavaClassData> classes;
        private readonly List<JavaEnumData> enums;
        private readonly TypeMap typeMap;
        private readonly ImportBuilder imports;
        private readonly Dictionary<string, int> createdInstCounter;

        public JavaCompiler(string srcPath, bool enforceConventions) :
            base(srcPath, CSharpParseOptions.Default, enforceConventions)
        {
            package = string.Empty;
            classes = new List<JavaClassData>();
            enums = new List<JavaEnumData>();
            createdInstCounter = new Dictionary<string, int>();

            typeMap = new TypeMap(new TypeMap.NonExistProp("void", "var"),
                ("string", "String"),
                ("bool", "Boolean"),
                ("int", "Integer"),
                ("float", "Float"),
                ("double", "Double"),
                ("Task", "!"),
                ("List", "ArrayList"),
                ("IEnumerable", "Iterable"));
            imports = new ImportBuilder(
                ("ArrayList", "java.util.ArrayList"),
                ("IStart", "Jarvis.API"),
                ("IStop", "Jarvis.API"),
                ("IWebUpdate", "Jarvis.API"),
                ("IUpdate", "Jarvis.API"),
                ("JarvisRequest", "Jarvis.API"),
                ("JarvisResponse", "Jarvis.API"),
                ("PrintWriter", "java.io.PrintWriter"),
                ("FileWriter", "java.io.FileWriter"));
        }

        public override void Run()
        {
            base.Run();
            SyntaxNode outline = root;
            if (root.FindChild(SyntaxKind.NamespaceDeclaration) is NamespaceDeclarationSyntax namespaceDec)
            {
                package = "package " + namespaceDec.Name + ";";
                outline = namespaceDec;
            }

            foreach (SyntaxNode packageChild in outline.ChildNodes())
            {
                if (packageChild.IsKind(SyntaxKind.ClassDeclaration))
                {
                    ClassDeclarationSyntax classSyn = (ClassDeclarationSyntax)packageChild;
                    string className = classSyn.Identifier.Text;
                    AddClass(classSyn);

                    foreach (SyntaxNode classChild in classSyn.ChildNodes())
                    {
                        if (classChild.IsKind(SyntaxKind.MethodDeclaration))
                        {
                            MethodDeclarationSyntax methodSyn = (MethodDeclarationSyntax)classChild;
                            AddMethod(className, methodSyn);
                        }
                        else if (classChild.IsKind(SyntaxKind.FieldDeclaration))
                        {
                            FieldDeclarationSyntax fieldSyn = (FieldDeclarationSyntax)classChild;
                            VariableDeclarationSyntax? varSyn =
                                fieldSyn.FindChild(SyntaxKind.VariableDeclaration) as VariableDeclarationSyntax;
                            AddField(className, fieldSyn, varSyn);
                        }
                        else if (classChild.IsKind(SyntaxKind.EnumDeclaration))
                        {
                            EnumDeclarationSyntax enumSyn = (EnumDeclarationSyntax)classChild;
                            AddEnum(className, enumSyn);
                        }
                        else if (classChild.IsKind(SyntaxKind.PropertyDeclaration))
                        {
                            PropertyDeclarationSyntax propSyn = (PropertyDeclarationSyntax)classChild;
                            AccessorDeclarationSyntax? getter = null, setter = null;
                            bool lambda = false;

                            if (propSyn.FindChild(SyntaxKind.AccessorList) is AccessorListSyntax propAccessorList)
                            {
                                foreach (SyntaxNode propAccessorChild in propAccessorList.ChildNodes())
                                {
                                    if (propAccessorChild.IsKind(SyntaxKind.GetAccessorDeclaration))
                                        getter = (AccessorDeclarationSyntax)propAccessorChild;
                                    else if (propAccessorChild.IsKind(SyntaxKind.SetAccessorDeclaration))
                                        setter = (AccessorDeclarationSyntax)propAccessorChild;
                                }
                            }
                            else if (propSyn.FindChild(SyntaxKind.ArrowExpressionClause) != null) lambda = true;
                            AddProperty(className, propSyn, getter, setter, lambda);
                        }
                    }

                }
                else if (packageChild.IsKind(SyntaxKind.EnumDeclaration))
                {
                    EnumDeclarationSyntax enumSyn = (EnumDeclarationSyntax)packageChild;
                    AddEnum(string.Empty, enumSyn);
                }
            }
            BuildOutput();
            Finish();
        }

        private void BuildOutput()
        {
            OutputText += package + newL;
            string[] builtImports = imports.GetImports();
            if (builtImports.Length > 0)
            {
                OutputText += newL;
                for (int i = 0; i < builtImports.Length; i++)
                    OutputText += "import " + builtImports[i] + ";" + newL;
            }

            for (int e = 0; e < enums.Count; e++)
            {
                JavaEnumData eData = enums[e];
                OutputText += newL;
                for (int k = 0; k < eData.mods.Count; k++)
                    OutputText += eData.mods[k] + " ";
                OutputText += "enum " + eData.enumName + " {" + newL + "\t";
                for (int v = 0; v < eData.enumVals.Count; v++)
                {
                    if (v != 0) OutputText += ", ";
                    OutputText += eData.enumVals[v];
                }
                OutputText += newL + "}" + newL;
            }

            for (int i = 0; i < classes.Count; i++)
            {
                JavaClassData cData = classes[i];
                OutputText += newL;
                for (int k = 0; k < cData.mods.Count; k++)
                    OutputText += cData.mods[k] + " ";
                if (cData.csMarkedStatic) OutputText += "final ";
                OutputText += "class " + cData.className;

                if (!string.IsNullOrEmpty(cData.inheritsFrom))
                    OutputText += " extends " + cData.inheritsFrom;
                if (cData.interfaces.Count > 0)
                {
                    OutputText += " implements ";
                    for (int n = 0; n < cData.interfaces.Count; n++)
                    {
                        if (n != 0) OutputText += ", ";
                        OutputText += cData.interfaces[n];
                    }
                }
                OutputText += " {" + newL;

                for (int e = 0; e < cData.enums.Count; e++)
                {
                    JavaEnumData eData = cData.enums[e];
                    OutputText += newL + "\t";
                    for (int k = 0; k < eData.mods.Count; k++)
                        OutputText += eData.mods[k] + " ";
                    OutputText += "enum " + eData.enumName + " {" + newL + "\t\t";
                    for (int v = 0; v < eData.enumVals.Count; v++)
                    {
                        if (v != 0) OutputText += ", ";
                        OutputText += eData.enumVals[v];
                    }
                    OutputText += newL + "\t}" + newL;
                }

                for (int f = 0; f < cData.fields.Count; f++)
                {
                    JavaFieldData fData = cData.fields[f];
                    OutputText += newL + "\t";
                    for (int k = 0; k < fData.mods.Count; k++)
                        OutputText += fData.mods[k] + " ";
                    OutputText += fData.typeName + " ";
                    for (int v = 0; v < fData.varNames.Length; v++)
                    {
                        if (v != 0) OutputText += ", ";
                        OutputText += fData.varNames[v];
                        if (!string.IsNullOrEmpty(fData.varVals[v]))
                            OutputText += " = " + fData.varVals[v];
                    }
                    OutputText += ";" + newL;
                }

                for (int p = 0; p < cData.properties.Count; p++)
                {
                    JavaPropertyData pData = cData.properties[p];
                    OutputText += newL + "\t";
                    for (int k = 0; k < pData.varMods.Count; k++)
                        OutputText += pData.varMods[k] + " ";
                    if (!string.IsNullOrEmpty(pData.initVal))
                        OutputText += pData.typeName + " " + pData.propName.CamelConvention() + ";" + newL;
                    else OutputText += pData.typeName + " " + pData.propName.CamelConvention() + ";" + newL;

                    if (pData.getter)
                    {
                        OutputText += newL + "\t";
                        for (int k = 0; k < pData.getMods.Count; k++)
                            OutputText += pData.getMods[k] + " ";
                        OutputText += pData.typeName + " get" + pData.propName + "() {" + newL;
                        OutputText += "\t\treturn this." + pData.propName.CamelConvention() + ";" + newL;
                        OutputText += "\t}" + newL;
                    }
                    if (pData.setter)
                    {
                        OutputText += newL + "\t";
                        for (int k = 0; k < pData.getMods.Count; k++)
                            OutputText += pData.getMods[k] + " ";
                        OutputText += pData.typeName + " set" + pData.propName +
                            "(" + pData.typeName + " " + pData.propName.CamelConvention() + ") {" + newL;
                        OutputText += "\t\tthis." + pData.propName.CamelConvention() + " = " +
                            pData.propName.CamelConvention() + ";" + newL;
                        OutputText += "\t}" + newL;
                    }
                }

                for (int m = 0; m < cData.methods.Count; m++)
                {
                    JavaMethodData mData = cData.methods[m];
                    OutputText += newL + "\t";
                    for (int k = 0; k < mData.mods.Count; k++)
                        OutputText += mData.mods[k] + " ";
                    OutputText += mData.typeName + " " + mData.methodName.CamelConvention() + "(";
                    for (int p = 0; p < mData.parameters.Count; p++)
                    {
                        if (p != 0) OutputText += ", ";
                        OutputText += mData.parameters[p].Item1 + " " + mData.parameters[p].Item2;
                    }

                    OutputText += ")";
                    if (mData.blockData != null) OutputText += mData.blockData?.blockText;
                    OutputText += newL;
                }

                OutputText += newL + "}" + newL;
            }
        }

        private void AddEnum(string className, EnumDeclarationSyntax enumSyn)
        {
            JavaEnumData enumData = new JavaEnumData
            {
                enumName = enumSyn.Identifier.Text,
                mods = new List<string>(),
                enumVals = new List<string>()
            };
            foreach (SyntaxToken token in enumSyn.Modifiers)
            {
                if (token.Internal()) enumData.mods.Add("default");
                else if (token.Private()) enumData.mods.Add("private");
                else if (token.Public()) enumData.mods.Add("public");
            }
            foreach (EnumMemberDeclarationSyntax member in enumSyn.Members)
                enumData.enumVals.Add(member.Identifier.Text);
            if (string.IsNullOrEmpty(className)) enums.Add(enumData);
            else classes[Get_CDI(className)].enums.Add(enumData);
        }

        private void AddClass(ClassDeclarationSyntax classSyn)
        {
            JavaClassData classData = new JavaClassData
            {
                className = classSyn.Identifier.Text,
                csMarkedStatic = false,
                inheritsFrom = string.Empty,
                mods = new List<string>(),
                interfaces = new List<string>(),
                methods = new List<JavaMethodData>(),
                enums = new List<JavaEnumData>(),
                fields = new List<JavaFieldData>(),
                properties = new List<JavaPropertyData>()
            };
            foreach (SyntaxToken token in classSyn.Modifiers)
            {
                if (token.Internal()) classData.mods.Add("default");
                else if (token.Private()) classData.mods.Add("private");
                else if (token.Public()) classData.mods.Add("public");
                else if (token.Static()) classData.csMarkedStatic = true;
            }
            if (classSyn.BaseList != null)
            {
                BaseListSyntax baseList = classSyn.BaseList;
                for (int i = 0; i < baseList.Types.Count; i++)
                {
                    string typeText = baseList.Types[i].Text(), mapped = typeMap.Map(typeText);
                    if (typeText.Length > 1 && typeText[0] == 'I' && char.IsUpper(typeText[1]))
                        classData.interfaces.Add(mapped);
                    else classData.inheritsFrom = mapped;
                    imports.AddType(mapped.JavaSubTypes());
                }
            }
            classes.Add(classData);
        }

        private void AddField(string className, FieldDeclarationSyntax fieldSyn,
            VariableDeclarationSyntax? varSyn)
        {
            JavaFieldData fieldData = new JavaFieldData
            {
                mods = new List<string>(),
            };
            foreach (SyntaxToken modifier in fieldSyn.Modifiers)
            {
                if (modifier.Public()) fieldData.mods.Add("public");
                else if (modifier.Private()) fieldData.mods.Add("private");
                else if (modifier.Static()) fieldData.mods.Add("static");
                else if (modifier.Protected()) fieldData.mods.Add("protected");
            }
            if (varSyn != null)
            {
                fieldData.typeName = typeMap.Map(varSyn.Type.Text());
                imports.AddType(fieldData.typeName.JavaSubTypes());
                List<SyntaxNode> varDeclarators = varSyn.TopChildren(SyntaxKind.VariableDeclarator);
                fieldData.varNames = new string[varDeclarators.Count];
                fieldData.varVals = new string?[varDeclarators.Count];
                for (int i = 0; i < varDeclarators.Count; i++)
                {
                    VariableDeclaratorSyntax varDec = (VariableDeclaratorSyntax)varDeclarators[i];
                    fieldData.varNames[i] = varDec.Identifier.Text;
                    JavaExpressionData? expressionData = ProcessExpression(varDec.Initializer?.Value, 0);
                    if (expressionData != null) fieldData.varVals[i] = expressionData?.expressionText;
                }
            }
            classes[Get_CDI(className)].fields.Add(fieldData);
        }

        private void AddProperty(string className, PropertyDeclarationSyntax propSyn,
            AccessorDeclarationSyntax? getter, AccessorDeclarationSyntax? setter, bool lambda)
        {
            JavaPropertyData propData = new JavaPropertyData
            {
                propName = propSyn.Identifier.Text,
                typeName = typeMap.Map(propSyn.Type.Text()),
                varMods = new List<string>(),
                getMods = new List<string>(),
                setMods = new List<string>()
            };
            imports.AddType(propData.typeName.JavaSubTypes());
            foreach (SyntaxToken modifier in propSyn.Modifiers)
            {
                if (modifier.Public()) propData.varMods.Add("private");
                else if (modifier.Private()) propData.varMods.Add("private");
                else if (modifier.Static()) propData.varMods.Add("static");
                else if (modifier.Protected()) propData.varMods.Add("protected");
            }
            if (propSyn.Initializer != null)
                propData.initVal = propSyn.Initializer.Value.ToFullString();
            if (getter != null || lambda)
            {
                propData.getter = true;
                if (!lambda && getter?.Modifiers.Count > 0)
                {
                    foreach (SyntaxToken modifier in getter.Modifiers)
                    {
                        if (modifier.Public()) propData.getMods.Add("public");
                        else if (modifier.Private()) propData.getMods.Add("private");
                        else if (modifier.Static()) propData.getMods.Add("static");
                        else if (modifier.Protected()) propData.getMods.Add("protected");
                    }
                }
                else
                {
                    foreach (SyntaxToken modifier in propSyn.Modifiers)
                    {
                        if (modifier.Public()) propData.getMods.Add("public");
                        else if (modifier.Private()) propData.getMods.Add("private");
                        else if (modifier.Static()) propData.getMods.Add("static");
                        else if (modifier.Protected()) propData.getMods.Add("protected");
                    }
                }
            }
            if (setter != null)
            {
                propData.setter = true;
                if (setter.Modifiers.Count > 0)
                {
                    foreach (SyntaxToken modifier in setter.Modifiers)
                    {
                        if (modifier.Public()) propData.setMods.Add("public");
                        else if (modifier.Private()) propData.setMods.Add("private");
                        else if (modifier.Static()) propData.setMods.Add("static");
                        else if (modifier.Protected()) propData.setMods.Add("protected");
                    }
                }
                else
                {
                    foreach (SyntaxToken modifier in propSyn.Modifiers)
                    {
                        if (modifier.Public()) propData.setMods.Add("public");
                        else if (modifier.Private()) propData.setMods.Add("private");
                        else if (modifier.Static()) propData.setMods.Add("static");
                        else if (modifier.Protected()) propData.setMods.Add("protected");
                    }
                }
            }
            classes[Get_CDI(className)].properties.Add(propData);
        }

        private void AddMethod(string className, MethodDeclarationSyntax methodSyn)
        {
            JavaMethodData methodData = new JavaMethodData
            {
                methodName = methodSyn.Identifier.Text,
                typeName = typeMap.Map(methodSyn.ReturnType.Text(), TypeMap.Context.Function),
                mods = new List<string>(),
                parameters = new List<(string, string)>()
            };
            imports.AddType(methodData.typeName.JavaSubTypes());
            foreach (SyntaxToken modifier in methodSyn.Modifiers)
            {
                if (modifier.Public()) methodData.mods.Add("public");
                else if (modifier.Private()) methodData.mods.Add("private");
                else if (modifier.Static()) methodData.mods.Add("static");
                else if (modifier.Protected()) methodData.mods.Add("protected");
            }
            foreach (ParameterSyntax param in methodSyn.ParameterList.Parameters)
                if (param.Type != null)
                    methodData.parameters.Add((typeMap.Map(param.Type.Text()), param.Identifier.Text));
            if (methodSyn.FindChild(SyntaxKind.Block) is BlockSyntax blockSyn)
                methodData.blockData = ProcessBlock(blockSyn, 1);
            classes[Get_CDI(className)].methods.Add(methodData);
        }

        private JavaExpressionData? ProcessExpression(ExpressionSyntax? expression, int tabs)
        {
            if (expression != null)
            {
                JavaExpressionData expressionData = new JavaExpressionData
                {
                    expressionText = string.Empty,
                    root = expression,
                    curTabs = tabs
                };
                ExpressionTree(expression, ref expressionData);
                return expressionData;
            }
            return null;
        }

        private void ExpressionTree(SyntaxNode current, ref JavaExpressionData curData)
        {
            if (current.IsKind(SyntaxKind.ElementAccessExpression))
            {
                ElementAccessExpressionSyntax syn = (ElementAccessExpressionSyntax)current;
                TypeInfo? info = sModel.GetTypeInfo(syn.Expression);

                if (syn.Expression.IsKind(SyntaxKind.IdentifierName) &&
                    info != null && info?.Type != null && info?.Type?.Name == "List")
                {
                    IdentifierNameSyntax nameSyn = (IdentifierNameSyntax)syn.Expression;
                    curData.expressionText += nameSyn.Identifier.Text + ".get(";
                    ExpressionTree(syn.ArgumentList.Arguments[0].Expression, ref curData);
                    curData.expressionText += ")";
                }
                else
                {
                    ExpressionTree(syn.Expression, ref curData);
                    for (int i = 0; i < syn.ArgumentList.Arguments.Count; i++)
                    {
                        curData.expressionText += "[";
                        ExpressionTree(syn.ArgumentList.Arguments[i].Expression, ref curData);
                        curData.expressionText += "]";
                    }
                }
            }
            else if (current.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                MemberAccessExpressionSyntax syn = (MemberAccessExpressionSyntax)current;
                ExpressionTree(syn.Expression, ref curData);
                SymbolInfo? info = sModel.GetSymbolInfo(syn.Name);
                bool typeExists = info.GetKind(out SymbolKind kind);
                string? containingType = info?.Symbol?.ContainingType.Name;

                if (syn.Name.Identifier.Text == "Empty" &&
                    ((typeExists && kind == SymbolKind.Field && containingType == "String") || !typeExists))
                    curData.expressionText += "new String()";
                else if (syn.Name.Identifier.Text == "ToString" &&
                    ((typeExists && kind == SymbolKind.Method) || !typeExists))
                    curData.expressionText += ".toString";
                else if (syn.Name.Identifier.Text == "Replace" &&
                    ((typeExists && kind == SymbolKind.Method) || !typeExists))
                    curData.expressionText += ".replace";
                else if (syn.Name.Identifier.Text == "Split" &&
                    ((typeExists && kind == SymbolKind.Method) || !typeExists))
                    curData.expressionText += ".split";
                else if (syn.Name.Identifier.Text == "ToLower" &&
                    ((typeExists && kind == SymbolKind.Method) || !typeExists))
                    curData.expressionText += ".toLowerCase";
                else if (syn.Name.Identifier.Text == "Trim" &&
                    ((typeExists && kind == SymbolKind.Method) || !typeExists))
                    curData.expressionText += ".trim";
                else if (syn.Name.Identifier.Text == "StartsWith" &&
                    ((typeExists && kind == SymbolKind.Method) || !typeExists))
                    curData.expressionText += ".startsWith";
                else if (syn.Name.Identifier.Text == "EndsWith" &&
                    ((typeExists && kind == SymbolKind.Method) || !typeExists))
                    curData.expressionText += ".endsWith";
                else if (syn.Name.Identifier.Text == "Length" &&
                    ((typeExists && kind == SymbolKind.Property && containingType == "Array") || !typeExists))
                    curData.expressionText += ".length";
                else if (syn.Name.Identifier.Text == "ToArray" &&
                    ((typeExists && kind == SymbolKind.Method && containingType == "List") || !typeExists))
                    curData.expressionText += ".toArray";
                else if (syn.Name.Identifier.Text == "Clear" &&
                    ((typeExists && kind == SymbolKind.Method && containingType == "List") || !typeExists))
                    curData.expressionText += ".clear";
                else if (syn.Name.Identifier.Text == "Contains" &&
                    ((typeExists && kind == SymbolKind.Method && containingType == "List") || !typeExists))
                    curData.expressionText += ".contains";
                else if (syn.Name.Identifier.Text == "Count" &&
                    ((typeExists && kind == SymbolKind.Property && containingType == "List") || !typeExists))
                    curData.expressionText += ".size()";
                else if (syn.Name.Identifier.Text == "Length" &&
                    ((typeExists && kind == SymbolKind.Property) || !typeExists))
                    curData.expressionText += ".length()";
                else if (syn.Name.Identifier.Text == "Add" &&
                    ((typeExists && kind == SymbolKind.Method) || !typeExists))
                    curData.expressionText += ".add";
                else curData.expressionText += "." + syn.Name.Identifier.Text;
            }
            else if (current.IsKind(SyntaxKind.InvocationExpression))
            {
                InvocationExpressionSyntax syn = (InvocationExpressionSyntax)current;
                JavaExpressionData? xData = ProcessExpression(syn.Expression, curData.curTabs);
                if (xData != null)
                {
                    if (xData?.expressionText == "File.WriteAllText")
                    {
                        imports.AddType("PrintWriter");
                        string instString = "writer_" + CreatedInstNum("PrintWriter");
                        curData.expressionText += "PrintWriter " + instString + " = new PrintWriter(";
                        ExpressionTree(syn.ArgumentList.Arguments[0], ref curData);
                        curData.expressionText += ");" + newL;
                        curData.expressionText += curData.TabString + instString + ".write(";
                        ExpressionTree(syn.ArgumentList.Arguments[1], ref curData);
                        curData.expressionText += ");" + newL;
                        curData.expressionText += curData.TabString + instString + ".flush();" + newL;
                        curData.expressionText += curData.TabString + instString + ".close()";
                    }
                    else
                    {
                        curData.expressionText += xData?.expressionText;
                        ExpressionTree(syn.ArgumentList, ref curData);
                    }
                }
            }
            else if (current.IsKind(SyntaxKind.CastExpression))
            {
                CastExpressionSyntax syn = (CastExpressionSyntax)current;
                JavaExpressionData? xData = ProcessExpression(syn.Expression, curData.curTabs);
                if (xData != null)
                {
                    if (xData?.expressionText.StartsWith("Enum.Parse") == true)
                    {
                        ExpressionTree(syn.Type, ref curData);
                        if (enforceConventions) curData.expressionText += ".valueOf(";

                        InvocationExpressionSyntax invokeSyn = (InvocationExpressionSyntax)syn.Expression;
                        for (int i = 0; i < invokeSyn.ArgumentList.Arguments.Count; i++)
                        {
                            TypeInfo? info = sModel.GetTypeInfo(invokeSyn.ArgumentList.Arguments[i].Expression);
                            if (info != null && info?.Type != null && info?.Type?.Name == "String")
                            {
                                ExpressionTree(invokeSyn.ArgumentList.Arguments[i].Expression, ref curData);
                                curData.expressionText += ")";
                                break;
                            }
                        }
                    }
                    else
                    {
                        curData.expressionText += "(";
                        ExpressionTree(syn.Type, ref curData);
                        curData.expressionText += ")";
                        if (enforceConventions) curData.expressionText += " " + xData?.expressionText;
                    }
                }
            }
            else if (current.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                ObjectCreationExpressionSyntax syn = (ObjectCreationExpressionSyntax)current;
                string typeString = typeMap.Map(syn.Type.Text());
                imports.AddType(typeString.JavaSubTypes());
                curData.expressionText += "new " + typeString;
                if (syn.ArgumentList != null) ExpressionTree(syn.ArgumentList, ref curData);
                if (syn.Initializer != null && syn.Parent != null && syn.Parent != null)
                {
                    curData.expressionText += "()";
                    if (syn.Parent?.Parent is VariableDeclaratorSyntax varSyn)
                    {
                        if (syn.Initializer.Expressions.Count > 0) curData.expressionText += ";";
                        for (int i = 0; i < syn.Initializer.Expressions.Count; i++)
                        {
                            curData.expressionText += newL + curData.TabString + varSyn.Identifier.Text + ".";
                            ExpressionTree(syn.Initializer.Expressions[i], ref curData);
                            if (i != syn.Initializer.Expressions.Count - 1) curData.expressionText += ";";
                        }
                    }
                }
            }
            else if (current.BinaryExpression())
            {
                BinaryExpressionSyntax syn = (BinaryExpressionSyntax)current;
                ExpressionTree(syn.Left, ref curData);
                curData.expressionText += " " + syn.OperatorToken.Text + " ";
                ExpressionTree(syn.Right, ref curData);
            }
            else if (current.IsKind(SyntaxKind.SimpleAssignmentExpression) ||
                current.IsKind(SyntaxKind.AddAssignmentExpression) ||
                current.IsKind(SyntaxKind.SubtractAssignmentExpression) ||
                current.IsKind(SyntaxKind.MultiplyAssignmentExpression) ||
                current.IsKind(SyntaxKind.DivideAssignmentExpression) ||
                current.IsKind(SyntaxKind.ModuloAssignmentExpression))
            {
                AssignmentExpressionSyntax syn = (AssignmentExpressionSyntax)current;
                ExpressionTree(syn.Left, ref curData);
                curData.expressionText += " " + syn.OperatorToken.Text + " ";
                ExpressionTree(syn.Right, ref curData);
            }
            else if (current.IsKind(SyntaxKind.PostIncrementExpression) ||
                current.IsKind(SyntaxKind.PostDecrementExpression))
            {
                PostfixUnaryExpressionSyntax syn = (PostfixUnaryExpressionSyntax)current;
                ExpressionTree(syn.Operand, ref curData);
                curData.expressionText += syn.OperatorToken.Text;
            }
            else if (current.IsKind(SyntaxKind.PreIncrementExpression) ||
                current.IsKind(SyntaxKind.PreDecrementExpression) ||
                current.IsKind(SyntaxKind.LogicalNotExpression) ||
                current.IsKind(SyntaxKind.UnaryMinusExpression))
            {
                PrefixUnaryExpressionSyntax syn = (PrefixUnaryExpressionSyntax)current;
                curData.expressionText += syn.OperatorToken.Text;
                ExpressionTree(syn.Operand, ref curData);
            }
            else if (current.IsKind(SyntaxKind.CharacterLiteralExpression) ||
                current.IsKind(SyntaxKind.NumericLiteralExpression) ||
                current.IsKind(SyntaxKind.TrueLiteralExpression) ||
                current.IsKind(SyntaxKind.FalseLiteralExpression) ||
                current.IsKind(SyntaxKind.NullLiteralExpression))
            {
                LiteralExpressionSyntax syn = (LiteralExpressionSyntax)current;
                curData.expressionText += syn.Token.Text;
            }
            else if (current.IsKind(SyntaxKind.StringLiteralExpression))
            {
                LiteralExpressionSyntax syn = (LiteralExpressionSyntax)current;
                curData.expressionText += syn.Token.Text.UnAtIfy();
            }
            else if (current.IsKind(SyntaxKind.AwaitExpression))
            {
                AwaitExpressionSyntax syn = (AwaitExpressionSyntax)current;
                ExpressionTree(syn.Expression, ref curData);
            }
            else if (current.IsKind(SyntaxKind.Argument))
            {
                ArgumentSyntax syn = (ArgumentSyntax)current;
                if (syn.Expression.IsKind(SyntaxKind.DeclarationExpression))
                {
                    DeclarationExpressionSyntax decSyn = (DeclarationExpressionSyntax)syn.Expression;
                    ExpressionTree(decSyn.Designation, ref curData);
                }
                else ExpressionTree(syn.Expression, ref curData);
            }
            else if (current.IsKind(SyntaxKind.ArrayType))
            {
                ArrayTypeSyntax syn = (ArrayTypeSyntax)current;
                string elementType = syn.ElementType.Text();
                curData.expressionText += typeMap.Map(elementType);
                imports.AddType(elementType.JavaSubTypes());
                for (int r = 0; r < syn.RankSpecifiers.Count; r++)
                {
                    curData.expressionText += "[";
                    if (syn.RankSpecifiers[r].Sizes.Count < 0)
                        curData.expressionText += 0;
                    else
                    {
                        for (int s = 0; s < syn.RankSpecifiers[r].Sizes.Count; s++)
                        {
                            JavaExpressionData? xData =
                                ProcessExpression(syn.RankSpecifiers[r].Sizes[s], curData.curTabs);
                            curData.expressionText += xData?.expressionText;
                        }
                    }
                    curData.expressionText += "]";
                }
            }
            else if (current.IsKind(SyntaxKind.ThisExpression))
            {
                ThisExpressionSyntax syn = (ThisExpressionSyntax)current;
                curData.expressionText += syn.Token.Text;
            }
            else if (current.IsKind(SyntaxKind.SingleVariableDesignation))
            {
                SingleVariableDesignationSyntax syn = (SingleVariableDesignationSyntax)current;
                curData.expressionText += syn.Identifier.Text;
            }
            else if (current.IsKind(SyntaxKind.IdentifierName))
            {
                IdentifierNameSyntax syn = (IdentifierNameSyntax)current;
                if (syn.Identifier.Text != "_")
                    curData.expressionText += syn.Identifier.Text;
                else
                {
                    curData.expressionText += "_dummy_" + discards;
                    discards++;
                }
            }
            else if (current.IsKind(SyntaxKind.IdentifierName))
                curData.expressionText += "null";
            else
            {
                ChildSyntaxList list = current.ChildNodesAndTokens();
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsNode)
                    {
                        SyntaxNode? node = list[i].AsNode();
                        if (node != null)
                        {
                            if (node.BinaryExpression() ||
                                node.IsKind(SyntaxKind.ArgumentList) ||
                                node.IsKind(SyntaxKind.SimpleMemberAccessExpression) ||
                                node.IsKind(SyntaxKind.AwaitExpression) ||
                                node.IsKind(SyntaxKind.Argument) ||
                                node.IsKind(SyntaxKind.InvocationExpression) ||
                                node.IsKind(SyntaxKind.CastExpression) ||
                                node.IsKind(SyntaxKind.ArrayType) ||
                                node.IsKind(SyntaxKind.IdentifierName))
                                ExpressionTree(node, ref curData);
                            else if (node.IsKind(SyntaxKind.SimpleAssignmentExpression))
                            {
                                curData.expressionText += newL;
                                ExpressionTree(node, ref curData);
                            }
                            else if (node.IsKind(SyntaxKind.GenericName))
                            {
                                string typeText = ((GenericNameSyntax)node).Text();
                                curData.expressionText += typeMap.Map(typeText);
                                imports.AddType(typeText.JavaSubTypes());
                            }
                            else if (node.IsKind(SyntaxKind.PredefinedType))
                            {
                                string typeText = ((PredefinedTypeSyntax)node).Text();
                                curData.expressionText += typeMap.Map(typeText);
                                imports.AddType(typeText);
                            }
                        }
                    }
                    else if (list[i].IsToken)
                    {
                        SyntaxToken token = list[i].AsToken();
                        curData.expressionText += Recursion.ProcessExpressionToken(token);
                    }
                }
            }
        }

        private JavaBlockData? ProcessBlock(BlockSyntax? block, int tabs)
        {
            if (block != null)
            {
                JavaBlockData blockData = new JavaBlockData
                {
                    blockText = string.Empty,
                    root = block,
                    curTabs = tabs
                };
                BlockTree(block, ref blockData);
                return blockData;
            }
            return null;
        }

        private void BlockTree(SyntaxNode current, ref JavaBlockData curData)
        {
            ChildSyntaxList list = current.ChildNodesAndTokens();
            if (current.IsKind(SyntaxKind.ExpressionStatement))
            {
                ExpressionStatementSyntax syn = (ExpressionStatementSyntax)current;
                JavaExpressionData? xData = ProcessExpression(syn.Expression, curData.curTabs);
                if (xData != null) curData.blockText += curData.TabString + xData?.expressionText + ";" + newL;
            }
            else if (current.IsKind(SyntaxKind.ForStatement))
            {
                ForStatementSyntax syn = (ForStatementSyntax)current;
                curData.blockText += curData.TabString + "for (";
                if (syn.Declaration != null) BlockTree(syn.Declaration, ref curData);
                JavaExpressionData? xData = ProcessExpression(syn.Condition, curData.curTabs);
                if (xData != null)
                {
                    if (syn.Declaration != null) curData.blockText += "; ";
                    curData.blockText += xData?.expressionText;
                }

                if (xData != null && syn.Incrementors.Count > 0) curData.blockText += "; ";
                for (int c = 0; c < syn.Incrementors.Count; c++)
                {
                    if (c != 0) curData.blockText += ", ";
                    JavaExpressionData? xDataInc = ProcessExpression(syn.Incrementors[c], curData.curTabs);
                    if (xDataInc != null) curData.blockText += xDataInc?.expressionText;
                }

                curData.blockText += ")";
                if (!syn.Statement.IsKind(SyntaxKind.Block))
                {
                    curData.curTabs++;
                    curData.blockText += newL;
                    BlockTree(syn.Statement, ref curData);
                    curData.curTabs--;
                }
                else
                {
                    BlockSyntax block = (BlockSyntax)syn.Statement;
                    JavaBlockData? xDataBlock = ProcessBlock(block, curData.curTabs);
                    if (xDataBlock != null) curData.blockText += xDataBlock?.blockText + newL;
                }

            }
            else if (current.IsKind(SyntaxKind.IfStatement))
            {
                IfStatementSyntax syn = (IfStatementSyntax)current;
                if (current.Parent.IsKind(SyntaxKind.ElseClause)) curData.blockText += "if (";
                else curData.blockText += curData.TabString + "if (";

                JavaExpressionData? xData = ProcessExpression(syn.Condition, curData.curTabs);
                if (xData != null) curData.blockText += xData?.expressionText;
                curData.blockText += ")";
                if (!syn.Statement.IsKind(SyntaxKind.Block))
                {
                    curData.curTabs++;
                    curData.blockText += newL;
                    BlockTree(syn.Statement, ref curData);
                    curData.curTabs--;
                }
                else
                {
                    BlockSyntax block = (BlockSyntax)syn.Statement;
                    JavaBlockData? xDataBlock = ProcessBlock(block, curData.curTabs);
                    if (xDataBlock != null) curData.blockText += xDataBlock?.blockText + newL;
                }

                if (syn.Else != null) BlockTree(syn.Else, ref curData);
            }
            else if (current.IsKind(SyntaxKind.ElseClause))
            {
                ElseClauseSyntax syn = (ElseClauseSyntax)current;
                if (syn.Statement.IsKind(SyntaxKind.IfStatement))
                {
                    curData.blockText += curData.TabString + "else ";
                    BlockTree(syn.Statement, ref curData);
                }
                else if (syn.Statement.IsKind(SyntaxKind.Block))
                {
                    curData.blockText += curData.TabString + "else";
                    BlockTree(syn.Statement, ref curData);
                }
                else
                {
                    curData.blockText += curData.TabString + "else " + newL;
                    curData.curTabs++;
                    BlockTree(syn.Statement, ref curData);
                    curData.curTabs--;
                }
            }
            else if (current.IsKind(SyntaxKind.ArrayType))
            {
                ArrayTypeSyntax syn = (ArrayTypeSyntax)current;
                string elementType = syn.ElementType.Text();
                curData.blockText += typeMap.Map(elementType);
                imports.AddType(elementType.JavaSubTypes());
                for (int r = 0; r < syn.RankSpecifiers.Count; r++)
                {
                    curData.blockText += "[";
                    if (syn.RankSpecifiers[r].Sizes.Count < 1)
                        curData.blockText += 0;
                    else
                    {
                        for (int s = 0; s < syn.RankSpecifiers[r].Sizes.Count; s++)
                        {
                            JavaExpressionData? xData =
                                ProcessExpression(syn.RankSpecifiers[r].Sizes[s], curData.curTabs);
                            curData.blockText += xData?.expressionText;
                        }
                    }

                    curData.blockText += "]";
                }
            }
            else if (current.IsKind(SyntaxKind.ReturnStatement))
            {
                ReturnStatementSyntax syn = (ReturnStatementSyntax)current;
                curData.blockText += curData.TabString + "return ";
                JavaExpressionData? xData = ProcessExpression(syn.Expression, curData.curTabs);
                if (xData != null) curData.blockText += xData?.expressionText;
                curData.blockText += ";" + newL;
            }
            else if (current.IsKind(SyntaxKind.ContinueStatement))
                curData.blockText += curData.TabString + "continue;" + newL;
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsNode)
                    {
                        SyntaxNode? node = list[i].AsNode();
                        if (node != null)
                        {
                            if (node.IsKind(SyntaxKind.VariableDeclaration) ||
                                node.IsKind(SyntaxKind.ForStatement) ||
                                node.IsKind(SyntaxKind.IfStatement) ||
                                node.IsKind(SyntaxKind.ExpressionStatement) ||
                                node.IsKind(SyntaxKind.ContinueStatement) ||
                                node.IsKind(SyntaxKind.ArrayType) ||
                                node.IsKind(SyntaxKind.ReturnStatement))
                                BlockTree(node, ref curData);
                            else if (node.IsKind(SyntaxKind.LessThanExpression) ||
                                node.IsKind(SyntaxKind.PostIncrementExpression))
                            {
                                ExpressionSyntax syn = (ExpressionSyntax)node;
                                JavaExpressionData? xData = ProcessExpression(syn, curData.curTabs);
                                if (xData != null) curData.blockText += xData?.expressionText;
                            }
                            if (node.IsKind(SyntaxKind.ArgumentList))
                            {
                                ArgumentListSyntax argumentList = (ArgumentListSyntax)node;
                                for (int a = 0; a < argumentList.Arguments.Count; a++)
                                {
                                    JavaExpressionData? xData =
                                        ProcessExpression(argumentList.Arguments[a].Expression, curData.curTabs);
                                    if (xData != null) curData.blockText += xData?.expressionText;
                                }
                            }
                            else if (node.IsKind(SyntaxKind.GenericName))
                            {
                                string typeText = ((GenericNameSyntax)node).Text();
                                curData.blockText += typeMap.Map(typeText);
                                imports.AddType(typeText.JavaSubTypes());
                            }
                            else if (node.IsKind(SyntaxKind.LocalDeclarationStatement))
                            {
                                curData.blockText += curData.TabString;
                                BlockTree(node, ref curData);
                            }
                            else if (node.IsKind(SyntaxKind.VariableDeclarator))
                            {
                                VariableDeclaratorSyntax syn = (VariableDeclaratorSyntax)node;
                                curData.blockText += " " + syn.Identifier.Text;
                                if (syn.Initializer != null)
                                {
                                    curData.blockText += " = ";
                                    JavaExpressionData? xData =
                                        ProcessExpression(syn.Initializer.Value, curData.curTabs);
                                    if (xData != null) curData.blockText += xData?.expressionText;
                                }
                            }
                            else if (node.IsKind(SyntaxKind.PredefinedType))
                            {
                                string typeText = ((PredefinedTypeSyntax)node).Text();
                                curData.blockText += typeMap.Map(typeText);
                                imports.AddType(typeText);
                            }
                            else if (node.IsKind(SyntaxKind.IdentifierName))
                            {
                                IdentifierNameSyntax identifier = (IdentifierNameSyntax)node;
                                curData.blockText += identifier.Identifier.Text;
                            }
                        }
                    }
                    else if (list[i].IsToken)
                    {
                        SyntaxToken token = list[i].AsToken();
                        if (token.IsKind(SyntaxKind.OpenBraceToken) &&
                            token.Parent.IsKind(SyntaxKind.Block))
                        {
                            curData.blockText += " {" + newL;
                            curData.curTabs++;
                        }
                        else if (token.IsKind(SyntaxKind.CloseBraceToken) &&
                            token.Parent.IsKind(SyntaxKind.Block))
                        {
                            curData.curTabs--;
                            curData.blockText += curData.TabString + "}";
                        }
                        else if (token.IsKind(SyntaxKind.CommaToken) &&
                            token.Parent.IsKind(SyntaxKind.VariableDeclaration))
                            curData.blockText += ", ";
                        else if (token.IsKind(SyntaxKind.SemicolonToken))
                        {
                            if (!token.Parent.IsKind(SyntaxKind.ForStatement))
                                curData.blockText += ";" + newL;
                        }
                    }
                }
            }
        }

        private int Get_CDI(string className)
        {
            int cdi = -1;
            for (int i = 0; i < classes.Count; i++)
            {
                if (classes[i].className == className)
                {
                    cdi = i;
                    break;
                }
            }
            return cdi;
        }

        private int CreatedInstNum(string mapped)
        {
            if (createdInstCounter.ContainsKey(mapped))
            {
                createdInstCounter[mapped]++;
                return createdInstCounter[mapped];
            }
            else
            {
                createdInstCounter.Add(mapped, 1);
                return 1;
            }
        }
    }
}
