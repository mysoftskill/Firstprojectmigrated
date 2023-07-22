import { Component, Inject } from "../../../module/app.module";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import * as Pdms from "../../../shared/pdms/pdms-types";
import * as SelectList from "../../../shared/select-list";
import template = require("./country-list-selector.html!text");


const useCmsHere_FailedToLoadCountryList = "Failed to load countries. You can still continue, but the request will be submitted with an unknown country.";

@Component({
    name: "pcdCountryListSelector",
    options: {
        template
    }
})
@Inject("pcdErrorService", "pdmsDataService", "$meeComponentRegistry")
export default class CountryListSelectorComponent implements ng.IComponentController {
    public errorCategory = "country-list-selector";

    private countryListPickerModel: SelectList.Model= {
        selectedId: "--", // Custom ISO code for Unknown/Unspecified in case getting the countries list fails.
        items: []
    };

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService
    ) {
        this.$meeComponentRegistry.register("CountryListSelectorComponent", "CountryListSelectorComponent", this);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeCountryListSelectorComponent")
    public $onInit(): ng.IPromise<any> {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        return this.pdmsDataService.getCountriesList()
            .then((countries: Pdms.Country[]) => {
                // Default selection to "US" when the countries are loaded successfully.
                this.countryListPickerModel.selectedId = "US";
                let countryListItems: SelectList.SelectListItem[] = _.map(countries, (country: Pdms.Country) => {
                    return {
                        id: country.isoCode,
                        label: country.countryName
                    };
                });

                this.countryListPickerModel.items = countryListItems;
                SelectList.enforceModelConstraints(this.countryListPickerModel);
            })
            .catch(() => {
                this.pcdError.setErrorForId(`${this.errorCategory}.generic`, useCmsHere_FailedToLoadCountryList);
            });
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister("CountryListSelectorComponent");
    }

    public getSelectedCountryIsoCode(): string {
        return this.countryListPickerModel.selectedId;
    }
}
