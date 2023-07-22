
Describe "DataAsset Tests"  {

    Function Script:FindAssets {
        Find-PdmsDataAsset "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod;RelativePath=/local/upload/PROD/DeleteSignal/CookedStream/v2"
    }

    It "can find a data asset" {
        $assets = FindAssets
        
        $assets | Should -Not -BeNullOrEmpty
    }
}
