﻿/////////////////////////////////////////////////////////////////////////////////////////////////////////////
// NOTICE: DO NOT EDIT THIS FILE!
// 
// This file is autogenerated and your changes will be OVERWRITTEN! 
// Edit the corresponding .tt file instead.
//
// Or, better yet, make a lasting contribution by submitting a Pull Request:  
//      https://github.com/dwcullop/BuildInfo
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Reflection;

namespace MGS2Trainer
{
    public static class BuildInfo
    {
        private const long              BUILD_DATE_BINARY_UTC       = 0x48d8bc405078cbe7;    // January 19, 2021 6:06:08.694781 AM UTC

        private static AssemblyName     BuildAssemblyName { get; }  = Assembly.GetExecutingAssembly().GetName();
        public static DateTimeOffset    BuildDateUtc { get; }       = DateTime.FromBinary(BUILD_DATE_BINARY_UTC);
        public static string            ModuleText { get; }         =  BuildAssemblyName.Name;
        public static string            VersionText { get; }        = "v" + BuildAssemblyName.Version.ToString()
#if DEBUG
                                                                                + " [DEBUG]"
#endif
                                                                                ;

        public static string            BuildDateText { get; }      = "19 January 2021 06:06:08 UTC";
        public static string            DisplayText { get; }        = $"{ModuleText} {VersionText} (Build Date: {BuildDateText})";
    }
}
