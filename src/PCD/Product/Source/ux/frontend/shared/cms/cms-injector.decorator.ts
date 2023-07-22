import { ComponentCmsContentsInternal } from "./cms-types";

export function InjectCmsContent(additionalCmsName?: string) {

    return (target: any, propertyName: string) => {

        let getter = () => {
            let contents = <ComponentCmsContentsInternal> target._getCmsContents();

            if (!additionalCmsName) {
                return contents.content;
            } else {

                if (!contents.additionalContents[additionalCmsName]) {
                    throw new Error(`Requested content ${additionalCmsName} must be added to @Component decorator using content.additional property bag`);
                }

                return contents.additionalContents[additionalCmsName];
            }
        };

        delete this[propertyName];

        Object.defineProperty(target, propertyName, {
            get: getter,
            enumerable: true,
            configurable: true
        });
    };
}
