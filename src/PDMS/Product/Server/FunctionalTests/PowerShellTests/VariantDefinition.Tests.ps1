
Describe "VariantDefinition Tests"  {

    Function Script:CreateVariant {
        $variant = New-PdmsObject VariantDefinition

        $variant.Name = New-Guid
        $variant.Description = New-Guid

        New-PdmsVariantDefinition -Value $variant
    }

    It "can create a new VariantDefinition" {
        $newVariant = CreateVariant

        $newVariant.Id | Should -Not -BeNullOrEmpty
    }

    It "can read a VariantDefinition by Id" {
        $newVariant = CreateVariant

        $variantRead = Get-PdmsVariantDefinition -Id $newVariant.Id

        $variantRead.Id | Should -BeExactly $newVariant.Id
        $variantRead.Name | Should -BeExactly $newVariant.Name
        $variantRead.Description | Should -BeExactly $newVariant.Description
    }

    It "can update a VariantDefinition" {
        $newVariant = CreateVariant

        # Read current variant so that we get ETag
        $variantRead = Get-PdmsVariantDefinition -Id $newVariant.Id

        # Update variant
        $variantRead.State = "Closed"
        $variantRead.Reason = "Expired"
        $variantUpdate = Set-PdmsVariantDefinition $variantRead

        # These fields shouldn't have changed
        $variantUpdate.Id | Should -BeExactly $newVariant.Id
        $variantUpdate.Name | Should -BeExactly $newVariant.Name
        $variantUpdate.Description | Should -BeExactly $newVariant.Description

        # Check updates
        $variantUpdate.State | Should -BeExactly "Closed"
        $variantUpdate.Reason | Should -BeExactly "Expired"

        # Cleanup
        Remove-PdmsVariantDefinition -Value $variantUpdate -Force
    }

    It "can delete a VariantDefinition" {
        $newVariant = CreateVariant

        $variantRead = Get-PdmsVariantDefinition -Id $newVariant.Id

        $variantRead.Id | Should -BeExactly $newVariant.Id
        $variantRead.Name | Should -BeExactly $newVariant.Name
        $variantRead.Description | Should -BeExactly $newVariant.Description

        # Can't delete a variant in the active state
        $variantRead.State = "Closed"
        $variantRead.Reason = "Intentional"
        $variantUpdate = Set-PdmsVariantDefinition $variantRead

        # Delete Variant Definition
        Remove-PdmsVariantDefinition -Value $variantUpdate -Force
    }
}
