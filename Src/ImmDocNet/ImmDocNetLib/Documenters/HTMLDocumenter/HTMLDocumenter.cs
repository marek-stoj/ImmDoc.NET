/*
 * Copyright 2007 - 2009 Marek Stój
 * 
 * This file is part of ImmDoc .NET.
 *
 * ImmDoc .NET is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * ImmDoc .NET is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ImmDoc .NET; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;

using Imm.ImmDocNetLib.MyReflection.MetaClasses;
using Imm.ImmDocNetLib.MyReflection.GenericConstraints;
using Mono.Cecil;

namespace Imm.ImmDocNetLib.Documenters.HTMLDocumenter
{
  public class HTMLDocumenter : Documenter
  {
    private const int MAX_FILES_PER_DIR = 500;
    private static readonly Dictionary<string, string> fileNamesMappings = new Dictionary<string, string>();
    private static int fileIndex = MAX_FILES_PER_DIR;
    private static int dirIndex = 0;
    private static string dirIndexStr;

    private static readonly string IMM_DOC_NET_PROJECT_HOMEPAGE = "http://immdocnet.codeplex.com/";
    private static readonly string MSDN_LIBRARY_URL_FORMAT = "http://msdn.microsoft.com/library/{0}.aspx";

    private const int IO_BUFFER_SIZE = 1024;

    private static readonly string DEFAULT_LANGUAGE = "C#";

    private static readonly string GRAPHICS_DIRECTORY = "GFX";
    private static readonly string CSS_DIRECTORY = "CSS";
    private static readonly string JS_DIRECTORY = "JS";
    private static readonly string CONTENTS_DIRECTORY = "Contents";
    private static readonly string CLASS_MEMBERS_DIRECTORY = "!Members";

    private static readonly string CONTENTS_CSS_FILE_NAME = "Contents.css";
    private static readonly string TREE_VIEW_CSS_FILE_NAME = "TreeView.css";
    private static readonly string TABLE_OF_CONTENTS_CSS_FILE_NAME = "TableOfContents.css";
    private static readonly string COMMON_JAVA_SCRIPT_FILE_NAME = "Common.js";
    private static readonly string IMMJSLIB_JAVA_SCRIPT_FILE_NAME = "ImmJSLib.js";
    private static readonly string TREE_VIEW_JAVA_SCRIPT_FILE_NAME = "TreeView.js";
    private static readonly string MAIN_INDEX_TEMPLATE_FILE_NAME = "IndexTemplate.html";
    private static readonly string MAIN_INDEX_FILE_NAME = "index.html";
    private static readonly string TABLE_OF_CONTENTS_TEMPLATE_FILE_NAME = "TableOfContentsTemplate.html";
    private static readonly string NAMESPACES_INDEX_FILE_NAME = "!namespaces.html";
    private static readonly string TYPES_INDEX_FILE_NAME = "!types.html";
    private static readonly string TABLE_OF_CONTENTS_FILE_NAME = "0.html";
    private static readonly string ASSEMBLIES_INDEX_FILE_NAME = "1.html";
    private static readonly string CLASS_LIBRARY_FILE_NAME = "ClassLibrary.csv.gz";
    private static readonly string GZIP_FILE_EXTENSION = ".gz";

    private static readonly string PROJECT_SUMMARY = "<p>This is the list of all assemblies in your project.</p>";
    private static readonly string NO_SUMMARY = "<p>There is no summary.</p>";
    private static readonly string NO_REMARKS = "<p>There are no remarks.</p>";
    private static readonly string NO_MEMBERS = "<p>There are no members.</p>";
    private static readonly string NO_ASSEMBLIES = "<p>There are no assemblies.</p>";
    private static readonly string NO_NAMESPACES = "<p>There are no namespaces.</p>";
    private static readonly string NO_DESCRIPTION = "<p>There is no description.</p>";

    private static readonly string[] DEFAULT_MEMBERS_COLUMNS_NAMES = new string[] { "", "Name", "Description" };
    private static readonly string[] ENUM_MEMBERS_COLUMNS_NAMES = new string[] { "", "Member name", "Description" };
    private static readonly string[] EXCEPTIONS_COLUMNS_NAMES = new string[] { "Exception type", "Condition" };

    private static readonly int[] DEFAULT_MEMBERS_COLUMNS_WIDTHS = new int[] { 2, 38, 60 };
    private static readonly int[] TYPE_MEMBERS_COLUMNS_WIDTHS = new int[] { 7, 38, 55 };
    private static readonly int[] EXCEPTIONS_COLUMNS_WIDTHS = new int[] { 25, 75 };

    private static readonly string[] GRAPHICS_FILES_NAMES = new string[] { "BigSquareExpanded.gif",
                                                                           "BigSquareCollapsed.gif",
                                                                           "SmallSquareExpanded.gif",
                                                                           "SmallSquareCollapsed.gif",
                                                                           "TV_Minus.gif",
                                                                           "TV_Null.gif",
                                                                           "TV_Plus.gif",
                                                                           "TV_VerticalDots.gif",
                                                                           "LeftArrow.gif",
                                                                           "RightArrow.gif"};

    private static readonly string PARAM_INDENT = "        ";
    private static readonly string GENERIC_CONSTRAINTS_INDENT = PARAM_INDENT.Replace(" ", "&nbsp;");

    private const string HHC_HOME_VARIABLE_NAME = "%HHC_HOME%";
    private const string HHC_DEFAULT_INSTALL_DIR = "HTML Help Workshop";
    private const string HHC_EXEC_NAME = "hhc.exe";
    private const string HHP_NAME = "Temp.hhp";
    private const string HHC_NAME = "Temp.hhc";
    private const string CHM_NAME = "Temp.chm";
    private const string HHC_LOG_NAME = "hhc.log";

    private static readonly Regex SECTION_REFERENCE_PATTERN = new Regex(@"\$\{(.*?):(.*?):(.*?)}", RegexOptions.Compiled);

    // matches full types names, eg. System.Collections.Generic.Dictionary+KeyValueCollection
    private static readonly Regex typePattern = new Regex(@"(([_A-Za-z][A-Za-z0-9_]*)(\.|\+))*([_A-Za-z][A-Za-z0-9_]*)",
                                                          RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex codePattern = new Regex("<code>(?<Contents>(.|\r|\n)*?)</code>",
                                                          RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex seePattern = new Regex("(<see cref=\"(?<XmlMemberId>.*?)\"[ ]?/>)|(<see cref=\"(?<XmlMemberId>.*?)\">(?<Contents>.*?)</see>)",
                                                         RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex paramrefPattern = new Regex("<paramref name=\"(?<ParamName>.*?)\" ?/>",
                                                              RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex typeparamrefPattern = new Regex("<typeparamref name=\"(?<TypeParamName>.*?)\" ?/>",
                                                                  RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly MatchEvaluator typeRegexEvaluator = new MatchEvaluator(OnProcessTypeMatch);
    private static readonly MatchEvaluator codeRegexEvaluator = new MatchEvaluator(OnCodePatternMatch);
    private static readonly MatchEvaluator paramrefRegexEvaluator = new MatchEvaluator(OnParamrefPatternMatch);
    private static readonly MatchEvaluator typeparamrefRegexEvaluator = new MatchEvaluator(OnTypeparamrefPatternMatch);
    private readonly MatchEvaluator seeRegexEvaluator;

    private static Dictionary<string, string> systemTypesConversions;
    private IDictionary<string, string> frameworkTypes;

    private static XslCompiledTransform listsXslt;
    private static XmlReaderSettings listsXmlReaderSettings;
    private static XmlReaderSettings xslReaderSettings;
    private static XmlWriterSettings xmlWriterSettings;

    private string outputDirectory;
    private string chmFileNameWithoutExtension;
    private DocumentationGenerationOptions options;

    private string assembliesDirectory;

    #region Static initializer

    static HTMLDocumenter()
    {
      systemTypesConversions = new Dictionary<string, string>();

      systemTypesConversions["System.Boolean"] = "bool";
      systemTypesConversions["System.Byte"] = "byte";
      systemTypesConversions["System.Char"] = "char";
      systemTypesConversions["System.Decimal"] = "decimal";
      systemTypesConversions["System.Double"] = "double";
      systemTypesConversions["System.Int16"] = "short";
      systemTypesConversions["System.Int32"] = "int";
      systemTypesConversions["System.Int64"] = "long";
      systemTypesConversions["System.Object"] = "object";
      systemTypesConversions["System.SByte"] = "sbyte";
      systemTypesConversions["System.Single"] = "float";
      systemTypesConversions["System.String"] = "string";
      systemTypesConversions["System.UInt16"] = "ushort";
      systemTypesConversions["System.UInt32"] = "uint";
      systemTypesConversions["System.UInt64"] = "ulong";
      systemTypesConversions["System.Void"] = "void";
    }

    #endregion

    #region Constructor(s)

    public HTMLDocumenter(AssembliesInfo assembliesInfo, string chmFileNameWithoutExtension)
      : base(assembliesInfo)
    {
      this.chmFileNameWithoutExtension = chmFileNameWithoutExtension;
      this.seeRegexEvaluator = new MatchEvaluator(OnSeePatternMatch);

    }

    public HTMLDocumenter(AssembliesInfo assembliesInfo)
      : this(assembliesInfo, null)
    {
    }

    #endregion

    #region Public methods

    public override bool GenerateDocumentation(string outputDirectory, DocumentationGenerationOptions options)
    {
      this.outputDirectory = Path.GetFullPath(outputDirectory);
      this.options = options;

      try
      {
        PrepareOutputDirectory();

        OnGeneratingStarted(EventArgs.Empty);

        GenerateMainIndex();
        ExtractStyleSheets();
        ExtractJavaScripts();
        ExtractGraphics();
        ProcessAssemblies();
        GenerateTableOfContents();

        if (chmFileNameWithoutExtension != null)
        {
          GenerateHTMLHelpTableOfContents();
          GenerateHTMLHelpProjectFile();
          GenerateCHM();
          CleanUpCHM();
        }

        OnGeneratingFinished(EventArgs.Empty);

        return true;
      }
      catch (Exception exc)
      {
#if DEBUG

        Logger.Error(exc.Message + "\n" + Utils.SimplifyStackTrace(exc.StackTrace));

#else

                Logger.Error(exc.Message);

#endif

        return false;
      }
    }

    #endregion

    #region Private helper methods

    private void PrepareOutputDirectory()
    {
      if (Directory.Exists(outputDirectory))
      {
        if ((options & DocumentationGenerationOptions.DeleteOutputDirIfItExists) == 0)
        {
          throw new Exception(String.Format("Output directory '{0}' already exists. You may want to use -ForceDelete option.", outputDirectory));
        }
        else
        {
          OnDirectoryDeleteStarted(EventArgs.Empty);

          Directory.Delete(outputDirectory, true);

          OnDirectoryDeleteFinished(EventArgs.Empty);
        }
      }

      Directory.CreateDirectory(outputDirectory);
    }

    private void GenerateMainIndex()
    {
      string indexTemplateContents = ReadStringResource(MAIN_INDEX_TEMPLATE_FILE_NAME);

      indexTemplateContents = indexTemplateContents.Replace("${DocumentationProjectName}",
                                                            assembliesInfo.ProjectName);

      indexTemplateContents = indexTemplateContents.Replace("${TableOfContentsPage}",
                                                            CONTENTS_DIRECTORY + "/0/0.html");

      indexTemplateContents = indexTemplateContents.Replace("${FirstContentsPage}",
                                                            CONTENTS_DIRECTORY + "/0/1.html");

      WriteStringToFile(Utils.CombinePaths(outputDirectory, MAIN_INDEX_FILE_NAME),
                        indexTemplateContents);
    }

    private void GenerateTableOfContents()
    {
      string tocTemplateContents = ReadStringResource(TABLE_OF_CONTENTS_TEMPLATE_FILE_NAME);

      tocTemplateContents = tocTemplateContents.Replace("${TOCTreeView}",
                                                        CreateTOCTreeView());

      WriteStringToFile(Utils.CombineMultiplePaths(outputDirectory, CONTENTS_DIRECTORY, "0", TABLE_OF_CONTENTS_FILE_NAME),
                        tocTemplateContents);
    }

    private string CreateTOCTreeView()
    {
      StringBuilder sb = new StringBuilder();

      if (assembliesInfo.AssembliesCount > 0)
      {
        sb.Append("<div class=\"TV_NodeContainer\">\n");
        sb.Append("<img id=\"TV_NodeExpansionIcon_1\" class=\"TV_NodeExpansionIcon\" src=\"../../" + GRAPHICS_DIRECTORY + "/TV_Minus.gif\" alt=\"Expand/Collapse\" onclick=\"TV_Node_Clicked('1');\" />\n");
        sb.Append("<span class=\"TV_NodeLabel\"><a href=\"../../" + CONTENTS_DIRECTORY + "/0/" + ASSEMBLIES_INDEX_FILE_NAME + "\" id=\"TV_RootNode\" class=\"TV_NodeLink_Selected\" target=\"ContentsFrame\" onclick=\"TV_NodeLink_Clicked(this, '1');\">Assemblies</a></span>\n");
        sb.Append("</div>\n");
      }
      else
      {
        sb.Append("<div class=\"TV_NodeContainer\">\n");
        sb.Append("<span class=\"TV_NodeLabel\"><a href=\"../../" + CONTENTS_DIRECTORY + "/0/" + ASSEMBLIES_INDEX_FILE_NAME + "\" id=\"TV_RootNode\" class=\"TV_NodeLink_Selected\" target=\"ContentsFrame\" onclick=\"TV_NodeLink_Clicked(this, '1');\">Assemblies</a></span>\n");
        sb.Append("</div>\n");
      }

      if (assembliesInfo.AssembliesCount > 0)
      {
        sb.Append(String.Format("<div class=\"TV_SubtreeContainer\" id=\"TV_Subtree_1\" style=\"visibility: visible; display: block;\">\n"));

        int number = 1;
        foreach (MyAssemblyInfo myAssemblyInfo in assembliesInfo.Assemblies)
        {
          CreateTOCTreeViewAux(myAssemblyInfo, "1_" + number, sb);
          number++;
        }

        sb.Append("</div>\n");
      }

      return sb.ToString();
    }

    private void CreateTOCTreeViewAux(MetaClass metaClass, string treePath, StringBuilder sb)
    {
      Debug.Assert(metaClass != null, "ArgumentNullException(metaClass)");
      Debug.Assert(sb != null, "ArgumentNullException(sb)");
      Debug.Assert(metaClass is ISummarisableMember, "Meta class must implement ISummarisableMember interface.");

      bool hasChildren = HasChildren(metaClass);

      string label = Utils.HTMLEncode(metaClass.Name.Replace('/', '.')) + " " + metaClass.GetMetaName();
      string href = ResolveLink(metaClass);
      string link = String.Format("<a href=\"{0}\" class=\"TV_NodeLink\" target=\"ContentsFrame\" onclick=\"TV_NodeLink_Clicked(this, '{2}');\">{1}</a>", string.IsNullOrEmpty(href) ? "javascript: void(0);" : href, label, treePath);

      string id = "";
      string imgSrc = "../../" + GRAPHICS_DIRECTORY + "/TV_Null.gif";
      string onclick = "";
      string alt = "";

      if (hasChildren)
      {
        id = " id=\"TV_NodeExpansionIcon_" + treePath + "\"";
        onclick = " onclick=\"TV_Node_Clicked('" + treePath + "');\"";
        imgSrc = "../../" + GRAPHICS_DIRECTORY + "/TV_Plus.gif";
        alt = "Expand/Collapse";
      }

      sb.Append("<div class=\"TV_NodeContainer\">\n");
      sb.Append(String.Format("<img{0} class=\"TV_NodeExpansionIcon\" src=\"{1}\"{2} alt=\"{3}\" />\n", id, imgSrc, onclick, alt));
      sb.Append(String.Format("<span class=\"TV_NodeLabel\">{0}</span>\n", link));
      sb.Append("</div>\n");

      if (hasChildren)
      {
        sb.Append(String.Format("<div class=\"TV_SubtreeContainer\" id=\"TV_Subtree_{0}\">\n", treePath));

        if (metaClass is MyAssemblyInfo)
        {
          MyAssemblyInfo myAssemblyInfo = (MyAssemblyInfo)metaClass;
          int number = 1;

          foreach (MyNamespaceInfo myNamespaceInfo in myAssemblyInfo.Namespaces.OrderBy(s => s.DisplayableName))
          {
            CreateTOCTreeViewAux(myNamespaceInfo, treePath + "_" + number, sb);
            number++;
          }
        }
        else if (metaClass is MyNamespaceInfo)
        {
          MyNamespaceInfo myNamespaceInfo = (MyNamespaceInfo)metaClass;
          int number1 = 1;

          IEnumerable<MetaClass> members = myNamespaceInfo.GetEnumerator();

          foreach (MetaClass member in members)
          {
            string newTreePath = treePath + "_" + number1;

            CreateTOCTreeViewAux(member, newTreePath + "_" + number1, sb);

            number1++;
          }
        }
        else if (metaClass is MyClassInfo)
        {
          if (!(metaClass is MyDelegateInfo) && !(metaClass is MyEnumerationInfo))
          {
            MyClassInfo myClassInfo = (MyClassInfo)metaClass;

            id = " id=\"TV_NodeExpansionIcon_" + treePath + "_1" + "\"";
            onclick = " onclick=\"TV_Node_Clicked('" + treePath + "_1" + "');\"";

            string namespaceDirName = GetNamespaceDirName(myClassInfo.Namespace);
            href = GetAliasName(myClassInfo.AssemblyName + "_" + myClassInfo.AssemblyName.GetHashCode()
                              + Path.DirectorySeparatorChar + namespaceDirName + Path.DirectorySeparatorChar
                              + "MS_" + myClassInfo.Name + "_" + myClassInfo.Name.GetHashCode() + ".html",
                                true, true);

            sb.Append("<div class=\"TV_NodeContainer\">\n");
            sb.Append("<img class=\"TV_NodeExpansionIcon\" src=\"../../" + GRAPHICS_DIRECTORY + "/TV_Null.gif\" alt=\"\" />\n");
            sb.Append(String.Format("<span class=\"TV_NodeLabel\"><a href=\"{0}\" class=\"TV_NodeLink\" target=\"ContentsFrame\" onclick=\"TV_NodeLink_Clicked(this, null);\">Members</a></span>\n", href));
            sb.Append("</div>\n");
          }
        }
        else
        {
          Debug.Assert(false, "Impossible! Couldn't recognize type of a metaclass (" + metaClass.GetType() + ").");
        }

        sb.Append("</div>\n");
      }
    }

    private bool HasChildren(MetaClass metaClass)
    {
      if (metaClass is MyAssemblyInfo)
      {
        MyAssemblyInfo myAssemblyInfo = (MyAssemblyInfo)metaClass;

        return myAssemblyInfo.HasMembers;
      }
      else if (metaClass is MyNamespaceInfo)
      {
        MyNamespaceInfo myNamespaceInfo = (MyNamespaceInfo)metaClass;

        return myNamespaceInfo.HasMembers;
      }
      else if (!(metaClass is MyDelegateInfo) && !(metaClass is MyEnumerationInfo) && (metaClass is MyClassInfo))
      {
        return ((MyClassInfo)metaClass).HasMembers;
      }

      return false;
    }

    private void ExtractStyleSheets()
    {
      string cssDir = Utils.CombinePaths(outputDirectory, CSS_DIRECTORY);

      Directory.CreateDirectory(cssDir);

      WriteStringToFile(Utils.CombinePaths(cssDir, CONTENTS_CSS_FILE_NAME),
                        ReadStringResource(CONTENTS_CSS_FILE_NAME));

      WriteStringToFile(Utils.CombinePaths(cssDir, TABLE_OF_CONTENTS_CSS_FILE_NAME),
                        ReadStringResource(TABLE_OF_CONTENTS_CSS_FILE_NAME));

      WriteStringToFile(Utils.CombinePaths(cssDir, TREE_VIEW_CSS_FILE_NAME),
                        ReadStringResource(TREE_VIEW_CSS_FILE_NAME));
    }

    private void ExtractJavaScripts()
    {
      string jsDir = Utils.CombinePaths(outputDirectory, JS_DIRECTORY);

      Directory.CreateDirectory(jsDir);

      WriteStringToFile(Utils.CombinePaths(jsDir, COMMON_JAVA_SCRIPT_FILE_NAME),
                        ReadStringResource(COMMON_JAVA_SCRIPT_FILE_NAME));

      WriteStringToFile(Utils.CombinePaths(jsDir, IMMJSLIB_JAVA_SCRIPT_FILE_NAME),
                        ReadStringResource(IMMJSLIB_JAVA_SCRIPT_FILE_NAME));

      WriteStringToFile(Utils.CombinePaths(jsDir, TREE_VIEW_JAVA_SCRIPT_FILE_NAME),
                        ReadStringResource(TREE_VIEW_JAVA_SCRIPT_FILE_NAME));
    }

    private void ExtractGraphics()
    {
      string graphicsDirectory = Utils.CombinePaths(outputDirectory, GRAPHICS_DIRECTORY);

      Directory.CreateDirectory(graphicsDirectory);

      foreach (string graphicsFile in Icons.FILES_NAMES)
      {
        ExtractBinaryResourceToFile(GRAPHICS_DIRECTORY + "." + graphicsFile,
                                    Utils.CombinePaths(graphicsDirectory, graphicsFile));
      }

      foreach (string graphicsFile in GRAPHICS_FILES_NAMES)
      {
        ExtractBinaryResourceToFile(GRAPHICS_DIRECTORY + "." + graphicsFile,
                                    Utils.CombinePaths(graphicsDirectory, graphicsFile));
      }
    }

    private void ProcessAssemblies()
    {
      assembliesDirectory = Utils.CombinePaths(outputDirectory, CONTENTS_DIRECTORY);

      Directory.CreateDirectory(assembliesDirectory);
      Directory.CreateDirectory(Utils.CombinePaths(assembliesDirectory, "0"));

      CreateAssembliesIndex();

      if (assembliesInfo.AssembliesCount > 0)
      {
        foreach (MyAssemblyInfo myAssemblyInfo in assembliesInfo.Assemblies)
        {
          try
          {
            ProcessAssembly(myAssemblyInfo);
          }
          catch (Exception exc)
          {
#if DEBUG

            Logger.Warning("Couldn't process assembly '{0}'.\n{1}\n{2}", myAssemblyInfo.Name, exc.Message, Utils.SimplifyStackTrace(exc.StackTrace));

#else

                        Logger.Warning("Couldn't process assembly '{0}' ({1}).", myAssemblyInfo.Name, exc.Message);

#endif
          }
        }
      }
    }

    private void CreateAssembliesIndex()
    {
      string indexFileName = Utils.CombineMultiplePaths(assembliesDirectory, "0", ASSEMBLIES_INDEX_FILE_NAME);
      FileStream fs = new FileStream(indexFileName, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      string pageTitle = String.Format("Assemblies");
      string[] sectionsNamesAndIndices = new string[] { "Assemblies:0" };

      WriteIndexHeader(sw, pageTitle, sectionsNamesAndIndices);

      if (assembliesInfo.ProjectSummary == "")
      {
        WriteIndexSummary(sw, PROJECT_SUMMARY);
      }
      else
      {
        WriteIndexSummary(sw, assembliesInfo.ProjectSummary);
      }

      string sectionHeader = "Assemblies";
      int sectionIndex = 0;

      if (assembliesInfo.AssembliesCount > 0)
      {
        WriteMembersIndex(sw, sectionHeader, sectionIndex,
                          assembliesInfo.GetEnumerator(),
                          DEFAULT_MEMBERS_COLUMNS_NAMES,
                          DEFAULT_MEMBERS_COLUMNS_WIDTHS,
                          0, 0);
      }
      else
      {
        WriteIndexSectionBegin(sw, sectionHeader, sectionIndex);
        WriteIndexText(sw, NO_ASSEMBLIES);
        WriteIndexSectionEnd(sw);
      }

      WriteIndexFooter(sw);

      sw.Close();
      fs.Close();
    }

    private void ProcessAssembly(MyAssemblyInfo myAssemblyInfo)
    {
      CreateNamespacesIndex(myAssemblyInfo);

      if (myAssemblyInfo.NamespacesCount > 0)
      {
        foreach (MyNamespaceInfo myNamespaceInfo in myAssemblyInfo.Namespaces)
        {
          try
          {
            ProcessNamespace(myNamespaceInfo);
          }
          catch (Exception exc)
          {
#if DEBUG

            Logger.Warning("Couldn't process namespace '{0}' in assembly '{1}'.\n{2}{3}", myNamespaceInfo.Name, myAssemblyInfo.Name, exc.Message, Utils.SimplifyStackTrace(exc.StackTrace));

#else

                        Logger.Warning("Couldn't process namespace '{0}' in assembly '{1}' ({2}).", myNamespaceInfo.Name, myAssemblyInfo.Name, exc.Message);

#endif
          }
        }
      }
    }

    private void CreateNamespacesIndex(MyAssemblyInfo myAssemblyInfo)
    {
      string indexFileName = Utils.CombineMultiplePaths(myAssemblyInfo.Name + "_" + myAssemblyInfo.Name.GetHashCode(),
                                                        NAMESPACES_INDEX_FILE_NAME);

	  string filename = Utils.CombineMultiplePaths(outputDirectory, CONTENTS_DIRECTORY, GetAliasName(indexFileName));
			
	  FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      string pageTitle = String.Format("{0} Assembly", myAssemblyInfo.Name);
      string[] sectionsNamesAndIndices = new string[] { "Namespaces:0" };

      WriteIndexHeader(sw, pageTitle, sectionsNamesAndIndices);

      WriteIndexSummary(sw, myAssemblyInfo.Summary);

      string sectionHeader = "Namespaces";
      int sectionIndex = 0;
      if (myAssemblyInfo.NamespacesCount > 0)
      {
        WriteMembersIndex(sw, sectionHeader, sectionIndex,
                          myAssemblyInfo.GetEnumerator(),
                          DEFAULT_MEMBERS_COLUMNS_NAMES,
                          DEFAULT_MEMBERS_COLUMNS_WIDTHS,
                          0, 0);
      }
      else
      {
        WriteIndexSectionBegin(sw, sectionHeader, sectionIndex);
        WriteIndexText(sw, NO_NAMESPACES);
        WriteIndexSectionEnd(sw);
      }

      WriteIndexFooter(sw);

      sw.Close();
      fs.Close();
    }

    private void ProcessNamespace(MyNamespaceInfo myNamespaceInfo)
    {
      CreateNamespaceMembersIndex(myNamespaceInfo);

      int namespaceMembersGroupTypeIndex = 0;
      while (Enum.IsDefined(typeof(NamespaceMembersGroups), namespaceMembersGroupTypeIndex))
      {
        NamespaceMembersGroups namespaceMembersGroupType = (NamespaceMembersGroups)namespaceMembersGroupTypeIndex;

        if (myNamespaceInfo.GetMembersCount(namespaceMembersGroupType) > 0)
        {
          Dictionary<string, MetaClass> namespaceMembers = myNamespaceInfo.GetMembers(namespaceMembersGroupType);

          foreach (MetaClass namespaceMember in namespaceMembers.Values)
          {
            ProcessNamespaceMember(namespaceMember, namespaceMembersGroupType);
          }
        }

        namespaceMembersGroupTypeIndex++;
      }
    }

    private void CreateNamespaceMembersIndex(MyNamespaceInfo myNamespaceInfo)
    {
      string namespaceDirName = GetNamespaceDirName(myNamespaceInfo.Name);
      string indexFileName = Utils.CombineMultiplePaths(myNamespaceInfo.AssemblyName + "_" + myNamespaceInfo.AssemblyName.GetHashCode(),
                                                        namespaceDirName,
                                                        TYPES_INDEX_FILE_NAME);

	  string filename = Utils.CombineMultiplePaths(outputDirectory, CONTENTS_DIRECTORY, GetAliasName(indexFileName));
			
      FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      string pageTitle = String.Format("{0} Namespace", myNamespaceInfo.Name);

      string[] sectionsShortcutsNamesAndIndices = ObtainListOfSectionsShortcutsNamesAndIndices(myNamespaceInfo);
      string[] sectionsNames = ObtainListOfSectionsNames(myNamespaceInfo);

      WriteIndexHeader(sw, pageTitle, sectionsShortcutsNamesAndIndices);

      WriteIndexSummary(sw, myNamespaceInfo.Summary);

      WriteIndexItemLocation(sw,
                             null,
                             null,
                             myNamespaceInfo.AssemblyName,
                             null,
                             null,
                             ResolveAssemblyLink(myNamespaceInfo.AssemblyName));

      int sectionIndex = 0;
      int sectionNameIndex = 0;

      int namespaceMembersGroupIndex = 0;
      while (Enum.IsDefined(typeof(NamespaceMembersGroups), namespaceMembersGroupIndex))
      {
        NamespaceMembersGroups namespaceMembersGroupType = (NamespaceMembersGroups)namespaceMembersGroupIndex;

        if (myNamespaceInfo.GetMembersCount(namespaceMembersGroupType) > 0)
        {
          WriteMembersIndex(sw, sectionsNames[sectionNameIndex++], sectionIndex,
                            myNamespaceInfo.GetEnumerator(namespaceMembersGroupType),
                            DEFAULT_MEMBERS_COLUMNS_NAMES,
                            TYPE_MEMBERS_COLUMNS_WIDTHS,
                            namespaceMembersGroupType, 0);
        }

        namespaceMembersGroupIndex++;
        sectionIndex++;
      }

      WriteIndexFooter(sw);

      sw.Close();
      fs.Close();
    }

    private void CreateNamespaceMemberMembersIndex(MetaClass namespaceMember, NamespaceMembersGroups namespaceMembersGroupType)
    {
      if (namespaceMembersGroupType == NamespaceMembersGroups.PublicEnumerations || namespaceMembersGroupType == NamespaceMembersGroups.PublicDelegates
       || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInternalEnumerations || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInternalDelegates
       || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedEnumerations || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedDelegates
       || namespaceMembersGroupType == NamespaceMembersGroups.InternalEnumerations || namespaceMembersGroupType == NamespaceMembersGroups.InternalDelegates
       || namespaceMembersGroupType == NamespaceMembersGroups.PrivateEnumerations || namespaceMembersGroupType == NamespaceMembersGroups.PrivateDelegates)
      {
        Debug.Assert(false, String.Format("Impossible! We don't want to create member's index of type '{0}'.", namespaceMember.GetType()));
        return;
      }

      MyClassInfo myClassInfo = (MyClassInfo)namespaceMember;

      string namespaceDirName = GetNamespaceDirName(myClassInfo.Namespace);
      string indexFileName = Utils.CombineMultiplePaths(myClassInfo.AssemblyName + "_" + myClassInfo.AssemblyName.GetHashCode(),
                                                        namespaceDirName,
                                                        "MS_" + myClassInfo.Name + "_" + myClassInfo.Name.GetHashCode() + ".html");

	  string filename = Utils.CombineMultiplePaths(outputDirectory, CONTENTS_DIRECTORY, GetAliasName(indexFileName));			
			
      FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      string memberTypeName = "";
      switch (namespaceMembersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.PublicInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.PublicStructures: memberTypeName = "Structure"; break;
        case NamespaceMembersGroups.ProtectedInternalClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.ProtectedInternalInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.ProtectedInternalStructures: memberTypeName = "Structure"; break;
        case NamespaceMembersGroups.ProtectedClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.ProtectedInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.ProtectedStructures: memberTypeName = "Structure"; break;
        case NamespaceMembersGroups.InternalClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.InternalInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.InternalStructures: memberTypeName = "Structure"; break;
        case NamespaceMembersGroups.PrivateClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.PrivateInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.PrivateStructures: memberTypeName = "Structure"; break;

        default: Debug.Assert(false, "Impossible! Couldn't obtain member's type name."); break;
      }
      string pageTitle = String.Format("{0} {1} Members", Utils.HTMLEncode(myClassInfo.Name.Replace('/', '.')), memberTypeName);

      string[] sectionsShortcutsNamesAndIndices = ObtainListOfMembersSectionsShortcutsNamesAndIndices(myClassInfo);
      string[] sectionsNames = ObtainListOfMembersSectionsNames(myClassInfo);

      WriteIndexHeader(sw, pageTitle, sectionsShortcutsNamesAndIndices);

      WriteIndexSummary(sw, myClassInfo.Summary);

      WriteIndexItemLocation(sw,
                             myClassInfo.DisplayableName,
                             myClassInfo.Namespace,
                             myClassInfo.AssemblyName,
                             ResolveNamespaceMemberLink(myClassInfo.AssemblyName, myClassInfo.Namespace, myClassInfo.Name),
                             ResolveNamespaceLink(myClassInfo.AssemblyName, myClassInfo.Namespace),
                             ResolveAssemblyLink(myClassInfo.AssemblyName));

      int sectionIndex = 0;
      int sectionNameIndex = 0;
      int classMembersGroupTypeIndex = 0;

      while (Enum.IsDefined(typeof(ClassMembersGroups), classMembersGroupTypeIndex))
      {
        ClassMembersGroups classMembersGroupType = (ClassMembersGroups)classMembersGroupTypeIndex;

        if (myClassInfo.GetMembersCount(classMembersGroupType) > 0)
        {
          WriteMembersIndex(sw, sectionsNames[sectionNameIndex++], sectionIndex,
                            myClassInfo.GetEnumerator(classMembersGroupType),
                            DEFAULT_MEMBERS_COLUMNS_NAMES,
                            TYPE_MEMBERS_COLUMNS_WIDTHS,
                            0, classMembersGroupType);
        }

        classMembersGroupTypeIndex++;
        sectionIndex++;
      }

      WriteIndexFooter(sw);

      sw.Close();
      fs.Close();
    }

    private void ProcessNamespaceMember(MetaClass namespaceMember, NamespaceMembersGroups namespaceMembersGroupType)
    {
      MyClassInfo myClassInfo = (MyClassInfo)namespaceMember;

      string namespaceDirName = GetNamespaceDirName(myClassInfo.Namespace);
      string indexFileName = Utils.CombineMultiplePaths(myClassInfo.AssemblyName + "_" + myClassInfo.AssemblyName.GetHashCode(),
                                                        namespaceDirName,
                                                        "SM_" + myClassInfo.Name + "_" + myClassInfo.Name.GetHashCode() + ".html");

	  string filename = Utils.CombineMultiplePaths(outputDirectory, CONTENTS_DIRECTORY, GetAliasName(indexFileName));			
			
      FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      string memberTypeName = "";
      switch (namespaceMembersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.PublicDelegates: memberTypeName = "Delegate"; break;
        case NamespaceMembersGroups.PublicEnumerations: memberTypeName = "Enumeration"; break;
        case NamespaceMembersGroups.PublicInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.PublicStructures: memberTypeName = "Structure"; break;
        case NamespaceMembersGroups.ProtectedInternalClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.ProtectedInternalDelegates: memberTypeName = "Delegate"; break;
        case NamespaceMembersGroups.ProtectedInternalEnumerations: memberTypeName = "Enumeration"; break;
        case NamespaceMembersGroups.ProtectedInternalInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.ProtectedInternalStructures: memberTypeName = "Structure"; break;
        case NamespaceMembersGroups.ProtectedClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.ProtectedDelegates: memberTypeName = "Delegate"; break;
        case NamespaceMembersGroups.ProtectedEnumerations: memberTypeName = "Enumeration"; break;
        case NamespaceMembersGroups.ProtectedInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.ProtectedStructures: memberTypeName = "Structure"; break;
        case NamespaceMembersGroups.InternalClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.InternalDelegates: memberTypeName = "Delegate"; break;
        case NamespaceMembersGroups.InternalEnumerations: memberTypeName = "Enumeration"; break;
        case NamespaceMembersGroups.InternalInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.InternalStructures: memberTypeName = "Structure"; break;
        case NamespaceMembersGroups.PrivateClasses: memberTypeName = "Class"; break;
        case NamespaceMembersGroups.PrivateDelegates: memberTypeName = "Delegate"; break;
        case NamespaceMembersGroups.PrivateEnumerations: memberTypeName = "Enumeration"; break;
        case NamespaceMembersGroups.PrivateInterfaces: memberTypeName = "Interface"; break;
        case NamespaceMembersGroups.PrivateStructures: memberTypeName = "Structure"; break;

        default: Debug.Assert(false, "Impossible! Couldn't obtain member's type name."); break;
      }
      string pageTitle = String.Format("{0} {1}", Utils.HTMLEncode(myClassInfo.Name.Replace('/', '.')), memberTypeName);

      List<string> sectionsNames = new List<string>();

      sectionsNames.Add("Syntax");

      if (namespaceMember.Remarks != "")
      {
        sectionsNames.Add("Remarks");
      }

      if (namespaceMembersGroupType != NamespaceMembersGroups.PublicDelegates
       && namespaceMembersGroupType != NamespaceMembersGroups.ProtectedInternalDelegates
       && namespaceMembersGroupType != NamespaceMembersGroups.ProtectedDelegates
       && namespaceMembersGroupType != NamespaceMembersGroups.InternalDelegates
       && namespaceMembersGroupType != NamespaceMembersGroups.PrivateDelegates)
      {
        sectionsNames.Add("Members");
      }

      if (myClassInfo.Example != "")
      {
        sectionsNames.Add("Example");
      }

      string[] sectionsNamesAndIndices = new string[sectionsNames.Count];
      for (int i = 0; i < sectionsNamesAndIndices.Length; i++)
      {
        sectionsNamesAndIndices[i] = sectionsNames[i] + ":" + i;
      }

      WriteIndexHeader(sw, pageTitle, sectionsNamesAndIndices);

      WriteIndexSummary(sw, myClassInfo.Summary);

      string declaringTypeLink = null;
      string declaringTypeName = GetDeclaringTypeNameOfANestedType(myClassInfo);

      if (declaringTypeName != null)
      {
        declaringTypeLink = ResolveNamespaceMemberLink(myClassInfo.AssemblyName, myClassInfo.Namespace, declaringTypeName);
        declaringTypeName = declaringTypeName.Replace('/', '.');
      }

      WriteIndexItemLocation(sw,
                             declaringTypeName,
                             myClassInfo.Namespace,
                             myClassInfo.AssemblyName,
                             declaringTypeLink,
                             ResolveNamespaceLink(myClassInfo.AssemblyName, myClassInfo.Namespace),
                             ResolveAssemblyLink(myClassInfo.AssemblyName));

      int sectionIndex = 0;

      WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
      string syntax = CreateNamespaceMemberSyntaxString((MyClassInfo)namespaceMember);
      WriteIndexCodeBlockTable(sw, DEFAULT_LANGUAGE, syntax);

      if (myClassInfo.GenericParametersCount > 0)
      {
        WriteGenericParametersDescriptions(sw, myClassInfo.GenericParameters);
      }

      if (namespaceMembersGroupType == NamespaceMembersGroups.PublicDelegates
       || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInternalDelegates
       || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedDelegates
       || namespaceMembersGroupType == NamespaceMembersGroups.InternalDelegates
       || namespaceMembersGroupType == NamespaceMembersGroups.PrivateDelegates)
      {
        MyDelegateInfo myDelegateInfo = (MyDelegateInfo)myClassInfo;
        WriteParametersAndReturnValueDescriptions(sw, myDelegateInfo.ParametersNames, myDelegateInfo.Parameters,
                                                  myDelegateInfo.ReturnTypeFullName, myDelegateInfo.ReturnValueSummary, false);
      }

      WriteIndexSectionEnd(sw);
      sectionIndex++;

      if (namespaceMember.Remarks != "")
      {
        WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
        WriteIndexRemarks(sw, namespaceMember.Remarks);
        WriteIndexSectionEnd(sw);
        sectionIndex++;
      }

      if (namespaceMembersGroupType != NamespaceMembersGroups.PublicDelegates
       && namespaceMembersGroupType != NamespaceMembersGroups.ProtectedInternalDelegates
       && namespaceMembersGroupType != NamespaceMembersGroups.ProtectedDelegates
       && namespaceMembersGroupType != NamespaceMembersGroups.InternalDelegates
       && namespaceMembersGroupType != NamespaceMembersGroups.PrivateDelegates)
      {
        if (namespaceMembersGroupType == NamespaceMembersGroups.PublicEnumerations
         || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInternalEnumerations
         || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedEnumerations
         || namespaceMembersGroupType == NamespaceMembersGroups.InternalEnumerations
         || namespaceMembersGroupType == NamespaceMembersGroups.PrivateEnumerations)
        {
          if (myClassInfo.GetMembersCount(ClassMembersGroups.PublicFields) == 0)
          {
            WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
            WriteIndexText(sw, NO_MEMBERS);
            WriteIndexSectionEnd(sw);

            sectionIndex++;
          }
          else
          {
            WriteMembersIndex(sw, "Members",
                              sectionIndex,
                              myClassInfo.GetEnumerator(ClassMembersGroups.PublicFields),
                              ENUM_MEMBERS_COLUMNS_NAMES,
                              DEFAULT_MEMBERS_COLUMNS_WIDTHS,
                              namespaceMembersGroupType, 0);

            sectionIndex++;
          }
        }
        else // Classes, Structures and Interfaces
        {
          Debug.Assert(namespaceMembersGroupType == NamespaceMembersGroups.PublicClasses || namespaceMembersGroupType == NamespaceMembersGroups.PublicInterfaces || namespaceMembersGroupType == NamespaceMembersGroups.PublicStructures
                    || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInternalClasses || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInternalInterfaces || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInternalStructures
                    || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedClasses || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInterfaces || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedStructures
                    || namespaceMembersGroupType == NamespaceMembersGroups.InternalClasses || namespaceMembersGroupType == NamespaceMembersGroups.InternalInterfaces || namespaceMembersGroupType == NamespaceMembersGroups.InternalStructures
                    || namespaceMembersGroupType == NamespaceMembersGroups.PrivateClasses || namespaceMembersGroupType == NamespaceMembersGroups.PrivateInterfaces || namespaceMembersGroupType == NamespaceMembersGroups.PrivateStructures,
                       "Impossible! It must've been a class, a structure, an interface or an enumeration.");

          WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
          sectionIndex++;

          if (myClassInfo.HasMembers)
          {
            string href = Utils.CombineMultiplePaths(myClassInfo.AssemblyName + "_" + myClassInfo.AssemblyName.GetHashCode(),
                                                     namespaceDirName,
                                                     "MS_" + myClassInfo.Name + "_" + myClassInfo.Name.GetHashCode() + ".html");

            sw.WriteLine("<p>Click <a href=\"{0}\">here</a> to see the list of members.</p>",
                         GetAliasName(href, true, true));
          }
          else
          {
            WriteIndexText(sw, NO_MEMBERS);
          }

          CreateNamespaceMemberMembersIndex(namespaceMember, namespaceMembersGroupType);

          WriteIndexSectionEnd(sw);
        }
      }

      if (myClassInfo.Example != "")
      {
        Debug.Assert(sectionsNames[sectionIndex] == "Example", "There should be 'Example' section now.");

        WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
        WriteIndexExample(sw, myClassInfo.Example);
        WriteIndexSectionEnd(sw);

        sectionIndex++;
      }

      WriteIndexFooter(sw);

      sw.Close();
      fs.Close();

      ProcessNamespaceMemberMembers(myClassInfo, namespaceMembersGroupType);
    }

    private void ProcessNamespaceMemberMembers(MyClassInfo namespaceMember, NamespaceMembersGroups namespaceMembersGroupType)
    {
      switch (namespaceMembersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.PublicStructures: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.PublicInterfaces: { ProcessInterfaceMembers((MyInterfaceInfo)namespaceMember); break; }
        case NamespaceMembersGroups.PublicDelegates: { break; }
        case NamespaceMembersGroups.PublicEnumerations: { ProcessEnumerationMembers((MyEnumerationInfo)namespaceMember); break; }
        case NamespaceMembersGroups.ProtectedInternalClasses: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.ProtectedInternalStructures: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.ProtectedInternalInterfaces: { ProcessInterfaceMembers((MyInterfaceInfo)namespaceMember); break; }
        case NamespaceMembersGroups.ProtectedInternalDelegates: { break; }
        case NamespaceMembersGroups.ProtectedInternalEnumerations: { ProcessEnumerationMembers((MyEnumerationInfo)namespaceMember); break; }
        case NamespaceMembersGroups.ProtectedClasses: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.ProtectedStructures: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.ProtectedInterfaces: { ProcessInterfaceMembers((MyInterfaceInfo)namespaceMember); break; }
        case NamespaceMembersGroups.ProtectedDelegates: { break; }
        case NamespaceMembersGroups.ProtectedEnumerations: { ProcessEnumerationMembers((MyEnumerationInfo)namespaceMember); break; }
        case NamespaceMembersGroups.InternalClasses: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.InternalStructures: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.InternalInterfaces: { ProcessInterfaceMembers((MyInterfaceInfo)namespaceMember); break; }
        case NamespaceMembersGroups.InternalDelegates: { break; }
        case NamespaceMembersGroups.InternalEnumerations: { ProcessEnumerationMembers((MyEnumerationInfo)namespaceMember); break; }
        case NamespaceMembersGroups.PrivateClasses: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.PrivateStructures: { ProcessClassOrStructureMembers(namespaceMember); break; }
        case NamespaceMembersGroups.PrivateInterfaces: { ProcessInterfaceMembers((MyInterfaceInfo)namespaceMember); break; }
        case NamespaceMembersGroups.PrivateDelegates: { break; }
        case NamespaceMembersGroups.PrivateEnumerations: { ProcessEnumerationMembers((MyEnumerationInfo)namespaceMember); break; }

        default: { Debug.Assert(false, "Impossible! Couldn't recognize type of namespace member."); break; }
      }
    }

    private void ProcessClassOrStructureMembers(MyClassInfo myClassOrStructureInfo)
    {
      string namespaceDirName = GetNamespaceDirName(myClassOrStructureInfo.Namespace);
      string dirName = Utils.CombineMultiplePaths(myClassOrStructureInfo.AssemblyName + "_" + myClassOrStructureInfo.AssemblyName.GetHashCode(),
                                                  namespaceDirName,
                                                  CLASS_MEMBERS_DIRECTORY,
                                                  myClassOrStructureInfo.Name + "_" + myClassOrStructureInfo.Name.GetHashCode());

      List<MetaClass> fields = myClassOrStructureInfo.GetMembers(ClassMembersGroups.PublicFields);

      fields.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedFields));
      fields.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedInternalFields));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        fields.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.InternalFields));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        fields.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.PrivateFields));
      }

      foreach (MyFieldInfo myFieldInfo in fields)
      {
        ProcessField(myFieldInfo, myClassOrStructureInfo, dirName);
      }

      List<MetaClass> constructorsOverloads = myClassOrStructureInfo.GetMembers(ClassMembersGroups.PublicConstructors);

      constructorsOverloads.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedConstructors));
      constructorsOverloads.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedInternalConstructors));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        constructorsOverloads.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.InternalConstructors));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        constructorsOverloads.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.PrivateConstructors));
      }

      foreach (MyInvokableMembersOverloadsInfo myInvokableMembersOverloadsInfo in constructorsOverloads)
      {
        ProcessInvokableMembersOverloads(myInvokableMembersOverloadsInfo, myClassOrStructureInfo, dirName);
      }

      List<MetaClass> methodsOverloads = myClassOrStructureInfo.GetMembers(ClassMembersGroups.PublicMethodsOverloads);

      methodsOverloads.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedMethodsOverloads));
      methodsOverloads.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedInternalMethodsOverloads));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        methodsOverloads.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.InternalMethodsOverloads));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        methodsOverloads.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.PrivateMethodsOverloads));
      }

      foreach (MyInvokableMembersOverloadsInfo myInvokableMembersOverloadsInfo in methodsOverloads)
      {
        ProcessInvokableMembersOverloads(myInvokableMembersOverloadsInfo, myClassOrStructureInfo, dirName);
      }

      List<MetaClass> properties = myClassOrStructureInfo.GetMembers(ClassMembersGroups.PublicPropertiesOverloads);

      properties.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedPropertiesOverloads));
      properties.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedInternalPropertiesOverloads));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        properties.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.InternalPropertiesOverloads));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        properties.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.PrivatePropertiesOverloads));
      }

      foreach (MyPropertiesOverloadsInfo myPropertiesOverloads in properties)
      {
        ProcessPropertiesOverloads(myPropertiesOverloads, myClassOrStructureInfo, dirName);
      }

      List<MetaClass> events = myClassOrStructureInfo.GetMembers(ClassMembersGroups.PublicEvents);

      events.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedEvents));
      events.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.ProtectedInternalEvents));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        events.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.InternalEvents));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        events.AddRange(myClassOrStructureInfo.GetMembers(ClassMembersGroups.PrivateEvents));
      }

      foreach (MyEventInfo myEventInfo in events)
      {
        ProcessEvent(myEventInfo, myClassOrStructureInfo, dirName);
      }
    }

    private void ProcessInterfaceMembers(MyInterfaceInfo myInterfaceInfo)
    {
      string namespaceDirName = GetNamespaceDirName(myInterfaceInfo.Namespace);
      string dirName = Utils.CombineMultiplePaths(myInterfaceInfo.AssemblyName + "_" + myInterfaceInfo.AssemblyName.GetHashCode(),
                                                  namespaceDirName,
                                                  CLASS_MEMBERS_DIRECTORY,
                                                  myInterfaceInfo.Name + "_" + myInterfaceInfo.Name.GetHashCode());

      List<MetaClass> methodsOverloads = myInterfaceInfo.GetMembers(ClassMembersGroups.PublicMethodsOverloads);

      methodsOverloads.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.ProtectedMethodsOverloads));
      methodsOverloads.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.ProtectedInternalMethodsOverloads));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        methodsOverloads.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.InternalMethodsOverloads));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        methodsOverloads.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.PrivateMethodsOverloads));
      }

      foreach (MyInvokableMembersOverloadsInfo myInvokableMembersOverloadsInfo in methodsOverloads)
      {
        ProcessInvokableMembersOverloads(myInvokableMembersOverloadsInfo, myInterfaceInfo, dirName);
      }

      List<MetaClass> properties = myInterfaceInfo.GetMembers(ClassMembersGroups.PublicPropertiesOverloads);

      properties.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.ProtectedPropertiesOverloads));
      properties.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.ProtectedInternalPropertiesOverloads));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        properties.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.InternalPropertiesOverloads));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        properties.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.PrivatePropertiesOverloads));
      }

      foreach (MyPropertiesOverloadsInfo myPropertiesOverloads in properties)
      {
        ProcessPropertiesOverloads(myPropertiesOverloads, myInterfaceInfo, dirName);
      }

      List<MetaClass> events = myInterfaceInfo.GetMembers(ClassMembersGroups.PublicEvents);

      events.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.ProtectedEvents));
      events.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.ProtectedInternalEvents));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        events.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.InternalEvents));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        events.AddRange(myInterfaceInfo.GetMembers(ClassMembersGroups.PrivateEvents));
      }

      foreach (MyEventInfo myEventInfo in events)
      {
        ProcessEvent(myEventInfo, myInterfaceInfo, dirName);
      }
    }

    private void ProcessEnumerationMembers(MyEnumerationInfo myEnumerationInfo)
    {
      string namespaceDirName = GetNamespaceDirName(myEnumerationInfo.Namespace);
      string dirName = Utils.CombineMultiplePaths(myEnumerationInfo.AssemblyName + "_" + myEnumerationInfo.AssemblyName.GetHashCode(),
                                                  namespaceDirName,
                                                  CLASS_MEMBERS_DIRECTORY,
                                                  myEnumerationInfo.Name + "_" + myEnumerationInfo.Name.GetHashCode());

      List<MetaClass> fields = myEnumerationInfo.GetMembers(ClassMembersGroups.PublicFields);

      fields.AddRange(myEnumerationInfo.GetMembers(ClassMembersGroups.ProtectedFields));
      fields.AddRange(myEnumerationInfo.GetMembers(ClassMembersGroups.ProtectedInternalFields));

      if ((options & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        fields.AddRange(myEnumerationInfo.GetMembers(ClassMembersGroups.InternalFields));
      }

      if ((options & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        fields.AddRange(myEnumerationInfo.GetMembers(ClassMembersGroups.PrivateFields));
      }

      foreach (MyFieldInfo myFieldInfo in fields)
      {
        ProcessField(myFieldInfo, myEnumerationInfo, dirName);
      }
    }

    private void ProcessInvokableMembersOverloads(MyInvokableMembersOverloadsInfo myInvokableMembersOverloadsInfo, MyClassInfo declaringType, string dirName)
    {
      if (myInvokableMembersOverloadsInfo.Count > 1)
      {
        CreateInvokableMembersOverloadsIndex(myInvokableMembersOverloadsInfo, declaringType, dirName);
      }

      bool constructors = myInvokableMembersOverloadsInfo[0] is MyConstructorInfo;

      string prefix = constructors ? "C_" : "M_";

      int index = -1;
      foreach (MyInvokableMemberInfo myInvokableMemberInfo in myInvokableMembersOverloadsInfo)
      {
        index++;
        string fileName = Utils.CombineMultiplePaths(dirName, prefix + GetMyInvokableMemberVisibilitModifiersCodeString(myInvokableMemberInfo) + "_" + myInvokableMemberInfo.Name + "_" + index + "_" + myInvokableMemberInfo.Name.GetHashCode() + ".html");

		string filename = Utils.CombineMultiplePaths(outputDirectory, CONTENTS_DIRECTORY, GetAliasName(fileName));				
				
        FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
        StreamWriter sw = new StreamWriter(fs);

        string paramsString = "(";
        bool first = true;
        foreach (string paramName in myInvokableMemberInfo.ParametersNames)
        {
          MyParameterInfo myParameterInfo = myInvokableMemberInfo.Parameters[paramName];

          if (!first)
          {
            paramsString += ", ";
          }

          if (myParameterInfo.IsOut)
          {
            paramsString += "out ";
          }
          else if (myParameterInfo.IsRef)
          {
            paramsString += "ref ";
          }

          paramsString += ProcessType(myParameterInfo.TypeFullName);

          first = false;
        }
        paramsString += ")";

        string pageTitle;
        if (constructors) { pageTitle = Utils.HTMLEncode(declaringType.DisplayableName) + " Constructor " + paramsString; }
        else { pageTitle = Utils.HTMLEncode(declaringType.DisplayableName) + "." + Utils.HTMLEncode(myInvokableMemberInfo.DisplayableName) + " Method " + paramsString; }

        List<string> sectionsNames = new List<string>();

        sectionsNames.Add("Syntax");

        if (myInvokableMemberInfo.ExceptionsDescrs.Count > 0)
        {
          sectionsNames.Add("Exceptions");
        }

        if (myInvokableMemberInfo.Remarks != "")
        {
          sectionsNames.Add("Remarks");
        }

        if (myInvokableMemberInfo.Example != "")
        {
          sectionsNames.Add("Example");
        }

        string[] sectionsNamesAndIndices = new string[sectionsNames.Count];
        for (int i = 0; i < sectionsNamesAndIndices.Length; i++)
        {
          sectionsNamesAndIndices[i] = sectionsNames[i] + ":" + i;
        }

        WriteIndexHeader(sw, pageTitle, sectionsNamesAndIndices);

        WriteIndexSummary(sw, myInvokableMemberInfo.Summary);

        WriteIndexItemLocation(sw, declaringType.DisplayableName, declaringType.Namespace, declaringType.AssemblyName,
                               ResolveNamespaceMemberLink(declaringType.AssemblyName, declaringType.Namespace, declaringType.Name),
                               ResolveNamespaceLink(declaringType.AssemblyName, declaringType.Namespace),
                               ResolveAssemblyLink(declaringType.AssemblyName));

        int sectionIndex = 0;

        WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);

        string syntax = CreateInvokableMemberSyntaxString(myInvokableMemberInfo);
        WriteIndexCodeBlockTable(sw, DEFAULT_LANGUAGE, syntax);

        if (myInvokableMemberInfo is MyMethodInfo)
        {
          MyMethodInfo myMethodInfo = (MyMethodInfo)myInvokableMemberInfo;

          if (myMethodInfo.GenericParametersCount > 0)
          {
            WriteGenericParametersDescriptions(sw, myMethodInfo.GenericParameters);
          }
        }

        WriteParametersAndReturnValueDescriptions(sw, myInvokableMemberInfo.ParametersNames, myInvokableMemberInfo.Parameters,
                                                  constructors ? null : ((MyMethodInfo)myInvokableMemberInfo).ReturnTypeFullName,
                                                  constructors ? null : ((MyMethodInfo)myInvokableMemberInfo).ReturnValueSummary,
                                                  false);

        WriteIndexSectionEnd(sw);

        sectionIndex++;

        if (myInvokableMemberInfo.ExceptionsDescrs.Count > 0)
        {
          Debug.Assert(sectionsNames[sectionIndex] == "Exceptions", "Exceptions section was expected.");

          WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
          WriteIndexExceptionsTable(sw, myInvokableMemberInfo.ExceptionsDescrs);
          WriteIndexSectionEnd(sw);

          sectionIndex++;
        }

        if (myInvokableMemberInfo.Remarks != "")
        {
          Debug.Assert(sectionsNames[sectionIndex] == "Remarks", "Remarks section was expected.");

          WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
          WriteIndexRemarks(sw, myInvokableMemberInfo.Remarks);
          WriteIndexSectionEnd(sw);

          sectionIndex++;
        }

        if (myInvokableMemberInfo.Example != "")
        {
          Debug.Assert(sectionsNames[sectionIndex] == "Example", "There should be 'Example' section now.");

          WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
          WriteIndexExample(sw, myInvokableMemberInfo.Example);
          WriteIndexSectionEnd(sw);

          sectionIndex++;
        }

        WriteIndexFooter(sw);

        sw.Close();
        fs.Close();
      }
    }

    private void ProcessPropertiesOverloads(MyPropertiesOverloadsInfo myPropertiesOverloadsInfo, MyClassInfo declaringType, string dirName)
    {
      if (myPropertiesOverloadsInfo.Count > 1)
      {
        CreatePropertiesOverloadsIndex(myPropertiesOverloadsInfo, declaringType, dirName);
      }

      string prefix = "P_";

      int index = -1;
      foreach (MyPropertyInfo myPropertyInfo in myPropertiesOverloadsInfo)
      {
        index++;
        string fileName = Utils.CombineMultiplePaths(dirName, prefix + GetMyPropertyInfoVisibilitModifiersCodeString(myPropertyInfo) + "_" + myPropertyInfo.Name + "_" + index + "_" + myPropertyInfo.Name.GetHashCode() + ".html");

	    string filename = Utils.CombineMultiplePaths(outputDirectory, CONTENTS_DIRECTORY, GetAliasName(fileName));
				
        FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
        StreamWriter sw = new StreamWriter(fs);

        string paramsString = "(";
        bool first = true;
        foreach (string paramName in myPropertyInfo.ParametersNames)
        {
          MyParameterInfo myParameterInfo = myPropertyInfo.Parameters[paramName];

          if (!first)
          {
            paramsString += ", ";
          }

          paramsString += ProcessType(myParameterInfo.TypeFullName);

          first = false;
        }
        paramsString += ")";

        if (paramsString == "()") { paramsString = ""; }

        string pageTitle = Utils.HTMLEncode(declaringType.DisplayableName) + "." + Utils.HTMLEncode(myPropertyInfo.DisplayableName) + " Property " + paramsString;

        List<string> sectionsNames = new List<string>();

        sectionsNames.Add("Syntax");

        if (myPropertyInfo.ExceptionsDescrs.Count > 0)
        {
          sectionsNames.Add("Exceptions");
        }

        if (myPropertyInfo.Remarks != "")
        {
          sectionsNames.Add("Remarks");
        }

        if (myPropertyInfo.Example != "")
        {
          sectionsNames.Add("Example");
        }

        string[] sectionsNamesAndIndices = new string[sectionsNames.Count];
        for (int i = 0; i < sectionsNamesAndIndices.Length; i++)
        {
          sectionsNamesAndIndices[i] = sectionsNames[i] + ":" + i;
        }

        WriteIndexHeader(sw, pageTitle, sectionsNamesAndIndices);

        WriteIndexSummary(sw, myPropertyInfo.Summary);

        WriteIndexItemLocation(sw, declaringType.DisplayableName, declaringType.Namespace, declaringType.AssemblyName,
                               ResolveNamespaceMemberLink(declaringType.AssemblyName, declaringType.Namespace, declaringType.Name),
                               ResolveNamespaceLink(declaringType.AssemblyName, declaringType.Namespace),
                               ResolveAssemblyLink(declaringType.AssemblyName));

        int sectionIndex = 0;

        WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);

        string syntax = CreatePropertySyntaxString(myPropertyInfo);
        WriteIndexCodeBlockTable(sw, DEFAULT_LANGUAGE, syntax);

        WriteParametersAndReturnValueDescriptions(sw, myPropertyInfo.ParametersNames, myPropertyInfo.Parameters,
                                                  ((MyPropertyInfo)myPropertyInfo).TypeFullName,
                                                  ((MyPropertyInfo)myPropertyInfo).ReturnValueSummary,
                                                  true);

        WriteIndexSectionEnd(sw);

        sectionIndex++;

        if (myPropertyInfo.ExceptionsDescrs.Count > 0)
        {
          Debug.Assert(sectionsNames[sectionIndex] == "Exceptions", "Exceptions section was expected.");

          WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
          WriteIndexExceptionsTable(sw, myPropertyInfo.ExceptionsDescrs);
          WriteIndexSectionEnd(sw);

          sectionIndex++;
        }

        if (myPropertyInfo.Remarks != "")
        {
          Debug.Assert(sectionsNames[sectionIndex] == "Remarks", "Remarks section was expected.");

          WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
          WriteIndexRemarks(sw, myPropertyInfo.Remarks);
          WriteIndexSectionEnd(sw);

          sectionIndex++;
        }

        if (myPropertyInfo.Example != "")
        {
          Debug.Assert(sectionsNames[sectionIndex] == "Example", "There should be 'Example' section now.");

          WriteIndexSectionBegin(sw, sectionsNames[sectionIndex], sectionIndex);
          WriteIndexExample(sw, myPropertyInfo.Example);
          WriteIndexSectionEnd(sw);

          sectionIndex++;
        }

        WriteIndexFooter(sw);

        sw.Close();
        fs.Close();
      }
    }

    private string CreateInvokableMemberSyntaxString(MyInvokableMemberInfo myInvokableMemberInfo)
    {
      string returnType = "";
      List<MyGenericParameterInfo> genericParameters = null;

      if (myInvokableMemberInfo is MyMethodInfo)
      {
        MyMethodInfo myMethodInfo = (MyMethodInfo)myInvokableMemberInfo;

        returnType = " " + ProcessType(myMethodInfo.ReturnTypeFullName);
        genericParameters = myMethodInfo.GenericParametersCount == 0 || !myMethodInfo.ContainsGenericParameterWithConstraints ? null : myMethodInfo.GenericParameters;
      }

      return CreateInvokableMemberOrDelegateSyntaxString(myInvokableMemberInfo.AttributesString,
                                                         returnType,
                                                         Utils.HTMLEncode(myInvokableMemberInfo.DisplayableName),
                                                         myInvokableMemberInfo.ParametersNames,
                                                         myInvokableMemberInfo.Parameters,
                                                         genericParameters);
    }

    private string CreatePropertySyntaxString(MyPropertyInfo myPropertyInfo)
    {
      string returnType = " " + ProcessType(((MyPropertyInfo)myPropertyInfo).TypeFullName);
      string name = myPropertyInfo.ParametersNames.Count > 0 ? "this" : Utils.HTMLEncode(myPropertyInfo.DisplayableName);
      string getSetString;

      if (myPropertyInfo.HasGetter && myPropertyInfo.HasSetter) { getSetString = "{ get; set; }"; }
      else if (myPropertyInfo.HasGetter) { getSetString = "{ get; }"; }
      else { getSetString = "{ set; }"; }

      return CreateInvokableMemberOrDelegateSyntaxString(myPropertyInfo.AttributesString,
                                                         returnType,
                                                         name,
                                                         myPropertyInfo.ParametersNames,
                                                         myPropertyInfo.Parameters,
                                                         null,
                                                         '[', ']', true,
                                                         getSetString);
    }

    private string CreateInvokableMemberOrDelegateSyntaxString(string attributesString,
                                                               string returnTypeFullName,
                                                               string memberDisplayableName,
                                                               List<string> parametersNames,
                                                               Dictionary<string, MyParameterInfo> parameters,
                                                               List<MyGenericParameterInfo> genericParameters,
                                                               char openingChar,
                                                               char closingChar,
                                                               bool dontIncludeBracketsIfNoParameters,
                                                               string suffix)
    {
      StringBuilder result = new StringBuilder("<pre style=\"margin-left: 2px;\">");
      int count = parametersNames.Count;

      if (memberDisplayableName == "operator implicit" || memberDisplayableName == "operator explicit")
      {
        string implOrExpl = memberDisplayableName == "operator implicit" ? "implicit" : "explicit";

        result.Append(String.Format("{0} {1} operator{2} {3}",
                                    attributesString,
                                    implOrExpl,
                                    returnTypeFullName,
                                    dontIncludeBracketsIfNoParameters ? (count == 0 ? "" : "" + openingChar) : "" + openingChar));
      }
      else
      {
        result.Append(String.Format("{0}{1} {2} {3}",
                      attributesString,
                      returnTypeFullName,
                      memberDisplayableName,
                      dontIncludeBracketsIfNoParameters ? (count == 0 ? "" : "" + openingChar) : "" + openingChar));
      }

      if (count > 0)
      {
        for (int i = 0; i < count; i++)
        {
          MyParameterInfo myParameterInfo = parameters[parametersNames[i]];

          result.Append('\n');

          result.Append(String.Format("{0}{1}{2} <i>{3}</i>{4}", PARAM_INDENT, myParameterInfo.AttributesString == "" ? "" : myParameterInfo.AttributesString + " ", ProcessType(myParameterInfo.TypeFullName), myParameterInfo.Name, i < count - 1 ? "," : ""));
        }
        result.Append("\n" + (dontIncludeBracketsIfNoParameters ? (count == 0 ? "" : "" + closingChar) : "" + closingChar) + " ");
      }
      else
      {
        result.Append(dontIncludeBracketsIfNoParameters ? (count == 0 ? "" : "" + closingChar) : "" + closingChar);
      }

      result.Append(suffix + "</pre>");

      if (genericParameters != null)
      {
        result.Append('\n');
        WriteGenericParametersConstraints(genericParameters, result);
      }

      return result.ToString();
    }

    private string CreateInvokableMemberOrDelegateSyntaxString(string attributesString,
                                                               string returnTypeFullName,
                                                               string memberDisplayableName,
                                                               List<string> parametersNames,
                                                               Dictionary<string, MyParameterInfo> parameters,
                                                               List<MyGenericParameterInfo> genericParameters)
    {
      return CreateInvokableMemberOrDelegateSyntaxString(attributesString, returnTypeFullName, memberDisplayableName, parametersNames, parameters, genericParameters, '(', ')', false, "");
    }

    private string CreateNamespaceMemberSyntaxString(MyClassInfo namespaceMember)
    {
      string name = Utils.HTMLEncode(Utils.GetUnqualifiedName(namespaceMember.DisplayableName));
      string metaName = "class";

      if (namespaceMember is MyStructureInfo) { metaName = "struct"; }
      else if (namespaceMember is MyInterfaceInfo) { metaName = "interface"; }
      else if (namespaceMember is MyDelegateInfo)
      {
        MyDelegateInfo myDelegateInfo = (MyDelegateInfo)namespaceMember;

        List<MyGenericParameterInfo> genericParameters = myDelegateInfo.GenericParametersCount == 0 || !myDelegateInfo.ContainsGenericParameterWithConstraints ? null : myDelegateInfo.GenericParameters;

        return CreateInvokableMemberOrDelegateSyntaxString(myDelegateInfo.AttributesString,
                                                           " " + ProcessType(myDelegateInfo.ReturnTypeFullName),
                                                           name,
                                                           myDelegateInfo.ParametersNames,
                                                           myDelegateInfo.Parameters,
                                                           genericParameters);
      }
      else if (namespaceMember is MyEnumerationInfo) { metaName = "enum"; }

      return CreateNamespaceMemberSyntaxTable(namespaceMember, name, metaName);
    }

    private string CreateNamespaceMemberSyntaxTable(MyClassInfo namespaceMember, string name, string metaName)
    {
      StringBuilder sb = new StringBuilder();

      sb.Append("<table style=\"width: 100%;\" class=\"InsideCodeBlock\">\n");

      sb.Append("<col width=\"0%\" />\n");
      sb.Append("<col width=\"100%\" />\n");

      string baseTypeName = null;

      if (namespaceMember is MyEnumerationInfo)
      {
        baseTypeName = ((MyEnumerationInfo)namespaceMember).UnderlyingTypeFullName;
      }
      else if (!(namespaceMember is MyStructureInfo) && !(namespaceMember is MyDelegateInfo) && !(namespaceMember is MyInterfaceInfo))
      {
        if (namespaceMember.BaseTypeName != null && namespaceMember.BaseTypeName.ToLower() != "system.object")
        {
          baseTypeName = namespaceMember.BaseTypeName;
        }
      }

      bool hasBaseTypeOrInterfaces = baseTypeName != null || namespaceMember.ImplementedInterfacesNames.Count > 0;

      sb.Append("<tr>\n");
      sb.Append(String.Format("<td class=\"NoWrapTop\">{0} {1} {2} {3}</td>\n", namespaceMember.AttributesString,
                                                                                metaName,
                                                                                name,
                                                                                hasBaseTypeOrInterfaces ? ":&nbsp;" : ""));

      if (hasBaseTypeOrInterfaces)
      {
        sb.Append("<td>\n");

        if (baseTypeName != null)
        {
          sb.Append(ProcessType(baseTypeName));
        }

        if (!(namespaceMember is MyEnumerationInfo))
        {
          string implementedInterfaces = GetImlpementedInterfacesString(namespaceMember.ImplementedInterfacesNames);

          if (baseTypeName != null)
          {
            if (implementedInterfaces != "")
            {
              sb.Append(',');
            }

            sb.Append("<br />");
          }

          sb.Append(implementedInterfaces);
        }

        sb.Append("\n</td>\n");
      }
      else
      {
        sb.Append("<td>&nbsp;</td>\n");
      }

      sb.Append("</tr>\n");
      sb.Append("</table>\n");

      if (namespaceMember.GenericParametersCount > 0 && namespaceMember.ContainsGenericParameterWithConstraints)
      {
        WriteGenericParametersConstraints(namespaceMember.GenericParameters, sb);
      }

      return sb.ToString();
    }

    private void CreateInvokableMembersOverloadsIndex(MyInvokableMembersOverloadsInfo myInvokableMembersOverloadsInfo, MyClassInfo declaringType, string dirName)
    {
      Debug.Assert(myInvokableMembersOverloadsInfo.Count > 1, "Impossible! We don't create overloads index when there are no overloads.");

      bool constructors = myInvokableMembersOverloadsInfo[0] is MyConstructorInfo;
      string prefix = constructors ? "CO_" : "MO_";

      string fileName = Utils.CombineMultiplePaths(dirName, prefix + GetMyInvokableMemberVisibilitModifiersCodeString(myInvokableMembersOverloadsInfo[0]) + "_" + myInvokableMembersOverloadsInfo.Name + "_" + myInvokableMembersOverloadsInfo.Name.GetHashCode() + ".html");

      FileStream fs = new FileStream(GetAliasName(fileName), FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      string pageTitle;
      if (constructors)
      {
        pageTitle = String.Format("{0} Constructor", Utils.HTMLEncode(declaringType.DisplayableName));
      }
      else
      {
        pageTitle = String.Format("{0}.{1} Method", Utils.HTMLEncode(declaringType.DisplayableName), Utils.HTMLEncode(myInvokableMembersOverloadsInfo[0].DisplayableName));
      }

      string[] sectionsNamesAndIndices = { "Overload List:0" };
      int sectionIndex = 0;

      WriteIndexHeader(sw, pageTitle, sectionsNamesAndIndices);

      WriteIndexSummary(sw, myInvokableMembersOverloadsInfo.SummaryWithoutPrefix);

      WriteIndexItemLocation(sw, declaringType.DisplayableName, declaringType.Namespace, declaringType.AssemblyName,
                             ResolveNamespaceMemberLink(declaringType.AssemblyName, declaringType.Namespace, declaringType.Name),
                             ResolveNamespaceLink(declaringType.AssemblyName, declaringType.Namespace),
                             ResolveAssemblyLink(declaringType.AssemblyName));

      WriteIndexSectionBegin(sw,
                             sectionsNamesAndIndices[sectionIndex].Substring(0, sectionsNamesAndIndices[sectionIndex].LastIndexOf(':')),
                             sectionIndex);

      WriteIndexMembersTableBegin(sw,
                                  DEFAULT_MEMBERS_COLUMNS_NAMES,
                                  TYPE_MEMBERS_COLUMNS_WIDTHS);

      int index = -1;
      foreach (MyInvokableMemberInfo myInvokableMemberInfo in myInvokableMembersOverloadsInfo)
      {
        IconsTypes iconsTypes = GetIconsTypes(myInvokableMemberInfo, 0, 0);

        index++;

        string link = ResolveInvokableMemberLink(myInvokableMemberInfo, declaringType, index);

        string name;
        if (myInvokableMemberInfo is MyConstructorInfo)
        {
          name = myInvokableMemberInfo.DisplayableName + " (";
        }
        else
        {
          Debug.Assert(myInvokableMemberInfo is MyMethodInfo, "Impossible!");

          name = Utils.HTMLEncode(declaringType.DisplayableName + "." + myInvokableMemberInfo.DisplayableName + " (");
        }

        if (myInvokableMemberInfo.Parameters.Count > 0)
        {
          bool first = true;
          foreach (string parameterName in myInvokableMemberInfo.ParametersNames)
          {
            MyParameterInfo myParameterInfo = myInvokableMemberInfo.Parameters[parameterName];
            string processedType = ProcessType(myParameterInfo.TypeFullName);

            if (!first)
            {
              name += ", ";
            }

            if (myParameterInfo.IsOut)
            {
              name += "out ";
            }
            else if (myParameterInfo.IsRef)
            {
              name += "ref ";
            }

            name += processedType;

            first = false;
          }
        }

        WriteIndexMembersTableRow(sw,
                                  name + ")",
                                  link,
                                  myInvokableMemberInfo.Summary,
                                  Icons.GetFileNames(iconsTypes),
                                  Icons.GetAltLabels(iconsTypes));
      }

      WriteIndexMembersTableEnd(sw);

      WriteIndexSectionEnd(sw);

      WriteIndexFooter(sw);

      sw.Close();
      fs.Close();
    }

    private void CreatePropertiesOverloadsIndex(MyPropertiesOverloadsInfo myPropertiessOverloadsInfo, MyClassInfo declaringType, string dirName)
    {
      Debug.Assert(myPropertiessOverloadsInfo.Count > 1, "Impossible! We don't create overloads index when there are no overloads.");

      string prefix = "PO_";

      string fileName = Utils.CombineMultiplePaths(dirName, prefix + GetMyPropertyInfoVisibilitModifiersCodeString(myPropertiessOverloadsInfo[0]) + "_" + myPropertiessOverloadsInfo.Name + "_" + myPropertiessOverloadsInfo.Name.GetHashCode() + ".html");

      FileStream fs = new FileStream(GetAliasName(fileName), FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      string pageTitle = String.Format("{0}.{1} Property", Utils.HTMLEncode(declaringType.DisplayableName), Utils.HTMLEncode(myPropertiessOverloadsInfo[0].DisplayableName));

      string[] sectionsNamesAndIndices = { "Overload List:0" };
      int sectionIndex = 0;

      WriteIndexHeader(sw, pageTitle, sectionsNamesAndIndices);

      WriteIndexSummary(sw, myPropertiessOverloadsInfo.SummaryWithoutPrefix);

      WriteIndexItemLocation(sw, declaringType.DisplayableName, declaringType.Namespace, declaringType.AssemblyName,
                             ResolveNamespaceMemberLink(declaringType.AssemblyName, declaringType.Namespace, declaringType.Name),
                             ResolveNamespaceLink(declaringType.AssemblyName, declaringType.Namespace),
                             ResolveAssemblyLink(declaringType.AssemblyName));

      WriteIndexSectionBegin(sw,
                             sectionsNamesAndIndices[sectionIndex].Substring(0, sectionsNamesAndIndices[sectionIndex].LastIndexOf(':')),
                             sectionIndex);

      WriteIndexMembersTableBegin(sw,
                                  DEFAULT_MEMBERS_COLUMNS_NAMES,
                                  TYPE_MEMBERS_COLUMNS_WIDTHS);

      int index = -1;
      foreach (MyPropertyInfo myPropertyInfo in myPropertiessOverloadsInfo)
      {
        IconsTypes iconsTypes = GetIconsTypes(myPropertyInfo, 0, 0);

        index++;

        string link = ResolvePropertyLink(myPropertyInfo, declaringType, index);

        string displayableName = Utils.HTMLEncode(myPropertyInfo.DisplayableName);
        string name = Utils.HTMLEncode(declaringType.DisplayableName + "." + displayableName + " (");

        if (myPropertyInfo.Parameters.Count > 0)
        {
          bool first = true;
          foreach (string parameterName in myPropertyInfo.ParametersNames)
          {
            MyParameterInfo myParameterInfo = myPropertyInfo.Parameters[parameterName];
            string processedType = ProcessType(myParameterInfo.TypeFullName);

            if (!first)
            {
              name += ", ";
            }

            name += processedType;

            first = false;
          }
        }

        WriteIndexMembersTableRow(sw,
                                  name + ")",
                                  link,
                                  myPropertyInfo.Summary,
                                  Icons.GetFileNames(iconsTypes),
                                  Icons.GetAltLabels(iconsTypes));
      }

      WriteIndexMembersTableEnd(sw);

      WriteIndexSectionEnd(sw);

      WriteIndexFooter(sw);

      sw.Close();
      fs.Close();
    }

    private void ProcessField(MyFieldInfo myFieldInfo, MyClassInfo declaringType, string dirName)
    {
      string constValue = "";
      if (myFieldInfo.IsConst)
      {
        if (myFieldInfo.ConstantValue == null)
        {
          constValue = " = null";
        }
        else if (myFieldInfo.TypeFullName.ToLower() == "system.string")
        {
          constValue = " = \"" + myFieldInfo.ConstantValue + "\"";
        }
        else
        {
          constValue = " = " + myFieldInfo.ConstantValue;
        }
      }

      string syntax = String.Format("{0} {1} {2}{3}", myFieldInfo.AttributesString,
                                                      ProcessType(myFieldInfo.TypeFullName),
                                                      myFieldInfo.DisplayableName,
                                                      constValue);

      ProcessFieldOrEvent("F_", myFieldInfo.Name, "Field", declaringType, dirName,
                          myFieldInfo.Summary, myFieldInfo.Remarks, syntax, myFieldInfo.Example);
    }

    private void ProcessEvent(MyEventInfo myEventInfo, MyClassInfo declaringType, string dirName)
    {
      string syntax = String.Format("{0} event {1} {2}", myEventInfo.AttributesString,
                                                         ProcessType(myEventInfo.TypeFullName),
                                                         myEventInfo.DisplayableName);

      ProcessFieldOrEvent("E_", myEventInfo.Name, "Field", declaringType, dirName,
                          myEventInfo.Summary, myEventInfo.Remarks, syntax, myEventInfo.Example);


    }

    private void ProcessFieldOrEvent(string prefix, string name, string metaName, MyClassInfo declaringType, string dirName, string summary, string remarks, string syntax, string example)
    {
      string fileName = Utils.CombineMultiplePaths(dirName, prefix + name + "_" + name.GetHashCode() + ".html");
      		
	  string filename = Utils.CombineMultiplePaths(outputDirectory, CONTENTS_DIRECTORY, GetAliasName(fileName));
			
	  FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      string pageTitle = String.Format("{0}.{1} {2}", Utils.HTMLEncode(declaringType.DisplayableName), name, metaName);
      string[] sectionsNamesAndIndices;

      if (remarks == null || remarks == "")
      {
        if (example == null || example == "")
        {
          sectionsNamesAndIndices = new string[] { "Syntax:0" };
        }
        else
        {
          sectionsNamesAndIndices = new string[] { "Syntax:0", "Example:1" };
        }
      }
      else
      {
        if (example == null || example == "")
        {
          sectionsNamesAndIndices = new string[] { "Syntax:0", "Remarks:1" };
        }
        else
        {
          sectionsNamesAndIndices = new string[] { "Syntax:0", "Remarks:1", "Example:2" };
        }
      }

      int sectionIndex = 0;

      WriteIndexHeader(sw, pageTitle, sectionsNamesAndIndices);

      WriteIndexSummary(sw, summary);

      WriteIndexItemLocation(sw, declaringType.DisplayableName, declaringType.Namespace, declaringType.AssemblyName,
                             ResolveNamespaceMemberLink(declaringType.AssemblyName, declaringType.Namespace, declaringType.Name),
                             ResolveNamespaceLink(declaringType.AssemblyName, declaringType.Namespace),
                             ResolveAssemblyLink(declaringType.AssemblyName));

      WriteIndexSectionBegin(sw,
                             sectionsNamesAndIndices[sectionIndex].Substring(0, sectionsNamesAndIndices[sectionIndex].LastIndexOf(':')),
                             sectionIndex);

      WriteIndexCodeBlockTable(sw, DEFAULT_LANGUAGE, syntax);
      WriteIndexSectionEnd(sw);

      sectionIndex++;

      if (remarks != "")
      {
        WriteIndexSectionBegin(sw,
                               sectionsNamesAndIndices[sectionIndex].Substring(0, sectionsNamesAndIndices[sectionIndex].LastIndexOf(':')),
                               sectionIndex);

        WriteIndexRemarks(sw, remarks);

        WriteIndexSectionEnd(sw);

        sectionIndex++;
      }

      if (example != "")
      {
        string sectionName = sectionsNamesAndIndices[sectionIndex].Substring(0, sectionsNamesAndIndices[sectionIndex].LastIndexOf(':'));

        Debug.Assert(sectionName == "Example", "There should be 'Example' section now.");

        WriteIndexSectionBegin(sw, sectionName, sectionIndex);
        WriteIndexExample(sw, example);
        WriteIndexSectionEnd(sw);

        sectionIndex++;
      }

      WriteIndexFooter(sw);

      sw.Close();
      fs.Close();
    }

    private void WriteParametersAndReturnValueDescriptions(StreamWriter sw, List<string> parametersNames, Dictionary<string, MyParameterInfo> parameters, string returnTypeFullName, string returnValueSummary, bool isProperty)
    {
      if (parametersNames.Count > 0)
      {
        WriteIndexCommentHeader(sw, "Parameters");

        foreach (string paramName in parametersNames)
        {
          MyParameterInfo myParameterInfo = parameters[paramName];

          WriteIndexParameterNameCommentHeader(sw, myParameterInfo.Name);
          WriteIndexParamOrReturnValueDescription(sw, myParameterInfo.Summary);
        }
      }

      if (returnTypeFullName != null && returnTypeFullName.ToLower() != "system.void")
      {
        if (isProperty)
        {
          WriteIndexCommentHeader(sw, "Property Value");
        }
        else
        {
          WriteIndexCommentHeader(sw, "Return Value");
        }

        WriteIndexParamOrReturnValueDescription(sw, returnValueSummary);
      }
    }

    private void WriteGenericParametersDescriptions(StreamWriter sw, List<MyGenericParameterInfo> genericParameters)
    {
      if (genericParameters.Count > 0)
      {
        WriteIndexCommentHeader(sw, "Type Parameters");

        foreach (MyGenericParameterInfo myGenericParameterInfo in genericParameters)
        {
          WriteIndexParameterNameCommentHeader(sw, myGenericParameterInfo.Name);
          WriteIndexParamOrReturnValueDescription(sw, myGenericParameterInfo.Summary);
        }
      }
    }

    private string GetNamespaceDirName(string namezpace)
    {
      StringBuilder result = new StringBuilder(namezpace.Length);
      string[] splitted = namezpace.Split('.');

      for (int i = 0; i < splitted.Length; i++)
      {
        result.Append(splitted[i]);
        result.Append('_');
        result.Append(splitted[i].GetHashCode());

        if (i < splitted.Length - 1)
        {
          result.Append(Path.DirectorySeparatorChar);
        }
      }

      return result.ToString();
    }

    private string GetImlpementedInterfacesString(List<string> implementedInterfacesNames)
    {
      StringBuilder implementedInterfaces = new StringBuilder();

      if (implementedInterfacesNames.Count > 0)
      {
        for (int i = 0; i < implementedInterfacesNames.Count; i++)
        {
          if (i > 0)
          {
            implementedInterfaces.Append(",<br />\n");
          }

          implementedInterfaces.Append(ProcessType(implementedInterfacesNames[i]));
        }
      }

      return implementedInterfaces.ToString();
    }

    private static void WriteGenericParametersConstraints(List<MyGenericParameterInfo> genericParameters, StringBuilder sb, int marginTopInPx, bool withoutLeftMargin)
    {
      if (withoutLeftMargin)
      {
        sb.Append(String.Format("<div style=\"margin-top: {0}px;\">\n", marginTopInPx));
      }
      else
      {
        sb.Append(String.Format("<div style=\"padding-left: 30px; margin-top: {0}px;\">\n", marginTopInPx));
      }
      sb.Append("<table class=\"InsideCodeBlock\">\n");
      sb.Append("<col />\n");
      sb.Append("<col width=\"100%\" />\n");

      foreach (MyGenericParameterInfo myGenericParamterInfo in genericParameters)
      {
        if (myGenericParamterInfo.ConstraintsCount == 0) { continue; }

        sb.Append("<tr>\n");
        sb.Append(String.Format("<td class=\"NoWrapTop\">where {0}</td>\n", myGenericParamterInfo.Name));
        sb.Append("<td>&nbsp;: ");

        bool first = true;

        foreach (GenericConstraint genericConstraint in myGenericParamterInfo.Constraints)
        {
          string constraintStr = genericConstraint.ToString();

          if (genericConstraint.NeedsTypeProcessing)
          {
            constraintStr = ProcessType(constraintStr);
          }

          if (!first) { sb.Append(", "); }

          sb.Append(constraintStr);

          first = false;
        }

        sb.Append("</td>\n");
        sb.Append("</td>\n</tr>\n");
      }

      sb.Append("</table>\n");
      sb.Append("</div>\n");
    }

    private static void WriteGenericParametersConstraints(List<MyGenericParameterInfo> genericParameters, StringBuilder sb)
    {
      WriteGenericParametersConstraints(genericParameters, sb, 0, false);
    }

    #endregion

    #region Private HTML helper methods

    private void WriteIndexHeader(StreamWriter sw, string pageTitle, string[] sectionsNamesAndIndices)
    {
      sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
      sw.WriteLine("<!-- This comment will force IE7 to go into quirks mode. -->");
      sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
      sw.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">");

      sw.WriteLine("<head>");
      sw.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></meta>");
      sw.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"../../{0}/Contents.css\"></link>", CSS_DIRECTORY);
      sw.WriteLine("<script type=\"text/javascript\" src=\"../../{0}/Common.js\"></script>", JS_DIRECTORY);
      sw.WriteLine("<title>{0}</title>", pageTitle);
      sw.WriteLine("</head>");

      sw.WriteLine("<body>");

      sw.WriteLine("<div id=\"Header\">");

      sw.WriteLine("<div id=\"ProjectTitle\">{0}</div>", assembliesInfo.ProjectName);
      sw.WriteLine("<div id=\"PageTitle\">{0}</div>", pageTitle);

      sw.WriteLine("<div id=\"HeaderShortcuts\">");
      foreach (string sectionNameAndIndex in sectionsNamesAndIndices)
      {
        int indexOfLastColon = sectionNameAndIndex.LastIndexOf(':');
        string sectionName = sectionNameAndIndex.Substring(0, indexOfLastColon);
        int sectionIndex = Int32.Parse(sectionNameAndIndex.Substring(indexOfLastColon + 1));

        sw.WriteLine("<a href=\"#SectionHeader{0}\" onclick=\"javascript: SetSectionVisibility({0}, true); SetExpandCollapseAllToCollapseAll();\">{1}</a>&nbsp;", sectionIndex, sectionName);
      }
      sw.WriteLine("</div>");

      sw.WriteLine("<div class=\"DarkLine\"></div>");
      sw.WriteLine("<div class=\"LightLine\"></div>");

      sw.WriteLine("<div id=\"HeaderToolbar\">");
      sw.WriteLine("<img id=\"ExpandCollapseAllImg\" src=\"../../{0}/SmallSquareExpanded.gif\" alt=\"\" style=\"vertical-align: top;\" onclick=\"javascript: ToggleAllSectionsVisibility();\" />", GRAPHICS_DIRECTORY);
      sw.WriteLine("<span id=\"ExpandCollapseAllSpan\" onclick=\"javascript: ToggleAllSectionsVisibility();\">Collapse All</span>");

      sw.WriteLine("</div>");
      sw.WriteLine("</div>");

      sw.WriteLine("<div id=\"Contents\">");
      sw.WriteLine("<a id=\"ContentsAnchor\">&nbsp;</a>");
    }

    private void WriteIndexFooter(StreamWriter sw)
    {
      sw.WriteLine("</div>");
      sw.WriteLine("<div id=\"Footer\">");
      sw.WriteLine("<span class=\"Footer\">Generated by <a href=\"{0}\" target=\"_blank\">ImmDoc .NET</a></span>.", IMM_DOC_NET_PROJECT_HOMEPAGE);
      sw.WriteLine("</div>");
      sw.WriteLine("");
      sw.WriteLine("</body>");
      sw.WriteLine("");
      sw.WriteLine("</html>");
    }

    private void WriteIndexSectionBegin(StreamWriter sw, string sectionName, int sectionIndex)
    {
      string refSectionString = null;
      MatchCollection matches = SECTION_REFERENCE_PATTERN.Matches(sectionName);

      if (matches.Count > 0)
      {
        sectionName = SECTION_REFERENCE_PATTERN.Replace(sectionName, "").Trim();
        refSectionString = " <span class=\"SeeAlsoInSectionHeader\">(see also: ";
      }

      bool first = true;
      foreach (Match match in matches)
      {
        if (!first)
        {
          refSectionString += ", ";
        }

        refSectionString += match.Result("<a href=\"#SectionHeader$2\" onclick=\"javascript: SetSectionVisibility($2, true); SetExpandCollapseAllToCollapseAll();\">$3</a>");

        first = false;
      }

      if (refSectionString != null)
      {
        string baseSectionName = "";
        int lastIndex = sectionName.LastIndexOf(' ');

        if (lastIndex != -1)
        {
          baseSectionName = sectionName.Substring(lastIndex);
        }

        refSectionString += baseSectionName + ")</span>";
      }

      sw.WriteLine("<div id=\"SectionHeader{0}\" class=\"SectionHeader\">", sectionIndex);
      sw.WriteLine("<img id=\"SectionExpanderImg{0}\" src=\"../../{1}/BigSquareExpanded.gif\" alt=\"Collapse/Expand\" onclick=\"javascript: ToggleSectionVisibility({0});\" />", sectionIndex, GRAPHICS_DIRECTORY);
      sw.WriteLine("<span class=\"SectionHeader\">");
      sw.WriteLine("<span class=\"ArrowCursor\" onclick=\"javascript: ToggleSectionVisibility({0});\">", sectionIndex);
      if (refSectionString != null)
      {
        sw.Write(sectionName);
        sw.WriteLine("</span>");
        sw.WriteLine(refSectionString);
      }
      else
      {
        sw.WriteLine(sectionName);
        sw.WriteLine("</span>");
      }
      sw.WriteLine("</span>");
      sw.WriteLine("</div>");
      sw.WriteLine("");
      sw.WriteLine("<div id=\"SectionContainerDiv{0}\" class=\"SectionContainer\">", sectionIndex);
    }

    private void WriteIndexSectionEnd(StreamWriter sw)
    {
      sw.WriteLine("<div class=\"TopLink\"><a href=\"#ContentsAnchor\">Top</a></div></div>");
    }

    private void WriteIndexItemLocation(StreamWriter sw, string declaringTypeName, string namezpace, string assemblyName, string declaringTypeLink, string namezpaceLink, string assemblyLink)
    {
      sw.WriteLine("<div id=\"ItemLocation\">");

      if (declaringTypeName != null)
      {
        declaringTypeName = Utils.HTMLEncode(declaringTypeName);

        sw.Write("<b>Declaring type:</b> ");
        if (declaringTypeLink == null)
        {
          sw.WriteLine("{0}<br />", declaringTypeName);
        }
        else
        {
          sw.WriteLine("<a href=\"{0}\">{1}</a><br />", declaringTypeLink, declaringTypeName);
        }
      }

      if (namezpace != null)
      {
        namezpace = Utils.HTMLEncode(namezpace);

        sw.Write("<b>Namespace:</b> ");
        if (namezpaceLink == null)
        {
          sw.WriteLine("{0}<br />", namezpace);
        }
        else
        {
          sw.WriteLine("<a href=\"{0}\">{1}</a><br />", namezpaceLink, namezpace);
        }
      }

      if (assemblyName != null)
      {
        assemblyName = Utils.HTMLEncode(assemblyName);

        sw.Write("<b>Assembly:</b> ");
        if (assemblyLink == null)
        {
          sw.WriteLine("{0}", assemblyName);
        }
        else
        {
          sw.WriteLine("<a href=\"{0}\">{1}</a>", assemblyLink, assemblyName);
        }
      }

      sw.WriteLine("</div>");
    }

    private void WriteIndexMembersTableBegin(StreamWriter sw, string[] columnsNames, int[] columnsWidthsPercentages)
    {
      Debug.Assert(columnsNames.Length == columnsWidthsPercentages.Length, "Impossible! Number of columns names is different from number of columns widths.");

      sw.WriteLine("<table class=\"MembersTable\">");

      for (int i = 0; i < columnsNames.Length; i++)
      {
        sw.WriteLine("<col width=\"{0}%\" />", columnsWidthsPercentages[i]);
      }

      sw.WriteLine("<tr>");

      for (int i = 0; i < columnsNames.Length; i++)
      {
        sw.WriteLine("<th>{0}</th>", columnsNames[i] == "" ? "&nbsp;" : columnsNames[i]);
      }

      sw.WriteLine("</tr>");
    }

    private void WriteIndexMembersTableEnd(StreamWriter sw)
    {
      sw.WriteLine("</table>");
    }

    private void WriteIndexMembersTableRow(StreamWriter sw, string name, string link, string summary,
                                           List<string> iconsFileNames, List<string> iconsAltLabels)
    {
      Debug.Assert((iconsFileNames == null && iconsAltLabels == null) || iconsFileNames.Count == iconsAltLabels.Count, "Impossible! Icons names and alt labels should have the same length.");

      sw.WriteLine("<tr>");

      if (iconsFileNames != null && iconsAltLabels != null)
      {
        sw.WriteLine("<td class=\"IconColumn\">");
        for (int i = 0; i < iconsFileNames.Count; i++)
        {
          if (i > 0)
          {
            sw.Write("&nbsp;");
          }

          sw.Write("<img src=\"../../{2}/{0}\" alt=\"{1}\" />", iconsFileNames[i], iconsAltLabels[i], GRAPHICS_DIRECTORY);
        }
        sw.WriteLine("</td>");
      }

      sw.Write("<td>");

      if (link != null)
      {
        sw.Write("<a href=\"{0}\">{1}</a>", link, name);
      }
      else
      {
        sw.Write("<span class=\"PseudoLink\">" + name + "</span>");
      }

      sw.WriteLine("</td>");

      if (summary == "")
      {
        summary = NO_SUMMARY;
      }

      sw.WriteLine("<td>{0}</td>", ProcessComment(summary));
      sw.WriteLine("</tr>");
    }

    private void WriteIndexSummary(StreamWriter sw, string summary)
    {
      if (summary == "")
      {
        summary = NO_SUMMARY;
      }

      sw.WriteLine(ProcessComment(summary));
    }

    private void WriteIndexRemarks(StreamWriter sw, string remarks)
    {
      if (remarks == "")
      {
        remarks = NO_REMARKS;
      }

      sw.WriteLine("<div class=\"RemarksContainer\">");
      sw.WriteLine(ProcessComment(remarks));
      sw.WriteLine("</div>");
    }

    private void WriteIndexText(StreamWriter sw, string text)
    {
      sw.WriteLine(ProcessComment(text));
    }

    private void WriteIndexExample(StreamWriter sw, string contents)
    {
      sw.WriteLine(ProcessComment(contents));
    }

    private static string CreateCodeBlockTable(string language, string contents, bool inExampleSetion)
    {
      return String.Format("<table class=\"{0}\"><col width=\"100%\" /><tr class=\"CodeTable\"><th class=\"CodeTable\">{1}</th></tr><tr class=\"CodeTable\"><td class=\"CodeTable\">{2}</td></tr></table>",
                           inExampleSetion ? "ExampleCodeTable" : "CodeTable",
                           language,
                           contents);
    }

    private string CreateCodeBlockTable(string language, string contents)
    {
      return CreateCodeBlockTable(language, contents, false);
    }

    private void WriteIndexCodeBlockTable(StreamWriter sw, string language, string contents)
    {
      sw.WriteLine(CreateCodeBlockTable(language, contents));
    }

    private void WriteMembersIndex(StreamWriter sw, string sectionHeader, int sectionIndex, IEnumerator<ISummarisableMember> members, string[] columnsNames, int[] columnsWidthsPercentages, NamespaceMembersGroups namespaceMembersGroupsType, ClassMembersGroups classMembersGroupsType)
    {
      WriteMembersIndex(sw, sectionHeader, sectionIndex, members, columnsNames, columnsWidthsPercentages, namespaceMembersGroupsType, classMembersGroupsType, false);
    }

    private void WriteMembersIndex(StreamWriter sw, string sectionHeader, int sectionIndex, IEnumerator<ISummarisableMember> members, string[] columnsNames, int[] columnsWidthsPercentages, NamespaceMembersGroups namespaceMembersGroupsType, ClassMembersGroups classMembersGroupsType, bool dontResolveLinks)
    {
      WriteIndexSectionBegin(sw, sectionHeader, sectionIndex);

      WriteIndexMembersTableBegin(sw, columnsNames, columnsWidthsPercentages);

      while (members.MoveNext())
      {
        ISummarisableMember member = members.Current;

        string summary = member.Summary;
        string memberName = Utils.HTMLEncode(member.DisplayableName);
        string link = null;

        if (!dontResolveLinks)
        {
          link = ResolveLink((MetaClass)member);
        }

        IconsTypes iconsTypes = GetIconsTypes(member, namespaceMembersGroupsType, classMembersGroupsType);

        WriteIndexMembersTableRow(sw,
                                  memberName,
                                  link,
                                  summary,
                                  Icons.GetFileNames(iconsTypes),
                                  Icons.GetAltLabels(iconsTypes));
      }

      WriteIndexMembersTableEnd(sw);

      WriteIndexSectionEnd(sw);
    }

    private void WriteIndexExceptionsTable(StreamWriter sw, List<ExceptionDescr> exceptionsDescrs)
    {
      WriteIndexMembersTableBegin(sw, EXCEPTIONS_COLUMNS_NAMES, EXCEPTIONS_COLUMNS_WIDTHS);

      foreach (ExceptionDescr exceptionDescr in exceptionsDescrs)
      {
        string link = null;

        if (exceptionDescr.ExceptionClassInfo != null)
        {
          link = ResolveLink(exceptionDescr.ExceptionClassInfo);
        }

        WriteIndexMembersTableRow(sw,
                                  ProcessType(exceptionDescr.TypeFullName),
                                  link,
                                  exceptionDescr.Condition,
                                  null, null);
      }

      WriteIndexMembersTableEnd(sw);
    }

    private IconsTypes GetIconsTypes(ISummarisableMember member, NamespaceMembersGroups namespaceMembersGroupType, ClassMembersGroups classMembersGroupType)
    {
      if (member is MyAssemblyInfo) { return IconsTypes.Assembly; }
      else if (member is MyNamespaceInfo) { return IconsTypes.Namespace; }
      else if (member is MyClassInfo)
      {
        MyClassInfo myClassInfo = (MyClassInfo)member;
        IconsTypes iconsTypes = Icons.GetIconType(namespaceMembersGroupType);

        if (myClassInfo.IsAbstract && !myClassInfo.IsSealed) { iconsTypes |= IconsTypes.Abstract; }
        if (myClassInfo.IsStatic) { iconsTypes |= IconsTypes.Static; }

        if (myClassInfo is MyInterfaceInfo) { iconsTypes &= ~IconsTypes.Abstract; }

        return iconsTypes;
      }
      else if (member is MyInvokableMembersOverloadsInfo)
      {
        MyInvokableMembersOverloadsInfo myInvokableMembersOverloadsInfo = (MyInvokableMembersOverloadsInfo)member;
        MyInvokableMemberInfo myInvokableMemberInfo = null;

        foreach (MyInvokableMemberInfo tmpMyInvokableMemberInfo in myInvokableMembersOverloadsInfo)
        {
          myInvokableMemberInfo = tmpMyInvokableMemberInfo;

          break;
        }

        if (myInvokableMemberInfo == null)
        {
          Debug.Assert(false, "Impossible! There should be at least one overload.");
        }

        return GetIconsTypes(myInvokableMemberInfo, namespaceMembersGroupType, classMembersGroupType);
      }
      else if (member is MyPropertiesOverloadsInfo)
      {
        MyPropertiesOverloadsInfo myPropertiesOverloadsInfo = (MyPropertiesOverloadsInfo)member;
        MyPropertyInfo myPropertyInfo = null;

        foreach (MyPropertyInfo tmpMyPropertyInfo in myPropertiesOverloadsInfo)
        {
          myPropertyInfo = tmpMyPropertyInfo;

          break;
        }

        if (myPropertyInfo == null)
        {
          Debug.Assert(false, "Impossible! There should be at least one overload.");
        }

        return GetIconsTypes(myPropertyInfo, namespaceMembersGroupType, classMembersGroupType);
      }
      else if (member is MyInvokableMemberInfo)
      {
        MyInvokableMemberInfo myInvokableMemberInfo = (MyInvokableMemberInfo)member;
        IconsTypes iconsTypes = IconsTypes.None;

        if (myInvokableMemberInfo.IsPublic) { iconsTypes = IconsTypes.PublicMethod; }
        else if (myInvokableMemberInfo.IsProtectedInternal) { iconsTypes = IconsTypes.ProtectedInternalMethod; }
        else if (myInvokableMemberInfo.IsProtected) { iconsTypes = IconsTypes.ProtectedMethod; }
        else if (myInvokableMemberInfo.IsInternal) { iconsTypes = IconsTypes.InternalMethod; }
        else if (myInvokableMemberInfo.IsPrivate) { iconsTypes = IconsTypes.PrivateMethod; }

        if (myInvokableMemberInfo.IsStatic) { iconsTypes |= IconsTypes.Static; }
        if (myInvokableMemberInfo.IsAbstract) { iconsTypes |= IconsTypes.Abstract; }
        if (myInvokableMemberInfo.IsVirtual && !myInvokableMemberInfo.IsAbstract) { iconsTypes |= IconsTypes.Virtual; }
        if (myInvokableMemberInfo.IsOverride && !myInvokableMemberInfo.IsSealed) { iconsTypes |= IconsTypes.Virtual; }

        return iconsTypes;
      }
      else if (member is MyFieldInfo)
      {
        if (namespaceMembersGroupType == NamespaceMembersGroups.PublicEnumerations
         || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedInternalEnumerations
         || namespaceMembersGroupType == NamespaceMembersGroups.ProtectedEnumerations
         || namespaceMembersGroupType == NamespaceMembersGroups.InternalEnumerations
         || namespaceMembersGroupType == NamespaceMembersGroups.PrivateEnumerations)
        {
          IconsTypes iconsTypes = IconsTypes.EnumField;

          return iconsTypes;
        }
        else
        {
          MyFieldInfo myFieldInfo = (MyFieldInfo)member;
          IconsTypes iconsTypes = Icons.GetIconType(classMembersGroupType);

          if (myFieldInfo.IsStatic) { iconsTypes |= IconsTypes.Static; }
          if (myFieldInfo.IsConst) { iconsTypes |= IconsTypes.Static; }

          return iconsTypes;
        }
      }
      else if (member is MyPropertyInfo)
      {
        MyPropertyInfo myPropertyInfo = (MyPropertyInfo)member;
        IconsTypes iconsTypes = Icons.GetIconType(classMembersGroupType);

        if (myPropertyInfo.IsStatic) { iconsTypes |= IconsTypes.Static; }
        if (myPropertyInfo.IsAbstract) { iconsTypes |= IconsTypes.Abstract; }
        if (myPropertyInfo.IsVirtual && !myPropertyInfo.IsAbstract) { iconsTypes |= IconsTypes.Virtual; }

        return iconsTypes;
      }
      else if (member is MyEventInfo)
      {
        MyEventInfo myEventInfo = (MyEventInfo)member;
        IconsTypes iconsTypes = Icons.GetIconType(classMembersGroupType);

        if (myEventInfo.IsStatic) { iconsTypes |= IconsTypes.Static; }
        if (myEventInfo.IsAbstract) { iconsTypes |= IconsTypes.Abstract; }
        if (myEventInfo.IsVirtual && !myEventInfo.IsAbstract) { iconsTypes |= IconsTypes.Virtual; }

        return iconsTypes;
      }
      else if (member is MyNestedTypeInfo)
      {
        MyNestedTypeInfo myNestedTypeInfo = (MyNestedTypeInfo)member;
        IconsTypes iconsTypes = Icons.GetIconType(classMembersGroupType);

        if (myNestedTypeInfo.IsAbstract && !myNestedTypeInfo.IsSealed) { iconsTypes |= IconsTypes.Abstract; }
        if (myNestedTypeInfo.IsStatic) { iconsTypes |= IconsTypes.Static; }

        if (myNestedTypeInfo.MetaType == NestedTypes.Interface) { iconsTypes &= ~IconsTypes.Abstract; }

        return iconsTypes;
      }
      else
      {
        Debug.Assert(false, "Impossible! Can't find icon for type " + member.GetType() + ".");
      }

      return IconsTypes.None;
    }

    private void WriteIndexCommentHeader(StreamWriter sw, string headerTitle)
    {
      sw.WriteLine("<div class=\"CommentHeader\">{0}</div>", headerTitle);
    }

    private void WriteIndexParameterNameCommentHeader(StreamWriter sw, string parameterName)
    {
      sw.WriteLine("<div class=\"CommentParameterName\">{0}</div>", parameterName);
    }

    private void WriteIndexParamOrReturnValueDescription(StreamWriter sw, string description)
    {
      if (description == "")
      {
        description = NO_DESCRIPTION;
      }

      sw.WriteLine("<div class=\"ParameterCommentContainer\">");
      sw.WriteLine(ProcessComment(description));
      sw.WriteLine("</div>");
    }

    private string ProcessComment(string contents)
    {
      if (contents.Contains("<list")) // process lists only if there's at least one
      {
        contents = ProcessListsInComment(contents);
      }

      contents = contents.Replace("<para>", "<p>").Replace("</para>", "</p>").Replace("<c>", "<span class=\"Code\">").Replace("</c>", "</span>");

      contents = codePattern.Replace(contents, codeRegexEvaluator);
      contents = seePattern.Replace(contents, seeRegexEvaluator);
      contents = paramrefPattern.Replace(contents, paramrefRegexEvaluator);
      contents = typeparamrefPattern.Replace(contents, typeparamrefRegexEvaluator);

      return contents;
    }

    private string ProcessListsInComment(string contents)
    {
      try
      {
        if (listsXslt == null)
        {
          // lazy loading of XSLT
          listsXslt = new XslCompiledTransform();

          xslReaderSettings = new XmlReaderSettings();
          xslReaderSettings.IgnoreWhitespace = true;
          xslReaderSettings.ProhibitDtd = false;

          listsXmlReaderSettings = new XmlReaderSettings();
          listsXmlReaderSettings.ConformanceLevel = ConformanceLevel.Fragment;
          listsXmlReaderSettings.IgnoreWhitespace = true;

          xmlWriterSettings = new XmlWriterSettings();
          xmlWriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
          xmlWriterSettings.Encoding = Encoding.UTF8;
          xmlWriterSettings.Indent = true;
          xmlWriterSettings.IndentChars = "    ";

          using (TextReader xslTextReader = new StringReader(ReadStringResource("Lists.xslt")))
          {
            using (XmlReader xslXmlReader = XmlReader.Create(xslTextReader, xslReaderSettings))
            {
              listsXslt.Load(xslXmlReader);
            }
          }
        }

        StringBuilder htmlOutput = new StringBuilder();
        using (TextReader xmlTextReader = new StringReader(contents))
        {
          using (XmlReader xmlReader = XmlReader.Create(xmlTextReader, listsXmlReaderSettings))
          {
            using (XmlWriter xmlWriter = XmlWriter.Create(htmlOutput, xmlWriterSettings))
            {
              listsXslt.Transform(xmlReader, xmlWriter);
            }
          }
        }

        return htmlOutput.ToString();
      }
      catch (Exception)
      {
        Logger.Warning("Couldn't process lists in a comment.");

        return contents;
      }
    }

    private static string OnCodePatternMatch(Match match)
    {
      string contents = match.Groups["Contents"].Value.Trim('\r', '\n');
      int index = 0;

      while (index < contents.Length && contents[index] == ' ')
      {
        index++;
      }

      if (index > 0)
      {
        Regex pattern = new Regex("^" + Utils.CreateNSpaces(index), RegexOptions.Multiline);
        contents = pattern.Replace(contents, "");
      }

      return CreateCodeBlockTable(DEFAULT_LANGUAGE, "<pre>" + contents + "</pre>", true);
    }

    private static string OnParamrefPatternMatch(Match match)
    {
      string paramName = match.Groups["ParamName"].Value;

      return String.Format("<span class=\"Code\">{0}</span>", paramName);
    }

    private static string OnTypeparamrefPatternMatch(Match match)
    {
      string typeParamName = match.Groups["TypeParamName"].Value;

      return String.Format("<span class=\"Code\">{0}</span>", typeParamName);
    }

    private string OnSeePatternMatch(Match match)
    {
      string xmlMemberId = match.Groups["XmlMemberId"].Value;
      string contents = match.Groups["Contents"].Value;
      string link = null;
      string target = null;

      MetaClass metaClass = assembliesInfo.FindMember(xmlMemberId);

      if (metaClass == null)
      {
        link = TryResolveFrameworkClass(xmlMemberId);
        if (link != null)
        {
          target = "_top";
        }

        if (contents == "")
        {
          if (xmlMemberId.Length > 1 && xmlMemberId[1] == ':')
          {
            contents = xmlMemberId.Substring(2);

            if (xmlMemberId[0] != '!')
            {
              contents = Utils.HTMLEncode(contents);
            }
          }
          else
          {
            contents = Utils.HTMLEncode(xmlMemberId);
          }
        }
      }
      else
      {
        link = ResolveLink(metaClass);

        if (contents == "")
        {
          if (metaClass is MyConstructorInfo)
          {
            Debug.Assert(metaClass.DeclaringType != null, "Impossible! No declaring type.");

            contents = Utils.HTMLEncode(metaClass.DeclaringType.DisplayableName + ".#ctor");
          }
          else
          {
            if (metaClass is MyMethodInfo && metaClass.Name == "operator +")
            {
              contents = Utils.HTMLEncode(metaClass.Name);
            }
            else
            {
              contents = Utils.HTMLEncode(metaClass.Name.Replace('/', '.'));
            }
          }
        }
      }

      if (link == null)
      {
        Logger.Warning("<XML> Couldn't resolve link for the referenced type with id '{0}'.", xmlMemberId);

        return "<span class=\"PseudoLink\">" + contents + "</span>";
      }
      else
      {
        return "<a href=\"" + link + "\"" + (target != null ? " target=\"" + target + "\"" : string.Empty) + ">" + contents + "</a>";
      }
    }

    #endregion

    #region Private CHM helper methods

    private void GenerateHTMLHelpTableOfContents()
    {
      if (assembliesInfo.AssembliesCount == 0)
      {
        return;
      }

      using (StreamWriter sw = new StreamWriter(Path.Combine(outputDirectory, HHC_NAME)))
      {
        sw.WriteLine("<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML//EN\">");
        sw.WriteLine("<HTML>");
        sw.WriteLine("<HEAD>");
        sw.WriteLine("<meta name=\"GENERATOR\" content=\"Microsoft&reg; HTML Help Workshop 4.1\">");
        sw.WriteLine("<!-- Sitemap 1.0 -->");
        sw.WriteLine("</HEAD><BODY>");

        sw.WriteLine("<OBJECT type=\"text/site properties\">");
        sw.WriteLine("<param name=\"ImageType\" value=\"Book\">");
        sw.WriteLine("</OBJECT>");

        sw.WriteLine("<UL>");
        CreateCHMTOC(sw);
        sw.WriteLine("</UL>");

        sw.WriteLine("</BODY></HTML>");
      }
    }

    private void CreateCHMTOC(StreamWriter sw)
    {
      foreach (MyAssemblyInfo myAssemblyInfo in assembliesInfo.Assemblies)
      {
        CreateCHMTOCAux(myAssemblyInfo, sw);
      }
    }

    private void CreateCHMTOCAux(MetaClass metaClass, StreamWriter sw)
    {
      Debug.Assert(metaClass != null, "ArgumentNullException(metaClass)");
      Debug.Assert(sw != null, "ArgumentNullException(sb)");
      Debug.Assert(metaClass is ISummarisableMember, "Meta class must implement ISummarisableMember interface.");

      bool hasChildren = HasChildren(metaClass);

      string label = Utils.HTMLEncode(metaClass.Name.Replace('/', '.')) + " " + metaClass.GetMetaName();
      string href = ResolveLink(metaClass);

      Debug.Assert(href.StartsWith("../../"), "Impossible (1)! href should start with '../../'!");
      href = href.Substring("../../".Length);

      sw.WriteLine("<LI> <OBJECT type=\"text/sitemap\">");
      sw.WriteLine("<param name=\"ImageNumber\" value=\"1\">");
      sw.WriteLine("<param name=\"Name\" value=\"{0}\">", label);
      sw.WriteLine("<param name=\"Local\" value=\"{0}\">", href);
      sw.WriteLine("</OBJECT>");

      if (hasChildren)
      {
        sw.Write("<UL>");

        if (metaClass is MyAssemblyInfo)
        {
          MyAssemblyInfo myAssemblyInfo = (MyAssemblyInfo)metaClass;
          int number = 1;

          foreach (MyNamespaceInfo myNamespaceInfo in myAssemblyInfo.Namespaces)
          {
            CreateCHMTOCAux(myNamespaceInfo, sw);
            number++;
          }
        }
        else if (metaClass is MyNamespaceInfo)
        {
          MyNamespaceInfo myNamespaceInfo = (MyNamespaceInfo)metaClass;
          IEnumerable<MetaClass> members = myNamespaceInfo.GetEnumerator();

          foreach (MetaClass member in members)
          {
            CreateCHMTOCAux(member, sw);
          }
        }
        else if (metaClass is MyClassInfo)
        {
          if (!(metaClass is MyDelegateInfo) && !(metaClass is MyEnumerationInfo))
          {
            MyClassInfo myClassInfo = (MyClassInfo)metaClass;
            string namespaceDirName = GetNamespaceDirName(myClassInfo.Namespace);

            href = GetAliasName(myClassInfo.AssemblyName + "_" + myClassInfo.AssemblyName.GetHashCode()
                              + Path.DirectorySeparatorChar + namespaceDirName + Path.DirectorySeparatorChar
                              + "MS_" + myClassInfo.Name + "_" + myClassInfo.Name.GetHashCode() + ".html",
                                true, true);

            Debug.Assert(href.StartsWith("../../"), "Impossible (2)! href should start with '../../'!");
            href = href.Substring("../../".Length);

            sw.WriteLine("<LI> <OBJECT type=\"text/sitemap\">");
            sw.WriteLine("<param name=\"Name\" value=\"Members\">");
            sw.WriteLine("<param name=\"Local\" value=\"{0}\">", href);
            sw.WriteLine("</OBJECT>");
          }
        }
        else
        {
          Debug.Assert(false, "Impossible! Couldn't recognize type of a metaclass (" + metaClass.GetType() + ").");
        }

        sw.Write("</UL>\n");
      }
    }

    private void GenerateHTMLHelpProjectFile()
    {
      using (StreamWriter sw = new StreamWriter(Path.Combine(outputDirectory, HHP_NAME)))
      {
        sw.WriteLine("[OPTIONS]");
        sw.WriteLine("Compatibility=1.1 or later");
        sw.WriteLine("Default topic={0}", "Contents\\0\\1.html");
        sw.WriteLine("Compiled file={0}", CHM_NAME);
        if (File.Exists(Path.Combine(outputDirectory, HHC_NAME)))
        {
          sw.WriteLine("Contents file={0}", HHC_NAME);
        }
        sw.WriteLine("Display compile progress=No");
        sw.WriteLine("Language=0x409 English (United States)");
        sw.WriteLine("Title={0}", assembliesInfo.ProjectName);
        sw.WriteLine("Error log file={0}", HHC_LOG_NAME);
        sw.WriteLine("Full-text search=Yes");
        sw.WriteLine("");
        sw.WriteLine("");
        sw.WriteLine("[FILES]");
        CreateCHMFilesSection(outputDirectory, sw);
        sw.WriteLine("");
        sw.WriteLine("[INFOTYPES]");
        sw.WriteLine("");
      }
    }

    private void CreateCHMFilesSection(string currentDir, StreamWriter sw)
    {
      string[] files = Directory.GetFiles(currentDir);

      foreach (string file in files)
      {
        string fileName = Path.GetFileName(file);
        string extension = Path.GetExtension(fileName).ToLower();

        if (fileName != MAIN_INDEX_FILE_NAME
         && fileName != TABLE_OF_CONTENTS_FILE_NAME
         && extension != ".hhp"
         && extension != ".hhc"
         && extension != ".hhk"
         && extension != ".log"
         && extension != ".chm")
        {
          sw.WriteLine(file);
        }
      }

      string[] dirs = Directory.GetDirectories(currentDir);

      foreach (string dir in dirs)
      {
        CreateCHMFilesSection(dir, sw);
      }
    }

    private void GenerateCHM()
    {
      string hhcHome = Environment.ExpandEnvironmentVariables(HHC_HOME_VARIABLE_NAME);

      if (hhcHome == HHC_HOME_VARIABLE_NAME)
      {
        hhcHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                               HHC_DEFAULT_INSTALL_DIR);
      }

      string hhcExec = Path.Combine(hhcHome, HHC_EXEC_NAME);

      if (!File.Exists(hhcExec))
      {
        Logger.Warning("HTML Help compiler was not found. Set HHC_HOME environment variable.");

        return;
      }

      ProcessStartInfo psi = new ProcessStartInfo(hhcExec,
                                                  "\"" + Path.Combine(outputDirectory, HHP_NAME) + "\"");


      psi.WindowStyle = ProcessWindowStyle.Hidden;
      psi.WorkingDirectory = outputDirectory;

      Process hhcProcess = Process.Start(psi);

      hhcProcess.WaitForExit();

      string source = Path.Combine(outputDirectory, CHM_NAME);
      string target = Path.GetFullPath(chmFileNameWithoutExtension) + ".chm";

      if (!File.Exists(source))
      {
        string logFileName = Path.Combine(outputDirectory, HHC_LOG_NAME);
        string log = "There was some error while creating CHM file";

        if (File.Exists(logFileName))
        {
          log += ":\n";

          using (StreamReader sr = new StreamReader(logFileName))
          {
            log += sr.ReadToEnd();
          }
        }
        else
        {
          log += ".";
        }

        Logger.Warning(log);

        return;
      }

      if (source != target)
      {
        File.Copy(source, target, true);
      }
    }

    private void CleanUpCHM()
    {
      string fileName = Path.Combine(outputDirectory, HHP_NAME);
      if (File.Exists(fileName)) { File.Delete(fileName); }

      fileName = Path.Combine(outputDirectory, HHC_NAME);
      if (File.Exists(fileName)) { File.Delete(fileName); }

      fileName = Path.Combine(outputDirectory, CHM_NAME);
      if (File.Exists(fileName)
       && fileName != Path.GetFullPath(chmFileNameWithoutExtension) + ".chm")
      {
        File.Delete(fileName);
      }

      fileName = Path.Combine(outputDirectory, HHC_LOG_NAME);
      if (File.Exists(fileName)) { File.Delete(fileName); }
    }

    #endregion

    #region Private resources helper methods

    private string ReadStringResource(string resourceName)
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      string resourceFullName = GetType().Namespace + "." + "Res." + resourceName;
      Stream resourceStream = assembly.GetManifestResourceStream(resourceFullName);

      Debug.Assert(resourceStream != null, String.Format("Impossible! Couldn't find resorce '{0}'", resourceFullName));

      StreamReader sr = new StreamReader(resourceStream);

      string result = sr.ReadToEnd();

      sr.Close();
      resourceStream.Close();

      return result;
    }

    private Stream GetResourceStream(string resourceName)
    {
      var assembly = Assembly.GetExecutingAssembly();
      var resourceFullName = GetType().Namespace + "." + "Res." + resourceName;
      var resourceStream = assembly.GetManifestResourceStream(resourceFullName);
      Debug.Assert(resourceStream != null, String.Format("Impossible! Couldn't find resorce '{0}'", resourceFullName));

      if (resourceName.EndsWith(GZIP_FILE_EXTENSION))
      {
        return new GZipStream(resourceStream, CompressionMode.Decompress, false);
      }

      return resourceStream;
    }

    private void ExtractBinaryResourceToFile(string resourceName, string fileName)
    {
      using (var resourceStream = GetResourceStream(resourceName))
      using (var fs = File.Create(fileName))
      {
        var buffer = new byte[IO_BUFFER_SIZE];
        int bytesToRead = buffer.Length;

        while (true)
        {
          int bytesRead = resourceStream.Read(buffer, 0, bytesToRead);
          if (bytesRead == 0)
          {
            // EOF
            break;
          }

          fs.Write(buffer, 0, bytesRead);
        }
      }
    }

    private void WriteStringToFile(string fileName, string contents)
    {
      FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      sw.Write(contents);

      sw.Close();
      fs.Close();
    }

    #endregion

    #region Private links helper methods

    private IDictionary<string, string> FrameworkTypes
    {
      get
      {
        if (frameworkTypes == null)
        {
          using (var stream = GetResourceStream(CLASS_LIBRARY_FILE_NAME))
          using (var sr = new StreamReader(stream))
          {
            var typeList =
              from rawLine in sr.ReadToEnd().Split('\n')
                let line = rawLine.Trim()
              where !string.IsNullOrEmpty(line) && !line.StartsWith("//")
                let parts = line.Split(',')
              select new
              {
                ClassName = parts.First(),
                ContentId = parts.Last()
              };

            frameworkTypes = typeList.ToDictionary(c => c.ClassName, c => c.ContentId);
          }
        }

        return frameworkTypes;
      }
    }

    private string TryResolveFrameworkClass(string xmlMemberId)
    {
      if (xmlMemberId.IndexOf(':') == 1)
      {
        xmlMemberId = xmlMemberId.Substring(2);
      }

      string contentId; 
      if (FrameworkTypes.TryGetValue(xmlMemberId.ToLowerInvariant(), out contentId))
      {
        return string.Format(MSDN_LIBRARY_URL_FORMAT, contentId);
      }

      return null;
    }

    private string ResolveLink(MetaClass metaClass)
    {
      if (metaClass is MyAssemblyInfo)
      {
        return ResolveAssemblyLink(((MyAssemblyInfo)metaClass).Name);
      }
      else if (metaClass is MyNamespaceInfo)
      {
        MyNamespaceInfo myNamespaceInfo = (MyNamespaceInfo)metaClass;

        return ResolveNamespaceLink(myNamespaceInfo.AssemblyName, myNamespaceInfo.Name);
      }
      else if (metaClass is MyClassInfo)
      {
        MyClassInfo namespaceMemberInfo = (MyClassInfo)metaClass;

        return ResolveNamespaceMemberLink(namespaceMemberInfo.AssemblyName, namespaceMemberInfo.Namespace, namespaceMemberInfo.Name);
      }
      else if ((metaClass is MyFieldInfo) || (metaClass is MyInvokableMembersOverloadsInfo) || (metaClass is MyPropertiesOverloadsInfo) || (metaClass is MyNestedTypeInfo) || (metaClass is MyEventInfo))
      {
        return ResolveTypeMemberLink(metaClass);
      }
      else if (metaClass is MyPropertyInfo)
      {
        MyPropertyInfo myPropertyInfo = (MyPropertyInfo)metaClass;

        Debug.Assert(myPropertyInfo.DeclaringType != null, "Impossible! No declaring type.");
        Debug.Assert(myPropertyInfo.IndexInOverloadsList != -1, "Impossible! Unknown index in overloads list.");

        return ResolvePropertyLink(myPropertyInfo, myPropertyInfo.DeclaringType, myPropertyInfo.IndexInOverloadsList);
      }
      else if (metaClass is MyInvokableMemberInfo)
      {
        MyInvokableMemberInfo myInvokableMemberInfo = (MyInvokableMemberInfo)metaClass;

        Debug.Assert(myInvokableMemberInfo.DeclaringType != null, "Impossible! No declaring type.");
        Debug.Assert(myInvokableMemberInfo.IndexInOverloadsList != -1, "Impossible! Unknown index in overloads list.");

        return ResolveInvokableMemberLink(myInvokableMemberInfo, myInvokableMemberInfo.DeclaringType, myInvokableMemberInfo.IndexInOverloadsList);
      }
      else
      {
        Debug.Assert(false, String.Format("Impossible! Couldn't recognize the type of member for which a link should be resolved ('{0}').", metaClass.Name + " : " + metaClass.GetType()));

        return null;
      }
    }

    private string ResolveTypeMemberLink(MetaClass metaClass)
    {
      if (metaClass == null)
      {
        return null;
      }

      string namespaceDirName;

      Debug.Assert(!(metaClass is MyInvokableMemberInfo), "Can't resolve link of invokableMember without knowing its number in overloadsList.");
      Debug.Assert(!(metaClass is MyPropertyInfo), "Can't resolve link of property without knowing its number in overloadsList.");

      MyClassInfo declaringType = metaClass.DeclaringType;

      Debug.Assert(declaringType != null, String.Format("Impossible! In order to resolve type member link this member has to contain reference to the declaring type ({0}).", metaClass.Name + " : " + metaClass.GetType()));

      if (metaClass is MyNestedTypeInfo)
      {
        namespaceDirName = GetNamespaceDirName(declaringType.Namespace);

        string fullName = ((MyNestedTypeInfo)metaClass).FullName;

        return GetAliasName(declaringType.AssemblyName + "_" + declaringType.AssemblyName.GetHashCode()
                          + Path.DirectorySeparatorChar + namespaceDirName
                          + Path.DirectorySeparatorChar + "SM_" + fullName
                          + "_" + fullName.GetHashCode() + ".html",
                          true, true);
      }

      string prefix = "";
      string suffix = "";

      if (metaClass is MyEventInfo) { prefix = "E_"; }
      else if (metaClass is MyFieldInfo) { prefix = "F_"; }
      else if (metaClass is MyInvokableMembersOverloadsInfo)
      {
        MyInvokableMembersOverloadsInfo overloads = (MyInvokableMembersOverloadsInfo)metaClass;

        if (overloads[0] is MyMethodInfo)
        {
          if (overloads.Count > 1) { prefix = "MO_" + GetMyInvokableMemberVisibilitModifiersCodeString(overloads[0]) + "_"; }
          else { prefix = "M_" + GetMyInvokableMemberVisibilitModifiersCodeString(overloads[0]) + "_"; suffix = "_0"; }
        }
        else // overloads[0] is MyConstructorInfo
        {
          Debug.Assert(overloads[0] is MyConstructorInfo, "Impossible! There should MyConstructorInfo object.");

          if (overloads.Count > 1) { prefix = "CO_" + GetMyInvokableMemberVisibilitModifiersCodeString(overloads[0]) + "_"; }
          else { prefix = "C_" + GetMyInvokableMemberVisibilitModifiersCodeString(overloads[0]) + "_"; suffix = "_0"; }
        }
      }
      else if (metaClass is MyPropertiesOverloadsInfo)
      {
        MyPropertiesOverloadsInfo overloads = (MyPropertiesOverloadsInfo)metaClass;

        if (overloads.Count > 1) { prefix = "PO_" + GetMyPropertyInfoVisibilitModifiersCodeString(overloads[0]) + "_"; }
        else { prefix = "P_" + GetMyPropertyInfoVisibilitModifiersCodeString(overloads[0]) + "_"; suffix = "_0"; }
      }

      namespaceDirName = GetNamespaceDirName(declaringType.Namespace);

      return GetAliasName(declaringType.AssemblyName + "_" + declaringType.AssemblyName.GetHashCode()
                        + Path.DirectorySeparatorChar + namespaceDirName
                        + Path.DirectorySeparatorChar + CLASS_MEMBERS_DIRECTORY
                        + Path.DirectorySeparatorChar + declaringType.Name + "_" + declaringType.Name.GetHashCode()
                        + Path.DirectorySeparatorChar + prefix + metaClass.Name + suffix + "_" + metaClass.Name.GetHashCode() + ".html",
                          true, true);
    }

    private string ResolveInvokableMemberLink(MyInvokableMemberInfo myInvokableMemberInfo, MyClassInfo declaringType, int indexInOverloadsList)
    {
      string prefix;

      if (myInvokableMemberInfo is MyConstructorInfo)
      {
        prefix = "C_" + GetMyInvokableMemberVisibilitModifiersCodeString(myInvokableMemberInfo) + "_";
      }
      else
      {
        Debug.Assert(myInvokableMemberInfo is MyMethodInfo, "Impossible!");

        prefix = "M_" + GetMyInvokableMemberVisibilitModifiersCodeString(myInvokableMemberInfo) + "_";
      }

      string namespaceDirName = GetNamespaceDirName(declaringType.Namespace);

      return GetAliasName(declaringType.AssemblyName + "_" + declaringType.AssemblyName.GetHashCode()
                        + Path.DirectorySeparatorChar + namespaceDirName
                        + Path.DirectorySeparatorChar + CLASS_MEMBERS_DIRECTORY
                        + Path.DirectorySeparatorChar + declaringType.Name + "_" + declaringType.Name.GetHashCode()
                        + Path.DirectorySeparatorChar + prefix + myInvokableMemberInfo.Name + "_" + indexInOverloadsList + "_" + myInvokableMemberInfo.Name.GetHashCode() + ".html",
                          true, true);
    }

    private string ResolvePropertyLink(MyPropertyInfo myPropertyInfo, MyClassInfo declaringType,
                                       int indexInOverloadsList)
    {
      string prefix = "P_" + GetMyPropertyInfoVisibilitModifiersCodeString(myPropertyInfo) + "_";
      string namespaceDirName = GetNamespaceDirName(declaringType.Namespace);

      return GetAliasName(declaringType.AssemblyName + "_" + declaringType.AssemblyName.GetHashCode()
                        + Path.DirectorySeparatorChar + namespaceDirName
                        + Path.DirectorySeparatorChar + CLASS_MEMBERS_DIRECTORY
                        + Path.DirectorySeparatorChar + declaringType.Name + "_" + declaringType.Name.GetHashCode()
                        + Path.DirectorySeparatorChar + prefix + myPropertyInfo.Name + "_" + indexInOverloadsList + "_" + myPropertyInfo.Name.GetHashCode() + ".html",
                          true, true);
    }

    private string ResolveAssemblyLink(string assemblyName)
    {
      MyAssemblyInfo myAssemblyInfo = assembliesInfo.FindAssembly(assemblyName);

      if (myAssemblyInfo == null)
      {
        return null;
      }

      return GetAliasName(assemblyName + "_" + assemblyName.GetHashCode() + Path.DirectorySeparatorChar + NAMESPACES_INDEX_FILE_NAME, true, true);
    }

    private string ResolveNamespaceLink(string assemblyName, string namezpace)
    {
      MyNamespaceInfo myNamespaceInfo = assembliesInfo.FindNamespace(assemblyName, namezpace);

      if (myNamespaceInfo == null)
      {
        return null;
      }

      string namespaceDirName = GetNamespaceDirName(namezpace);

      return GetAliasName(assemblyName + "_" + assemblyName.GetHashCode() + Path.DirectorySeparatorChar
                        + namespaceDirName + Path.DirectorySeparatorChar + TYPES_INDEX_FILE_NAME,
                          true, true);
    }

    private string ResolveNamespaceMemberLink(string assemblyName, string namezpace, string memberName)
    {
      MyClassInfo namespaceMemberInfo = null;
      int membersGroupTypeIndex = 0;

      while (namespaceMemberInfo == null && Enum.IsDefined(typeof(NamespaceMembersGroups), membersGroupTypeIndex))
      {
        NamespaceMembersGroups membersGroupType = (NamespaceMembersGroups)membersGroupTypeIndex;

        namespaceMemberInfo = assembliesInfo.FindNamespaceMember(assemblyName, namezpace, membersGroupType, memberName) as MyClassInfo;

        membersGroupTypeIndex++;
      }

      if (namespaceMemberInfo == null)
      {
        return null;
      }

      string namespaceDirName = GetNamespaceDirName(namezpace);

      return GetAliasName(assemblyName + "_" + assemblyName.GetHashCode()
                        + Path.DirectorySeparatorChar + namespaceDirName + Path.DirectorySeparatorChar
                        + "SM_" + memberName + "_" + memberName.GetHashCode() + ".html",
                          true, true);
    }

    private string GetMyInvokableMemberVisibilitModifiersCodeString(MyInvokableMemberInfo myInvokableMemberInfo)
    {
      if (myInvokableMemberInfo.IsPublic) { return "Public"; }
      else if (myInvokableMemberInfo.IsProtectedInternal) { return "ProtectedInternal"; }
      else if (myInvokableMemberInfo.IsProtected) { return "Protected"; }
      else if (myInvokableMemberInfo.IsInternal) { return "Internal"; }
      else if (myInvokableMemberInfo.IsPrivate) { return "Private"; }

      throw new Exception("Impossible!");
    }

    private string GetMyPropertyInfoVisibilitModifiersCodeString(MyPropertyInfo myPropertyInfo)
    {
      if (myPropertyInfo.IsPublic) { return "Public"; }
      else if (myPropertyInfo.IsProtectedInternal) { return "ProtectedInternal"; }
      else if (myPropertyInfo.IsProtected) { return "Protected"; }
      else if (myPropertyInfo.IsInternal) { return "Internal"; }
      else if (myPropertyInfo.IsPrivate) { return "Private"; }

      throw new Exception("Impossible!");
    }

    #endregion

    #region Private helper methods

    private int GetDotsCount(string name)
    {
      int length = name.Length;
      int result = 0;

      for (int i = 0; i < length; i++)
      {
        if (name[i] == '.')
        {
          result++;
        }
      }

      return result;
    }

    private string[] ObtainListOfSectionsShortcutsNamesAndIndices(MyNamespaceInfo myNamespaceInfo)
    {
      List<string> result = new List<string>();
      int sectionIndex = 0;
      int namespaceMembersGroupTypeIndex = 0;

      while (Enum.IsDefined(typeof(NamespaceMembersGroups), namespaceMembersGroupTypeIndex))
      {
        NamespaceMembersGroups membersGroupType = (NamespaceMembersGroups)namespaceMembersGroupTypeIndex;

        if (myNamespaceInfo.GetMembersCount(membersGroupType) > 0)
        {
          if (MyNamespaceInfo.IsMembersGroupTypePublic(membersGroupType))
          {
            result.Add(MyNamespaceInfo.GetBaseGroupName(membersGroupType) + ":" + sectionIndex);
          }
        }

        namespaceMembersGroupTypeIndex++;
        sectionIndex++;
      }

      return result.ToArray();
    }

    private string[] ObtainListOfSectionsNames(MyNamespaceInfo myNamespaceInfo)
    {
      List<string> result = new List<string>();
      int sectionIndex = 0;
      int namespaceMembersGroupTypeIndex = 0;

      while (Enum.IsDefined(typeof(NamespaceMembersGroups), namespaceMembersGroupTypeIndex))
      {
        NamespaceMembersGroups membersGroupType = (NamespaceMembersGroups)namespaceMembersGroupTypeIndex;

        if (myNamespaceInfo.GetMembersCount(membersGroupType) > 0)
        {
          string sectionName = MyNamespaceInfo.NamespaceMembersGroupToString(membersGroupType);
          int tmpSectionIndex = sectionIndex;

          tmpSectionIndex++;
          if (myNamespaceInfo.HasProtectedInternalGroupOfTheSameType(membersGroupType))
          {
            sectionName += String.Format(" ${{SectionReference:{0}:{1}}}", tmpSectionIndex, "Protected Internal");
          }

          tmpSectionIndex++;
          if (myNamespaceInfo.HasProtectedGroupOfTheSameType(membersGroupType))
          {
            sectionName += String.Format(" ${{SectionReference:{0}:{1}}}", tmpSectionIndex, "Protected");
          }

          tmpSectionIndex++;
          if (myNamespaceInfo.HasInternalGroupOfTheSameType(membersGroupType))
          {
            sectionName += String.Format(" ${{SectionReference:{0}:{1}}}", tmpSectionIndex, "Internal");
          }

          tmpSectionIndex++;
          if (myNamespaceInfo.HasPrivateGroupOfTheSameType(membersGroupType))
          {
            sectionName += String.Format(" ${{SectionReference:{0}:{1}}}", tmpSectionIndex, "Private");
          }

          result.Add(sectionName);
        }

        namespaceMembersGroupTypeIndex++;
        sectionIndex++;
      }

      return result.ToArray();
    }

    private string[] ObtainListOfMembersSectionsShortcutsNamesAndIndices(MyClassInfo myClassInfo)
    {
      if (myClassInfo is MyEnumerationInfo || myClassInfo is MyDelegateInfo)
      {
        Debug.Assert(false, String.Format("Impossible! We don't want to create member's index of type '{0}'.", myClassInfo.GetType()));
        return null;
      }

      List<string> result = new List<string>();

      int classMembersGroupTypeIndex = 0;
      int sectionIndex = 0;
      while (Enum.IsDefined(typeof(ClassMembersGroups), classMembersGroupTypeIndex))
      {
        ClassMembersGroups classMembersGroupType = (ClassMembersGroups)classMembersGroupTypeIndex;

        if (myClassInfo.GetMembersCount(classMembersGroupType) > 0)
        {
          if (MyClassInfo.IsMembersGroupPublic(classMembersGroupType))
          {
            result.Add(MyClassInfo.GetBaseGroupName(classMembersGroupType) + ":" + sectionIndex);
          }
        }

        classMembersGroupTypeIndex++;
        sectionIndex++;
      }

      return result.ToArray();
    }

    private string[] ObtainListOfMembersSectionsNames(MyClassInfo myClassInfo)
    {
      if (myClassInfo is MyEnumerationInfo || myClassInfo is MyDelegateInfo)
      {
        Debug.Assert(false, String.Format("Impossible! We don't want to create member's index of type '{0}'.", myClassInfo.GetType()));
        return null;
      }

      List<string> result = new List<string>();

      int classMembersGroupTypeIndex = 0;
      int sectionIndex = 0;
      while (Enum.IsDefined(typeof(ClassMembersGroups), classMembersGroupTypeIndex))
      {
        ClassMembersGroups classMembersGroupType = (ClassMembersGroups)classMembersGroupTypeIndex;

        if (myClassInfo.GetMembersCount(classMembersGroupType) > 0)
        {
          string sectionName = MyClassInfo.ClassMembersGroupsToString(classMembersGroupType);
          int tmpSectionIndex = sectionIndex;

          tmpSectionIndex++;
          if (myClassInfo.HasProtectedInternalGroupOfTheSameType(classMembersGroupType))
          {
            sectionName += String.Format(" ${{SectionReference:{0}:{1}}}", tmpSectionIndex, "Protected Internal");
          }

          tmpSectionIndex++;
          if (myClassInfo.HasProtectedGroupOfTheSameType(classMembersGroupType))
          {
            sectionName += String.Format(" ${{SectionReference:{0}:{1}}}", tmpSectionIndex, "Protected");
          }

          tmpSectionIndex++;
          if (myClassInfo.HasInternalGroupOfTheSameType(classMembersGroupType))
          {
            sectionName += String.Format(" ${{SectionReference:{0}:{1}}}", tmpSectionIndex, "Internal");
          }

          tmpSectionIndex++;
          if (myClassInfo.HasPrivateGroupOfTheSameType(classMembersGroupType))
          {
            sectionName += String.Format(" ${{SectionReference:{0}:{1}}}", tmpSectionIndex, "Private");
          }

          result.Add(sectionName);
        }

        classMembersGroupTypeIndex++;
        sectionIndex++;
      }

      return result.ToArray();
    }

    private string GetDeclaringTypeNameOfANestedType(MyClassInfo myClassInfo)
    {
      int indexOfLastPlus = myClassInfo.Name.LastIndexOf('/');

      if (indexOfLastPlus == -1)
      {
        return null;
      }

      return myClassInfo.Name.Substring(0, indexOfLastPlus);
    }

    public static string ProcessType(string typeFullName)
    {
      string result = typeFullName.Contains("System.Nullable") ? ProcessNullables(typeFullName) : typeFullName;

      result = typePattern.Replace(result, typeRegexEvaluator);

      return Utils.HTMLEncode(result.Replace('/', '.'));
    }

    private static string ProcessNullables(string typeFullName)
    {
      string result = typeFullName;

      while (true)
      {
        bool found;

        result = ProcessNullablesAux(result, out found);

        if (!found)
        {
          break;
        }
      }

      return result;
    }

    private static string ProcessNullablesAux(string result, out bool found)
    {
      string pattern = "System.Nullable<";
      int index = 0;

      while (true)
      {
        index = result.IndexOf(pattern, index);
        if (index == -1)
        {
          found = false;

          return result;
        }
        else if (index == 0 || result[index - 1] != '.')
        {
          break;
        }

        index++;
        if (index >= result.Length)
        {
          // normally shouldn't happen
          // (because of the previous if clauses and the fact that pattern.Length > 1)

          found = false;

          return result;
        }
      }

      int innerStartIndex = index + pattern.Length;
      int closingBracketIndex = innerStartIndex;
      int depth = 1;

      while (closingBracketIndex < result.Length)
      {
        if (result[closingBracketIndex] == '<')
        {
          depth++;
        }
        else if (result[closingBracketIndex] == '>')
        {
          depth--;
          if (depth == 0)
          {
            break;
          }
        }

        closingBracketIndex++;
      }

      if (closingBracketIndex >= result.Length)
      {
        Debug.Assert(false, "Impossible! Couldn't find matching closing angle bracket.");

        found = false;

        return result;
      }

      found = true;

      return result.Substring(0, index) + result.Substring(innerStartIndex, closingBracketIndex - innerStartIndex) + "?" + (closingBracketIndex + 1 < result.Length ? result.Substring(closingBracketIndex + 1) : "");
    }

    private static string OnProcessTypeMatch(Match match)
    {
      if (systemTypesConversions.ContainsKey(match.Value))
      {
        return systemTypesConversions[match.Value];
      }
      else
      {
        return Utils.GetUnqualifiedName(match.Value);
      }
    }

    private string GetAliasName(string fileName)
    {
      return GetAliasName(fileName, false, false);
    }

    private string GetAliasName(string fileName, bool relative, bool asLink)
    {
      if (!fileNamesMappings.ContainsKey(fileName))
      {
        fileIndex++;
        if (fileIndex > MAX_FILES_PER_DIR)
        {
          fileIndex = 1;
          dirIndex++;

          dirIndexStr = dirIndex.ToString();

          Directory.CreateDirectory(Utils.CombinePaths(assembliesDirectory, dirIndexStr));
        }

        fileNamesMappings[fileName] = Utils.CombineMultiplePaths(dirIndexStr, fileIndex.ToString() + Utils.GetExtension(fileName));
      }

      string result;

      if (relative)
      {
        result = @"..\..\" + CONTENTS_DIRECTORY + Path.DirectorySeparatorChar + fileNamesMappings[fileName];
      }
      else
      {
        result = Utils.CombinePaths(assembliesDirectory, fileNamesMappings[fileName]);
      }

      if (asLink) { return result.Replace(Path.DirectorySeparatorChar, '/'); }
      else { return result; }
    }

    #endregion
  }
}
