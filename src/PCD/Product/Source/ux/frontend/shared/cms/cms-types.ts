import { BreadcrumbNavigation } from "../../components/shared/breadcrumb-heading/breadcrumb-heading.component";

//  Page CMS content
export interface CmsPageContent {
    //  BreadCrumb Content
    breadCrumbs?: BreadcrumbContent;
    //  gets the string value associated with specified key from strings dictionary *
    _s(key: string): string;
    //  gets the paragraph value associated with specified key from paragraphs dictionary *
    _p(key: string): RichParagraph;
    //  dictionary of links
    links: { [key: string]: MeePortal.OneUI.Angular.CmsLink };
}

//  Breadcrumb CMS content
export interface BreadcrumbContent {
    links: BreadcrumbNavigation[];
}

//  Component CMS content
export interface CmsComponentContent {
    //  gets the string value associated with specified key from strings dictionary *
    _s(key: string): string;
    //  gets the paragraph value associated with specified key from paragraphs dictionary *
    _p(key: string): RichParagraph;
    //  dictionary of links
    links: { [key: string]: MeePortal.OneUI.Angular.CmsLink };
}

//  Provides operation on CMS data 
export interface ICmsDataService {
    getContentItem<TCmsType>(cmsKey: CmsKey): TCmsType;
    loadContentItems(CmsKey: CmsKey[]): ng.IPromise<CmsContentCollection>;
}

//  Provides operations for CMS caching  
export interface ICmsCache {
    get(cmsKey: CmsKey): any;
    set(cmsCompositeKey: string, content: any): void;
    isInCache(cmsKey: CmsKey): boolean;
}

//  Provides access to CMS API. Do not use this directly, use CMS data service instead.
export interface ICmsApiService {
    getContentItems(cmsKeys: CmsKey[]): ng.IHttpPromise<CmsContentCollection>;
}

//  represents dictionary of CMS content items
export type CmsContentCollection = {
    //  key is CompositeKey of the CMSKey, value is CMSContent
    [key: string]: any,
};

//  Identifies CmsContent being requested
export type CmsKey = {
    /** cmsId is dot separated string containing content type and item name in Compass.
     * Last segment of CmsId determines Content Item Name is Compass.
     * Everything except last segment of CmsId determines the type of CMS.
     *  e.g. "page.agent-health" or "component.shared-content" */
    cmsId: string,
    //  AreaName maps to Compass folder name (currently sub-folders are not supported).
    areaName: CmsAreaName;
};

//  Rich paragraph CMS Content
export interface RichParagraph {
    text: string;
    cssClassName: string;
    style: TextStyleMultiType[];
}

//  Discriminated union of supported styles
export type TextStyleMultiType = SpanWithClassType | SpanWithIconType | SpanWithDynamicTextType;

export interface SpanWithClassType {
    kind: "spanwithclass";
    key: string;
    cssClassName: string;
    text: string;
}

export interface SpanWithIconType {
    kind: "spanwithicon";
    key: string;
    cssClassName: string;
}

export interface SpanWithDynamicTextType {
    kind: "dynamictext";
    key: string;
    cssClassName: string;
}

//  CMSAreaName maps to Compass folder name
export type CmsAreaName = 
    "agent-health" | 
    "shared";

//  Used internally by Component to store CMS contents
export interface ComponentCmsContentsInternal {
    //  primary content
    content: any;
    //  any additional CMS contents
    additionalContents: CmsContentCollection;
}

//  shared content
export const SharedCmsContent = "sharedContent";
export const SharedComponentContent: CmsKey = { areaName: "shared", cmsId: "component.shared-content" };
