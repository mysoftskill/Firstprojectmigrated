import { Component } from "../../../module/app.module";

@Component({
    name: "pcdDefaultProgressSpinner",
    options: {
        template: `<mee-progress-ants-mwf kind="local" class="progress-centered"></mee-progress-ants-mwf><p mee-paragraph data-use-cms>Please wait...</p>`
    }
})
export default class DefaultProgressSpinnerComponent implements ng.IComponentController {
}
