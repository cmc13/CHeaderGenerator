// Guids.cs
// MUST match guids.h
using System;

namespace CHeaderGenerator
{
    static class GuidList
    {
        public const string guidCHeaderGeneratorPkgString = "62a30c8e-a179-4d9a-a8cf-f8a1454a7359";
        public const string SolutionExplorerCmdSetString = "fa3a1e9c-0f42-4316-8b00-f4e1298b8ce3";
        public const string CurrentDocumentCmdSetString = "584D7D14-5442-4BD6-A56A-E26B054C8323";

        public static readonly Guid SolutionExplorerCmdSet = new Guid(SolutionExplorerCmdSetString);
        public static readonly Guid CurrentDocumentCmdSet = new Guid(CurrentDocumentCmdSetString);
    }
}