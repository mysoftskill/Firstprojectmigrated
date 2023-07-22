import { ILazyInitializer } from "./../shared-types";

export class Lazy<T> {
    private instance: T = null;
    private initializer: ILazyInitializer<T>;

    constructor(initializer: ILazyInitializer<T>) {
        this.initializer = initializer;
    }

    public getInstance(): T {
        if (!this.instance) {
            this.instance = this.initializer();
        }

        return this.instance;
    }
}
