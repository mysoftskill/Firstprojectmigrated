export const EmptyGuid = "00000000-0000-0000-0000-000000000000";

/**
 * Checks if GUID can be parsed by .NET's Guid.Parse().
 * @param guid GUID to check.
 */
export function isValidGuid(guid: string): boolean {
    if (!guid) {
        return false;
    }

    if (guid.match(/^(\{)?[a-f\d]{8}\-[a-f\d]{4}\-[a-f\d]{4}\-[a-f\d]{4}\-[a-f\d]{12}\}?$/i)) {
        //  If GUID is wrapped with curly brackets, make sure they're balanced.
        if ("{" === guid[0]) {
            return "}" === guid[guid.length - 1];
        }

        return "}" !== guid[guid.length - 1];
    }

    return false;
}

export function routeParamGuidType($urlMatcherFactoryProvider: ng.ui.IUrlMatcherFactory): ng.ui.IType {
    let stringType = $urlMatcherFactoryProvider.type("string");

    return {
        encode: stringType.encode,
        decode: stringType.decode,
        is: stringType.is,
        pattern: /[a-f\d]{8}\-[a-f\d]{4}\-[a-f\d]{4}\-[a-f\d]{4}\-[a-f\d]{12}/i
    };
}

/**
 * Pads an empty Guid with the number to the right side.
 * @param numberSeed number to be padded.
 */
export function generateFuzzyGuidFrom(numberSeed: number): string {
    let numberStr = numberSeed && numberSeed.toString();
    if (!numberSeed || numberSeed <= 0 || numberStr.length > 12) {
        return EmptyGuid;
    }

    let sliceEndIndex = EmptyGuid.length - numberStr.length;
    let fuzzyGuid = `${EmptyGuid.slice(0, sliceEndIndex)}${numberSeed}`;

    if (!isValidGuid(fuzzyGuid)) {
        throw new Error("Fuzzy Guid tranformation is invalid.");
    }

    return fuzzyGuid;
}
