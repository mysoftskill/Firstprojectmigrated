import * as angular from "angular";
import { Component, Inject } from "../../module/app.module";

@Component({
    name: "pcdFeedback",
    options: {
        template: `
            <div id="feedback-floater" tabindex="0" role="button" aria-label="Feedback" data-use-cms>
                <a id="feedback-floater-link" mee-action-trigger class="c-glyph glyph-chat-bubbles" data-use-cms href="javascript:pcdCollectFeedback()">Issues and feedback</a>
            </div>`
    }
})
@Inject("$scope", "$window", "$timeout", "$meeUtil", "$meeWatchOperationProgress")
export default class FeedbackComponent implements ng.IComponentController {
    private feedbackUrlFormat: string;

    constructor(
        private readonly $scope: ng.IScope,
        private readonly $window: ng.IWindowService,
        private readonly $timeout: ng.ITimeoutService,
        private readonly $meeUtil: MeePortal.OneUI.Angular.IMeeUtil,
        private readonly $meeWatchOperationProgress: MeePortal.OneUI.Angular.IWatchOperationProgress) {
    }

    public $onInit(): void {
        let debouncedScrollHandler = this.$meeUtil.debounce(() => this.showOrHideFeedbackFloater(), /* waitMsec */ 50);
        this.$window.addEventListener("scroll", () => debouncedScrollHandler());
        this.$window.addEventListener("resize", () => debouncedScrollHandler());

        //  Sometimes operations result in new content showing up and pushing footer out of the viewport. Make sure floater button shows up in these cases.
        this.$meeWatchOperationProgress(this.$scope, () => debouncedScrollHandler());

        //  Update floater visibility on page load.
        this.$timeout(() => this.showOrHideFeedbackFloater());
    }

    private showOrHideFeedbackFloater(): void {
        let footerLink = this.footerFeedbackLink;
        if (footerLink.offset().top + footerLink.height() < this.$window.innerHeight + this.$window.scrollY) {
            //  Floater is invisible, if footer link is visible.
            this.feedbackFloater.hide();
        } else {
            this.feedbackFloater.show();
        }
    }

    private get footerFeedbackLink(): JQuery {
        return angular.element("#feedback-footer-link");
    }

    private get feedbackFloater(): JQuery {
        return angular.element("#feedback-floater");
    }

    private get floaterFeedbackLink(): JQuery {
        return angular.element("#feedback-floater-link");
    }
}
