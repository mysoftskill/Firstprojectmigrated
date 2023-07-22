using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    /*
    * This is a local clone of AMC's Product\WebRole\Source\Core\Cms\Enums\TargetOption.cs
    * Keep changes minimal, or backport them to AMC.
    */
    public enum TargetOption
    {
        /// <summary>
        /// Default value if no target was specified.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Target type for the current window.  "_self" is the correct string so keeping the enum inline so that "ToString()" can be called.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "self", Justification = "Keep inline with HTML standard for the value.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Justification = "Keep inline with HTML standard for the value.")]
        _self,

        /// <summary>
        /// Target type for a new window.  "_blank" is the correct string so keeping the enum inline so that "ToString()" can be called.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "blank", Justification = "Keep inline with HTML standard for the value.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Justification = "Keep inline with HTML standard for the value.")]
        _blank
    }
}
