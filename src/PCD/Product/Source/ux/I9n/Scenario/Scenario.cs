namespace Microsoft.PrivacyServices.UX.I9n.Scenario
{
    /// <summary>
    /// Represents the hierarchy of scenarios.
    /// </summary>
    public static class Scenario
    {
        public static readonly ScenarioName Default = new ScenarioName("default");

        /// <summary>
        /// Register team related scenarios.
        /// </summary>
        public static class RegisterTeam
        {
            private const string BaseName = "register-team";

            public static readonly ScenarioName Default = new ScenarioName(BaseName);
            public static readonly ScenarioName TeamAlreadyExists = new ScenarioName($"{BaseName}.team-already-exists");
        }

        /// <summary>
        /// Manage team related scenarios.
        /// </summary>
        public static class ManageTeam
        {
            private const string BaseName = "manage-team";

            public static readonly ScenarioName Default = new ScenarioName(BaseName);
            public static readonly ScenarioName DeleteTeam = new ScenarioName($"{BaseName}.delete-team");
        }

        /// <summary>
        /// Manage data assets related scenarios.
        /// </summary>
        public static class ManageDataAssets
        {
            private const string BaseName = "manage-data-assets";

            public static readonly ScenarioName Default = new ScenarioName(BaseName);
            public static readonly ScenarioName RemoveAsset = new ScenarioName($"{BaseName}.remove-asset");
        }

        /// <summary>
        /// Manage data agents related scenarios.
        /// </summary>
        public static class ManageDataAgents
        {
            private const string BaseName = "manage-data-agents";

            public static readonly ScenarioName Default = new ScenarioName(BaseName);
            public static readonly ScenarioName RemoveAgent = new ScenarioName($"{BaseName}.remove-agent");
        }

        /// <summary>
        /// Manual requests related scenarios.
        /// </summary>
        public static class ManualRequests
        {
            private const string BaseName = "manual-requests";
            public static readonly ScenarioName Default = new ScenarioName(BaseName);
            public static readonly ScenarioName Status = new ScenarioName($"{BaseName}.status");

            private static readonly string DeleteBaseName = $"{BaseName}.delete";
            public static readonly ScenarioName DeleteAltSubject = new ScenarioName($"{DeleteBaseName}.alt-subject");
            public static readonly ScenarioName DeleteAltSubjectInsufficientAddress = new ScenarioName($"{DeleteAltSubject.Name}.insufficient-address");
            public static readonly ScenarioName DeleteMsa = new ScenarioName($"{DeleteBaseName}.msa");
            public static readonly ScenarioName DeleteEmployee = new ScenarioName($"{DeleteBaseName}.employee");

            private static readonly string ExportBaseName = $"{BaseName}.export";
            public static readonly ScenarioName ExportAltSubject = new ScenarioName($"{ExportBaseName}.alt-subject");
            public static readonly ScenarioName ExportAltSubjectInsufficientAddress = new ScenarioName($"{ExportAltSubject.Name}.insufficient-address");
            public static readonly ScenarioName ExportMsa = new ScenarioName($"{ExportBaseName}.msa");
            public static readonly ScenarioName ExportEmployee = new ScenarioName($"{ExportBaseName}.employee");
        }

        /// <summary>
        /// Scenarios related to flighting.
        /// </summary>
        public static class Flighting
        {
            private const string BaseName = "flighting";

            public static readonly ScenarioName Default = new ScenarioName(BaseName);
            public static readonly ScenarioName NoFlights = new ScenarioName($"{BaseName}.no-flights");
        }
    }
}
