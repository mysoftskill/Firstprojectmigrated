# Flighting

We use Carbon Flighting for controlling feature exposure on PCD.

[Flighting control panel][Flighting control panel]

## Flight configuration

Flights are available for `INT` and `PROD` environments.

* Flights configured for `INT` environment are evaluated in INT and PPE
  environments.
* Flights configured for `PROD` environment are evaluated only in PROD
  environment.

If the flight is defined in one environment, but not in another, users will be
considered "out of flight" for the environment, where the flight is not defined.
Use this to streamline development process: first define flight in `INT`,
complete development of the feature, deploy PCD to INT and PPE environments,
test, once happy with results, deploy to PROD and create flight with the same
name in `PROD`.

### Flight dimensions

Currently these dimensions are evaluated for each flight, in addition to
environment:

* UserID - e.g. "joedoe@microsoft.com"
* Market - e.g. "en-US"

## Using flights on frontend

Frontend code is the primary consumer of flights.

Follow this pattern when you need to access the flight ("BlockInheritance" in
this case) on frontend.

```typescript
@Inject("groundControlDataService")
export default class CreateDataAssetComponent implements ng.IComponentController {
    public isInBlockInheritanceFlight: boolean;

    constructor(
        private readonly groundControlDataService: IGroundControlDataService) { }

    public $onInit(): void {
        this.groundControlDataService.isUserInFlight("BlockInheritance").then(isInFlight => {
            this.isInBlockInheritanceFlight = isInFlight;
        });
    }
}
```

Then you can use this property to show/hide controls in templates.

```html
<!-- Other markup... -->

<pcd-storage-picker>
    <pcd-more-controls>
        <h3 mee-heading>Control asset group inheritance</h3>
        <pcd-edit-asset-group-properties ng-if="$ctrl.isInBlockInheritanceFlight"></pcd-edit-asset-group-properties>
    </pcd-more-controls>
</pcd-storage-picker>

<!-- Other markup... -->
```

### Allowing flight in unit tests

When flights are used for feature exposure, the easiest way to exercise the code
behind the flight is by short-circuiting the flight-check call at the very
beginning of your test run:

```typescript
describe("My Component", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();

        spec.dataServiceMocks.groundControlDataService.mockAsyncResultOf("isUserInFlight", /* value: */ true);
    });
});
```

If you want to be more selective about which flight must be allowed, change mock
configuration to look like shown below:

```typescript
describe("My Component", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();

        spec.dataServiceMocks.groundControlDataService.getFor("isUserInFlight").and.callFake((flightName: string) => {
            if (flightName === "MyFlight") {
                return spec.$promises.resolve(true);
            }

            return spec.$promises.resolve(false);
        });
    });
});
```

## Using flights on web role

It's recommended to use flighting exclusively on frontend.

If you do need to check flight on the web role, update you controller's code to
look as shown below:

```csharp
public class ApiController : Controller
{
    private readonly IClientProviderAccessor<IGroundControlProvider> groundControlProviderAccessor;

    public ApiController(IClientProviderAccessor<IGroundControlProvider> groundControlProviderAccessor)
    {
        this.groundControlProviderAccessor = groundControlProviderAccessor ?? throw new ArgumentNullException(nameof(groundControlProviderAccessor));
    }

    [HttpPut]
    public async Task<IActionResult> CreateAssetGroup([FromBody] PdmsModels.AssetGroup assetGroup)
    {
        var userAllowedToDoX =
            await groundControlProviderAccessor.ProviderInstance.Instance.IsUserInFlight("AllowedToDoX");
        if (userAllowedToDoX)
        {
            DoX();
        }

        //  Do other stuff...
    }
}
```

## Faking a flight

You can simulate currently signed in user being in flight by passing
`mocks=true&flights=flight1,flight2` parameters in the query string.

For example, navigating to this URL will simulate user being in flights
"flight1" and "flight2":

`https://dev.manage.privacy.microsoft-int.com?mocks=true&flights=flight1,flight2`

To remove the flight from the list, pass a new list of flights in the query
string parameter.

Faked flight is respected by both frontend and web role code.

Use this approach, when you need to simulate presence/absence of a flight in i9n
tests.

## Deprecating a flight

1. Search through the codebase for flight name.
1. Remove all code that accesses `groundControlDataService` on frontend and
   `IClientProviderAccessor<IGroundControlProvider>` on web role in the context
   of flight name.
1. If frontend code saves result of a flight evaluation in controller property
   (usual pattern), remove `ng-if` or `ng-show`/`ng-hide` that use this variable
   from all affected templates, then remove the property from controller
   altogether.
1. Make sure unit and i9n tests are passing.
1. Disable the flight in `INT` environment using [control panel][Flighting
   control panel] (*do not* disable `PROD` environment!). Verify whether the
   feature works as you expect it to, with the flight being disabled, on your
   devbox. Re-enable flight in `INT`.
1. Check-in the change.
1. After the change is deployed to INT (happens automatically, as part of CD),
   deploy to PPE, then disable the flight in `INT` environment using [control
   panel][Flighting control panel].
1. Deploy to PROD, then disable the flight in `PROD` environment.
1. Verify that all is functioning correctly, delete both `INT` and `PROD`
   flights using [control panel][Flighting control panel].

[Flighting control panel]: https://flighting.carbon.microsoft.com/WorkItem/ServiceDetail?serviceId=5881557
