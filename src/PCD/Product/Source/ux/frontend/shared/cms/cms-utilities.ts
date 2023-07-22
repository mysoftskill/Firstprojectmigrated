import {  CmsKey, RichParagraph } from "./cms-types";
  
export class CmsUtilities {

    public static getCmsCompositeKey(cmsKey: CmsKey): string {
        return `${cmsKey.cmsId}@${cmsKey.areaName}`.toUpperCase();
    }
        
}

export function getCmsString(cmsContent: any, key: string): string {
    if (!cmsContent.strings) {
        throw new Error("cmsContent does not contain strings property");
    }

    if(!_.isString(cmsContent.strings[key])) {
        return `ERROR: ${key}`;
    }

    return cmsContent.strings[key];
}

export function getCmsParagraph(cmsContent: any, key: string): RichParagraph {
    if (!cmsContent.paragraphs) {
        throw new Error("cmsContent does not contain paragraphs property");
    }

    if (!_.isObject(cmsContent.paragraphs[key]) || _.isEmpty(cmsContent.paragraphs[key])) {
        return <RichParagraph> {
            text: `ERROR: ${key}`,
            style: {},
            cssClassName: ``
        };
    }

    return cmsContent.paragraphs[key];
}

export function getWrappedCmsContent(cmsContent: any): any {
    return {
        ...cmsContent, 
        _s: (key: string) => getCmsString(cmsContent, key),
        _p: (key: string) => getCmsParagraph(cmsContent, key)
    };
}
