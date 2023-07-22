/**
 * List of all scenarios.
 */
export type ScenarioName =
    "default" |
    "cold-start" |

    // Team picker
    "team-picker" |
    "team-picker.one-team" |
    "team-picker.several-teams" |
    "team-picker.gazillion-teams" |

    // Register/Manage team scenarios
    "register-team" |
    "register-team.team-already-exists" |
    "manage-team" |
    "manage-team.delete-team" |

    // Manage data assets related scenarios
    "manage-data-assets" |
    "manage-data-assets.remove-asset" |

    // Manage data agents related scenarios
    "manage-data-agents" |
    "manage-data-agents.remove-agent" |

    // Manual requests related scenarios
    "manual-requests" |
    "manual-requests.status" |

    "manual-requests.delete.alt-subject" |
    "manual-requests.delete.alt-subject.insufficient-address" |
    "manual-requests.delete.msa" |
    "manual-requests.delete.employee" |

    "manual-requests.export.alt-subject" |
    "manual-requests.export.alt-subject.insufficient-address" |
    "manual-requests.export.msa" |
    "manual-requests.export.employee" |

    // Flighting
    "flighting" |
    "flighting.no-flights";
