import * as Pdms from "./pdms/pdms-types";

export type PropertyBag = {
    [key: string]: string;
};

export class StringUtilities {
    /**
     * Returns the translated strings (if found) or keys as a comma separated string.
     * If there are no keys, return defaultValue.
     */
    public static getCommaSeparatedList(keys: string[], displayStringsByKey: PropertyBag, defaultValue: string): string {
        let displayStrings: string[] = [];
        displayStrings = _.map(keys, (k: string) => displayStringsByKey[k] || k);
        if (displayStrings.length > 0) {
            return displayStrings.join(", ");
        } else {
            return defaultValue;
        }
    }

    /**
     * Converts a bag of key-value pairs into a query string.
     * @param params A bag of key-value pairs. Values can be strings or array of strings. If value
     * is an array of strings, they will be joined into a comma-separated string.
     */
    public static queryStringOf(params: { [key: string]: string | string[] }): string {
        return Object.keys(params).map(key => {
            let valueStr = params[key];

            if (valueStr instanceof Array) {
                valueStr = valueStr.join();
            }

            return `${key}=${encodeURIComponent(valueStr)}`;
        }).join("&");
    }

    /**
     *  Compares strings, ignoring case. 
     **/
    public static areEqualIgnoreCase(value1: string, value2: string): boolean {
        if (value1 && value2) {
            return value1.toLocaleUpperCase() === value2.toLocaleUpperCase();
        }

        return value1 === value2;
    }

    /**
     *  Checks if a string contains another string, ignoring case. 
     **/
    public static containsIgnoreCase(where: string, what: string): boolean {
        if (typeof where === "string" && typeof what === "string") {
            return where.toLocaleUpperCase().includes(what.toLocaleUpperCase());
        }

        return false;
    }
}
