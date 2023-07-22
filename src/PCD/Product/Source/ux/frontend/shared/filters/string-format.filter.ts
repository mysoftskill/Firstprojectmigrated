import { appModule } from "../../module/app.module";

export interface IStringFormatFilter {
    (input: string, values: any | any[]): string;
}

appModule.filter("stringFormat", () => {
    function replace(input: string, values: any | any[]): string {
        if (!input) {
            return input;
        }

        if (Array.isArray(values)) {
            return input.replace(/\{([0-9]+)\}/g, (_: any, index: number) => { 
                return values[index]; 
            });
        } else {
            return input.replace(/\{0\}/g, values);
        }
    }

    return replace;
});
