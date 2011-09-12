using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("mDCM.SP.Sandbox")]
[assembly: AssemblyDescription("C# DICOM Library - Modified for SharePoint Sandbox")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("mDCM.SP.Sandbox")]
[assembly: AssemblyCopyright("Copyright © Colby Dillion 2010")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("DDAC0745-DE99-4199-9129-BAF17F26DB05")]

[assembly: AssemblyVersion("0.9.6.0")]

//Allow partially trusted callers - required in the SharePoint sandbox
[assembly: System.Security.AllowPartiallyTrustedCallers()]