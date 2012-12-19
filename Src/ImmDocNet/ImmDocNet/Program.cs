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
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Diagnostics;

using Imm.ImmDocNetLib;
using Imm.ImmDocNetLib.Documenters;
using Imm.ImmDocNetLib.Documenters.HTMLDocumenter;

namespace Imm.ImmDocNet
{
  class Program
  {
    private const int MAX_VERBOSE_LEVEL = 3;

    // 0 - nothing
    // 1 - errors
    // 2 - errors and warnings
    // 3 - progress and errors and warnings
    private static int verboseLevel = MAX_VERBOSE_LEVEL;

    private static long generatingStartTime;
    private static float generatingTime;
    private static long preparationStartTime;
    private static float preparationTime;

    private static string projectName;
    private static string chmFileNameWithoutExtension;
    private static string outputDirectory = "doc";
    private static AssembliesInfo assembliesInfo;
    private static Dictionary<string, bool> excludedFilesNames;
    private static HashSet<string> excludedNamespaces;
    private static DocumentationGenerationOptions docGenOptions = DocumentationGenerationOptions.None;

    private static readonly string ASSEMBLY_CODE_BASE;

    #region Constructor(s)

    static Program()
    {
      ASSEMBLY_CODE_BASE = Assembly.GetExecutingAssembly().GetName().CodeBase;

      excludedFilesNames = new Dictionary<string, bool>();
      excludedNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Application entry point

    private static void Main(string[] args)
    {
      long totalStartTime = Environment.TickCount;

      List<string> options = new List<string>();
      List<string> filesNames = new List<string>();

      foreach (string arg in args)
      {
        if (arg.StartsWith("-"))
        {
          if (arg.Length > 1)
          {
            options.Add(arg.Substring(1));
          }
          else
          {
            PrintUsageAndExit();
          }
        }
        else
        {
          filesNames.Add(arg);
        }
      }

      ProcessOptions(options);

      if ((docGenOptions & DocumentationGenerationOptions.IncludeInternalMembers) != 0)
      {
        Utils.IncludeInternalMembers = true;
      }

      if ((docGenOptions & DocumentationGenerationOptions.IncludePrivateMembers) != 0)
      {
        Utils.IncludePrivateMembers = true;
      }

      if (verboseLevel > 2)
      {
        Console.WriteLine("ImmDoc.NET");
        string yearString;

        if (DateTime.Now.Year > 2007)
        {
          yearString = "2007 - " + DateTime.Now.Year;
        }
        else
        {
          yearString = "2007";
        }

        Console.WriteLine("Copyright (C) " + yearString + " Marek \"Immortal\" Stój");
        Console.WriteLine();
      }

      List<int> indicesToBeRemoved;

      if (filesNames.Count == 0)
      {
        // add files from the current directory
        filesNames.AddRange(Directory.GetFiles(Environment.CurrentDirectory, "*.exe"));
        filesNames.AddRange(Directory.GetFiles(Environment.CurrentDirectory, "*.dll"));
        filesNames.AddRange(Directory.GetFiles(Environment.CurrentDirectory, "*.xml"));
        filesNames.AddRange(Directory.GetFiles(Environment.CurrentDirectory, "*.docs"));
      }
      else
      {
        // remove files with unknown extensions
        indicesToBeRemoved = new List<int>();

        for (int i = 0; i < filesNames.Count; i++)
        {
          string fullFileName = filesNames[i];
          string ext = Path.GetExtension(fullFileName).ToLower();

          if (ext != ".exe" && ext != ".dll" && ext != ".xml" && ext != ".docs")
          {
            if (verboseLevel > 2) { Console.WriteLine("Excluded file {0} (unsupported type).", Path.GetFileName(fullFileName)); }

            indicesToBeRemoved.Add(i);
          }
        }

        Utils.RemoveItems(filesNames, indicesToBeRemoved);
      }

      // remove program files
      indicesToBeRemoved = new List<int>();

      string programExeFileNameLower = Path.GetFileName(ASSEMBLY_CODE_BASE).ToLower();

      for (int i = 0; i < filesNames.Count; i++)
      {
        string fileName = Path.GetFileName(filesNames[i]);

        if (fileName.ToLower() == programExeFileNameLower)
        {
          if (verboseLevel > 2) { Console.WriteLine("Excluded file {0} (program executable).", fileName); }

          indicesToBeRemoved.Add(i);
        }
      }

      Utils.RemoveItems(filesNames, indicesToBeRemoved);

      // remove vshost files
      indicesToBeRemoved = new List<int>();

      for (int i = 0; i < filesNames.Count; i++)
      {
        string fullFileName = filesNames[i];

        if (fullFileName.ToLower().EndsWith(".vshost.exe"))
        {
          if (verboseLevel > 2) { Console.WriteLine("Excluded file {0} (file generated by Visual Studio).", Path.GetFileName(fullFileName)); }

          indicesToBeRemoved.Add(i);
        }
      }

      Utils.RemoveItems(filesNames, indicesToBeRemoved);

      long processingStartTime = Environment.TickCount;

      ProcessFilesNames(filesNames, excludedNamespaces);

      if (verboseLevel > 2) { Console.WriteLine(); }

      float processingTime = (Environment.TickCount - processingStartTime) / 1000.0f;

      Documenter documenter = new HTMLDocumenter(assembliesInfo, chmFileNameWithoutExtension);

      documenter.DirectoryDeleteStarted += documenter_DirectoryDeleteStarted;
      documenter.DirectoryDeleteFinished += documenter_DirectoryDeleteFinished;
      documenter.GeneratingStarted += documenter_GeneratingStarted;
      documenter.GeneratingFinished += documenter_GeneratingFinished;

      preparationStartTime = Environment.TickCount;

      bool success = documenter.GenerateDocumentation(outputDirectory, docGenOptions);

      if (verboseLevel > 2)
      {
        Console.WriteLine();

        Console.WriteLine("Processing time  : {0:F2} s", processingTime);
        Console.WriteLine("Preparation time : {0:F2} s", preparationTime);
        Console.WriteLine("Generating time  : {0:F2} s", generatingTime);
        Console.WriteLine("Total time       : {0:F2} s", (Environment.TickCount - totalStartTime) / 1000.0f);

        Console.WriteLine();

        Console.WriteLine("Warnings: {0}", Logger.WarningsCount);
        Console.WriteLine("Errors:   {0}", Logger.ErrorsCount);
      }

      if (verboseLevel > 1 && Logger.WarningsCount > 0)
      {
        if (verboseLevel > 2)
        {
          Console.Error.WriteLine();
        }

        Logger.WriteWarnings(Console.Error);
      }

      if (!success || Logger.ErrorsCount > 0)
      {
        if (verboseLevel > 0 && Logger.ErrorsCount > 0)
        {
          if (verboseLevel > 2 || (verboseLevel > 1 && Logger.WarningsCount > 0))
          {
            Console.Error.WriteLine();
          }

          Logger.WriteErrors(Console.Error);
        }

        Environment.Exit(1);
      }
      else
      {
        Environment.Exit(0);
      }
    }

    #endregion

    #region Private helper methods

    private static void ProcessOptions(List<string> options)
    {
      foreach (string option in options)
      {
        string opName;
        string opArg = null;
        int indexOfColon = option.IndexOf(':');

        if (indexOfColon == -1)
        {
          opName = option.ToLower();
        }
        else
        {
          opName = option.Substring(0, indexOfColon).ToLower();
          opArg = indexOfColon + 1 < option.Length ? option.Substring(indexOfColon + 1) : null;
        }

        if (opName == "help" || opName == "h")
        {
          PrintUsageAndExit();
        }
        else if (opName == "projectname" || opName == "pn")
        {
          if (opArg == null) { PrintUsageAndExit(); }

          projectName = opArg;
        }
        else if (opName == "chmname" || opName == "cn")
        {
          if (opArg == null) { PrintUsageAndExit(); }

          chmFileNameWithoutExtension = opArg;

          if (chmFileNameWithoutExtension.EndsWith(".chm", true, null))
          {
            chmFileNameWithoutExtension = chmFileNameWithoutExtension.Substring(0, chmFileNameWithoutExtension.Length - 4);
          }
        }
        else if (opName == "verboselevel" || opName == "vl")
        {
          if (opArg == null) { PrintUsageAndExit(); }

          if (!Int32.TryParse(opArg, out verboseLevel) || verboseLevel < 0 || verboseLevel > MAX_VERBOSE_LEVEL)
          {
            PrintUsageAndExit();
          }
        }
        else if (opName == "exclude" || opName == "ex")
        {
          if (opArg == null) { PrintUsageAndExit(); }

          excludedFilesNames[opArg.ToLower()] = true;
        }
        else if (opName == "excludenamespace" || opName == "exn")
        {
          if (opArg == null) { PrintUsageAndExit(); }

          excludedNamespaces.Add(opArg.ToLower());
        }
        else if (opName == "outputdirectory" || opName == "od")
        {
          if (opArg == null) { PrintUsageAndExit(); }

          if (Path.GetFileName(opArg) == "")
          {
            Console.WriteLine("Error: Enter non-empty output directory.");
            Environment.Exit(1);
          }

          outputDirectory = opArg;
        }
        else if (opName == "forcedelete" || opName == "fd")
        {
          if (opArg != null) { PrintUsageAndExit(); }

          docGenOptions |= DocumentationGenerationOptions.DeleteOutputDirIfItExists;
        }
        else if (opName == "includeinternalmembers" || opName == "iim")
        {
          if (opArg != null) { PrintUsageAndExit(); }

          docGenOptions |= DocumentationGenerationOptions.IncludeInternalMembers;
        }
        else if (opName == "includeprivatemembers" || opName == "ipm")
        {
          if (opArg != null) { PrintUsageAndExit(); }

          docGenOptions |= DocumentationGenerationOptions.IncludePrivateMembers;
        }
        else
        {
          PrintUsageAndExit();
        }
      }
    }

    private static void ProcessFilesNames(List<string> filesNames, IEnumerable<string> excludedNamespaces)
    {
      assembliesInfo = new AssembliesInfo(projectName == null ? "Documentation Project" : projectName);

      // proces assemblies (*.exe || *.dll)
      foreach (string fullFileName in filesNames)
      {
        string fileName = Path.GetFileName(fullFileName);
        string ext = Path.GetExtension(fileName).ToLower();

        if ((ext != ".exe" && ext != ".dll"))
        {
          continue;
        }

        if (excludedFilesNames.ContainsKey(fileName.ToLower()))
        {
          if (verboseLevel > 2) { Console.WriteLine("Excluded file {0} (exclusion specified in options).", Path.GetFileName(fullFileName)); }

          continue;
        }

        if (!File.Exists(fullFileName))
        {
          if (verboseLevel > 2) { Console.WriteLine("Omitted assembly {0} (file doesn't exist).", fileName); }

          continue;
        }

        if (verboseLevel > 2) { Console.Write("Processing assembly {0}... ", fileName); }

        assembliesInfo.ReadMyAssemblyInfoFromAssembly(fullFileName, excludedNamespaces);

        if (verboseLevel > 2) { Console.WriteLine("DONE"); }
      }

      // process xml documentations (*.xml)
      foreach (string fullFileName in filesNames)
      {
        string fileName = Path.GetFileName(fullFileName);
        string ext = Path.GetExtension(fileName).ToLower();

        if (ext != ".xml" || excludedFilesNames.ContainsKey(fileName.ToLower()))
        {
          continue;
        }

        if (!File.Exists(fullFileName))
        {
          if (verboseLevel > 2) { Console.WriteLine("Omitted documentation file {0} (file doesn't exist).", fileName); }

          continue;
        }

        if (verboseLevel > 2) { Console.Write("Processing documentation file {0}... ", fileName); }

        assembliesInfo.ReadMyAssemblyInfoFromXmlDocumentation(fullFileName);

        if (verboseLevel > 2) { Console.WriteLine("DONE"); }
      }

      // process additional documentation (*.docs)
      foreach (string fullFileName in filesNames)
      {
        string fileName = Path.GetFileName(fullFileName);
        string ext = Path.GetExtension(fileName).ToLower();

        if (ext != ".docs" || excludedFilesNames.ContainsKey(fileName.ToLower()))
        {
          continue;
        }

        if (!File.Exists(fullFileName))
        {
          if (verboseLevel > 2) { Console.WriteLine("Omitted additional documentation file {0} (file doesn't exist).", fileName); }

          continue;
        }

        if (!ValidateAdditionalDocumentationFile(fullFileName))
        {
          if (verboseLevel > 2) { Console.WriteLine("Omitted additional documentation file {0} (wrong file format).", fileName); }

          continue;
        }

        if (verboseLevel > 2) { Console.Write("Reading additional documentation file {0}... ", fileName); }

        assembliesInfo.ReadAdditionalDocumentation(fullFileName);

        if (verboseLevel > 2) { Console.WriteLine("DONE"); }
      }
    }

    private static void PrintUsageAndExit()
    {
      string verboseLevels = "";

      for (int i = 0; i <= MAX_VERBOSE_LEVEL; i++)
      {
        if (i == MAX_VERBOSE_LEVEL && i != 0) { verboseLevels += " or "; }
        else if (i > 0) { verboseLevels += ", "; }

        verboseLevels += i;
      }

      Console.WriteLine("Usage: {0} [OPTION]... [FILE]... ", Path.GetFileName(ASSEMBLY_CODE_BASE));
      Console.WriteLine("Generate HTML documentation from a set of assemblies and XML files.");

      Console.WriteLine();
      Console.WriteLine("Options:");
      Console.WriteLine("  -h,   -Help                    displays this message");
      Console.WriteLine("  -pn,  -ProjectName:STRING      sets STRING as the name of the project");
      Console.WriteLine("  -cn,  -CHMName:STRING          sets STRING as the name of the output CHM file");
      Console.WriteLine("  -ex,  -Exclude:FILE            excludes FILE from processing");
      Console.WriteLine("  -exn, -ExcludeNamespace:STRING excludes namespace STRING from processing");
      Console.WriteLine("  -od,  -OutputDirectory:DIR     sets DIR as the output directory");
      Console.WriteLine("  -fd,  -ForceDelete             forces the program to delete output directory");
      Console.WriteLine("  -iim, -IncludeInternalMembers  internal members will be processed");
      Console.WriteLine("  -ipm, -IncludePrivateMembers   private members will be processed");
      Console.WriteLine("  -vl,  -VerboseLevel:LEVEL      sets verbose level; LEVEL is {0}", verboseLevels);

      Console.WriteLine();
      Console.WriteLine("If no files are explicitly given then all files with extensions of .exe, .dll,");
      Console.WriteLine(".xml and .docs from the current directory will be processed (except for the");
      Console.WriteLine("files excluded by -Exclude option and the program executable itself).");

      Console.WriteLine();
      Console.WriteLine("In order to create CHM output you need to have HTML Help Workshop installed.");
      Console.WriteLine("If you have HTML Help Workshop installed in a folder different than default");
      Console.WriteLine("then you have to set HHC_HOME environment variable to point to the correct");
      Console.WriteLine("installation directory.");

      Console.WriteLine();
      Console.WriteLine("Send comments and bug reports to <admin@immortal.pl>.");

      Environment.Exit(1);
    }

    #endregion

    #region Event handlers

    private static void documenter_DirectoryDeleteStarted(object sender, EventArgs e)
    {
      if (verboseLevel > 2) { Console.Write("Deleting output directory...     "); }
    }

    private static void documenter_DirectoryDeleteFinished(object sender, EventArgs e)
    {
      preparationTime = (Environment.TickCount - preparationStartTime) / 1000.0f;

      if (verboseLevel > 2) { Console.WriteLine("DONE"); }
    }

    private static void documenter_GeneratingStarted(object sender, EventArgs e)
    {
      generatingStartTime = Environment.TickCount;

      if (verboseLevel > 2) { Console.Write("Generating HTML documentation... "); }
    }

    private static void documenter_GeneratingFinished(object sender, EventArgs e)
    {
      generatingTime = (Environment.TickCount - generatingStartTime) / 1000.0f;

      if (verboseLevel > 2) { Console.WriteLine("DONE"); }
    }

    #endregion

    #region Additional documentation file validation

    private static bool valid;
    private static XmlReaderSettings xrs = null;

    private static bool ValidateAdditionalDocumentationFile(string fullFileName)
    {
      valid = true;

      XmlReaderSettings xrs = GetXmlReaderSettings();
      XmlReader xr = null;

      try
      {
        xr = XmlReader.Create(fullFileName, xrs);

        while (xr.Read())
          ;
      }
      catch (Exception)
      {
        valid = false;
      }
      finally
      {
        if (xr != null)
        {
          xr.Close();
        }
      }

      return valid;
    }

    private static XmlReaderSettings GetXmlReaderSettings()
    {
      if (xrs != null)
      {
        return xrs;
      }

      Assembly assembly = Assembly.GetExecutingAssembly();
      Stream stream = assembly.GetManifestResourceStream(typeof(Program).Namespace + ".AdditionalDocumentation.xsd");
      XmlSchema xmlSchema = XmlSchema.Read(stream, new ValidationEventHandler(SchemaValidation));

      stream.Close();

      xrs = new XmlReaderSettings();
      xrs.Schemas.Add(xmlSchema);
      xrs.ValidationType = ValidationType.Schema;
      xrs.ValidationEventHandler += new ValidationEventHandler(xrs_ValidationEventHandler);

      return xrs;
    }

    private static void SchemaValidation(object sender, ValidationEventArgs e)
    {
      Debug.Assert(false, "Impossible!");
    }

    private static void xrs_ValidationEventHandler(object sender, ValidationEventArgs e)
    {
#if DEBUG

      Console.WriteLine("");

#endif

      Console.WriteLine("XSD: " + e.Severity.ToString() + ": " + e.Message);

      valid = false;
    }

    #endregion
  }
}
