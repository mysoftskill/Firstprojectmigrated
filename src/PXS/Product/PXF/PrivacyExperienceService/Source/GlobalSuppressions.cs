// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Scope = "type", Target = "Microsoft.Membership.MemberServices.PrivacyExperience.Service.Startup")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Membership.MemberServices.PrivacyExperience.Service.Startup.#Configuration(Owin.IAppBuilder)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnhandledExceptionHandler.#HandleAsync(System.Web.Http.ExceptionHandling.ExceptionHandlerContext,System.Threading.CancellationToken)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "Microsoft.Membership.MemberServices.PrivacyExperience.Service.Host.ExceptionDecorator.#Execute()", Justification = "This class is a catch all for unhandled exception")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers.PrivacyController.#CreateHttpActionResult`1(Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.ServiceResponse`1<!!0>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Membership.MemberServices.PrivacyExperience.Service.Handlers.PerfCounterHandler.#.cctor()")]
