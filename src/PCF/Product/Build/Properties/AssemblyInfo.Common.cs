using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// NuGet is using that field for figuring out what version to give to packages based on assemblies.
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyProduct("Microsoft(R) Privacy Command Feed")]
[assembly: AssemblyCopyright("Copyright (c) Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyCulture("")]

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=002400000480000094000000060200000024000052534131000400000100010077f5e87030dadccce6902c6adab7a987bd69cb5819991531f560785eacfc89b6fcddf6bb2a00743a7194e454c0273447fc6eec36474ba8e5a3823147d214298e4f9a631b1afee1a51ffeae4672d498f14b000e3d321453cdd8ac064de7e1cf4d222b7e81f54d4fd46725370d702a05b48738cc29d09228f1aa722ae1a9ca02fb")]

#if !STRONG_NAME_SIGNED
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif

#if INTERNALS_VISIBLE_TO_TESTS && !STRONG_NAME_SIGNED
[assembly: InternalsVisibleTo("Pcf.UnitTests")]
#endif

#if DEBUG || _DEBUG
[assembly: AssemblyConfiguration("Debug")]
#endif