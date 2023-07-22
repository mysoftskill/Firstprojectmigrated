// ----------------------------------------------------------------------------------------------
// <copyright file="INgpLockdownConfig_3.DynamicStorageSelection.Implementation.Generated.cs" company="Microsoft">
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
    internal sealed class _DynamicStorageSelection_INgpLockdownConfig_Implementation_ : INgpLockdownConfig, global::Microsoft.Search.Platform.Parallax.Core.Model.IDataAccessorBackedObject<_DynamicStorageSelection_INgpLockdownConfig_DataAccessor_>
    {
        private _DynamicStorageSelection_INgpLockdownConfig_DataAccessor_ dataAccessor;

        private global::Microsoft.Search.Platform.Parallax.Core.Model.VariantContextSnapshot context;

        global::Microsoft.Search.Platform.Parallax.Core.Model.VariantContextSnapshot Microsoft.Search.Platform.Parallax.Core.Model.IVariantObjectInstance.Context
        {
            get
            {
                return this.context;
            }
        }

        _DynamicStorageSelection_INgpLockdownConfig_DataAccessor_ global::Microsoft.Search.Platform.Parallax.Core.Model.IDataAccessorBackedObject<_DynamicStorageSelection_INgpLockdownConfig_DataAccessor_>.DataAccessor
        {
            get
            {
                return this.dataAccessor;
            }
        }

        void global::Microsoft.Search.Platform.Parallax.Core.Model.IDataAccessorBackedObject<_DynamicStorageSelection_INgpLockdownConfig_DataAccessor_>.Initialize(_DynamicStorageSelection_INgpLockdownConfig_DataAccessor_ dataAccessor, global::Microsoft.Search.Platform.Parallax.Core.Model.VariantContextSnapshot context)
        {
            this.dataAccessor = dataAccessor;
            this.context = context;
        }

        public global::Microsoft.PrivacyServices.UX.Configuration.NgpLockdownKind @Kind
        {
            get
            {
                if (this.dataAccessor._Kind_ValueProvider_ != null)
                {
                    return this.dataAccessor._Kind_ValueProvider_.GetValue(this.context);
                }
                
                return this.dataAccessor._Kind_MaterializedValue_;
            }
        }

        public string @StartedUtc
        {
            get
            {
                if (this.dataAccessor._StartedUtc_ValueProvider_ != null)
                {
                    return this.dataAccessor._StartedUtc_ValueProvider_.GetValue(this.context);
                }
                
                return this.dataAccessor._StartedUtc_MaterializedValue_;
            }
        }

        public string @EndedUtc
        {
            get
            {
                if (this.dataAccessor._EndedUtc_ValueProvider_ != null)
                {
                    return this.dataAccessor._EndedUtc_ValueProvider_.GetValue(this.context);
                }
                
                return this.dataAccessor._EndedUtc_MaterializedValue_;
            }
        }
    }
}