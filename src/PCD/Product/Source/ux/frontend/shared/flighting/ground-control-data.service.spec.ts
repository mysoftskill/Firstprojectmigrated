import * as angular from "angular";
import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { ITestableGroundControlDataService } from "./ground-control-data.service";
import { IGroundControlApiService } from "./ground-control-api.service";

describe("Ground Control data service", () => {
    let spec: TestSpec;
    let groundControlDataService: ITestableGroundControlDataService;
    let groundControlApiServiceMock: SpyCache<IGroundControlApiService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_groundControlApiService_: IGroundControlApiService, _groundControlDataService_: ITestableGroundControlDataService) => {
            groundControlApiServiceMock = new SpyCache(_groundControlApiService_);
            groundControlDataService = _groundControlDataService_;
        });

        groundControlDataService.resetState();
    });

    describe("initializeForCurrentUser", () => {
        it("retrieves list of user flights from API service", () => {
            groundControlApiServiceMock.getFor("getUserFlights").and.returnValue(spec.asHttpPromise([]));

            groundControlDataService.initializeForCurrentUser();
            spec.runDigestCycle();

            expect(groundControlApiServiceMock.getFor("getUserFlights")).toHaveBeenCalled();
        });

        it("cannot be called multiple times", () => {
            groundControlApiServiceMock.getFor("getUserFlights").and.returnValue(spec.asHttpPromise([]));

            //  First call.
            groundControlDataService.initializeForCurrentUser();

            //  Second call.
            expect(() => groundControlDataService.initializeForCurrentUser()).toThrowError();
        });

        it("can be omitted before other methods are called", () => {
            groundControlApiServiceMock.getFor("getUserFlights").and.returnValue(spec.asHttpPromise([]));

            //  A call before service was initialized.
            expect(() => groundControlDataService.isUserInFlight("someFlight")).not.toThrowError();
        });

        it("swallows exceptions from API service", () => {
            groundControlApiServiceMock.getFor("getUserFlights").and.returnValue(spec.$promises.reject());

            let succeeded = false;
            let failed = false;

            groundControlDataService.initializeForCurrentUser()
                .then(() => succeeded = true)
                .catch(() => failed = true);
            spec.runDigestCycle();

            expect(succeeded).toBe(true);
            expect(failed).toBe(false);
        });
    });

    describe("isUserInFlight", () => {
        it("returns true, if called with a flight name that is on the list of user flights", () => {
            groundControlApiServiceMock.getFor("getUserFlights").and.returnValue(spec.asHttpPromise(["FLIGHTX", "FLIGHTY"]));
            groundControlDataService.initializeForCurrentUser();

            let result: boolean;
            groundControlDataService.isUserInFlight("flightX").then(v => result = v);
            spec.runDigestCycle();

            expect(result).toBe(true);
        });

        it("returns false, if called with a flight name that is not on the list of user flights", () => {
            groundControlApiServiceMock.getFor("getUserFlights").and.returnValue(spec.asHttpPromise(["FLIGHTX", "FLIGHTY"]));
            groundControlDataService.initializeForCurrentUser();

            let result: boolean;
            groundControlDataService.isUserInFlight("flightZ").then(v => result = v);
            spec.runDigestCycle();

            expect(result).toBe(false);
        });
    });
});
