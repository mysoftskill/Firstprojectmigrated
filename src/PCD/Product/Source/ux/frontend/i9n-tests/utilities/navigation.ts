import { browser } from "protractor";
import { ScenarioName } from "../../shared/scenario/scenario-types";
import { StringUtilities } from "../../shared/string-utilities";
import { Action } from "./action";

export class Navigation {

    //  Loads a page with partcular urlPath.
    public static loadPage(urlPath: string, ...scenarios: ScenarioName[]): void {
        if (!urlPath) {
            urlPath = "";
        }

        let scenarioStr = "";
        if (scenarios && scenarios.length) {
            scenarioStr = `&${StringUtilities.queryStringOf({ scenarios })}`;
        }

        browser.get(`${browser.baseUrl}${urlPath}?mocks=true${scenarioStr}`);
        Action.waitForPageLoad();
    }

    //  Loads the landing dashboard page.
    public static loadLandingDashboard(...scenarios: ScenarioName[]): void {
        if (!scenarios || !scenarios.length) {
            scenarios = ["default"];
        }

        Navigation.loadPage("", ...scenarios);
    }
}
