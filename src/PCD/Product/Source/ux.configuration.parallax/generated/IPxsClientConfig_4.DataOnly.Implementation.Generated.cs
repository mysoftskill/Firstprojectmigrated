// ----------------------------------------------------------------------------------------------
// <copyright file="IPxsClientConfig_4.DataOnly.Implementation.Generated.cs" company="Microsoft">
//     This file is generated by a tool.
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

//////////////////////////////////////////////////////////////////////////////////////////////////
//// GENERATED CODE - PLEASE DO NOT MODIFY BY HAND!!! ALL YOUR CHANGES WILL BE OVERWRITTEN!!! ////
//////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.PrivacyServices.UX.Configuration
{
    /// <summary>
    /// Parallax-generated implementation of a variant object interface. Subject to change without notice.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("microsoft.search.platform.parallax.tools.codegenerator.exe", "1.0.0.0")]
    internal sealed class _DataOnly_IPxsClientConfig_Implementation_ : IPxsClientConfig, global::Microsoft.Search.Platform.Parallax.Core.Model.IVariantObjectInstance, global::Microsoft.Search.Platform.Parallax.Core.Model.IVariantObjectInstanceProvider
    {
        global::Microsoft.Search.Platform.Parallax.Core.Model.VariantContextSnapshot global::Microsoft.Search.Platform.Parallax.Core.Model.IVariantObjectInstance.Context
        {
            get
            {
                return null;
            }
        }

        global::Microsoft.Search.Platform.Parallax.Core.Model.IVariantObjectInstance global::Microsoft.Search.Platform.Parallax.Core.Model.IVariantObjectInstanceProvider.GetVariantObjectInstance(global::Microsoft.Search.Platform.Parallax.Core.Model.VariantContextSnapshot context)
        {
            return this;
        }

        internal string _ApiEndpoint_MaterializedValue_ = default(string);

        public string @ApiEndpoint
        {
            get
            {
                return this._ApiEndpoint_MaterializedValue_;
            }
        }

        internal string _ResourceId_MaterializedValue_ = default(string);

        public string @ResourceId
        {
            get
            {
                return this._ResourceId_MaterializedValue_;
            }
        }
    }
}
