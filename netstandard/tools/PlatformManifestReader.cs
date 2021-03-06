﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Build.Tasks
{
    static class PlatformManifestReader
    {
        static readonly char[] s_manifestLineSeparator = new[] { '|' };
        public static IEnumerable<ConflictItem> LoadConflictItems(string manifestPath, ILog log)
        {
            if (manifestPath == null)
            {
                throw new ArgumentNullException(nameof(manifestPath));
            }

            if (!File.Exists(manifestPath))
            {
                log.LogError($"Could not load PlatformManifest from {manifestPath} because it did not exist");
                yield break;
            }

            using (var manfiestStream = File.Open(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            using (var manifestReader = new StreamReader(manfiestStream))
            {
                for (int lineNumber = 0; !manifestReader.EndOfStream; lineNumber++)
                {
                    var line = manifestReader.ReadLine().Trim();

                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    var lineParts = line.Split(s_manifestLineSeparator);

                    if (lineParts.Length != 4)
                    {
                        log.LogError($"Error parsing PlatformManifest from {manifestPath} line {lineNumber}.  Lines must have the format fileName|packageId|assemblyVersion|fileVersion");
                        yield break;
                    }

                    var fileName = lineParts[0].Trim();
                    var packageId = lineParts[1].Trim();
                    var assemblyVersionString = lineParts[2].Trim();
                    var fileVersionString = lineParts[3].Trim();

                    Version assemblyVersion = null, fileVersion = null;

                    if (assemblyVersionString.Length != 0 && !Version.TryParse(assemblyVersionString, out assemblyVersion))
                    {
                        log.LogError($"Error parsing PlatformManfiest from {manifestPath} line {lineNumber}.  AssemblyVersion {assemblyVersionString} was invalid.");
                    }

                    if (fileVersionString.Length != 0 && !Version.TryParse(fileVersionString, out fileVersion))
                    {
                        log.LogError($"Error parsing PlatformManifest from {manifestPath} line {lineNumber}.  FileVersion {fileVersionString} was invalid.");
                    }

                    yield return new ConflictItem(fileName, packageId, assemblyVersion, fileVersion);
                }
            }
        }
    }
}
