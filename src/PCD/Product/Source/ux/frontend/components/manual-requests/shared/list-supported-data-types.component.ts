import { Component } from "../../../module/app.module";
import template = require("./list-supported-data-types.html!text");

@Component({
    name: "pcdListSupportedDataTypes",
    options: {
        bindings: {
            title: "@pcdTitle",
            dataTypes: "<pcdDataTypes"
        },
        template
    }
})
export default class ListSupportedDataTypes implements ng.IComponentController {
    public title: string;
    public dataTypes: string[];
}
