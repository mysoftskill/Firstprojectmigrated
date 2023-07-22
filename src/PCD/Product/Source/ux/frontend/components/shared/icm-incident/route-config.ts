import * as angular from "angular";

import { Route, Inject, appModule } from "../../../module/app.module";
import * as Pdms from "../../../shared/pdms/pdms-types";

export interface AgentIcmConfirmationModalData {
    onConfirm: () => ng.IPromise<Pdms.Incident>;
    owner: Pdms.DataOwner;
    incident: Pdms.Incident;
}

export function registerIcmConfirmationModalRoutes(
    $stateProvider: ng.ui.IStateProvider,
    parentState: string,
) {
    $stateProvider.state(`${parentState}.icm`, {
        views: {
            "modalContent@": {
                template: "<pcd-agent-icm-confirmation></pcd-agent-icm-confirmation>"
            }
        }
    });
    $stateProvider.state(`${parentState}.icm-response`, {
        views: {
            "modalContent@": {
                template: "<pcd-agent-icm-confirmation-response></pcd-agent-icm-confirmation-response>"
            }
        }
    });
}