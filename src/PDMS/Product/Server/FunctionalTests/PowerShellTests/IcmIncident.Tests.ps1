Describe "IcmIncident Tests"  {

    Function Script:CreateIncident {
        $i = New-PdmsObject Incident

        Set-PdmsProperty $i Title "IcmIncident PowerShell Test"
        Set-PdmsProperty $i Severity 4
        Set-PdmsProperty $i Body "IcmIncident PowerShell Test Body"

        $r = New-PdmsObject RouteData

        Set-PdmsProperty $r OwnerId ([GUID]"3a803621-fcaa-4727-899f-34c9398cb02b")

        Set-PdmsProperty $r EventName "PowerShell Test"
        Set-PdmsProperty $i Routing $r

        New-PdmsIncident -Value $i
    }

    It "can create a new IcmIncident" {
        $newIncident = CreateIncident
        
        $newIncident.Id | Should -Not -BeNullOrEmpty
    }
}
