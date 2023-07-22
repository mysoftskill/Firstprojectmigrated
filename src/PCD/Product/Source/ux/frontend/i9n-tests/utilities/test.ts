import { Navigation } from "./navigation";
import { Search } from "./search";
import { Action } from "./action";
import { DoneUtil } from "./done-util";
import { Verify } from "./verification/verify";

export class Test {

    public static Navigation = Navigation;

    public static Search = Search;

    public static Action = Action;

    public static Verify = Verify;

    public static asyncTest(itFunction: (doneUtil: DoneUtil) => void): (done: DoneFn) => void {
        return (done: DoneFn) => {
            let doneUtil = new DoneUtil();

            itFunction && itFunction(doneUtil);
            doneUtil.finishWithDone(done);
        };
    }
}
