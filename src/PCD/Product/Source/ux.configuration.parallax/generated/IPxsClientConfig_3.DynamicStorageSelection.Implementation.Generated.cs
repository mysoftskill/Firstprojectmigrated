// ----------------------------------------------------------------------------------------------
// <copyright file="IPxsClientConfig_3.DynamicStorageSelection.Implementation.Generated.cs" company="Microsoft">
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
    internal sealed class _DynamicStorageSelection_IPxsClientConfig_Implementation_ : IPxsClientConfig, global::Microsoft.Search.Platform.Parallax.Core.Model.IDataAccessorBackedObject<_DynamicStorageSelection_IPxsClientConfig_DataAccessor_>
    {
        private _DynamicStorageSelection_IPxsClientConfig_DataAccessor_ dataAccessor;

        private global::Microsoft.Search.Platform.Parallax.Core.Model.VariantContextSnapshot context;

        global::Microsoft.Search.Platform.Parallax.Core.Model.VariantContextSnapshot Microsoft.Search.Platform.Parallax.Core.Model.IVariantObjectInstance.Context
        {
            get
            {
                return this.context;
            }
        }

        _DynamicStorageSelection_IPxsClientConfig_DataAccessor_ global::Microsoft.Search.Platform.Parallax.Core.Model.IDataAccessorBackedObject<_DynamicStorageSelection_IPxsClientConfig_DataAccessor_>.DataAccessor
        {
            get
            {
                return this.dataAccessor;
            }
        }

        void global::Microsoft.Search.Platform.Parallax.Core.Model.IDataAccessorBackedObject<_DynamicStorageSelection_IPxsClientConfig_DataAccessor_>.Initialize(_DynamicStorageSelection_IPxsClientConfig_DataAccessor_ dataAccessor, global::Microsoft.Search.Platform.Parallax.Core.Model.VariantContextSnapshot context)
        {
            this.dataAccessor = dataAccessor;
            this.context = context;
        }

        public string @ApiEndpoint
        {
            get
            {
                if (this.dataAccessor._ApiEndpoint_ValueProvider_ != null)
                {
                    return this.dataAccessor._ApiEndpoint_ValueProvider_.GetValue(this.context);
                }
                
                return this.dataAccessor._ApiEndpoint_MaterializedValue_;
            }
        }

        public string @ResourceId
        {
            get
            {
                if (this.dataAccessor._ResourceId_ValueProvider_ != null)
                {
                    return this.dataAccessor._ResourceId_ValueProvider_.GetValue(this.context);
                }
                
                return this.dataAccessor._ResourceId_MaterializedValue_;
            }
        }
    }
}
