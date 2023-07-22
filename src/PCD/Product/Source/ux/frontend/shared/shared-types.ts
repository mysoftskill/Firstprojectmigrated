/**
 * Throws an exception, when unsupported literal type value was used in the code, expecting particular literal type.
 * @param value Actual value.
 */
export function throwUnsupportedLiteralType(value: any): never {
    throw new TypeError(`Unsupported literal provided: ${value}.`);
}

/**
 * Ensures that build is broken, if type variation was not handled in the logic.
 * @param value Actual value.
 */
export function invalidConditionBreakBuild(value: never): never {
    throw new TypeError("You must handle all possible values of the type.");
}

export interface IDataEntryForm extends ng.IComponentController {
    errorCategory: string;

    /**
     * Checks if there are any validation errors.
     * If so, return true and set the errors in the UI. Otherwise return false.
     */
    hasDataEntryErrors(): boolean;

    /**
     *  Resets the form errors on the UI. 
     **/
    resetErrors(): void;

    /**
     *  Resets the form data and errors on the UI. 
     **/
    resetForm(): void;
}

export interface RadioGroup {
    /**
     *  This contains the value of the selected RadioOption. 
     **/
    model: string;
    options: RadioOption[];
}

export interface RadioOption {
    value: string;
    label: string;
    description: string;
}

export interface ILazyInitializer<T> {
    (): T;
}
