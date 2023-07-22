import { Component } from "../../../module/app.module";
import template = require("./permalink.html!text");

const useCmsHere_PermalinkDefaultTitle = "Direct link";

@Component({
    name: "pcdPermalink",
    options: {
        template,
        bindings: {
            url: "@pcdUrl",
            title: "@?pcdTitle",
            glyph: "@?pcdGlyph"
        }
    }
})
export class PermalinkComponent implements ng.IComponentController {
    public url: string;

    public title: string = useCmsHere_PermalinkDefaultTitle;

    public glyph = "mee-icon-Link";
}
