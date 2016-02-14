using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ContractOnlyStoredProcedures")]
[assembly: AssemblyDescription("Creates a proxy to a database's stored procedures by defining an interface. Uses ContractOnlyStoredProcedures to call the stored procedures.")]
[assembly: AssemblyProduct("ContractOnlyStoredProcedures")]
[assembly: AssemblyCopyright("Copyright © Abraham Heidebrecht 2016")]
[assembly: ComVisible(false)]

// These will be set during build in Appveyor
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion("0.0.0.0")]
[assembly: AssemblyInformationalVersion("0.0.0.0")]


#if NET40
[assembly: InternalsVisibleTo("ContractOnlyStoredProcedures.Tests-NET40")]
#else
[assembly: InternalsVisibleTo("ContractOnlyStoredProcedures.Tests")]
#endif
