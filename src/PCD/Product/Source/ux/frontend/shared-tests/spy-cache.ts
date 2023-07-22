/**
 * Caches spies for instance of class.
 */
export class SpyCache<T> {
    /**
     *  Cache of Jasmine spies. 
     **/
    private readonly spies: {
        [key in keyof T]: jasmine.Spy;
    };

    /**
     * Constructor.
     * @param instance Instance of a class to cache spies for.
     */
    constructor(
        private readonly instance: T) {

        this.spies = <any> {};
    }

    /**
     * Gets service method spy.
     * @param method Method name.
     */
     public getFor(method: keyof T): jasmine.Spy {
        let result = this.spies[method] || spyOn<any>(this.instance, method);
        return this.spies[method] = result;
    }

    /**
     * Fails test, if method was called.
     * @param method Method name.
     * @param overrideMessage Optional message that will override default failure message.
     */
    //  TODO: make `keyof T` a type alias.
    public failIfCalled(method: keyof T, overrideMessage?: string): void {
        this.getFor(method).and.callFake(() => fail(overrideMessage || `'${method}' was not expected at this time.`));
    }
}
