import { promise } from "protractor";

/**
 *  Utility class for chaining child promises with 'done' function. Note: this class uses `promise` sourced from `protractor`.
 **/
export class DoneUtil {
    private childPromises: promise.Promise<{}>[];
    private doneFn: Function;

    constructor() {
        this.childPromises = [];
    }

    //  Add child promise.
    public addPromiseToDone(): promise.Deferred<{}> {
        let newChildPromise = promise.defer();

        this.childPromises.push(newChildPromise.promise);
        return newChildPromise;
    }

    //  Finish the test by chaining `done` promise appropriately.
    public finishWithDone(done: DoneFn): promise.Deferred<{}> {
        let newChildPromise: promise.Deferred<{}>;

        if (!this.childPromises.length) {
            newChildPromise = this.addPromiseToDone();
        }

        promise.all(this.childPromises)
            .then(() => {
                done && done();
            });

        return newChildPromise;
    }
}
